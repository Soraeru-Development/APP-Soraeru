# 16 — App：詳情頁隨時編修個人空耳

**What to build:** 學習者在單字卡詳情頁可**隨時**編修卡上個人空耳並持久化到本機 SoT；之後若已登入且同步可用，變更經既有推拉會合（不另做衝突 UI）。

**Blocked by:** 13（done）

**Status:** ready-for-agent

## Parent

[`docs/specs/client-first-wordcards-sync.md`](../specs/client-first-wordcards-sync.md)（User Story 4）· ADR-0007 · Glossary：個人空耳、單字卡

## What to build

補齊產品核心編修路徑：從單字本進入詳情後，可改卡上個人空耳（助記文字／既有契約欄位），不限「剛分析完」當下。寫入遵守 13 門檻（需登入工作階段；可離線寫本機）。未登入詳情為唯讀。本票不重做列表／同步協定；同步就緒後編修應帶正確 `UpdatedAt`（或等效）以便 LWW。不做匯出、不做金標回寫。

## Acceptance criteria

- [ ] 已登入學習者可在詳情頁修改個人空耳並保存；重開詳情／列表所見為新內容。
- [ ] 離線已登入亦可完成本機編修；連線後若 14／15 已就緒則可被推拉（本票至少保證本機持久化正確）。
- [ ] 未登入不可編修（唯讀）。
- [ ] 編修不觸發分析額度、不呼叫產空耳 LLM。
- [ ] 可用 App 煙測＋本機倉儲測試驗證。

## Blocked by

- 13 — App：本機單字卡儲存與列表／存／刪（Client-first）（done）
