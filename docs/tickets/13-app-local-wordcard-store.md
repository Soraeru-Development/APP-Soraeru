# 13 — App：本機單字卡儲存與列表／存／刪（Client-first）

**What to build:** App 上單字本以**本機為真相來源**：已登入（含離線）可新增／列表／刪除單字卡；未登入僅能唯讀既有本機資料。本票先交付可獨立 demo 的本機閉環，不等待雲端同步完成。

**Blocked by:** None — can start immediately（登入／分析存卡入口與 03 鏡像基礎已在；本票改寫 App 擁有權模型）

**Status:** done（殘餘不阻擋關閉：離線 JWT 過期偵測、刪帳 UsageDaily 孤兒列——見 Notes）

## Parent

[`docs/specs/client-first-wordcards-sync.md`](../specs/client-first-wordcards-sync.md) · [`docs/adr/0007-client-first-wordcards-optional-sync.md`](../adr/0007-client-first-wordcards-optional-sync.md) · Glossary：單字卡、單字本、本機真相來源

## What to build

把 App 單字卡從「以 Server notebook 為 SoT」轉成 **Client-first**：本機可持久化列表與卡片內容（含選定／個人空耳等既有學習欄位）。寫入門檻＝有效登入工作階段（可離線寫本機）；未登入不可新增／編輯／刪除，但若裝置上已有本機本則可唯讀瀏覽。登出或刪帳後本機該帳相關單字本應被清除（或不再屬於該工作階段）。本票**不**要求多裝置會合完成（見 14／15）；可先本機自洽。穩定卡 ID、`UpdatedAt`、tombstone 欄位宜在本票資料模型預留，避免 14 大改 schema。

## Acceptance criteria

- [x] 已登入學習者可在 App 將分析結果候選存成本機單字卡，並於單字本列表查看。
- [x] 已登入學習者可刪除本機單字卡；未登入不可寫入（新增／刪除）。
- [x] 未登入時若本機已有資料，單字本為唯讀；無資料時行為清楚（空態／引導登入），不假寫入成功。
- [x] 已登入離線時仍可完成本機新增／刪除；重開 App 後本機資料仍在。
- [x] 登出或刪除帳號後，本機單字本依 ADR-0007 清除（或不屬於該帳／該工作階段）。
- [x] 行為可用本機倉儲／ClientLogic（或同等）單元測試＋App 煙測獨立驗證；不以「打通 Server 才算存檔」為本票通過條件。

## Blocked by

- None — can start immediately

## Notes

- App 存／列表／刪改走 `LocalNotebookService`＋`JsonFileLocalWordCardStore`（`FileSystem.AppDataDirectory/local-wordcards.json`）；不再打 Notebook API 為 SoT。
- 刪除為本機 soft-delete（`DeletedAtUtc`／`UpdatedAtUtc`）以預留票 14；登出呼叫 `ClearLocalNotebookAsync`。
- **刪帳（ADR-0007 Q5=A／Q9=A）**：Settings「刪除帳號」→ `DELETE /api/v1/me`（清該使用者雲端鏡像 WordCards＋Users 列）→ 成功或 401 後 `ClearLocalNotebookAsync`＋清 session → Login。決策邏輯在 `SessionAuthGate`。
- **審查補洞（2026-08-11）**：
  - Splash：本機有 token 且 API 不可達 → 進已登入離線流（依本機 onboarding flag）；**不**丟 Login。
  - Splash／Settings：GetMe 回傳 null（401/403）→ `ClearLocalNotebookAsync`＋清 session（與明確登出一致）；連線失敗不清庫。
  - 同 normalized key 再存：票 **17** 起改為回傳既有卡、**不**覆寫 `SelectedMnemonic`（與 Server 鏡像 Save 一致；編修走票 16）。
  - Splash／Settings 路徑自動化測：`SessionAuthGateTests`（ClientLogic）；刪帳應用層：`MeServiceDeleteAccountTests`。
- **煙測建議**：登入→離線重開 App 應進 Home／Onboarding 且可本機存刪；模擬 401 應清庫並回 Login；設定頁刪帳後雲端／本機皆空且回 Login；結果頁對同字再存應開既有卡且個人空耳不變（改空耳用詳情編修）。
- **Android 模擬器斷網（票 13 離線煙測）**：
  1. 模擬器右側工具列開 **⋯ Extended Controls** → **Cellular** → **Signal strength** 設 **None**（或開 **Airplane mode**）。
  2. 或本機終端：`adb shell svc wifi disable` 再 `adb shell svc data disable`；恢復用 `enable`。
  3. VS 2026 除錯中亦可先暫停 App → 斷網 → 繼續操作本機存刪。恢復網路後再測 Splash 離線／重連行為。
- **殘餘**：離線時無法驗證 JWT 是否已過期（僅有 token＋網路失敗仍會進離線寫）；連回線後 GetMe 401 才清庫。刪帳不刪 UsageDaily 孤兒列（無 FK；完整法遵清檔可後做）。
- **2026-08-11 午後視覺**：Stitch 對齊 NotebookList（細節／動態語言 chip）＋Shell 底欄、NotebookDetail 卡片化；不改本票 AC（已 done）。
- **2026-08-13 UI／缺陷**：語言選項（不含「全部」）>5 改下拉 picker；注音直立排改橫向。曾現：阿語／語言>5 picker 模式下單字卡列表「讀取失敗」——`AutomationId may only be set one time`（`NotebookListPage.RebuildLanguageFilter`）。
- **2026-08-21 修復／調查**：
  - **已修**：語言 >5 走 picker 時勿對同一控制重複設 `AutomationId`，列表「讀取失敗」UI bug 已消。
  - **調查結論**：本機單字卡 SoT 為 **JSON**（`JsonFileLocalWordCardStore`／`local-wordcards.json`），**非** SQLite；模擬器資料仍在與「讀取失敗」為 UI／AutomationId 問題，非資料遺失。
