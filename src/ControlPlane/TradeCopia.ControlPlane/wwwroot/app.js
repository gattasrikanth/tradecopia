const state = { csrf: "", route: "overview" };

async function api(path, options) {
  const headers = Object.assign({ "Accept": "application/json" }, options && options.headers);
  if (options && options.method && options.method !== "GET") {
    headers["X-CSRF-Token"] = state.csrf;
    headers["Content-Type"] = "application/json";
  }
  const res = await fetch(path, Object.assign({}, options, { headers }));
  if (!res.ok) {
    throw new Error(path + " failed: " + res.status);
  }
  return res.json();
}

function render(title, html) {
  document.getElementById("title").textContent = title;
  document.getElementById("content").innerHTML = html;
}

function table(headers, rows) {
  return "<table><thead><tr>" + headers.map((h) => "<th>" + h + "</th>").join("") +
    "</tr></thead><tbody>" + rows.join("") + "</tbody></table>";
}

const pages = {
  async overview() {
    const [status, health, groups, divergences] = await Promise.all([
      api("/api/v1/system/status"),
      api("/api/v1/live/health"),
      api("/api/v1/groups"),
      api("/api/v1/live/divergences")
    ]);
    document.getElementById("status-pill").textContent = health.groupHealth;
    document.getElementById("alerts").innerHTML =
      "<div class=\"warning\">Copying starts disabled. This dashboard cannot place discretionary trades.</div>";
    render("Overview",
      "<div class=\"grid\">" +
      card("Engine", status.details.engineState) +
      card("Copying", status.details.copyingEnabled ? "Enabled" : "Disabled") +
      card("Demo mode", String(status.demoMode)) +
      card("Telemetry", status.details.telemetry) +
      "</div><h3>Groups</h3>" +
      table(["Name", "Leader", "State", "Health"], groups.map((g) =>
        "<tr><td>" + g.name + "</td><td>" + g.leader + "</td><td>" + g.enabledState + "</td><td>" + g.health + "</td></tr>")) +
      "<h3>Alerts</h3>" + divergences.map((d) => "<div class=\"critical\">" + d.severity + " · " + d.account + " · " + d.detail + "</div>").join(""));
  },
  async groups() {
    const groups = await api("/api/v1/groups");
    render("Copy Groups", table(["Name", "Leader", "Followers", "Mode", "State"], groups.map((g) =>
      "<tr><td>" + g.name + "</td><td>" + g.leader + "</td><td>" + g.followers.join(", ") + "</td><td>" + g.copyMode + "</td><td>" + g.enabledState + "</td></tr>")) +
      "<p class=\"sub\">Draft → validate → activate is required before any follower order can be generated.</p>");
  },
  async live() {
    const trades = await api("/api/v1/live/trades");
    render("Live Trades", table(["Trade", "Instrument", "Side", "Leader qty", "Followers"], trades.map((t) =>
      "<tr><td>" + t.logicalTradeId.slice(0, 8) + "</td><td>" + t.instrument + "</td><td>" + t.side + "</td><td>" + t.leaderQty + "</td><td>" +
      t.followers.map((f) => f.account + " " + f.qty + " @ " + f.fill).join("<br>") + "</td></tr>")));
  },
  async divergences() {
    const items = await api("/api/v1/live/divergences");
    render("Divergences", items.map((d) => "<div class=\"critical\"><strong>" + d.className + "</strong> · " + d.detail + "</div>").join(""));
  },
  async journal() {
    const rows = await api("/api/v1/journal/trades");
    render("Journal", table(["Opened", "Group", "Instrument", "Side", "Latency"], rows.map((r) =>
      "<tr><td>" + r.openedAtUtc + "</td><td>" + r.group + "</td><td>" + r.instrument + "</td><td>" + r.side + "</td><td>" + r.decisionLatencyMs + " ms</td></tr>")));
  },
  async analytics() {
    const data = await api("/api/v1/analytics/overview");
    const r = data.reliability;
    render("Analytics",
      "<div class=\"grid\">" +
      card("Attempts", r.actionsAttempted) +
      card("Acknowledged", r.actionsAcknowledged) +
      card("Rejects", r.rejects) +
      card("p95 decision ms", r.decisionLatencyP95Ms) +
      "</div><p class=\"sub\">" + data.disclaimer + "</p>");
  },
  async diagnostics() {
    const d = await api("/api/v1/diagnostics/status");
    render("Diagnostics", "<div class=\"grid\">" +
      card("Control plane", d.controlPlane) +
      card("Engine", d.engine) +
      card("SQLite", d.sqlite) +
      card("Order submission", d.orderSubmission) +
      "</div><p class=\"sub\">" + d.lastError + "</p>");
  },
  async settings() {
    const p = await api("/api/v1/system/privacy");
    render("Settings", "<div class=\"card\"><h2>Privacy</h2><p style=\"font-size:14px;color:var(--muted)\">Local only: " +
      p.dataStoredLocally + ". Cloud upload: " + p.cloudUpload + ". Telemetry: " + p.telemetry + ".</p></div>");
  }
};

function card(label, value) {
  return "<article class=\"card\"><h2>" + label + "</h2><p>" + value + "</p></article>";
}

async function go(route) {
  state.route = route;
  document.querySelectorAll(".nav button").forEach((b) => b.classList.toggle("active", b.dataset.route === route));
  await pages[route]();
}

async function boot() {
  const boot = await api("/api/v1/system/bootstrap");
  state.csrf = boot.csrfToken;
  document.querySelectorAll(".nav button").forEach((b) => b.addEventListener("click", () => go(b.dataset.route)));
  await go("overview");
}

boot().catch((err) => {
  document.getElementById("content").innerHTML = "<div class=\"critical\">" + err.message + "</div>";
});
