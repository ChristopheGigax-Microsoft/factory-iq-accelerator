import { useEffect, useState } from 'react';

import { ErrorBanner } from '@/components/ErrorBanner';
import { useAuth } from '@/hooks/AuthContext';
import {
  getBaselineHierarchy,
  type BaselineNode,
  removeBaselineNode,
  upsertBaselineNode,
} from '@/services/baselineClient';

const nodeTypeConfig: Record<string, { color: string; badge: string }> = {
  Enterprise: { color: 'from-blue-500 to-indigo-600', badge: 'bg-blue-50 text-blue-700' },
  Site: { color: 'from-emerald-400 to-emerald-600', badge: 'bg-emerald-50 text-emerald-700' },
  Area: { color: 'from-amber-400 to-orange-500', badge: 'bg-amber-50 text-amber-700' },
  WorkCenter: { color: 'from-purple-400 to-purple-600', badge: 'bg-purple-50 text-purple-700' },
  WorkUnit: { color: 'from-rose-400 to-rose-600', badge: 'bg-rose-50 text-rose-700' },
};

export function BaselineManager() {
  const { user } = useAuth();
  const [nodes, setNodes] = useState<BaselineNode[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [newName, setNewName] = useState('');
  const [newType, setNewType] = useState('Site');
  const [parentNodeId, setParentNodeId] = useState('');
  const [busyNodeId, setBusyNodeId] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);

  useEffect(() => {
    getBaselineHierarchy()
      .then((result) => setNodes(result.items))
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load hierarchy'))
      .finally(() => setLoading(false));
  }, []);

  async function handleCreate() {
    setError(null);
    const actor = user?.id;
    if (!actor) {
      setError('User identity is not available. Please sign in again.');
      return;
    }

    if (newType !== 'Enterprise' && !parentNodeId) {
      setError('Select a parent node for non-enterprise entities.');
      return;
    }

    setBusyNodeId('create');
    try {
      const created = await upsertBaselineNode({
        nodeId: `${newType.toLowerCase()}-${Date.now()}`,
        nodeType: newType,
        parentNodeId: newType === 'Enterprise' ? undefined : parentNodeId,
        displayName: newName || 'New Site',
        userId: actor,
      });
      setNodes((prev) => [...prev, created]);
      setNewName('');
      if (newType !== 'Enterprise') {
        setParentNodeId('');
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save baseline');
    } finally {
      setBusyNodeId(null);
    }
  }

  async function handleRemove(id: string) {
    setError(null);
    setBusyNodeId(id);
    try {
      await removeBaselineNode(id);
      setNodes((prev) => prev.filter((node) => node.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to remove baseline node');
    } finally {
      setBusyNodeId(null);
    }
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between animate-fade-in-up">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 tracking-tight">Baseline Manager</h1>
          <p className="mt-1 text-sm text-gray-500">
            Manage your ISA-95 hierarchy nodes
          </p>
        </div>
        <button
          onClick={() => setShowForm(!showForm)}
          className="inline-flex items-center gap-2 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 px-4 py-2.5 text-sm font-medium text-white shadow-md shadow-blue-600/20 transition-all hover:shadow-lg hover:shadow-blue-600/30 hover:brightness-110 active:scale-[0.98]"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Add Node
        </button>
      </div>

      {error ? <ErrorBanner message={error} /> : null}

      {/* Create Form */}
      {showForm && (
        <div className="animate-fade-in-up rounded-2xl border border-blue-100 bg-gradient-to-r from-blue-50/50 to-indigo-50/50 p-6 shadow-sm">
          <h3 className="mb-4 text-sm font-semibold text-gray-900">Create New Node</h3>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <div>
              <label className="mb-1.5 block text-xs font-medium text-gray-600">Type</label>
              <select
                value={newType}
                onChange={(event) => setNewType(event.target.value)}
                className="w-full rounded-xl border border-gray-200 bg-white px-3 py-2.5 text-sm shadow-sm transition-colors focus:border-blue-300 focus:ring-2 focus:ring-blue-100 focus:outline-none"
              >
                <option>Enterprise</option>
                <option>Site</option>
                <option>Area</option>
                <option>WorkCenter</option>
                <option>WorkUnit</option>
              </select>
            </div>
            <div>
              <label className="mb-1.5 block text-xs font-medium text-gray-600">Parent</label>
              <select
                value={parentNodeId}
                onChange={(event) => setParentNodeId(event.target.value)}
                className="w-full rounded-xl border border-gray-200 bg-white px-3 py-2.5 text-sm shadow-sm transition-colors focus:border-blue-300 focus:ring-2 focus:ring-blue-100 focus:outline-none disabled:opacity-40"
                disabled={newType === 'Enterprise'}
              >
                <option value="">Select parent...</option>
                {nodes.map((node) => (
                  <option key={node.id} value={node.nodeId}>
                    {node.displayName} ({node.nodeType})
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1.5 block text-xs font-medium text-gray-600">Name</label>
              <input
                value={newName}
                onChange={(event) => setNewName(event.target.value)}
                className="w-full rounded-xl border border-gray-200 bg-white px-3 py-2.5 text-sm shadow-sm transition-colors focus:border-blue-300 focus:ring-2 focus:ring-blue-100 focus:outline-none"
                placeholder="e.g. Assembly Line 1"
              />
            </div>
            <div className="flex items-end gap-2">
              <button
                onClick={() => void handleCreate()}
                disabled={busyNodeId !== null}
                className="flex-1 rounded-xl bg-blue-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-blue-700 disabled:opacity-50 active:scale-[0.98]"
              >
                {busyNodeId === 'create' ? (
                  <span className="inline-block h-4 w-4 rounded-full border-2 border-white/30 border-t-white animate-spin" />
                ) : (
                  'Create'
                )}
              </button>
              <button
                onClick={() => setShowForm(false)}
                className="rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-medium text-gray-600 transition-colors hover:bg-gray-50"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Node List */}
      <div className="animate-fade-in-up rounded-2xl bg-white shadow-sm border border-gray-100 overflow-hidden" style={{ animationDelay: '100ms' }}>
        {/* Table Header */}
        <div className="grid grid-cols-12 gap-4 border-b border-gray-100 bg-gray-50/50 px-6 py-3">
          <div className="col-span-5 text-xs font-semibold text-gray-500 uppercase tracking-wide">Node</div>
          <div className="col-span-2 text-xs font-semibold text-gray-500 uppercase tracking-wide">Type</div>
          <div className="col-span-2 text-xs font-semibold text-gray-500 uppercase tracking-wide">Version</div>
          <div className="col-span-2 text-xs font-semibold text-gray-500 uppercase tracking-wide">Status</div>
          <div className="col-span-1" />
        </div>

        {/* Loading State */}
        {loading && (
          <div className="flex items-center justify-center py-16">
            <div className="h-8 w-8 rounded-full border-2 border-blue-200 border-t-blue-600 animate-spin" />
          </div>
        )}

        {/* Empty State */}
        {!loading && nodes.length === 0 && (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-gray-100">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="text-gray-400">
                <circle cx="12" cy="5" r="3" />
                <line x1="12" y1="8" x2="12" y2="14" />
                <line x1="6" y1="14" x2="18" y2="14" />
              </svg>
            </div>
            <p className="mt-4 text-sm font-medium text-gray-900">No nodes yet</p>
            <p className="mt-1 text-xs text-gray-400">Get started by creating your first ISA-95 baseline node.</p>
          </div>
        )}

        {/* Node Rows */}
        {!loading && nodes.length > 0 && (
          <div className="divide-y divide-gray-50">
            {nodes.map((node, i) => {
              const config = nodeTypeConfig[node.nodeType] || { color: 'from-gray-400 to-gray-500', badge: 'bg-gray-50 text-gray-700' };
              return (
                <div
                  key={node.id}
                  className="group grid grid-cols-12 gap-4 items-center px-6 py-4 hover:bg-gray-50/50 transition-colors animate-slide-in-left"
                  style={{ animationDelay: `${i * 40}ms` }}
                >
                  {/* Name + Icon */}
                  <div className="col-span-5 flex items-center gap-3 min-w-0">
                    <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br ${config.color} shadow-sm`}>
                      <span className="text-xs font-bold text-white">{node.nodeType.substring(0, 2).toUpperCase()}</span>
                    </div>
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-gray-900 truncate">{node.displayName}</p>
                      <p className="text-[11px] text-gray-400 font-mono truncate">{node.nodeId}</p>
                    </div>
                  </div>

                  {/* Type Badge */}
                  <div className="col-span-2">
                    <span className={`inline-flex items-center rounded-lg px-2.5 py-1 text-[11px] font-medium ${config.badge}`}>
                      {node.nodeType}
                    </span>
                  </div>

                  {/* Version */}
                  <div className="col-span-2">
                    <span className="text-sm text-gray-600">v{node.version}</span>
                  </div>

                  {/* Status */}
                  <div className="col-span-2">
                    <span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-600">
                      <span className="h-1.5 w-1.5 rounded-full bg-emerald-400 animate-pulse-ring" />
                      Active
                    </span>
                  </div>

                  {/* Actions */}
                  <div className="col-span-1 flex justify-end">
                    <button
                      onClick={() => void handleRemove(node.id)}
                      disabled={busyNodeId === node.id}
                      className="rounded-lg p-2 text-gray-300 opacity-0 transition-all group-hover:opacity-100 hover:bg-red-50 hover:text-red-500 disabled:opacity-50"
                      title="Remove node"
                    >
                      {busyNodeId === node.id ? (
                        <span className="inline-block h-4 w-4 rounded-full border-2 border-red-200 border-t-red-500 animate-spin" />
                      ) : (
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                          <polyline points="3 6 5 6 21 6" />
                          <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                        </svg>
                      )}
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}

        {/* Footer */}
        {!loading && nodes.length > 0 && (
          <div className="border-t border-gray-100 bg-gray-50/30 px-6 py-3">
            <p className="text-xs text-gray-400">
              Showing {nodes.length} node{nodes.length !== 1 ? 's' : ''} • Last synced just now
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
