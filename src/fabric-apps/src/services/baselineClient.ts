export type BaselineNode = {
  nodeId: string;
  nodeType: string;
  parentNodeId?: string;
  displayName: string;
  version: number;
};

export type BaselineHierarchyResponse = {
  items: BaselineNode[];
};

const API_BASE = import.meta.env.VITE_BASELINE_API_BASE ?? '/baseline';

export async function getBaselineHierarchy(includeInactive = false): Promise<BaselineHierarchyResponse> {
  const response = await fetch(`${API_BASE}/hierarchy?includeInactive=${includeInactive}`);
  if (!response.ok) {
    throw new Error(`Failed to load hierarchy (${response.status})`);
  }
  return response.json() as Promise<BaselineHierarchyResponse>;
}

export async function upsertBaselineNode(payload: {
  nodeId: string;
  nodeType: string;
  parentNodeId?: string;
  displayName: string;
  expectedVersion?: number;
}): Promise<BaselineNode> {
  const response = await fetch(`${API_BASE}/nodes`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const detail = await response.text();
    throw new Error(`Baseline write failed (${response.status}): ${detail}`);
  }

  return response.json() as Promise<BaselineNode>;
}
