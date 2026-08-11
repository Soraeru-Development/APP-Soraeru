# Client-first 單字本與可選同步（薄規格）

架構決策以 [`docs/adr/0007-client-first-wordcards-optional-sync.md`](../adr/0007-client-first-wordcards-optional-sync.md) 為準；術語見 [`docs/glossary.md`](../glossary.md)。本文只補產品行為邊界，不重複 ADR 論證。

## Problem Statement

每人空耳不同，個人助記是產品核心；單字卡若永遠以雲端為唯一真相，離線弱、且易與「金標覆寫分析」混淆成「改掉我卡上的空耳」。學習者需要：App 上本機可靠、登入後可選跨裝置會合，且已存個人空耳不被金標強蓋。

## Solution

- **App**：本機單字本為真相來源；有登入工作階段即可離線寫入，連線後與**雲端鏡像**推拉至最終一致。  
- **雲端鏡像**：既有帳號綁定之單字卡儲存（含今日 Server notebook 列）改扮同步／Web 過渡持久化角色。  
- **Web MVP1（過渡）**：暫以雲端鏡像為該端持久化；非最終「瀏覽器本機 SoT」。  
- **Server 仍擁有**：帳號、額度、已驗證空耳與分析管線（金標只進分析，不回寫覆盖已存卡）。

## User Stories（精簡）

1. As a 已登入學習者 on App, I want 離線也能新增／編輯／刪除單字卡並在連線後同步, so that 弱網仍能維護個人空耳。  
2. As a 未登入學習者, I want 若裝置上已有本機單字本仍可唯讀瀏覽, so that 不必登入也能複習；但我不能在未登入時寫入。  
3. As a 多裝置學習者, I want 登入後開 App 推拉至最終一致（整卡 LWW／tombstone）, so that 換機或平板能會合同一本帳號的卡。  
4. As a 學習者, I want 在詳情頁隨時改卡上個人空耳, so that 助記可貼近自己的聽感。  
5. As a 學習者, I want 分析命中已驗證空耳時只影響該次結果候選、不改寫我已存卡上的空耳, so that 金標不會蓋掉個人版本。  
6. As a 學習者, I want 換帳時本機單字本與前一帳號隔離, so that 不會把別人的卡推進我的雲端。  
7. As a 學習者, I want 刪帳時雲端單字本一併刪除, so that 備份不殘留。  
8. As a Web MVP1 學習者, I want 登入後經雲端鏡像使用單字本, so that 薄 Web 不阻塞，同時與 App 同步會合。

## Implementation Decisions

- **真相來源**：App 本機；雲端＝鏡像；Web MVP1＝讀寫鏡像（過渡）。  
- **同步**：前景／開 App 推拉；穩定卡 ID 聯集；整卡 LWW；刪除 tombstone 較新則刪除勝出。  
- **寫入門檻**：需登入工作階段（可離線寫本機）；未登入唯讀。  
- **金標邊界**：僅分析管線；單字卡個人空耳欄位不受其強制覆寫。  
- **隱私**：雲端內容伺服器可讀；使用者匯出本階段不做。  
- **不變**：共用 API、Users／UsageDaily、VerifiedMnemonics 仍在 Server（ADR-0001、0006）。

## Testing Decisions

- 好測試：外部行為（未登入不可寫、已登入離線寫入後連線會合、LWW／tombstone、換帳隔離、金標不改已存卡）。  
- 優先 seams：本機倉儲＋同步合併純邏輯；鏡像 API 授權（僅本人）；分析覆寫不回寫卡。  
- Prior art：既有 Notebook 應用層／合約測試可演進為「鏡像」語意，而非刪除覆蓋。

## Out of Scope

- 即時多裝置协同、CRDT、衝突詢問 UI、欄位級 LWW。  
- 端到端加密、匯出檔、完整刪帳法遵流程細節以外的行銷／設定重整。  
- Web IndexedDB Client-first（另決策）。  
- 拆票與實作時程（交 Station 3）。  
- iOS、完整 SRS。

## Further Notes

- **Next**：用 `to-tickets` 拆垂直切片（本機 DB → 同步協定 → 鏡像 API 語意調整 → App UI 編輯 → 回歸金標不蓋卡）。  
- 與 [`parallel-web-curator-trust.md`](./parallel-web-curator-trust.md) 張力：Web／單字本「共用帳戶語意」保留，但**擁有權模型**以 ADR-0007 為準。
