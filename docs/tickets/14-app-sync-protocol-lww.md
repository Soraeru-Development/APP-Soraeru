# 14 — App：可選同步協定（整卡 LWW／tombstone／換帳隔離）

**What to build:** 已登入 App 在開 App／回到前景時與雲端鏡像推拉，達成多裝置**最終一致**（非即時）：穩定卡 ID 聯集、整卡 LWW、tombstone 刪除傳播、換帳隔離。

**Blocked by:** 13（done）

**Status:** ready-for-agent

## Parent

[`docs/specs/client-first-wordcards-sync.md`](../specs/client-first-wordcards-sync.md) · ADR-0007 · Glossary：可選雲端同步、整卡 LWW、tombstone、雲端鏡像

## What to build

在本機 SoT（13）之上實作同步合併與協調：每卡穩定 ID、`UpdatedAt`、刪除以 tombstone 表示；合併＝卡 ID 聯集＋整卡 LWW（較新勝出；較新 tombstone 則刪除勝出）。觸發＝開 App／前景推拉，不做即時协同／CRDT／衝突詢問 UI。換帳時本機單字本與前一帳號隔離，不得把 A 帳資料推進 B 帳雲端。合併純邏輯應可單測；與真實鏡像 API 的端到端會合在 15 完成後驗收。時鐘偏移偶發錯序本階段接受（ADR-0007）。

## Acceptance criteria

- [ ] 已登入且在線時，開 App 或回前景會觸發與雲端鏡像的推拉（可先對測試雙端／假鏡像驗證協定行為）。
- [ ] 同帳兩端對同一卡 ID 的衝突以整卡 LWW（`UpdatedAt`）裁決；較新 tombstone 刪除勝出。
- [ ] 僅一端存在的卡經同步後出現在另一端（聯集合併）。
- [ ] 換帳後本機與前帳隔離；不會錯誤推送前帳卡片到新帳雲端。
- [ ] 合併／LWW／tombstone 規則有聚焦單元測試（優先 seams：同步合併純邏輯）。
- [ ] 不做即時协同、欄位級 LWW、衝突詢問 UI。

## Blocked by

- 13 — App：本機單字卡儲存與列表／存／刪（Client-first）（done；殘餘見票 13 Notes，不阻本票開工）
