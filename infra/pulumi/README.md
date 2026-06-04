# Pulumi Engine

## Configure

```bash
pulumi -C infra/pulumi stack init dev
pulumi -C infra/pulumi config set plantCode plant1
pulumi -C infra/pulumi config set environment dev
pulumi -C infra/pulumi config set region westeurope
pulumi -C infra/pulumi config set capacitySku F2
```

## Deploy

```bash
pulumi -C infra/pulumi up
```

## Output Contract

```bash
pulumi -C infra/pulumi stack output connectionContract > connection.json
```

Validate `connection.json` against `contracts/connection-contract.md`.
