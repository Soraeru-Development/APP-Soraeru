# 09 — App／API：同字重產 ≤3 與分析錯誤態

**What to build:** 同一使用者對同一語言＋正規化字串的「重新產生」最多 3 次且計入日額度；額度用盡、硬閘失敗、重產達上限等錯誤在 App 有清楚可恢复訊息。

**Blocked by:** None — can start immediately（分析 API／結果頁已存在）

**Status:** ready-for-agent

## Parent

[`docs/AI 空耳外語學習 APP－MVP 系統規劃書/Cursor-MVP App 規劃書.md`](../AI%20空耳外語學習%20APP－MVP%20系統規劃書/Cursor-MVP%20App%20規劃書.md)（結果頁重產規則、§14.2、W5 錯誤態）  
策略補充：[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md) Further Notes（App-first）

## What to build

現行結果頁已有「重新產生」（force refresh）但無同字上限。本票端到端：後端強制同字重產 ≤3（與日額度並列），App 顯示剩餘重產或達上限原因；額度用盡、硬閘／schema 失敗耗盡、網路失敗等路徑回傳可區分錯誤並在分析中／結果流用繁中提示，使用者知道可否稍後再試或改手動輸入。不開完整 SRS、不改 Prompt 主文（除非為對齊錯誤碼契約之必要欄位）。

## Acceptance criteria

- [ ] 同使用者＋語言＋正規化字串：重產（強制刷新）超過 3 次被拒絕，並有清楚錯誤碼／文案。
- [ ] 每次成功的重產仍計入每日 AI 額度（與規劃書一致）。
- [ ] 額度用盡時 App 提示明確，不會假裝分析成功。
- [ ] 硬閘／分析失敗路徑有可理解訊息（可返回重試或稍後再試）。
- [ ] 以 Application／API 行為測試為主覆蓋上限與錯誤碼；App 手動煙測達上限與額度用盡。

## Blocked by

- None — can start immediately
