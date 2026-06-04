import * as pulumi from "@pulumi/pulumi";

export interface EventstreamArgs {
  name: string;
  workspaceId: pulumi.Input<string>;
}

export function createEventstream(args: EventstreamArgs) {
  return {
    id: pulumi.output(`/fabric/eventstreams/${args.name}`),
    name: pulumi.output(args.name),
    workspaceId: pulumi.output(args.workspaceId),
  };
}
