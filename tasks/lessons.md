# Lessons Learned

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
