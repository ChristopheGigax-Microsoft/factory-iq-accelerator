import * as pulumi from "@pulumi/pulumi";

export interface WorkspaceArgs {
  name: string;
  capacityId: pulumi.Input<string>;
}

export function createWorkspace(args: WorkspaceArgs) {
  return {
    id: pulumi.output(`/fabric/workspaces/${args.name}`),
    name: pulumi.output(args.name),
    capacityId: pulumi.output(args.capacityId),
  };
}
