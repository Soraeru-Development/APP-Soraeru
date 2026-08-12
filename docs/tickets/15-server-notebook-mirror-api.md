# 15 — Server：Notebook API 雲端鏡像語意（推拉）

**What to build:** 既有帳號綁定之 Server 單字卡儲存**角色重釋**為雲端鏡像：支援 App 推拉至最終一致；既有列＝各帳號首次拉取來源。刪帳時刪除該使用者雲端單字本（含 tombstone）。Web MVP1 過渡期可持續讀寫此鏡像（非 App SoT）。

**Blocked by:** 14（done）

**Status:** done

## Parent

[`docs/specs/client-first-wordcards-sync.md`](../specs/client-first-wordcards-sync.md) · ADR-0007 · 歷史基礎：[`03-notebook-end-to-end.md`](03-notebook-end-to-end.md)

## What to build

將 Notebook／WordCards API 從「Server 擁有 App 單字本」語意改為**雲端鏡像**：授權仍僅本人；契約需承載同步所需欄位（穩定卡 ID、`UpdatedAt`、tombstone 等，與 14 一致）。支援推（本機變更上傳）與拉（雲端→客戶端）；既有列視為該帳雲端副本，供 App 首次同步納入本機。刪除帳號時清除該使用者雲端單字本。不另起「第二本帳」資料面。App 端到端：登入多裝置（或模擬）經推拉會合。Web MVP1（06）過渡仍打此鏡像，本票不實作 Web UI。帳號／額度／已驗證空耳邊界不變。

## Acceptance criteria

- [x] 已登入客戶端可對本人鏡像執行推拉；不可讀寫他人資源。
- [x] 既有 Server 單字卡列可被 App 首次拉取納入本機（角色＝雲端副本，非另庫搬家）。
- [x] 契約支援穩定卡 ID、`UpdatedAt`、tombstone（或等效刪除傳播），與 14 合併規則相容。
- [x] 刪除帳號後該使用者雲端單字本（含墓碑）不殘留。
- [ ] 與 App（13＋14）可 demo：離線寫本機 → 連線推拉 → 第二端會見合同一帳卡集。
- [x] 既有應用層／合約測試演進為鏡像語意，而非留下「Server SoT」成功假象。

## Blocked by

- 14 — App：可選同步協定（整卡 LWW／tombstone／換帳隔離）（done）
- 03 — 單字本端到端（done；歷史鏡像基礎）

## Notes

- **契約**：`GET/PUT /api/v1/notebook/mirror`；欄位對齊 `LocalWordCard`（含 tombstone）。Push＝依卡 ID 整卡 LWW upsert（非 replace-all）；較新 tombstone 刪除勝。
- **CRUD 過渡**：List／Get 隱藏墓碑；Delete 寫 tombstone；Save 帶 `UpdatedAt`。
- **Migration**：`AddWordCardMirrorTimestamps`（`UpdatedAt`／`DeletedAt`；既有列 `UpdatedAt = CreatedAt`）。
- **刪帳**：`DeleteAllByUserAsync` 硬清含墓碑（EF 測覆蓋）。
- **App**：`HttpCloudWordCardMirror` 經 `ISoraeruApiClient` 推拉；`MauiProgram` 替換 `UnavailableCloudWordCardMirror`。
- **驗證**：`dotnet test tests/Soraeru.Application.Tests`（Notebook／Me）、`tests/Soraeru.Infrastructure.Tests`（EfWordCard）；Api／App Windows TFM 建置成功。
- **程式已接線、現場雙端 demo 待驗證**（上述 App demo AC 未勾選，勿當成已現場驗證）。
- **Id 衝突**：`WordCards.Id` 全域 PK；Push／Upsert 若 Id 已被他帳占用 → `CONFLICT`（非裸 500）。
