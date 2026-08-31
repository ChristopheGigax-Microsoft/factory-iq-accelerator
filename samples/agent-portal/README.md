# Factory IQ Agent Portal

A minimal, self-contained HTML/JS chat portal for demoing all five Factory IQ
agents — Operations, Maintenance, Quality, Plant Manager, and Continuous
Improvement — from a single browser tab. It is designed to showcase the
**same agent codebase running against two different runtimes**:

- **Cloud** — Azure AI Foundry Agent Service (`FoundryAgentBase`-derived agents)
- **Local** — Foundry Local, on-device inference (`LocalFactoryAgent`)

Switching between them is a single environment variable (`AI_RUNTIME`) — no
code changes, no redeploy. That "flip a switch, same agents" story is the
core of the demo.

## Architecture

```
samples/agent-portal/
├── api/                         ASP.NET Core Web API (FactoryIQ.AgentPortal.Api)
│   ├── Program.cs               Minimal API: /api/health, /api/agents, /api/agents/{id}/chat
│   └── Properties/launchSettings.json   Runs on http://localhost:5080
└── web/                         Static frontend, no build step
    ├── index.html
    ├── styles.css
    └── app.js                   fetch()-based client
```

The API project references the **existing** `FactoryIQ.Agents.Shared` library
and all 5 agent projects under `src/foundry-agents/agents/*` — it does not
duplicate any agent logic. It reuses `ServiceRegistration.LoadConfigFromEnvironment()`
and the `AI_RUNTIME` switch exactly like the console host
(`AgentConsoleHost`) does, so the portal is just a different front door onto
the same agents.

> This is a **demo-grade** API: CORS is wide open (`AllowAnyOrigin`) and there
> is no auth. Do not expose it outside a local/controlled network.

## Endpoints

| Method | Path                          | Description                                   |
|--------|-------------------------------|------------------------------------------------|
| GET    | `/api/health`                 | Current runtime (`Cloud`/`Local`) + model name |
| GET    | `/api/agents`                 | List of the 5 agents (id, name, icon, description) |
| POST   | `/api/agents/{agentId}/chat`  | Send a message, get the agent's reply          |

`POST` body: `{ "message": "..." }` → response: `{ "reply": "..." }`.

## Running — Cloud runtime

Requires the same Azure AI Foundry configuration used by the console agents
(see `src/foundry-agents/README.md` / root `README.md` for `appsettings`/env
vars: project endpoint, model deployment name, credentials).

```powershell
cd samples/agent-portal/api
$env:AI_RUNTIME = "cloud"
dotnet run -r win-x64
```

## Running — Local runtime (Foundry Local)

Requires Foundry Local installed and a model loaded (see `docs/foundry-local.md`).

```powershell
foundry model run phi-4-mini   # or your configured local model

cd samples/agent-portal/api
$env:AI_RUNTIME = "local"
dotnet run -r win-x64
```

The API listens on `http://localhost:5080` either way (`launchSettings.json`,
with a `UseUrls` fallback in `Program.cs`).

## Opening the portal

The frontend is static — no build step, no npm. Simplest option: open
`web/index.html` directly in a browser, or serve the folder:

```powershell
cd samples/agent-portal/web
python -m http.server 8080
# then browse to http://localhost:8080
```

The frontend defaults to `http://localhost:5080` for the API. To point it
elsewhere, set `window.FACTORY_IQ_API_BASE` before `app.js` loads (e.g. add a
small inline `<script>` in `index.html`).

## Demo flow suggestion

1. Start the API in **Cloud** mode, open the portal, chat with the
   **Plant Manager** agent — show the runtime badge says "Cloud".
2. Stop the API, restart with `AI_RUNTIME=local` (optionally disconnect from
   the network first to make the point emphatically), reload the portal —
   badge flips to "Local", same UI, same agents, same questions work.
3. Ask the **Maintenance** or **Operations** agent a question that would use
   live telemetry, and pair it with the `samples/opcua-data-generator`
   sample running alongside to show a realistic edge data source feeding the
   agents (once `OpcUaMachineDataTool` is wired up).

## Verified

- `dotnet build -r win-x64` succeeds (0 warnings, 0 errors).
- `dotnet run -r win-x64` starts and listens on `http://localhost:5080`.
- `GET /api/health` and `GET /api/agents` return expected JSON.
- CORS header (`Access-Control-Allow-Origin: *`) confirmed on responses.
