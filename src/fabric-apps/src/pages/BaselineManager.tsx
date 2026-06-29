import { useEffect, useState } from 'react';

import { ErrorBanner } from '@/components/ErrorBanner';
import { useAuth } from '@/hooks/AuthContext';
import {
  getBaselineHierarchy,
  type BaselineNode,
  removeBaselineNode,
  upsertBaselineNode,
} from '@/services/baselineClient';

export function BaselineManager() {
  const { user } = useAuth();
  const [nodes, setNodes] = useState<BaselineNode[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [newName, setNewName] = useState('');
  const [newType, setNewType] = useState('Site');
  const [parentNodeId, setParentNodeId] = useState('');
  const [busyNodeId, setBusyNodeId] = useState<string | null>(null);

  useEffect(() => {
    getBaselineHierarchy()
      .then((result) => setNodes(result.items))
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load hierarchy'));
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
    <div className="mx-auto w-full max-w-4xl p-6">
      <h1 className="mb-4 text-2xl font-semibold">ISA-95 Baseline Manager</h1>
      {error ? <ErrorBanner message={error} /> : null}
      <div className="mb-6 flex gap-2">
        <select
          value={newType}
          onChange={(event) => setNewType(event.target.value)}
          className="rounded border border-gray-300 px-3 py-2"
        >
          <option>Enterprise</option>
          <option>Site</option>
          <option>Area</option>
          <option>WorkCenter</option>
          <option>WorkUnit</option>
        </select>
        <select
          value={parentNodeId}
          onChange={(event) => setParentNodeId(event.target.value)}
          className="rounded border border-gray-300 px-3 py-2"
          disabled={newType === 'Enterprise'}
        >
          <option value="">Parent (optional)</option>
          {nodes.map((node) => (
            <option key={node.id} value={node.nodeId}>
              {node.displayName} ({node.nodeType})
            </option>
          ))}
        </select>
        <input
          value={newName}
          onChange={(event) => setNewName(event.target.value)}
          className="flex-1 rounded border border-gray-300 px-3 py-2"
          placeholder="New node name"
        />
        <button
          onClick={() => void handleCreate()}
          disabled={busyNodeId !== null}
          className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
        >
          Add Node
        </button>
      </div>

      <ul className="space-y-2">
        {nodes.map((node) => (
          <li key={node.id} className="flex items-center justify-between rounded border border-gray-200 p-3">
            <div>
              <p className="font-medium">{node.displayName}</p>
              <p className="text-xs text-gray-500">
                {node.nodeType} | {node.nodeId} | v{node.version}
              </p>
            </div>
            <button
              onClick={() => void handleRemove(node.id)}
              disabled={busyNodeId === node.id}
              className="rounded bg-red-600 px-3 py-2 text-xs text-white hover:bg-red-700"
            >
              Remove
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
