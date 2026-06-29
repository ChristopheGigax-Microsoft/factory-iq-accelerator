import * as pulumi from "@pulumi/pulumi";
import { createCapacity } from "./src/capacity";
import { createWorkspace } from "./src/workspace";
import { createEventhouse } from "./src/eventhouse";
import { createEventstream } from "./src/eventstream";
import { createDataAgent } from "./src/data-agent";

const config = new pulumi.Config();
const plantCode = config.require("plantCode");
const environment = config.require("environment");
const region = config.require("region");
const capacitySku = config.get("capacitySku") || "F2";
const tenantId = config.get("tenantId") || "";
const subscriptionId = config.get("subscriptionId") || "";
const resourceGroup = config.get("resourceGroup") || "";

const baseName = `fiq-${plantCode}-${environment}`;

const capacity = createCapacity({
  name: `${baseName}-cap`,
  location: region,
  sku: capacitySku,
});

const workspace = createWorkspace({
  name: `${baseName}-ws`,
  capacityId: capacity.id,
});

const eventhouse = createEventhouse({
  name: `${baseName}-eh`,
  workspaceId: workspace.id,
  kqlDatabaseName: `${baseName}-kql`,
});

const eventstream = createEventstream({
  name: `${baseName}-es`,
  workspaceId: workspace.id,
});

const dataAgent = createDataAgent({
  name: `${baseName}-agent`,
  description: `Factory IQ Data Agent for plant ${plantCode}`,
  workspaceId: workspace.id,
});

export const connectionContract = {
  tenantId,
  subscriptionId,
  resourceGroup,
  region,
  workspaceId: workspace.id,
  eventhouseId: eventhouse.id,
  kqlDatabase: eventhouse.kqlDatabaseName,
  generatedAt: new Date().toISOString(),
  schemaVersion: "1.0",
  eventstreamId: eventstream.id,
  dataAgentId: dataAgent.id,
};
