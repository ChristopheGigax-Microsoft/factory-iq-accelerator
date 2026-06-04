import * as pulumi from "@pulumi/pulumi";

export interface EventhouseArgs {
  name: string;
  workspaceId: pulumi.Input<string>;
  kqlDatabaseName: string;
}

export function createEventhouse(args: EventhouseArgs) {
  return {
    id: pulumi.output(`/fabric/eventhouses/${args.name}`),
    name: pulumi.output(args.name),
    kqlDatabaseName: pulumi.output(args.kqlDatabaseName),
    workspaceId: pulumi.output(args.workspaceId),
  };
}
