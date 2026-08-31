using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace OpcUaDataGenerator.Server;

/// <summary>
/// Standalone OPC UA server exposing one demo production line as an address space.
/// Wraps the OPC Foundation .NET Standard stack (StandardServer) with a single
/// custom node manager (<see cref="FactoryNodeManager"/>).
/// </summary>
public sealed class FactoryOpcUaServer : StandardServer
{
    private FactoryNodeManager? _nodeManager;

    public FactoryNodeManager NodeManager =>
        _nodeManager ?? throw new InvalidOperationException("Server has not started yet.");

    protected override MasterNodeManager CreateMasterNodeManager(
        IServerInternal server,
        ApplicationConfiguration configuration)
    {
        _nodeManager = new FactoryNodeManager(server, configuration);
        return new MasterNodeManager(server, configuration, dynamicNamespaceUri: null, [_nodeManager]);
    }

    public static async Task<(FactoryOpcUaServer Server, ApplicationInstance App)> StartAsync(
        string configPath,
        CancellationToken ct = default)
    {
        var application = new ApplicationInstance
        {
            ApplicationName = "FactoryIQ OPC UA Data Generator",
            ApplicationType = ApplicationType.Server,
            ConfigSectionName = "OpcUaDataGenerator",
        };

        await application.LoadApplicationConfigurationAsync(configPath, silent: false, ct);

        bool haveCert = await application.CheckApplicationInstanceCertificatesAsync(silent: true, ct: ct);
        if (!haveCert)
        {
            throw new InvalidOperationException(
                "OPC UA application certificate is missing or invalid. " +
                "Delete the pki/own folder to force regeneration and re-run.");
        }

        var server = new FactoryOpcUaServer();
        await application.StartAsync(server);

        return (server, application);
    }
}
