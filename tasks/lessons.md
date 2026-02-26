# Lessons Learned

## API / Type Boundary

### Lesson: Never format primitive types on the backend for frontend consumption
**Context**: `MonthDayAttendanceDto.Start/End` were initially emitted as pre-formatted `"HH:mm"` strings. `MonthDayDto.Date` was emitted as a `"yyyy-MM-dd"` string instead of `DateOnly`.

**Rule**: The API should return the richest type the domain has (`DateOnly`, `TimeOnly`, `DateTimeOffset`, etc.) and let the serializer produce the canonical wire format. The frontend owns all presentation formatting. Pre-formatting on the backend:
- couples the API to a single display style
- loses type information for consumers
- violates separation of concerns

**Pattern**:
- `DateOnly` → serializes as `"yyyy-MM-dd"` (System.Text.Json, .NET 7+) ✓
- `TimeOnly` → serializes as `"HH:mm:ss"` ✓
- `DateTimeOffset` → serializes as ISO 8601 ✓
- Frontend uses `fmtTimeStr`, `fmtTime`, `Intl.DateTimeFormat`, etc. to present

**Wrong**: `r.Range.Start.ToString(@"HH\:mm")`
**Right**: `r.Range.Start` (let System.Text.Json serialize `TimeOnly` directly)

---

## UI / Display Convention

### Lesson: Distinguish aggregate stats from per-row detail display
**Context**: Changed all minute values to H:MM; user corrected that stat cards (彈性餘額, 累計不足) should stay as raw minutes with 分 unit.

**Rule**: Aggregate summary numbers (bank balance, total deficit) read better as plain integers with a unit label. Per-row table cells benefit from H:MM for scannability. Apply format by context, not uniformly.

| Location | Format | Example |
|---|---|---|
| Stat card summaries | `N 分` | `+55 分` |
| Table: 工時, 彈性增減, 不足 | H:MM | `8:40`, `+0:55` |
| TodayCard daily stats | `N 分` | `520 / 540 分` |


## Frontend: Consistent Icon/Symbol Sizing

**Session**: revise/client-uiux-improval — MonthReport status column

**Mistake pattern** (3-step failure):
1. Used Unicode chars (●◆▲) directly in text → sizes inconsistent across glyphs
2. Switched to inline SVG → still appeared inconsistent (shapes had same mathematical bounding box but no optical sizing compensation)
3. Went back to Unicode with monospace font → same root problem persisted

**Root cause**:
Unicode geometric glyphs are designed with different proportions within the font's em square. No font guarantees visual size parity across ●, ◆, ▲. SVG works in principle but requires careful optical sizing per shape (a triangle needs ~15–20% more area than a circle to appear the same visual weight).

**Correct solution: CSS-generated shapes**
- Use `background: currentColor` + CSS geometry instead of font glyphs or SVG
- Circle: `border-radius: 50%`
- Diamond: small square + `transform: rotate(45deg)` — size by diagonal (side × √2)
- Triangle: `clip-path: polygon(50% 0%, 0% 100%, 100% 100%)` — make slightly wider to compensate optical weight
- Wrap in `display: inline-flex; align-items: center; gap: Xpx` for reliable vertical alignment

**Rule**: When multiple icon shapes must appear visually equal in size, never use Unicode characters or raw SVG without optical compensation. Default to CSS-generated shapes — they are font-independent and pixel-exact.
