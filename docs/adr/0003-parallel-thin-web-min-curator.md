# 平行交付：薄 Web 學習端＋最小策展 CRUD

第一個可出貨切片採 **平行**：Web 學習端子集與策展端最小 CRUD 一起推進，共用「金標優先覆寫」與（預期）同一後端分析／帳號語意。

**Status**: accepted  
**Decided in**: Station 1 Round 1 Q7=C；Round 2 Q11=B、Q13=B

## Considered Options

- **先策展／先 Web／平行薄切片（選定）** — 見初稿。

## Consequences

- **策展 MVP1 欄位**：語言、原詞、displayText、notationText、explanation、啟用／下架。  
- **Web MVP1 頁面**：登入、手動輸入、分析結果、單字本（薄起點不變）。  
- **Web 定位補充**：App canonical、Web 一級學習通道（Duolingo 式 App-first／parity 軌跡），非永久玩具站。  
- 初期已驗證覆蓋率低；未驗證路徑靠「LLM 草稿標示＋後處理硬閘」（ADR-0004），並並行 Prompt 迭代。  
- 策展為獨立站（ADR-0005）；三端共用同一 API（ADR-0006）。
