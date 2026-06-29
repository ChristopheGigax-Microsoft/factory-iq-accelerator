import { authenticated, entity, int, text, uuid } from '@microsoft/rayfin-core';

@entity()
@authenticated('*')
export class Isa95BaselineNode {
  @uuid()
  id!: string;

  @text({ max: 128, unique: true })
  nodeId!: string;

  @text({ max: 32 })
  nodeType!: string;

  @text({ max: 128, optional: true })
  parentNodeId?: string;

  @text({ max: 256 })
  displayName!: string;

  @text({ max: 16, default: 'Active' })
  status!: string;

  @int({ default: 1 })
  version!: number;

  @text({ max: 128 })
  user_id!: string;
}