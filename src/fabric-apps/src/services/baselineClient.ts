import { getRayfinClient } from './rayfinClient';

export type BaselineNode = {
  id: string;
  nodeId: string;
  nodeType: string;
  parentNodeId?: string;
  displayName: string;
  status: string;
  version: number;
  user_id: string;
};

export type BaselineHierarchyResponse = {
  items: BaselineNode[];
};

function asText(value: unknown, fallback = ''): string {
  if (typeof value === 'string') {
    return value;
  }

  // Some preview runtimes can return wrapped scalar values: { value: "..." }.
  if (value && typeof value === 'object' && 'value' in value) {
    const wrapped = (value as { value?: unknown }).value;
    if (typeof wrapped === 'string') {
      return wrapped;
    }
  }

  return fallback;
}

function normalizeKey(value: string): string {
  return value.replace(/_/g, '').toLowerCase();
}

function pickField<T = unknown>(item: Record<string, unknown>, ...keys: string[]): T | undefined {
  const desired = keys.map(normalizeKey);

  for (const [rawKey, rawValue] of Object.entries(item)) {
    if (rawValue === undefined || rawValue === null) {
      continue;
    }

    if (desired.includes(normalizeKey(rawKey))) {
      return rawValue as T;
    }
  }

  for (const key of keys) {
    if (item[key] !== undefined && item[key] !== null) {
      return item[key] as T;
    }
  }
  return undefined;
}

export async function getBaselineHierarchy(includeInactive = false): Promise<BaselineHierarchyResponse> {
  const client = getRayfinClient();
  const query = client.data.Isa95BaselineNode.select([
    'id',
    'nodeId',
    'nodeType',
    'parentNodeId',
    'displayName',
    'status',
    'version',
    'user_id',
  ]);

  if (!includeInactive) {
    query.where({ status: { eq: 'Active' } });
  }

  const items = await query.execute();

  return {
    items: items
      .map((raw) => {
        const item = raw as unknown as Record<string, unknown>;
        const nodeId = asText(pickField(item, 'nodeId', 'node_id', 'NodeId', 'NODE_ID'));
        const displayName = asText(
          pickField(item, 'displayName', 'display_name', 'DisplayName', 'DISPLAY_NAME'),
          nodeId || 'Unnamed Node'
        );

        return {
          id: asText(pickField(item, 'id', 'Id', 'ID')),
          nodeId,
          nodeType: asText(pickField(item, 'nodeType', 'node_type', 'NodeType', 'NODE_TYPE')),
          parentNodeId: asText(
            pickField(item, 'parentNodeId', 'parent_node_id', 'ParentNodeId', 'PARENT_NODE_ID'),
            undefined
          ),
          displayName,
          status: asText(pickField(item, 'status', 'Status'), 'Active'),
          version:
            typeof pickField(item, 'version', 'Version') === 'number'
              ? (pickField(item, 'version', 'Version') as number)
              : 1,
          user_id: asText(pickField(item, 'user_id', 'userId', 'UserId', 'USER_ID'), 'seed-runner'),
        };
      })
      .sort((a, b) =>
        asText(a.displayName, a.nodeId).localeCompare(asText(b.displayName, b.nodeId))
      ),
  };
}

export async function upsertBaselineNode(payload: {
  nodeId: string;
  nodeType: string;
  parentNodeId?: string;
  displayName: string;
  userId: string;
}): Promise<BaselineNode> {
  const client = getRayfinClient();
  const existing = await client.data.Isa95BaselineNode.select([
    'id',
    'nodeId',
    'nodeType',
    'parentNodeId',
    'displayName',
    'status',
    'version',
    'user_id',
  ])
    .where({ nodeId: { eq: payload.nodeId } })
    .first(1)
    .findFirst();

  if (!existing) {
    const created = await client.data.Isa95BaselineNode.create({
      nodeId: payload.nodeId,
      nodeType: payload.nodeType,
      parentNodeId: payload.parentNodeId,
      displayName: payload.displayName,
      status: 'Active',
      version: 1,
      user_id: payload.userId,
    });
    return created;
  }

  const updated = await client.data.Isa95BaselineNode.update(
    { id: existing.id },
    {
      nodeType: payload.nodeType,
      parentNodeId: payload.parentNodeId,
      displayName: payload.displayName,
      version: existing.version + 1,
    }
  );

  return updated;
}

export async function removeBaselineNode(id: string): Promise<void> {
  const client = getRayfinClient();
  await client.data.Isa95BaselineNode.delete({ id });
}
