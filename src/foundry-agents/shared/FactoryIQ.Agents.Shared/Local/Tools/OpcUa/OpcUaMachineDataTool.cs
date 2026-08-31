using FactoryIQ.Agents.Shared.Local.Tools.Contracts;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Globalization;
using System.Text;

namespace FactoryIQ.Agents.Shared.Local.Tools.OpcUa;

public sealed class OpcUaMachineDataTool(
    ILogger<OpcUaMachineDataTool> logger) :
    IEquipmentOperations,
    IAlarmOperations,
    ITelemetryOperations,
    IPerformanceOperations
{
    const string DefaultEndpoint = "opc.tcp://localhost:4855/FactoryIQ/OpcUaDataGenerator";
    const string FactoryNamespaceUri = "http://factoryiq.local/opcua/";

    static readonly IReadOnlyDictionary<string, WorkUnitMetadata> WorkUnits = BuildWorkUnits();

    readonly SemaphoreSlim _sessionLock = new(1, 1);
    Session? _session;
    ushort _namespaceIndex;

    public async Task<EquipmentStatus?> GetEquipmentStatusAsync(
        string equipmentId,
        CancellationToken ct = default)
    {
        if (!WorkUnits.ContainsKey(equipmentId))
        {
            return null;
        }

        var session = await GetSessionAsync(ct);
        var state = await ReadValueAsync<uint>(session, $"{equipmentId}.State", ct);
        return new EquipmentStatus(equipmentId, StateName(state), DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<MachineAlarm>> GetActiveAlarmsAsync(
        string? equipmentId = null,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(ct);
        var alarms = new List<MachineAlarm>();
        foreach (var workUnit in FilterWorkUnits(equipmentId))
        {
            var alarmCode = await ReadValueAsync<string>(session, $"{workUnit.Id}.ActiveAlarmCode", ct);
            if (string.IsNullOrWhiteSpace(alarmCode))
            {
                continue;
            }

            var severity = await ReadValueAsync<string>(session, $"{workUnit.Id}.ActiveAlarmSeverity", ct);
            alarms.Add(new MachineAlarm(
                workUnit.Id,
                alarmCode,
                $"Active OPC UA alarm on {workUnit.Name}",
                string.IsNullOrWhiteSpace(severity) ? "Unknown" : severity,
                DateTimeOffset.UtcNow));
        }

        return alarms;
    }

    public async Task<IReadOnlyList<TelemetryPoint>> GetTelemetryAsync(
        string equipmentId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        if (!WorkUnits.TryGetValue(equipmentId, out var workUnit))
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow;
        if (to < now.AddMinutes(-2) || from > now.AddMinutes(2))
        {
            return [];
        }

        var session = await GetSessionAsync(ct);
        var points = new List<TelemetryPoint>();
        foreach (var signal in workUnit.Signals)
        {
            var value = await ReadValueAsync<double>(session, $"{equipmentId}.{signal.Name}", ct);
            points.Add(new TelemetryPoint(equipmentId, signal.Name, value, signal.Unit, now));
        }

        return points;
    }

    public async Task<PerformanceSummary?> GetPerformanceAsync(
        string scopeId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var scopedWorkUnits = WorkUnits.Values
            .Where(w => w.Id.Equals(scopeId, StringComparison.OrdinalIgnoreCase)
                || w.WorkCenterId.Equals(scopeId, StringComparison.OrdinalIgnoreCase)
                || w.AreaId.Equals(scopeId, StringComparison.OrdinalIgnoreCase)
                || w.SiteId.Equals(scopeId, StringComparison.OrdinalIgnoreCase)
                || scopeId.Equals("all", StringComparison.OrdinalIgnoreCase)
                || scopeId.Equals("line", StringComparison.OrdinalIgnoreCase)
                || scopeId.Equals("plant", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (scopedWorkUnits.Count == 0)
        {
            return null;
        }

        var session = await GetSessionAsync(ct);
        int active = 0;
        var healthScores = new List<double>();

        foreach (var workUnit in scopedWorkUnits)
        {
            var state = await ReadValueAsync<uint>(session, $"{workUnit.Id}.State", ct);
            if (state == 0)
            {
                active++;
            }

            foreach (var signal in workUnit.Signals)
            {
                var value = await ReadValueAsync<double>(session, $"{workUnit.Id}.{signal.Name}", ct);
                healthScores.Add(signal.HealthScore(value));
            }
        }

        var availability = Math.Round(active / (double)scopedWorkUnits.Count * 100, 1);
        var performance = Math.Round(healthScores.Count == 0 ? 100 : healthScores.Average(), 1);
        var alarmCount = (await GetActiveAlarmsAsync(null, ct))
            .Count(a => scopedWorkUnits.Any(w => w.Id.Equals(a.EquipmentId, StringComparison.OrdinalIgnoreCase)));
        var quality = Math.Round(Math.Max(70, 100 - alarmCount * 8 - healthScores.Count(h => h < 85) * 1.5), 1);
        var oee = Math.Round((availability / 100) * (performance / 100) * (quality / 100) * 100, 1);

        return new PerformanceSummary(scopeId, oee, availability, performance, quality, from, to);
    }

    public async Task<string> BuildFactorySnapshotAsync(string userQuery, CancellationToken ct = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var selected = SelectWorkUnits(userQuery).ToList();
            if (selected.Count == 0)
            {
                selected = WorkUnits.Values.Take(5).ToList();
            }

            var session = await GetSessionAsync(ct);
            var sb = new StringBuilder();
            sb.AppendLine("Local OPC UA live context from the Factory IQ Lyon Motor Line 1 controller simulator:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Endpoint: {EndpointUrl()}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Snapshot time UTC: {now:O}");

            var linePerformance = await GetPerformanceAsync("line", now.AddMinutes(-10), now, ct);
            if (linePerformance is not null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"- Line derived OEE: {linePerformance.Oee}% (availability {linePerformance.Availability}%, performance {linePerformance.Performance}%, quality {linePerformance.Quality}%).");
            }

            var alarms = await GetActiveAlarmsAsync(null, ct);
            sb.AppendLine(alarms.Count == 0
                ? "- Active alarms: none reported by OPC UA."
                : $"- Active alarms: {alarms.Count}.");

            foreach (var workUnit in selected)
            {
                var state = StateName(await ReadValueAsync<uint>(session, $"{workUnit.Id}.State", ct));
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"- {workUnit.Id} ({workUnit.Name}, {workUnit.MachineType}, {workUnit.AreaName}): state={state}.");

                var matchingAlarm = alarms.FirstOrDefault(a => a.EquipmentId == workUnit.Id);
                if (matchingAlarm is not null)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture,
                        $"  Alarm: {matchingAlarm.AlarmCode}, severity={matchingAlarm.Severity}, description={matchingAlarm.Description}.");
                }

                foreach (var signal in workUnit.Signals)
                {
                    var value = await ReadValueAsync<double>(session, $"{workUnit.Id}.{signal.Name}", ct);
                    sb.AppendLine(CultureInfo.InvariantCulture,
                        $"  {signal.Name}: {value:0.####} {signal.Unit} (nominal {signal.NominalMin:0.##}-{signal.NominalMax:0.##}).");
                }
            }

            sb.AppendLine("Use this OPC UA context as the only live production-line data source. If the user asks for another line, plant-wide history, MES, work orders, or root cause beyond these nodes, say what is unavailable locally.");
            return sb.ToString();
        }
        catch (ServiceResultException ex) when (IsConnectionFailure(ex))
        {
            logger.LogWarning(ex, "OPC UA generator is not reachable.");
            return $"Local OPC UA live context is unavailable: cannot connect to {EndpointUrl()}. Start samples/opcua-data-generator first.";
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timed out reading OPC UA generator.");
            return $"Local OPC UA live context is unavailable: timed out reading {EndpointUrl()}.";
        }
    }

    async Task<Session> GetSessionAsync(CancellationToken ct)
    {
        if (_session?.Connected == true)
        {
            return _session;
        }

        await _sessionLock.WaitAsync(ct);
        try
        {
            if (_session?.Connected == true)
            {
                return _session;
            }

            var config = await BuildClientConfigurationAsync();
            var endpointDescription = CoreClientUtils.SelectEndpoint(config, EndpointUrl(), false, 5000);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, EndpointConfiguration.Create(config));
            _session = await Session.Create(
                config,
                endpoint,
                updateBeforeConnect: false,
                checkDomain: false,
                sessionName: "FactoryIQ.LocalAgents",
                sessionTimeout: 60000,
                identity: new UserIdentity(new AnonymousIdentityToken()),
                preferredLocales: null,
                ct);

            _namespaceIndex = (ushort)_session.NamespaceUris.GetIndex(FactoryNamespaceUri);
            if (_namespaceIndex == ushort.MaxValue)
            {
                throw new InvalidOperationException($"OPC UA namespace '{FactoryNamespaceUri}' was not found.");
            }

            return _session;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    static async Task<ApplicationConfiguration> BuildClientConfigurationAsync()
    {
        var config = new ApplicationConfiguration
        {
            ApplicationName = "FactoryIQ Local Agents OPC UA Client",
            ApplicationUri = "urn:factoryiq:local-agents:opcua-client",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "%LocalApplicationData%/FactoryIQ/LocalAgents/pki/own",
                    SubjectName = "FactoryIQ Local Agents OPC UA Client",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "%LocalApplicationData%/FactoryIQ/LocalAgents/pki/issuer",
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "%LocalApplicationData%/FactoryIQ/LocalAgents/pki/trusted",
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "%LocalApplicationData%/FactoryIQ/LocalAgents/pki/rejected",
                },
                AutoAcceptUntrustedCertificates = true,
                RejectSHA1SignedCertificates = false,
                AddAppCertToTrustedStore = true,
            },
            TransportConfigurations = [],
            TransportQuotas = new TransportQuotas
            {
                OperationTimeout = 5000,
                SecurityTokenLifetime = 3600000,
            },
            ClientConfiguration = new ClientConfiguration
            {
                DefaultSessionTimeout = 60000,
            },
            DisableHiResClock = false,
        };

        await config.ValidateAsync(ApplicationType.Client);
        config.CertificateValidator.CertificateValidation += (_, e) => e.Accept = true;
        return config;
    }

    async Task<T> ReadValueAsync<T>(Session session, string nodeId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var value = await session.ReadValueAsync(new NodeId(nodeId, _namespaceIndex));
        if (StatusCode.IsBad(value.StatusCode))
        {
            throw new ServiceResultException(value.StatusCode, $"Bad OPC UA status reading node '{nodeId}'.");
        }

        if (value.Value is T typed)
        {
            return typed;
        }

        return (T)Convert.ChangeType(value.Value, typeof(T), CultureInfo.InvariantCulture);
    }

    static IEnumerable<WorkUnitMetadata> FilterWorkUnits(string? equipmentId)
    {
        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            return WorkUnits.Values;
        }

        return WorkUnits.TryGetValue(equipmentId, out var workUnit)
            ? [workUnit]
            : [];
    }

    static IEnumerable<WorkUnitMetadata> SelectWorkUnits(string userQuery)
    {
        var query = userQuery.ToLowerInvariant();
        var selected = WorkUnits.Values
            .Where(w => query.Contains(w.Id.ToLowerInvariant())
                || query.Contains(w.Name.ToLowerInvariant())
                || query.Contains(w.MachineType.ToLowerInvariant())
                || query.Contains(w.AreaName.ToLowerInvariant()))
            .ToList();

        if (selected.Count > 0)
        {
            return selected;
        }

        if (query.Contains("quality") || query.Contains("defect") || query.Contains("scrap") || query.Contains("cmm"))
        {
            return WorkUnits.Values.Where(w => w.MachineType is "CMM" or "TestBench");
        }

        if (query.Contains("maintenance") || query.Contains("alarm") || query.Contains("fault") || query.Contains("vibration"))
        {
            return WorkUnits.Values.Where(w => w.Signals.Any(s => s.Name == "Vibration.Velocity"));
        }

        if (query.Contains("line") || query.Contains("plant") || query.Contains("oee") || query.Contains("manager") || query.Contains("summary"))
        {
            return WorkUnits.Values;
        }

        return WorkUnits.Values.Where(w => w.AreaId == "area-lyon-motor-line");
    }

    static string EndpointUrl() =>
        Environment.GetEnvironmentVariable("OPCUA_ENDPOINT_URL")
        ?? Environment.GetEnvironmentVariable("FACTORY_IQ_OPCUA_ENDPOINT")
        ?? DefaultEndpoint;

    static string StateName(uint state) => state switch
    {
        0 => "Active",
        1 => "Idle",
        2 => "Held",
        3 => "Fault",
        4 => "Setup",
        _ => $"Unknown({state})",
    };

    static bool IsConnectionFailure(ServiceResultException ex) =>
        ex.StatusCode == StatusCodes.BadCommunicationError
        || ex.StatusCode == StatusCodes.BadConnectionClosed
        || ex.StatusCode == StatusCodes.BadConnectionRejected
        || ex.StatusCode == StatusCodes.BadTcpEndpointUrlInvalid
        || ex.StatusCode == StatusCodes.BadTimeout;

    static IReadOnlyDictionary<string, WorkUnitMetadata> BuildWorkUnits()
    {
        var cnc = new[]
        {
            new SignalMetadata("Spindle.Speed", "rpm", 800, 2800),
            new SignalMetadata("Temperature.Spindle", "degC", 20, 75),
            new SignalMetadata("Vibration.Velocity", "mm/s", 0.5, 4.0),
            new SignalMetadata("CuttingForce", "N", 100, 900),
            new SignalMetadata("FeedRate", "mm/min", 50, 600),
            new SignalMetadata("Coolant.FlowRate", "L/min", 8, 20),
        };
        var grinder = new[]
        {
            new SignalMetadata("Spindle.Speed", "rpm", 1000, 2800),
            new SignalMetadata("Temperature.Spindle", "degC", 20, 70),
            new SignalMetadata("Vibration.Velocity", "mm/s", 0.5, 3.5),
            new SignalMetadata("NormalForce", "N", 20, 180),
            new SignalMetadata("WheelWear", "um", 0, 50),
        };
        var cmm = new[]
        {
            new SignalMetadata("ProbeForce", "mN", 50, 180),
            new SignalMetadata("ScanVelocity", "mm/s", 5, 45),
            new SignalMetadata("Vibration.Velocity", "mm/s", 0.1, 0.8),
        };
        var testBench = new[]
        {
            new SignalMetadata("Engine.Speed", "rpm", 500, 3500),
            new SignalMetadata("Torque.Output", "Nm", 50, 400),
            new SignalMetadata("Temperature.Oil", "degC", 40, 90),
            new SignalMetadata("Vibration.Velocity", "mm/s", 0.5, 5.0),
            new SignalMetadata("Noise.Level", "dB(A)", 65, 85),
        };

        WorkUnitMetadata[] workUnits =
        [
            new("wu-lyon-prod-tour1", "CNC Lathe #1", "CNC", "line-lyon-motor-01", "area-lyon-motor-line", "Lyon Motor Line 1", "site-lyon-edge", cnc),
            new("wu-lyon-prod-tour2", "CNC Lathe #2", "CNC", "line-lyon-motor-01", "area-lyon-motor-line", "Lyon Motor Line 1", "site-lyon-edge", cnc),
            new("wu-lyon-prod-rect1", "Crankshaft Grinder", "Grinder", "line-lyon-motor-01", "area-lyon-motor-line", "Lyon Motor Line 1", "site-lyon-edge", grinder),
            new("wu-lyon-qual-cmm1", "Inline CMM Station", "CMM", "line-lyon-motor-01", "area-lyon-motor-line", "Lyon Motor Line 1", "site-lyon-edge", cmm),
            new("wu-lyon-qual-bench1", "End-of-Line Test Rig", "TestBench", "line-lyon-motor-01", "area-lyon-motor-line", "Lyon Motor Line 1", "site-lyon-edge", testBench),
        ];

        return workUnits.ToDictionary(w => w.Id, StringComparer.OrdinalIgnoreCase);
    }

    sealed record WorkUnitMetadata(
        string Id,
        string Name,
        string MachineType,
        string WorkCenterId,
        string AreaId,
        string AreaName,
        string SiteId,
        IReadOnlyList<SignalMetadata> Signals);

    sealed record SignalMetadata(string Name, string Unit, double NominalMin, double NominalMax)
    {
        public double HealthScore(double value)
        {
            if (NominalMax <= NominalMin)
            {
                return 100;
            }

            if (value >= NominalMin && value <= NominalMax)
            {
                return 100;
            }

            var range = NominalMax - NominalMin;
            var distance = value < NominalMin ? NominalMin - value : value - NominalMax;
            return Math.Round(Math.Max(50, 100 - (distance / range * 80)), 1);
        }
    }
}
