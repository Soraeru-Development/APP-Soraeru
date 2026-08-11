# 06 — Web 學習端薄 MVP1（登入／輸入／結果／單字本）

**What to build:** 學習者在瀏覽器以 Google 與／或 Email 登入後，手動輸入外語、取得含信任標示的分析結果、選定空耳存入單字本並查看／刪除；與 App 共用帳戶與額度語意；無相機／OCR。單字本在 MVP1 **暫以雲端鏡像**為該端持久化（ADR-0007；非瀏覽器 Client-first）。

**Blocked by:** 02、03（done）；另見策略阻擋：App MVP 完成前不開工

**Status:** deferred

## Deferral（2026-08 策略）

**App-first：** 優先完成 Android App MVP。本票（Web 學習端薄 MVP1）延後至 App 功能閉環（約 07–10）完成後再恢復 frontier。Web 仍為一級學習通道（規格定位不變），僅**排程**後移。開工時應對齊票 **15** 鏡像語意（若已落地）與 ADR-0007：Web 讀寫的是雲端鏡像，不是「Server 擁有 App 本機卡」。

## Parent

[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md) · [`docs/specs/client-first-wordcards-sync.md`](../specs/client-first-wordcards-sync.md) · ADR-0002、0003、0006、**0007**

## What to build

新建 Web 學習端 Blazor 應用（與策展端分開；宿主型態本票鎖定即可）：薄 MVP1 頁面＝登入、手動輸入、分析結果（含 `llm_draft`／若管線已有則 `verified` 區分標示）、單字本列表／詳情（含儲存選定候選）。App 為 canonical（本機 SoT）；Web MVP1 **過渡**經共用 API 讀寫**雲端鏡像**，與 App 經同步會合，而非第二套玩具產品或另一本帳。不做相機、相簿、裝置 OCR、完整商場化設定、瀏覽器 IndexedDB Client-first。隱私／AI 內容聲明入口可極簡但不可缺。額度與分析打同一共用 API。04／05 非硬性 blocker：未上架金標時仍應完整跑通草稿＋硬閘路徑；若環境已有已驗證條目，結果頁須正確顯示已核定狀態。

## Acceptance criteria

- [ ] 學習者可用 Google 與／或既有 Email 流程在 Web 登入，並使用同一帳號資料／額度語意。
- [ ] 手動輸入 → 分析結果頁顯示詞義、正式讀音與空耳候選；信任標示語義正確（草稿 vs 已核定若可得）。
- [ ] 可選一個空耳候選存成單字卡（寫入雲端鏡像）；單字本可查看與刪除。
- [ ] 無相機／OCR 路徑；核心記憶流程仍可完成。
- [ ] 有極簡隱私／AI 內容相關聲明入口。
- [ ] 不另起後端；CORS／Auth 與共用 API 可同時服務 Web；單字本語意為鏡像過渡（ADR-0007），非與 App 爭 SoT。
- [ ] 驗收以 API 合約＋Web 手動煙測為主即可（UI 自動化可後補）。

## Blocked by

- 02 — 後處理硬閘＋LLM 草稿標示（含 App）（done）
- 03 — 單字本端到端可存可查（done）
- App MVP 功能閉環（07–10）完成後才恢復本票為 frontier
