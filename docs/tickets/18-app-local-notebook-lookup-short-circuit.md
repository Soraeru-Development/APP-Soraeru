# 18 — App：同字再查本機短路＋詳情重新分析



**What to build:** 手動輸入與 OCR 選字後，以本機單字本查鍵；命中直接開詳情（無確認、不打 analyze）；未命中走分析。詳情提供「重新分析」進分析／結果流並計額度；重產 ≤3 銜接票 09。



**Blocked by:** 13（done）



**Status:** done



## Parent



[`docs/specs/local-notebook-lookup-short-circuit.md`](../specs/local-notebook-lookup-short-circuit.md) · [`docs/adr/0008-local-notebook-lookup-short-circuit.md`](../adr/0008-local-notebook-lookup-short-circuit.md) · Glossary：本機短路、重新分析、正規化鍵、本機真相來源  

本機 SoT：[client-first-wordcards-sync](../specs/client-first-wordcards-sync.md) · ADR-0007 · 票 13



## What to build



在進入分析前插入本機查鍵：鍵＝`OwnerUserId`＋`DetectedLanguage`＋`NormalizedText`（與存卡一致）。**必須有可用語言碼才短路**；語言未知時不得撞本機，先走分析（含語種偵測）。OCR 選字與手動輸入共用同一套短路。命中 → 直接導向既有詳情，略過 analyze 與結果頁，且**無**「是否再開分析」確認框。未命中 → 既有分析路徑（含額度）。未登入：本機命中仍可開詳情；本機未命中則須登入才能分析。詳情頁加明確「重新分析」CTA → 分析／結果流，計額度；同字重產上限語意與票 09 **shared contract**（若 09 尚未落地 ≤3 UI／後端上限，本票不強制實作 ≤3，但入口／計額意圖須接得上，避免兩套計數）。刪卡（含 tombstone／已刪不參與命中）後再查同一鍵應分析。查重**只**打本機單字本，禁止為短路打雲端鏡像或金標庫；金標仍只影響分析管線（≠ 開卡短路）。表面＝App only。



## Acceptance criteria



- [x] 已登入：手動輸入本機已存字（有語言碼）→ 直接開該卡詳情；不發 analyze；無確認框。

- [x] OCR 選字後與手動同一短路行為（命中開詳情／未命中分析）。

- [x] 無可用語言碼時不短路，走分析管線。

- [x] 本機未命中走既有分析路徑；未登入未命中須登入才能分析。

- [x] 未登入本機命中仍可開詳情（唯讀複習）。

- [x] 刪除（或 tombstone）後再查同一鍵會分析，不再短路到已刪卡。

- [x] 同一正規化字串、不同 `DetectedLanguage` 為不同卡，互不誤開。

- [x] 詳情「重新分析」進入分析／結果流且可觀測計額意圖；與票 09 重產上限／錯誤態銜接（shared contract）。若 09 未做完，本票可不強制 ≤3 UI，但不得另開不相容計數。

- [x] 短路路徑不呼叫雲端鏡像／Notebook 鏡像 API、不查已驗證空耳庫當開卡條件。

- [x] 聚焦單元測試＋煙測：查鍵純邏輯（或 ClientLogic 同等）、導航／登入門檻；不以「打了鏡像 API」當成功。



## Blocked by



- 13 — App：本機單字卡儲存與列表／存／刪（Client-first）（done）



## Out of scope



- Web 學習端短路對齊

- 為短路查雲端鏡像或金標庫當開卡

- 命中確認框、合併多卡 UI

- 票 09 重產 ≤3／錯誤態本體（本票只銜接）

- 同步協定／LWW（14／15）、個人空耳編修 UI（16）

- iOS、完整 SRS



## Notes



- 完成（done），2026-08-12。

- **與 09**：結果頁 ForceRefresh／重產仍允許打 AI；上限實作屬 09。本票負責詳情「重新分析」入口與「計額度」語意對齊，方便 09 接 shared contract。

- **與 14／16／17**：無硬依賴；可與 14／16／17 平行。他機新建尚未同步到本機的卡，再查仍會分析——接受為 Client-first 後果（ADR-0008）。

- **Prior art**：票 13 本機倉儲／同鍵覆寫／刪除；票 09 重產上限（可後接）。

- **Station 4（2026-08-12）**：

  - ClientLogic：`LocalNotebookLookupKey`、`AnalyzeEntryGate`、`LocalNotebookService.FindActiveByLookupKeyAsync`（只打本機 List／store）。

  - App：`WordInputPage`／`OcrSelectPage`（含來源語言 picker）進分析前短路；`NotebookDetailPage`「重新分析」→ `ForceRefresh=true`（與票 09 同一計數語意）。

  - 驗證：`dotnet test tests/Soraeru.ClientLogic.Tests`（含 Lookup／Gate 24 測＋全套綠）。

  - 殘餘：實機煙測（手動／OCR 命中直開詳情、未登入門檻、重新分析計額）；Android 全量打包若遇本機 javac 環境問題與本票無關。

