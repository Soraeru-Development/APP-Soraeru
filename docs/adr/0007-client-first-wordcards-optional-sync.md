# Client-first 單字卡＋可選雲端同步

單字卡改以**客戶端本機為真相來源（Client-first）**；登入後可選**雲端備份／多裝置最終一致同步**。已驗證空耳、帳號與額度仍在 Server。金標優先覆寫**只影響分析結果**，**不得**強蓋使用者已存進單字卡的個人空耳。既有 Server `WordCards` **角色重釋**為雲端鏡像／同步儲存（非另起第二本帳）。

**Status**: accepted  
**Decided in**: Station 1 Round 1 Q1=B、Q2=B、Q3=A、Q4=A、Q5=A；Round 2 Q6=A、Q7=A、Q8=A、Q9=A、Q10=A  
**Supersedes (direction)**: 票 03／平行規格中「雲端擁有單字本」之路徑；帳號／額度／已驗證空耳邊界不變（ADR-0001、0006）。

## Considered Options

- **Client-first＋可選同步（選定）**：本機可離線讀；寫入需登入工作階段（可離線寫本機，連線後推拉）。  
- **持續 Server-owned notebook**：與「個人空耳為產品核心、每人不同」張力大，已拒為長期形狀。  
- **僅備份、不做多裝置**：未採（Round 1 Q2=B）。  
- **即時协同／CRDT／端到端加密**：未採（成本與產品階段不符）。  
- **衝突時詢問使用者／欄位級 LWW**：未採；採整卡 LWW。  
- **Web 與 App 同步協議立刻對齊 IndexedDB**：未採為本階段；Web MVP1 **暫以雲端鏡像**為該端持久化（非終極）。

## Locked policy (摘要)

| 主題 | 決定 |
|------|------|
| 未登入 | 單字本唯讀（可離線讀既有本機資料） |
| 已登入離線 | 可寫本機；連線後推拉 |
| 同步深度 | 多裝置最終一致（開 App／前景推拉；非即時） |
| 衝突 | 整卡 LWW（`UpdatedAt`＋穩定卡 ID）；較新 tombstone 刪除勝出 |
| 合併 | 卡 ID 聯集＋上列規則；**換帳隔離**本機資料 |
| 刪除／刪帳 | tombstone 同步；刪帳刪該使用者雲端單字本；登出／刪帳清本機（或不再屬該帳） |
| 個人空耳編輯 | 詳情頁隨時可改 |
| 雲端隱私 | 伺服器可讀；匯出可後做 |
| 金標 vs 卡 | 分析可覆寫空耳候選；**永不**強蓋已存卡上個人空耳 |
| 既有 Server 列 | 視為各帳號雲端副本，供首次同步拉入 App 本機 |

## Consequences

- App 需本機單字卡儲存＋同步協定（穩定卡 ID、`UpdatedAt`、tombstone）；Notebook API 語意從「SoT」改為「雲端鏡像」。  
- Web MVP1 可續打雲端鏡像，與 App 經同步會合；長期若 Web 也 Client-first 需另決策／ADR。  
- 票 06 等「Web 單字本＝共用帳戶資料」仍成立，但資料面是**鏡像＋同步**，不是「Server 永遠擁有 App 的卡」。  
- 時鐘偏移可能造成偶發錯序；本階段接受整卡 LWW，不以詢問 UI 補救。  
- 薄規格見 `docs/specs/client-first-wordcards-sync.md`。
