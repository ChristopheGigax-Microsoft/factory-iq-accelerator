import * as pulumi from "@pulumi/pulumi";

export interface CapacityArgs {
  name: string;
  location: string;
  sku: string;
}

export function createCapacity(args: CapacityArgs) {
  return {
    id: pulumi.output(`/fabric/capacities/${args.name}`),
    name: pulumi.output(args.name),
  };
}
