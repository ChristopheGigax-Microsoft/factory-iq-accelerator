import { useEffect, useState } from 'react';

import { ErrorBanner } from '@/components/ErrorBanner';
import {
  getBaselineHierarchy,
  type BaselineNode,
  upsertBaselineNode,
} from '@/services/baselineClient';

export function BaselineManager() {
  const [nodes, setNodes] = useState<BaselineNode[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [newName, setNewName] = useState('');

  useEffect(() => {
    getBaselineHierarchy()
      .then((result) => setNodes(result.items))
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load hierarchy'));
  }, []);

  async function handleCreate() {
    setError(null);
    try {
      const created = await upsertBaselineNode({
        nodeId: `site-${Date.now()}`,
        nodeType: 'Site',
        parentNodeId: nodes.find((n) => n.nodeType === 'Enterprise')?.nodeId,
        displayName: newName || 'New Site',
      });
      setNodes((prev) => [...prev, created]);
      setNewName('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save baseline');
    }
  }

  return (
    <div className="mx-auto w-full max-w-4xl p-6">
      <h1 className="mb-4 text-2xl font-semibold">ISA-95 Baseline Manager</h1>
      {error ? <ErrorBanner message={error} /> : null}
      <div className="mb-6 flex gap-2">
        <input
          value={newName}
          onChange={(event) => setNewName(event.target.value)}
          className="flex-1 rounded border border-gray-300 px-3 py-2"
          placeholder="New site name"
        />
        <button
          onClick={() => void handleCreate()}
          className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
        >
          Add Site
        </button>
      </div>

      <ul className="space-y-2">
        {nodes.map((node) => (
          <li key={node.nodeId} className="rounded border border-gray-200 p-3">
            <p className="font-medium">{node.displayName}</p>
            <p className="text-xs text-gray-500">
              {node.nodeType} | {node.nodeId} | v{node.version}
            </p>
          </li>
        ))}
      </ul>
    </div>
  );
}
