# App：同字再查只查本機並短路開詳情

在 Client-first（ADR-0007）下，App「再查同一字」以**本機單字本**為唯一查重 SoT：命中則**直接開詳情**（無確認、不打 analyze）；金標命中≠開卡短路。短路鍵須含語言碼；未知語先分析。詳情提供「重新分析」計額度。本切片只釘 App；Web 另對齊。

**Status**: accepted  
**Decided in**: 已釘共識（查重＝本機／不查鏡像；命中直開詳情；OCR 同套；鍵＝OwnerUserId＋DetectedLanguage＋NormalizedText 且須有語言；未登入可短路讀；Q7=A 未知語先分析；Q8=A 詳情重新分析；Q9=A 未命中須登入才分析；Q10=B Web 延後；結果頁 ForceRefresh 仍允、上限票 09）  
**Depends on**: ADR-0007（本機 SoT）；金標邊界 ADR-0001／0007（分析管線 only）

## Considered Options

- **本機短路（選定）**：查重只打本機單字本；命中開詳情、省額度與等待。  
- **再查一律分析**：簡單但與「卡已在本機」矛盾，浪費額度；已拒為預設。  
- **查重含雲端鏡像**：未同步完／離線時行為不穩，且違背「App SoT＝本機」；已拒。  
- **金標命中當開卡**：混淆策展庫與個人單字本；已拒（金標只影響分析）。  
- **無語言碼也試撞本機**：誤開風險高；選定「須有語言碼才短路，未知語先分析」（Q7=A）。  
- **命中確認框／Web 同步改**：確認框多餘；Web 對齊另切片（Q10=B）。

## Locked policy (摘要)

| 主題 | 決定 |
|------|------|
| 查重 SoT | 僅本機單字本（票 13）；不查雲端鏡像 |
| 命中 | 直接詳情；無確認；不 analyze |
| 鍵 | OwnerUserId＋DetectedLanguage＋NormalizedText；**無語言碼不短路** |
| OCR | 選字後與手動同一套短路 |
| 未登入 | 可短路開既有本機詳情；未命中須登入才分析 |
| 詳情 | 「重新分析」→ 分析／結果；計額度；上限票 09 |
| 結果頁重產 | ForceRefresh 仍允許；≤3 歸票 09 |
| 金標 | ≠ 開卡短路；只影響分析管線 |
| 其他邊界 | 刪卡後再查應分析；同鍵再存覆寫；不同語言兩張卡 |
| 表面 | App only；Web 維持現況 |

## Consequences

- App 輸入／OCR 進入分析前須有本機查鍵步驟；缺語言則走分析。  
- 他機新建、尚未同步到本機的卡，再查仍會分析（同步後才短路）——接受為 Client-first 後果。  
- 「重新分析」與結果頁重產共用額度／票 09 上限語意，避免兩套計數。  
- Web 短期可能與 App 不一致（再查仍分析）；對齊前勿假設 parity。  
- 薄規格見 `docs/specs/local-notebook-lookup-short-circuit.md`；執行票見 `docs/tickets/18-app-local-notebook-lookup-short-circuit.md`。
