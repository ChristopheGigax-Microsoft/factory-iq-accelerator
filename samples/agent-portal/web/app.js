// Factory IQ Agent Portal — plain JS, no build step.
// Talks to the FactoryIQ.AgentPortal.Api project (../api).

const API_BASE = window.FACTORY_IQ_API_BASE || "http://localhost:5080";

const agentListEl = document.getElementById("agent-list");
const runtimeBadgeEl = document.getElementById("runtime-badge");
const activeAgentNameEl = document.getElementById("active-agent-name");
const activeAgentDescriptionEl = document.getElementById("active-agent-description");
const chatThreadEl = document.getElementById("chat-thread");
const emptyStateEl = document.getElementById("empty-state");
const chatForm = document.getElementById("chat-form");
const chatInput = document.getElementById("chat-input");
const chatSend = document.getElementById("chat-send");

let activeAgentId = null;
let agents = [];

async function init() {
  await loadHealth();
  await loadAgents();
}

async function loadHealth() {
  try {
    const res = await fetch(`${API_BASE}/api/health`);
    const health = await res.json();
    runtimeBadgeEl.textContent = `runtime: ${health.runtime} · ${health.modelDeploymentName}`;
    runtimeBadgeEl.classList.toggle("local", health.runtime === "Local");
  } catch (err) {
    runtimeBadgeEl.textContent = "runtime: unreachable";
  }
}

async function loadAgents() {
  try {
    const res = await fetch(`${API_BASE}/api/agents`);
    agents = await res.json();
    renderAgentList();
    if (agents.length > 0) {
      selectAgent(agents[0].id);
    }
  } catch (err) {
    agentListEl.innerHTML = `<div class="empty-state">Could not reach the agent API at ${API_BASE}. Is FactoryIQ.AgentPortal.Api running?</div>`;
  }
}

function renderAgentList() {
  agentListEl.innerHTML = "";
  for (const agent of agents) {
    const card = document.createElement("div");
    card.className = "agent-card";
    card.dataset.agentId = agent.id;
    card.innerHTML = `
      <div class="icon">${agent.icon}</div>
      <div>
        <div class="name">${agent.displayName}</div>
        <div class="desc">${agent.description}</div>
      </div>
    `;
    card.addEventListener("click", () => selectAgent(agent.id));
    agentListEl.appendChild(card);
  }
}

function selectAgent(agentId) {
  activeAgentId = agentId;
  const agent = agents.find((a) => a.id === agentId);

  document.querySelectorAll(".agent-card").forEach((el) => {
    el.classList.toggle("active", el.dataset.agentId === agentId);
  });

  activeAgentNameEl.textContent = `${agent.icon} ${agent.displayName}`;
  activeAgentDescriptionEl.textContent = agent.description;

  chatThreadEl.innerHTML = "";
  emptyStateEl.remove?.();
  chatInput.disabled = false;
  chatSend.disabled = false;
  chatInput.focus();
}

function appendBubble(text, kind) {
  const bubble = document.createElement("div");
  bubble.className = `bubble ${kind}`;
  bubble.textContent = text;
  chatThreadEl.appendChild(bubble);
  chatThreadEl.scrollTop = chatThreadEl.scrollHeight;
  return bubble;
}

chatForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const message = chatInput.value.trim();
  if (!message || !activeAgentId) return;

  appendBubble(message, "user");
  chatInput.value = "";
  chatInput.disabled = true;
  chatSend.disabled = true;

  const pending = appendBubble("Thinking…", "pending");

  try {
    const res = await fetch(`${API_BASE}/api/agents/${activeAgentId}/chat`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ message }),
    });

    const data = await res.json();
    pending.remove();

    if (!res.ok) {
      appendBubble(data.error || data.detail || "Agent invocation failed.", "error");
    } else {
      appendBubble(data.response, "agent");
    }
  } catch (err) {
    pending.remove();
    appendBubble(`Network error: ${err.message}`, "error");
  } finally {
    chatInput.disabled = false;
    chatSend.disabled = false;
    chatInput.focus();
  }
});

init();
