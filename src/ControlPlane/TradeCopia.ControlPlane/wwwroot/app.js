const state = { csrf: "", route: "overview" };

async function api(path, options) {
  const headers = Object.assign({ "Accept": "application/json" }, options && options.headers);
  if (options && options.method && options.method !== "GET") {
    headers["X-CSRF-Token"] = state.csrf;
    headers["Content-Type"] = "application/json";
  }
  const res = await fetch(path, Object.assign({}, options, { headers }));
  if (!res.ok) {
    var text = await res.text();
    throw new Error(path + " failed: " + res.status + " " + text);
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
    const [status, health, groups, accounts] = await Promise.all([
      api("/api/v1/system/status"),
      api("/api/v1/live/health"),
      api("/api/v1/groups"),
      api("/api/v1/accounts")
    ]);
    document.getElementById("status-pill").textContent = health.groupHealth;
    document.getElementById("alerts").innerHTML =
      "<div class=\"warning\">Copying starts disabled. This dashboard cannot place discretionary trades.</div>";
    var engineLabel = status.engineConnected ? status.engineState : "Unknown";
    var discovered = (accounts && accounts.accounts) ? accounts.accounts : [];
    var waiting = status.engineConnected ? (discovered.length + " discovered") : "waiting for NinjaTrader";
    render("Overview",
      "<div class=\"grid\">" +
      card("Engine", engineLabel) +
      card("Copying", status.copyingEnabled ? "Enabled" : "Disabled") +
      card("Engine link", status.engineConnected ? "Connected" : "Disconnected") +
      card("Accounts", waiting) +
      "</div><h3>Groups</h3>" +
      table(["Name", "Leader", "Status"], groups.map((g) =>
        "<tr><td>" + g.name + "</td><td>" + (g.leaderKey || "") + "</td><td>" + (g.status || "") + "</td></tr>")));
  },
  async groups() {
    const [groups, accounts] = await Promise.all([
      api("/api/v1/groups"),
      api("/api/v1/accounts")
    ]);
    const discovered = accounts.accounts || [];
    const selectable = discovered.filter((a) => a.selectable);
    const rows = groups.map((g) =>
      "<tr><td>" + g.name + "</td><td>" + g.leaderKey + "</td><td>" + (g.followerKeys || []).join(", ") +
      "</td><td>" + g.status + "</td><td>" + g.version +
      "</td><td><button data-act=\"validate\" data-id=\"" + g.id + "\">Validate</button> " +
      "<button data-act=\"activate\" data-id=\"" + g.id + "\" data-ver=\"" + g.version + "\">Activate</button> " +
      "<button data-act=\"enable\" data-id=\"" + g.id + "\">Enable non-live</button> " +
      "<button data-act=\"pause-new-entries\" data-id=\"" + g.id + "\">Pause new entries</button> " +
      "<button data-act=\"disable\" data-id=\"" + g.id + "\">Disable</button></td></tr>");
    const accountOpts = selectable.map((a) => "<option value=\"" + a.stableKey + "\">" + a.displayName + " (" + a.safetyClass + ")</option>").join("");
    const followerBoxes = selectable.map((a) =>
      "<label><input type=\"checkbox\" name=\"follower\" value=\"" + a.stableKey + "\"> " + a.displayName + " (" + a.safetyClass + ")</label><br>").join("");
    const unavailable = accounts.error
      ? "<p class=\"sub\">NinjaTrader engine disconnected. Launch/connect NinjaTrader to discover accounts.</p>"
      : "";
    render("Copy Groups",
      unavailable +
      "<p id=\"gerr\" class=\"critical\"></p>" +
      "<h3>Discovered accounts</h3>" +
      table(["Account", "Class", "Selectable"], discovered.map((a) =>
        "<tr><td>" + a.displayName + "</td><td>" + a.safetyClass + "</td><td>" + a.selectable + "</td></tr>")) +
      "<h3>Create draft</h3>" +
      "<p><input id=\"gname\" placeholder=\"Group name\" value=\"Primary\"></p>" +
      "<p>Leader <select id=\"gleader\">" + accountOpts + "</select></p>" +
      "<p>Followers</p>" + followerBoxes +
      "<p><button id=\"gcreate\">Save draft</button></p>" +
      table(["Name", "Leader", "Followers", "Status", "Ver", "Actions"], rows) +
      "<p class=\"sub\">Draft → validate → activate → enable. Live and Unknown cannot be selected. Copying starts disabled.</p>");
    document.getElementById("gcreate").onclick = async () => {
      const followers = Array.from(document.querySelectorAll("input[name=follower]:checked")).map((el) => el.value);
      try {
        await api("/api/v1/groups", { method: "POST", body: JSON.stringify({
          name: document.getElementById("gname").value,
          leaderKey: document.getElementById("gleader").value,
          followerKeys: followers
        })});
        await pages.groups();
      } catch (err) {
        document.getElementById("gerr").textContent = String(err.message || err);
      }
    };
    document.querySelectorAll("button[data-act]").forEach((btn) => {
      btn.onclick = async () => {
        const id = btn.getAttribute("data-id");
        const act = btn.getAttribute("data-act");
        const path = "/api/v1/groups/" + id + "/" + act;
        const body = act === "activate" ? { expectedVersion: Number(btn.getAttribute("data-ver")) } : {};
        try {
          await api(path, { method: "POST", body: JSON.stringify(body) });
          await pages.groups();
        } catch (err) {
          document.getElementById("gerr").textContent = String(err.message || err);
        }
      };
    });
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
