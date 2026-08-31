using Opc.Ua;
using Opc.Ua.Server;
using OpcUaDataGenerator.Models;
using OpcUaDataGenerator.Services;

namespace OpcUaDataGenerator.Server;

/// <summary>
/// Builds the OPC UA address space from the ISA-95 plant topology and applies
/// simulated telemetry/state/alarm updates to the corresponding nodes.
///
/// Address space layout:
///   Objects
///     └─ FactoryIQ
///          └─ {SiteId} (e.g. site-lyon)
///               └─ {AreaId}
///                    └─ {WorkCenterId}
///                         └─ {WorkUnitId}
///                              ├─ State                (UInt32 — EquipmentState)
///                              ├─ ActiveAlarmCode       (String, empty when no alarm)
///                              ├─ ActiveAlarmSeverity   (String)
///                              └─ {Signal name}         (Double, one per SignalDefinition)
/// </summary>
public sealed class FactoryNodeManager : CustomNodeManager2
{
    private const string RootNamespace = "http://factoryiq.local/opcua/";

    private readonly Dictionary<(string WorkUnitId, string Signal), BaseDataVariableState> _signalNodes = [];
    private readonly Dictionary<string, BaseDataVariableState> _stateNodes = [];
    private readonly Dictionary<string, BaseDataVariableState> _alarmCodeNodes = [];
    private readonly Dictionary<string, BaseDataVariableState> _alarmSeverityNodes = [];

    public FactoryNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration, RootNamespace)
    {
        SystemContext.NodeIdFactory = this;
    }

    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();
        var root = CreateFolder(null, "FactoryIQ", "FactoryIQ");
        AddRootNotifier(root);

        foreach (var site in DemoPlant.Instance.Sites)
        {
            var siteFolder = CreateFolder(root, site.SiteId, site.Name);

            foreach (var area in site.Areas)
            {
                var areaFolder = CreateFolder(siteFolder, area.AreaId, area.Name);

                foreach (var wc in area.WorkCenters)
                {
                    var wcFolder = CreateFolder(areaFolder, wc.WorkCenterId, wc.Name);

                    foreach (var wu in wc.WorkUnits)
                    {
                        var wuFolder = CreateFolder(wcFolder, wu.WorkUnitId, $"{wu.Name} ({wu.MachineType})");

                        _stateNodes[wu.WorkUnitId] = CreateVariable(
                            wuFolder, $"{wu.WorkUnitId}.State", "State", DataTypeIds.UInt32, (uint)EquipmentState.Active);

                        _alarmCodeNodes[wu.WorkUnitId] = CreateVariable(
                            wuFolder, $"{wu.WorkUnitId}.ActiveAlarmCode", "ActiveAlarmCode", DataTypeIds.String, string.Empty);

                        _alarmSeverityNodes[wu.WorkUnitId] = CreateVariable(
                            wuFolder, $"{wu.WorkUnitId}.ActiveAlarmSeverity", "ActiveAlarmSeverity", DataTypeIds.String, string.Empty);

                        foreach (var signal in wu.Signals)
                        {
                            var node = CreateVariable(
                                wuFolder,
                                $"{wu.WorkUnitId}.{signal.Signal}",
                                signal.Signal,
                                DataTypeIds.Double,
                                0.0);
                            node.Description = new LocalizedText($"Unit: {signal.Unit}");
                            _signalNodes[(wu.WorkUnitId, signal.Signal)] = node;
                        }
                    }
                }
            }

            predefinedNodes.Add(root);
        }

        return predefinedNodes;
    }

    FolderState CreateFolder(NodeState? parent, string path, string displayName)
    {
        var folder = new FolderState(parent)
        {
            SymbolicName = path,
            ReferenceTypeId = ReferenceTypes.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId(path, NamespaceIndex),
            BrowseName = new QualifiedName(displayName, NamespaceIndex),
            DisplayName = displayName,
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            EventNotifier = EventNotifiers.None,
        };

        parent?.AddChild(folder);
        AddPredefinedNode(SystemContext, folder);
        return folder;
    }

    BaseDataVariableState CreateVariable(NodeState parent, string path, string displayName, NodeId dataType, object initialValue)
    {
        var variable = new BaseDataVariableState(parent)
        {
            SymbolicName = path,
            ReferenceTypeId = ReferenceTypes.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId(path, NamespaceIndex),
            BrowseName = new QualifiedName(displayName, NamespaceIndex),
            DisplayName = displayName,
            DataType = dataType,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Historizing = false,
            Value = initialValue,
            StatusCode = StatusCodes.Good,
            Timestamp = DateTime.UtcNow,
        };

        parent.AddChild(variable);
        AddPredefinedNode(SystemContext, variable);
        return variable;
    }

    /// <summary>Applies the latest telemetry readings to their OPC UA nodes.</summary>
    public void ApplyTelemetry(IReadOnlyList<TelemetryReading> readings)
    {
        lock (Lock)
        {
            foreach (var reading in readings)
            {
                if (_signalNodes.TryGetValue((reading.WorkUnitId, reading.Signal), out var node))
                {
                    node.Value = reading.Value;
                    node.Timestamp = reading.Timestamp;
                    node.StatusCode = StatusCodes.Good;
                    node.ClearChangeMasks(SystemContext, includeChildren: false);
                }
            }
        }
    }

    /// <summary>Applies state transitions and active alarms to their OPC UA nodes.</summary>
    public void ApplyStateChanges(IReadOnlyList<StateChange> changes, IReadOnlyList<MachineAlarmState> activeAlarms)
    {
        lock (Lock)
        {
            foreach (var change in changes)
            {
                if (_stateNodes.TryGetValue(change.WorkUnitId, out var stateNode))
                {
                    stateNode.Value = (uint)change.State;
                    stateNode.Timestamp = change.Timestamp;
                    stateNode.ClearChangeMasks(SystemContext, includeChildren: false);
                }
            }

            var alarmsByWorkUnit = activeAlarms.ToDictionary(a => a.WorkUnitId);
            foreach (var workUnitId in _alarmCodeNodes.Keys)
            {
                var codeNode = _alarmCodeNodes[workUnitId];
                var severityNode = _alarmSeverityNodes[workUnitId];

                if (alarmsByWorkUnit.TryGetValue(workUnitId, out var alarm))
                {
                    codeNode.Value = alarm.AlarmCode;
                    severityNode.Value = alarm.Severity;
                }
                else
                {
                    codeNode.Value = string.Empty;
                    severityNode.Value = string.Empty;
                }

                codeNode.ClearChangeMasks(SystemContext, includeChildren: false);
                severityNode.ClearChangeMasks(SystemContext, includeChildren: false);
            }
        }
    }
}
