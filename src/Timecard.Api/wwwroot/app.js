import { timecardApi } from "./timecardApi.js";

// ============================================================================
// Utility Functions
// ============================================================================

const $ = (sel) => document.querySelector(sel);

/**
 * Wraps an async function with UI error handling (alert on error)
 */
function withUiError(fn) {
  return async (...args) => {
    try {
      return await fn(...args);
    } catch (e) {
      alert(e.message || "發生錯誤");
    }
  };
}

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

function setTodayInputs(dateStr) {
  $("#adjDate").value = dateStr;
  $("#nwDate").value = dateStr;
}

// ============================================================================
// Day View Rendering
// ============================================================================

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

// ============================================================================
// Day Operations (using timecardApi)
// ============================================================================

const refreshToday = withUiError(async () => {
  const day = await timecardApi.getToday();
  setTodayInputs(day.date);
  renderDay(day);
});

const clockIn = withUiError(async () => {
  const day = await timecardApi.clockIn();
  renderDay(day);
});

const clockOut = withUiError(async () => {
  const day = await timecardApi.clockOut();
  renderDay(day);
});

const deleteSession = withUiError(async (id) => {
  const day = await timecardApi.deleteSession(id);
  renderDay(day);
});

const addAdjustment = withUiError(async (e) => {
  e.preventDefault();
  const payload = {
    date: $("#adjDate").value,
    kind: $("#adjKind").value.trim(),
    minutes: Number($("#adjMinutes").value || 0),
    note: $("#adjNote").value.trim()
  };
  const day = await timecardApi.addAdjustment(payload);
  renderDay(day);
});

const deleteAdjustment = withUiError(async (id) => {
  const day = await timecardApi.deleteAdjustment(id);
  renderDay(day);
});

const setNonWorking = withUiError(async (e) => {
  e.preventDefault();
  const date = $("#nwDate").value;
  const payload = {
    isNonWorkingDay: $("#nwFlag").checked,
    note: $("#nwNote").value.trim()
  };
  const day = await timecardApi.setNonWorking(date, payload);
  renderDay(day);
});

// ============================================================================
// Month View Rendering
// ============================================================================

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

// ============================================================================
// Month Operations (using timecardApi)
// ============================================================================

const loadMonth = withUiError(async () => {
  const ym = $("#monthPick").value; // yyyy-MM
  if (!ym) return;
  const [y, m] = ym.split("-").map(Number);
  const includeEmpty = $("#includeEmpty").checked;
  const data = await timecardApi.getMonth(y, m, includeEmpty);
  renderMonth(data);
});

// ============================================================================
// Initialization
// ============================================================================

function initDefaults() {
  const now = new Date();
  const yyyy = now.getFullYear();
  const mm = String(now.getMonth() + 1).padStart(2, "0");
  $("#monthPick").value = `${yyyy}-${mm}`;
}

// ============================================================================
// Event Listeners
// ============================================================================

// Delegated event handling for delete buttons
document.addEventListener("click", async (e) => {
  const t = e.target;
  if (t?.dataset?.delSession) {
    await deleteSession(t.dataset.delSession);
  }
  if (t?.dataset?.delAdj) {
    await deleteAdjustment(t.dataset.delAdj);
  }
});

// Clock in/out buttons
$("#btnIn").addEventListener("click", clockIn);
$("#btnOut").addEventListener("click", clockOut);
$("#btnRefresh").addEventListener("click", refreshToday);

// Form submissions
$("#adjForm").addEventListener("submit", addAdjustment);
$("#nonWorkForm").addEventListener("submit", setNonWorking);

// Month view
$("#btnMonth").addEventListener("click", loadMonth);

// ============================================================================
// Main Entry Point
// ============================================================================

(async function main() {
  initDefaults();
  await refreshToday();
  await loadMonth();
})();
