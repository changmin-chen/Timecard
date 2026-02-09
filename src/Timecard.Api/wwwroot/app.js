const $ = (sel) => document.querySelector(sel);

function fmtTime(dt) {
  if (!dt) return "—";
  const d = new Date(dt);
  return new Intl.DateTimeFormat("zh-Hant", { hour: "2-digit", minute: "2-digit" }).format(d);
}
function fmtDate(isoDate) {
  // yyyy-MM-dd
  return isoDate;
}
function mins(m) {
  const sign = m > 0 ? "+" : "";
  return `${sign}${m}`;
}

async function api(url, options) {
  const res = await fetch(url, options);
  const txt = await res.text();
  const data = txt ? JSON.parse(txt) : null;
  if (!res.ok) {
    const msg = data?.error || `${res.status} ${res.statusText}`;
    throw new Error(msg);
  }
  return data;
}

function setTodayInputs(dateStr) {
  $("#adjDate").value = dateStr;
  $("#nwDate").value = dateStr;
}

function renderDay(day) {
  $("#todaySummary").textContent =
    `date=${day.date} planned=${day.plannedMinutes} worked=${day.workedMinutes} credited=${day.creditedMinutes} effective=${day.effectiveMinutes} delta=${mins(day.deltaMinutes)} flexCandidate=${mins(day.flexCandidate)}`;

  // nonworking form reflect
  $("#nwDate").value = day.date;
  $("#nwFlag").checked = day.isNonWorkingDay;
  $("#nwNote").value = day.note || "";

  // sessions
  const sess = $("#sessions");
  sess.innerHTML = "";
  if (!day.sessions.length) {
    sess.innerHTML = `<div class="hint">尚無 session。按「上班」開始。</div>`;
  } else {
    for (const s of day.sessions) {
      const open = s.end == null;
      const el = document.createElement("div");
      el.className = "item";
      el.innerHTML = `
        <div class="meta">
          <div class="title">#${s.id} ${fmtTime(s.start)} → ${fmtTime(s.end)} ${open ? '<span class="badge">OPEN</span>' : ""}</div>
          <div class="sub">start=${s.start} end=${s.end ?? "null"}</div>
        </div>
        <div class="right">
          ${open ? "" : `<button class="danger" data-del-session="${s.id}">刪除</button>`}
        </div>`;
      sess.appendChild(el);
    }
  }

  // adjustments
  const adjs = $("#adjustments");
  adjs.innerHTML = "";
  if (!day.adjustments.length) {
    adjs.innerHTML = `<div class="hint">尚無調整。</div>`;
  } else {
    for (const a of day.adjustments) {
      const el = document.createElement("div");
      el.className = "item";
      el.innerHTML = `
        <div class="meta">
          <div class="title">#${a.id} [${a.kind}] minutes=${mins(a.minutes)}</div>
          <div class="sub">${a.note || ""}</div>
        </div>
        <div class="right">
          <button class="danger" data-del-adj="${a.id}">刪除</button>
        </div>`;
      adjs.appendChild(el);
    }
  }
}

async function refreshToday() {
  const day = await api("/api/day/today");
  setTodayInputs(day.date);
  renderDay(day);
}

async function clockIn() {
  const day = await api("/api/clock/in", { method: "POST" });
  renderDay(day);
}

async function clockOut() {
  const day = await api("/api/clock/out", { method: "POST" });
  renderDay(day);
}

async function deleteSession(id) {
  const day = await api(`/api/sessions/${id}`, { method: "DELETE" });
  renderDay(day);
}

async function addAdjustment(e) {
  e.preventDefault();
  const payload = {
    date: $("#adjDate").value,
    kind: $("#adjKind").value.trim(),
    minutes: Number($("#adjMinutes").value || 0),
    note: $("#adjNote").value.trim()
  };
  const day = await api("/api/adjustments", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  renderDay(day);
}

async function deleteAdjustment(id) {
  const day = await api(`/api/adjustments/${id}`, { method: "DELETE" });
  renderDay(day);
}

async function setNonWorking(e) {
  e.preventDefault();
  const date = $("#nwDate").value;
  const payload = {
    isNonWorkingDay: $("#nwFlag").checked,
    note: $("#nwNote").value.trim()
  };
  const day = await api(`/api/day/${date}/nonworking`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  renderDay(day);
}

function renderMonth(m) {
  $("#monthSummary").textContent = `month=${m.year}-${String(m.month).padStart(2, "0")} flexBankEnd=${m.flexBankEnd} days=${m.days.length}`;

  const tbody = $("#monthTable tbody");
  tbody.innerHTML = "";
  for (const d of m.days) {
    const tr = document.createElement("tr");
    const deltaCls = d.deltaMinutes < 0 ? "bad" : (d.deltaMinutes > 0 ? "good" : "");
    const deficitCls = d.deficitMinutes > 0 ? "bad" : "";
    tr.innerHTML = `
      <td class="mono">${fmtDate(d.date)}${d.isNonWorkingDay ? ' <span class="badge">OFF</span>' : ""}</td>
      <td class="mono">${d.plannedMinutes}</td>
      <td class="mono">${d.workedMinutes}</td>
      <td class="mono">${mins(d.creditedMinutes)}</td>
      <td class="mono">${d.effectiveMinutes}</td>
      <td class="mono ${deltaCls}">${mins(d.deltaMinutes)}</td>
      <td class="mono">${mins(d.flexCandidate)}</td>
      <td class="mono">${mins(d.flexApplied)}</td>
      <td class="mono">${d.flexBankEnd}</td>
      <td class="mono ${deficitCls}">${d.deficitMinutes ? d.deficitMinutes : ""}</td>
      <td>${d.note || ""}</td>
    `;
    tbody.appendChild(tr);
  }
}

async function loadMonth() {
  const ym = $("#monthPick").value; // yyyy-MM
  if (!ym) return;
  const [y, m] = ym.split("-").map(Number);
  const includeEmpty = $("#includeEmpty").checked;
  const data = await api(`/api/month/${y}/${m}?includeEmpty=${includeEmpty}`);
  renderMonth(data);
}

function initDefaults() {
  const now = new Date();
  const yyyy = now.getFullYear();
  const mm = String(now.getMonth() + 1).padStart(2, "0");
  $("#monthPick").value = `${yyyy}-${mm}`;
}

document.addEventListener("click", async (e) => {
  const t = e.target;
  if (t?.dataset?.delSession) {
    try { await deleteSession(t.dataset.delSession); } catch (err) { alert(err.message); }
  }
  if (t?.dataset?.delAdj) {
    try { await deleteAdjustment(t.dataset.delAdj); } catch (err) { alert(err.message); }
  }
});

$("#btnIn").addEventListener("click", async () => { try { await clockIn(); } catch (e) { alert(e.message); } });
$("#btnOut").addEventListener("click", async () => { try { await clockOut(); } catch (e) { alert(e.message); } });
$("#btnRefresh").addEventListener("click", async () => { try { await refreshToday(); } catch (e) { alert(e.message); } });

$("#adjForm").addEventListener("submit", async (e) => { try { await addAdjustment(e); } catch (err) { alert(err.message); } });
$("#nonWorkForm").addEventListener("submit", async (e) => { try { await setNonWorking(e); } catch (err) { alert(err.message); } });

$("#btnMonth").addEventListener("click", async () => { try { await loadMonth(); } catch (err) { alert(err.message); } });

(async function main(){
  initDefaults();
  await refreshToday();
  await loadMonth();
})();
