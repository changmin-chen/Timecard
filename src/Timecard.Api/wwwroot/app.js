import {timecardApi} from "./timecardApi.js";

const $ = (sel) => document.querySelector(sel);

function fmtTime(dt) {
    if (!dt) return "—";
    const d = new Date(dt);
    return new Intl.DateTimeFormat("zh-Hant", {hour: "2-digit", minute: "2-digit"}).format(d);
}

function mins(m) {
    const sign = m > 0 ? "+" : "";
    return `${sign}${m}`;
}

// Error handling utilities
function showError(message) {
    // // For now using alert, but can be replaced with toast notifications
    alert(message);
}

function withErrorHandling(asyncFn) {
    return async (...args) => {
        try {
            await asyncFn(...args);
        } catch (err) {
            showError(err.message);
        }
    };
}


function setTodayInputs(dateStr) {
    $("#arDate").value = dateStr;
    $("#nwDate").value = dateStr;
}

function renderDay(day) {
    const span = `start=${day.start ?? "null"} end=${day.end ?? "null"} punches=${day.punchCount}`;
    $("#todaySummary").textContent =
        `date=${day.date} ${span} planned=${day.plannedMinutes} worked=${day.workedMinutes} extension=${day.extensionMinutes} effective=${day.effectiveMinutes} delta=${mins(day.deltaMinutes)} flexCandidate=${mins(day.flexCandidate)}`;

    // nonworking reflect
    $("#nwDate").value = day.date;
    $("#nwFlag").checked = day.isNonWorkingDay;
    $("#nwNote").value = day.note || "";

    // punches
    const list = $("#punches");
    list.innerHTML = "";
    if (!day.punches.length) {
        list.innerHTML = `<div class="hint">尚無 punch。按「打卡」新增一筆。</div>`;
    } else {
        for (const p of day.punches) {
            const el = document.createElement("div");
            el.className = "item";
            el.innerHTML = `
        <div class="meta">
          <div class="title">#${p.id} ${fmtTime(p.at)}</div>
          <div class="sub">${p.at} ${p.note ? `| ${p.note}` : ""}</div>
        </div>
        <div class="right">
          <button class="danger" data-del-punch="${p.id}">刪除</button>
        </div>`;
            list.appendChild(el);
        }
    }

    // attendance requests
    const arList = $("#attendanceRequests");
    arList.innerHTML = "";
    if (!day.attendanceRequests.length) {
        arList.innerHTML = `<div class="hint">尚無出勤申請。</div>`;
    } else {
        for (const a of day.attendanceRequests) {
            const el = document.createElement("div");
            el.className = "item";
            el.innerHTML = `
        <div class="meta">
          <div class="title">#${a.id} [${a.category}] ${a.start} ~ ${a.end}</div>
          <div class="sub">${a.note || ""}</div>
        </div>
        <div class="right">
          <button class="danger" data-del-ar="${a.id}">刪除</button>
        </div>`;
            arList.appendChild(el);
        }
    }
}

async function refreshToday() {
    const day = await timecardApi.getToday();
    setTodayInputs(day.date);
    renderDay(day);
}

async function punch() {
    const day = await timecardApi.punch();
    renderDay(day);
}

async function deletePunch(id) {
    const day = await timecardApi.deletePunch(id);
    renderDay(day);
}

async function addAttendanceRequest(e) {
    e.preventDefault();
    const payload = {
        date: $("#arDate").value,
        category: $("#arCategory").value.trim(),
        start: $("#arStart").value,
        end: $("#arEnd").value,
        note: $("#arNote").value.trim()
    };
    const day = await timecardApi.addAttendanceRequest(payload);
    renderDay(day);
}

async function deleteAttendanceRequest(id) {
    const day = await timecardApi.deleteAttendanceRequest(id);
    renderDay(day);
}

async function setNonWorking(e) {
    e.preventDefault();
    const date = $("#nwDate").value;
    const payload = {
        isNonWorkingDay: $("#nwFlag").checked,
        note: $("#nwNote").value.trim()
    };
    const day = await timecardApi.setNonWorking(date, payload);
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
      <td class="mono">${d.date}${d.isNonWorkingDay ? ' <span class="badge">OFF</span>' : ""}</td>
      <td class="mono">${d.punchCount}</td>
      <td class="mono">${d.plannedMinutes}</td>
      <td class="mono">${d.workedMinutes}</td>
      <td class="mono">${mins(d.extensionMinutes)}</td>
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
    const ym = $("#monthPick").value;
    if (!ym) return;
    const [y, m] = ym.split("-").map(Number);
    const includeEmpty = $("#includeEmpty").checked;
    const data = await timecardApi.getMonth(y, m, includeEmpty);
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
    if (t?.dataset?.delPunch) {
        await withErrorHandling(deletePunch)(t.dataset.delPunch);
    }
    if (t?.dataset?.delAr) {
        await withErrorHandling(deleteAttendanceRequest)(t.dataset.delAr);
    }
});

$("#btnPunch").addEventListener("click", withErrorHandling(punch));
$("#btnRefresh").addEventListener("click", withErrorHandling(refreshToday));

$("#arForm").addEventListener("submit", withErrorHandling(addAttendanceRequest));
$("#nonWorkForm").addEventListener("submit", withErrorHandling(setNonWorking));

$("#btnMonth").addEventListener("click", withErrorHandling(loadMonth));

(async function main() {
    initDefaults();
    await refreshToday();
    await loadMonth();
})();
