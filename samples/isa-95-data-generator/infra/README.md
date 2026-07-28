# ISA-95 Data Generator — Infrastructure

This folder will contain the Terraform and/or Bicep code to provision the infrastructure required for the ISA-95 data generator demo.

## Planned resources

| Resource | Purpose |
|----------|---------|
| **Azure IoT Hub** (F1 / S1) | Receives telemetry messages from the Azure Function |
| **IoT Hub Device** | Logical device representing the data generator |
| **Azure Function App** (Consumption / EP1) | Hosts the timer-triggered generator |
| **Storage Account** | Required by Azure Functions runtime |
| **Application Insights** | Observability for the Function |

## Connection to Factory IQ Accelerator

Once the IoT Hub is provisioned, its built-in Event Hub endpoint is wired to the **Fabric Eventstream** as a custom source:

```
Azure Function → IoT Hub → Eventstream (Custom Source: IoT Hub) → TelemetryLanding
```

The Eventstream configuration is managed in the main accelerator Terraform (`infra/terraform/modules/eventstream`).

## TODO

- [ ] `main.tf` — IoT Hub + device + Function App + Storage + App Insights
- [ ] `variables.tf` / `outputs.tf`
- [ ] Wire IoT Hub endpoint into accelerator Eventstream module
