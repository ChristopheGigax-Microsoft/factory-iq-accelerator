import { useEffect, useMemo, useState } from 'react';
import {
  Background,
  BackgroundVariant,
  Controls,
  type Edge,
  Handle,
  type Node,
  type NodeProps,
  Panel,
  Position,
  ReactFlow,
  ReactFlowProvider,
  useEdgesState,
  useNodesState,
  useReactFlow,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import { getBaselineHierarchy, type BaselineNode } from '@/services/baselineClient';

const nodeTypeConfig: Record<string, { gradient: string; ring: string; text: string }> = {
  Enterprise: { gradient: 'from-blue-500 to-indigo-600', ring: 'ring-blue-200', text: 'text-blue-700' },
  Site: { gradient: 'from-emerald-400 to-emerald-600', ring: 'ring-emerald-200', text: 'text-emerald-700' },
  Area: { gradient: 'from-amber-400 to-orange-500', ring: 'ring-amber-200', text: 'text-amber-700' },
  WorkCenter: { gradient: 'from-purple-400 to-purple-600', ring: 'ring-purple-200', text: 'text-purple-700' },
  WorkUnit: { gradient: 'from-rose-400 to-rose-600', ring: 'ring-rose-200', text: 'text-rose-700' },
};

function HierarchyNode({ data }: NodeProps) {
  const config = nodeTypeConfig[data.nodeType as string] || nodeTypeConfig.Enterprise;
  return (
    <div className={`group relative rounded-2xl bg-white px-5 py-4 shadow-lg ring-2 ${config.ring} transition-all duration-200 hover:shadow-xl hover:-translate-y-0.5 min-w-[180px]`}>
      {/* Connection handles */}
      <Handle
        type="target"
        position={Position.Top}
        className="!w-3 !h-3 !bg-gray-300 !border-2 !border-white !-top-1.5"
      />
      <Handle
        type="source"
        position={Position.Bottom}
        className="!w-3 !h-3 !bg-gray-300 !border-2 !border-white !-bottom-1.5"
      />

      {/* Colored top accent */}
      <div className={`absolute inset-x-0 top-0 h-1 rounded-t-2xl bg-gradient-to-r ${config.gradient}`} />

      <div className="flex items-center gap-3">
        <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br ${config.gradient} shadow-md`}>
          <span className="text-xs font-bold text-white">{(data.nodeType as string).substring(0, 2).toUpperCase()}</span>
        </div>
        <div className="min-w-0">
          <p className="text-sm font-semibold text-gray-900 truncate">{data.label as string}</p>
          <p className={`text-[11px] font-medium ${config.text}`}>{data.nodeType as string}</p>
        </div>
      </div>

      {/* Version badge */}
      <div className="absolute -top-2 -right-2 flex h-5 min-w-5 items-center justify-center rounded-full bg-white px-1.5 text-[10px] font-bold text-gray-500 shadow ring-1 ring-gray-100">
        v{data.version as number}
      </div>
    </div>
  );
}

const nodeTypes = { hierarchy: HierarchyNode };

function buildGraph(nodes: BaselineNode[]): { nodes: Node[]; edges: Edge[] } {
  const nodeMap = new Map(nodes.map((n) => [n.nodeId, n]));

  // Group by parent
  const childrenMap = new Map<string, BaselineNode[]>();
  const roots: BaselineNode[] = [];

  for (const node of nodes) {
    if (!node.parentNodeId || !nodeMap.has(node.parentNodeId)) {
      roots.push(node);
    } else {
      const siblings = childrenMap.get(node.parentNodeId) || [];
      siblings.push(node);
      childrenMap.set(node.parentNodeId, siblings);
    }
  }

  const flowNodes: Node[] = [];
  const flowEdges: Edge[] = [];
  const xSpacing = 240;
  const ySpacing = 160;

  // Use a simple incremental x-counter for leaf nodes,
  // then center parents above their children.
  let leafCounter = 0;

  function getSubtreeX(node: BaselineNode, depth: number): number {
    const children = childrenMap.get(node.nodeId) || [];

    // Build edges
    for (const child of children) {
      flowEdges.push({
        id: `${node.nodeId}-${child.nodeId}`,
        source: node.nodeId,
        target: child.nodeId,
        type: 'smoothstep',
        animated: true,
        style: { stroke: '#c7d2fe', strokeWidth: 2 },
      });
    }

    if (children.length === 0) {
      // Leaf: place at the next available slot
      const x = leafCounter * xSpacing;
      leafCounter++;

      flowNodes.push({
        id: node.nodeId,
        type: 'hierarchy',
        position: { x, y: depth * ySpacing },
        data: {
          label: node.displayName,
          nodeType: node.nodeType,
          version: node.version,
          nodeId: node.nodeId,
        },
        sourcePosition: Position.Bottom,
        targetPosition: Position.Top,
      });

      return x;
    }

    // Internal node: recurse children first, then center this node
    const childXPositions: number[] = [];
    for (const child of children) {
      childXPositions.push(getSubtreeX(child, depth + 1));
    }

    const minX = Math.min(...childXPositions);
    const maxX = Math.max(...childXPositions);
    const x = (minX + maxX) / 2;

    flowNodes.push({
      id: node.nodeId,
      type: 'hierarchy',
      position: { x, y: depth * ySpacing },
      data: {
        label: node.displayName,
        nodeType: node.nodeType,
        version: node.version,
        nodeId: node.nodeId,
      },
      sourcePosition: Position.Bottom,
      targetPosition: Position.Top,
    });

    return x;
  }

  for (const root of roots) {
    getSubtreeX(root, 0);
  }

  return { nodes: flowNodes, edges: flowEdges };
}

function GraphContent() {
  const [baselineNodes, setBaselineNodes] = useState<BaselineNode[]>([]);
  const [loading, setLoading] = useState(true);
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const { fitView } = useReactFlow();

  useEffect(() => {
    getBaselineHierarchy(true)
      .then((result) => {
        setBaselineNodes(result.items);
        const graph = buildGraph(result.items);
        setNodes(graph.nodes);
        setEdges(graph.edges);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [setNodes, setEdges]);

  useEffect(() => {
    if (nodes.length > 0) {
      setTimeout(() => fitView({ padding: 0.3, duration: 800 }), 100);
    }
  }, [nodes.length, fitView]);

  const legend = useMemo(
    () =>
      Object.entries(nodeTypeConfig).map(([type, config]) => ({
        type,
        ...config,
      })),
    []
  );

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <div className="h-8 w-8 rounded-full border-2 border-blue-200 border-t-blue-600 animate-spin" />
          <p className="text-sm text-gray-400">Loading hierarchy graph...</p>
        </div>
      </div>
    );
  }

  if (baselineNodes.length === 0) {
    return (
      <div className="flex h-full flex-col items-center justify-center text-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-gray-100">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="text-gray-400">
            <circle cx="12" cy="5" r="3" />
            <line x1="12" y1="8" x2="12" y2="14" />
            <line x1="6" y1="14" x2="18" y2="14" />
            <line x1="6" y1="14" x2="6" y2="17" />
            <line x1="18" y1="14" x2="18" y2="17" />
            <circle cx="6" cy="19" r="2" />
            <circle cx="18" cy="19" r="2" />
          </svg>
        </div>
        <p className="mt-4 text-sm font-medium text-gray-900">No nodes to visualize</p>
        <p className="mt-1 text-xs text-gray-400">Add baseline nodes to see the hierarchy graph.</p>
      </div>
    );
  }

  return (
    <ReactFlow
      nodes={nodes}
      edges={edges}
      onNodesChange={onNodesChange}
      onEdgesChange={onEdgesChange}
      nodeTypes={nodeTypes}
      fitView
      fitViewOptions={{ padding: 0.3 }}
      minZoom={0.3}
      maxZoom={2}
      className="bg-gray-50/30"
    >
      <Background variant={BackgroundVariant.Dots} gap={20} size={1} color="#e5e7eb" />
      <Controls
        className="!rounded-xl !border-gray-200 !bg-white/90 !shadow-lg !backdrop-blur-sm"
        showInteractive={false}
      />

      {/* Legend */}
      <Panel position="top-right">
        <div className="rounded-xl border border-gray-100 bg-white/90 p-3 shadow-lg backdrop-blur-sm">
          <p className="mb-2 text-[10px] font-semibold text-gray-500 uppercase tracking-wider">Legend</p>
          <div className="space-y-1.5">
            {legend.map(({ type, gradient }) => (
              <div key={type} className="flex items-center gap-2">
                <div className={`h-3 w-3 rounded bg-gradient-to-br ${gradient}`} />
                <span className="text-[11px] text-gray-600">{type}</span>
              </div>
            ))}
          </div>
        </div>
      </Panel>

      {/* Stats */}
      <Panel position="bottom-left">
        <div className="rounded-xl border border-gray-100 bg-white/90 px-3 py-2 shadow-lg backdrop-blur-sm">
          <p className="text-[11px] text-gray-500">
            <span className="font-semibold text-gray-700">{baselineNodes.length}</span> nodes •{' '}
            <span className="font-semibold text-gray-700">{edges.length}</span> connections
          </p>
        </div>
      </Panel>
    </ReactFlow>
  );
}

export function HierarchyGraph() {
  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="animate-fade-in-up">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">Hierarchy Graph</h1>
        <p className="mt-1 text-sm text-gray-500">
          Visual representation of your ISA-95 plant structure
        </p>
      </div>

      {/* Graph Container */}
      <div className="animate-fade-in-up rounded-2xl border border-gray-100 bg-white shadow-sm overflow-hidden" style={{ animationDelay: '100ms', height: 'calc(100vh - 200px)' }}>
        <ReactFlowProvider>
          <GraphContent />
        </ReactFlowProvider>
      </div>
    </div>
  );
}
