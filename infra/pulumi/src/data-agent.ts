import * as pulumi from "@pulumi/pulumi";

export interface DataAgentArgs {
  name: string;
  description?: string;
  workspaceId: pulumi.Input<string>;
}

export function createDataAgent(args: DataAgentArgs) {
  return {
    id: pulumi.output(`/fabric/dataAgents/${args.name}`),
    name: pulumi.output(args.name),
    description: pulumi.output(args.description || ""),
    workspaceId: pulumi.output(args.workspaceId),
  };
}
