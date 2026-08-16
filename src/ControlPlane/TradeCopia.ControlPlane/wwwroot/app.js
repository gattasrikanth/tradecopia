const state = { csrf: "", route: "overview", leaderKey: "" };

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

function escapeHtml(value) {
  return String(value == null ? "" : value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

function visibleName(value) {
  var text = String(value == null ? "" : value);
  if (!text || text.indexOf("|") >= 0) {
    return "Account";
  }
  return text;
}

function render(title, html) {
  document.getElementById("title").textContent = title;
  document.getElementById("content").innerHTML = html;
}

function setAlerts(html) {
  document.getElementById("alerts").innerHTML = html || "";
}

function setPill(text, kind) {
  var pill = document.getElementById("status-pill");
  pill.textContent = text;
  pill.className = "pill" + (kind ? " " + kind : "");
}

function table(headers, rows) {
  if (!rows.length) {
    return "<div class=\"empty\">Nothing to show.</div>";
  }
  return "<table><thead><tr>" + headers.map((h) => "<th>" + h + "</th>").join("") +
    "</tr></thead><tbody>" + rows.join("") + "</tbody></table>";
}

const pages = {
  async overview() {
    const [status, groups, accounts, divergences] = await Promise.all([
      api("/api/v1/system/status"),
      api("/api/v1/groups"),
      api("/api/v1/accounts"),
      api("/api/v1/live/divergences")
    ]);
    var p = status.presentation || {};
    setPill(p.headline || (status.engineConnected ? "Engine Connected" : "Engine Disconnected"),
      status.copyingEnabled ? "ok" : (status.engineConnected ? "warn" : "bad"));
    setAlerts(p.alertHtml || "");
    var discovered = (accounts && accounts.accounts) ? accounts.accounts : [];
    var eligible = discovered.filter((a) => a.availableAsLeader);
    var blocked = discovered.length - eligible.length;
    var activeGroups = groups.filter((g) => g.status === "active");
    var pre = (p.preflight && p.preflight.checks) ? p.preflight.checks : [];
    var ready = p.preflight && p.preflight.ready;
    render("Overview",
      "<div class=\"grid\">" +
      card("Engine", status.engineConnected ? "Connected" : "Disconnected") +
      card("Copying", status.copyingEnabled ? "Enabled" : "Disabled") +
      card("Environment", "SIM / Demo Only") +
      card("Accounts", discovered.length + " discovered",
        eligible.length + " eligible non-live · " + blocked + " blocked/other") +
      card("Active groups", String(activeGroups.length)) +
      card("Divergences", String((divergences || []).length)) +
      "</div>" +
      "<h3>First-trade preflight</h3>" +
      "<div class=\"card\">" +
      "<h2>" + (ready ? "Ready for non-live copying" : "Not ready") + "</h2>" +
      "<ul class=\"checks\">" + pre.map((c) =>
        "<li class=\"" + (c.passed ? "pass" : "fail") + "\">" + (c.passed ? "✓ " : "✗ ") + escapeHtml(c.label) + "</li>"
      ).join("") + "</ul></div>");
  },
  async groups() {
    const [groups, accounts, status] = await Promise.all([
      api("/api/v1/groups"),
      api("/api/v1/accounts"),
      api("/api/v1/system/status")
    ]);
    var p = status.presentation || {};
    setPill(p.headline || (status.engineConnected ? "Engine Connected" : "Engine Disconnected"),
      status.copyingEnabled ? "ok" : "warn");
    setAlerts(p.alertHtml || "");
    const discovered = accounts.accounts || [];
    if (!state.leaderKey && discovered.length) {
      state.leaderKey = preferredLeaderKey(discovered);
    }
    const form = accounts.error
      ? "<div class=\"empty\">" + escapeHtml(accounts.message || "NinjaTrader engine disconnected. Launch/connect NinjaTrader to discover accounts.") + "</div>"
      : groupForm(discovered, status);
    const cards = groups.map((g) => groupCard(g, status)).join("");
    render("Copy Groups",
      form +
      "<p id=\"gerr\" class=\"critical\" hidden></p>" +
      (cards || "<div class=\"empty\">No copy groups yet. Create a group to choose one NinjaTrader account as a leader and mirror supported activity to one or more followers.</div>"));
    bindGroupForm(discovered, status);
    bindGroupCards(status);
  },
  async live() {
    const trades = await api("/api/v1/live/trades");
    render("Live Trades", table(["Trade", "Instrument", "Side", "Leader qty", "Followers"], trades.map((t) =>
      "<tr><td>" + escapeHtml(t.logicalTradeId.slice(0, 8)) + "</td><td>" + escapeHtml(t.instrument) + "</td><td>" + escapeHtml(t.side) + "</td><td>" + t.leaderQty + "</td><td>" +
      t.followers.map((f) => escapeHtml(visibleName(f.account)) + " " + f.qty + " @ " + f.fill).join("<br>") + "</td></tr>")));
  },
  async divergences() {
    const items = await api("/api/v1/live/divergences");
    render("Divergences", items.length
      ? items.map((d) => "<div class=\"critical\"><strong>" + escapeHtml(d.className) + "</strong> · " + escapeHtml(d.detail) + "</div>").join("")
      : "<div class=\"empty\">No divergences.</div>");
  },
  async journal() {
    const rows = await api("/api/v1/journal/trades");
    render("Journal", table(["Opened", "Group", "Instrument", "Side", "Latency"], rows.map((r) =>
      "<tr><td>" + escapeHtml(r.openedAtUtc) + "</td><td>" + escapeHtml(r.group) + "</td><td>" + escapeHtml(r.instrument) + "</td><td>" + escapeHtml(r.side) + "</td><td>" + r.decisionLatencyMs + " ms</td></tr>")));
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
      "</div><p class=\"sub\">" + escapeHtml(data.disclaimer) + "</p>");
  },
  async diagnostics() {
    const d = await api("/api/v1/diagnostics/status");
    render("Diagnostics", "<div class=\"grid\">" +
      card("Control plane", d.controlPlane) +
      card("Engine", d.engine) +
      card("SQLite", d.sqlite) +
      card("Order submission", d.orderSubmission) +
      "</div><p class=\"sub\">" + escapeHtml(d.lastError) + "</p>");
  },
  async settings() {
    const p = await api("/api/v1/system/privacy");
    render("Settings", "<div class=\"card\"><h2>Privacy</h2><p style=\"font-size:14px;color:var(--muted)\">Local only: " +
      p.dataStoredLocally + ". Cloud upload: " + p.cloudUpload + ". Telemetry: " + p.telemetry + ".</p></div>");
  }
};

function card(label, value, meta) {
  return "<article class=\"card\"><h2>" + escapeHtml(label) + "</h2><p>" + escapeHtml(value) + "</p>" +
    (meta ? "<div class=\"meta\">" + escapeHtml(meta) + "</div>" : "") + "</article>";
}

function groupForm(discovered, status) {
  const leaders = discovered.filter((a) => a.availableAsLeader);
  const leaderOpts = leaders.map((a) =>
    "<option value=\"" + escapeHtml(a.stableKey) + "\"" + (a.stableKey === state.leaderKey ? " selected" : "") + ">" +
    escapeHtml(visibleName(a.displayName)) + " · " + escapeHtml(a.safetyLabel) + "</option>").join("");
  return "<div class=\"card\"><h2>Create Copy Group</h2>" +
    "<div class=\"field\"><label for=\"gname\">Group name</label><input id=\"gname\" value=\"Primary\"></div>" +
    "<div class=\"field\"><label for=\"gleader\">Leader</label><select id=\"gleader\">" + leaderOpts + "</select></div>" +
    "<div class=\"field\"><label>Followers</label><div id=\"gfollowers\">" + followerChoices(discovered, state.leaderKey) + "</div></div>" +
    "<div class=\"field\"><label for=\"gsizing\">Sizing</label><select id=\"gsizing\">" +
    "<option value=\"OneToOne\" selected>1 : 1</option>" +
    "<option value=\"Multiplier\">Multiplier</option>" +
    "<option value=\"Fixed\">Fixed quantity</option></select></div>" +
    "<div class=\"field\"><label>Instrument behavior</label><input value=\"Same instrument / contract\" disabled></div>" +
    "<p class=\"help\">Save &amp; Activate validates locally and natively, then activates atomically. Live and Unknown stay locked.</p>" +
    "<div class=\"actions\"><button class=\"btn primary\" id=\"gsave\"" + (status.engineConnected ? "" : " disabled") + ">Save &amp; Activate</button></div></div>";
}

function preferredLeaderKey(discovered) {
  var sims = discovered.filter((a) => a.availableAsLeader && a.safetyLabel === "Simulation");
  var named = sims.find((a) => String(a.displayName || "").toLowerCase().indexOf("backtest") < 0);
  var pick = named || sims[0] || discovered.find((a) => a.availableAsLeader);
  return pick ? pick.stableKey : "";
}

function preferredFollowerKeys(discovered, leaderKey) {
  return discovered.filter((a) => a.availableAsFollower && a.stableKey !== leaderKey && a.safetyLabel === "Demo / Paper")
    .map((a) => a.stableKey);
}

function followerChoices(discovered, leaderKey) {
  var preferred = preferredFollowerKeys(discovered, leaderKey);
  return discovered.map((a) => {
    var locked = !a.availableAsFollower || a.stableKey === leaderKey;
    var reason = a.stableKey === leaderKey ? "This account is the leader" : (a.lockReason || a.eligibilityLabel);
    var checked = !locked && preferred.indexOf(a.stableKey) >= 0;
    return "<label class=\"choice" + (locked ? " locked" : "") + "\">" +
      "<input type=\"checkbox\" name=\"follower\" value=\"" + escapeHtml(a.stableKey) + "\"" +
      (locked ? " disabled" : "") + (checked ? " checked" : "") + ">" +
      "<span><div class=\"name\">" + escapeHtml(visibleName(a.displayName)) + "</div>" +
      "<div class=\"hint\">" + escapeHtml(a.safetyLabel) + " · " + escapeHtml(a.connectionLabel || "Connected") +
      " · " + escapeHtml(locked ? reason : "Available") + "</div></span></label>";
  }).join("");
}

function groupCard(g, status) {
  var enabled = status.copyingEnabled && g.status === "active";
  var actions = "";
  if (g.status === "active" && !enabled) {
    actions = "<button class=\"btn primary\" data-act=\"enable\" data-id=\"" + g.id + "\">Enable Non-Live Copying</button>";
  }
  if (enabled) {
    actions = "<button class=\"btn\" data-act=\"pause-new-entries\" data-id=\"" + g.id + "\" title=\"" + escapeHtml(g.pauseHelp || "") + "\">Pause New Entries</button>" +
      "<button class=\"btn danger\" data-act=\"disable\" data-id=\"" + g.id + "\" title=\"" + escapeHtml(g.disableHelp || "") + "\">Disable Copying</button>";
  }
  return "<article class=\"card group-card\" data-group=\"" + g.id + "\">" +
    "<h2>" + escapeHtml(g.name) + "</h2>" +
    "<div class=\"group-flow\">" +
    "<div><div class=\"meta\">Leader</div><p>" + escapeHtml(visibleName(g.leaderDisplayName)) + "</p></div>" +
    "<div class=\"arrow\">" + escapeHtml(g.sizingLabel || "1 : 1") + "<br>↓</div>" +
    "<div><div class=\"meta\">Follower</div><p>" + escapeHtml((g.followerDisplayNames || []).map(visibleName).join(", ")) + "</p></div>" +
    "</div>" +
    "<div class=\"meta\">Configuration " + escapeHtml(g.status === "active" ? "Active" : "Draft") +
    " · Copying " + (enabled ? "Enabled" : "Disabled") +
    " · Version v" + escapeHtml(g.version) + "</div>" +
    "<p class=\"help\">" + escapeHtml(g.pauseHelp || "") + " " + escapeHtml(g.disableHelp || "") + "</p>" +
    "<div class=\"actions\">" + actions + "</div></article>";
}

function bindGroupForm(discovered, status) {
  var leader = document.getElementById("gleader");
  var box = document.getElementById("gfollowers");
  if (leader && box) {
    leader.onchange = function () {
      state.leaderKey = leader.value;
      box.innerHTML = followerChoices(discovered, state.leaderKey);
    };
  }
  var save = document.getElementById("gsave");
  if (save) {
    save.onclick = async function () {
      var err = document.getElementById("gerr");
      err.hidden = true;
      try {
        var followers = Array.from(document.querySelectorAll("input[name=follower]:checked")).map((el) => el.value);
        var existing = document.querySelector(".group-card");
        await api("/api/v1/groups/save-and-activate", { method: "POST", body: JSON.stringify({
          id: existing ? existing.getAttribute("data-group") : "",
          name: document.getElementById("gname").value,
          leaderKey: document.getElementById("gleader").value,
          followerKeys: followers,
          sizing: document.getElementById("gsizing").value
        })});
        await pages.groups();
      } catch (ex) {
        err.hidden = false;
        err.textContent = String(ex.message || ex);
      }
    };
  }
}

function bindGroupCards(status) {
  document.querySelectorAll("button[data-act]").forEach((btn) => {
    btn.onclick = async () => {
      var err = document.getElementById("gerr");
      if (err) { err.hidden = true; }
      var act = btn.getAttribute("data-act");
      var id = btn.getAttribute("data-id");
      if (act === "enable") {
        openEnableModal(id, status);
        return;
      }
      try {
        await api("/api/v1/groups/" + id + "/" + act, { method: "POST", body: "{}" });
        await pages.groups();
      } catch (ex) {
        if (err) { err.hidden = false; err.textContent = String(ex.message || ex); }
      }
    };
  });
}

function openEnableModal(id, status) {
  var existing = document.querySelector(".group-card[data-group=\"" + id + "\"]");
  var summary = existing ? existing.innerText : "Enable Non-Live Copying";
  var back = document.createElement("div");
  back.className = "modal-back";
  back.innerHTML = "<div class=\"modal\"><h2>Enable Non-Live Copying?</h2>" +
    "<p>" + escapeHtml(summary.replace(/\s+/g, " ").trim()) + "</p>" +
    "<p class=\"help\">Environment: Simulation / Demo only. TradeCopia will reject Live and Unknown accounts. Enabling does not place an order.</p>" +
    "<div class=\"actions\"><button class=\"btn\" id=\"mcancel\">Cancel</button>" +
    "<button class=\"btn primary\" id=\"mgo\">Enable Non-Live Copying</button></div></div>";
  document.body.appendChild(back);
  document.getElementById("mcancel").onclick = function () { back.remove(); };
  document.getElementById("mgo").onclick = async function () {
    try {
      await api("/api/v1/groups/" + id + "/enable", { method: "POST", body: "{}" });
      back.remove();
      await pages.groups();
    } catch (ex) {
      back.remove();
      var err = document.getElementById("gerr");
      if (err) { err.hidden = false; err.textContent = String(ex.message || ex); }
    }
  };
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
  document.getElementById("content").innerHTML = "<div class=\"critical\">" + escapeHtml(err.message) + "</div>";
});
