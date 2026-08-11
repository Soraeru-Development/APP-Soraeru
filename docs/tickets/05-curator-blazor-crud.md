# 05 — 策展端 Blazor：允許清單登入＋最小 CRUD

**What to build:** 策展者用允許清單內 Google 帳號登入**獨立**策展站，以 UI 維護已驗證空耳（新增／編輯／啟用下架／列表搜尋）；非清單帳號進不了維護面。

**Blocked by:** 04 — 已驗證空耳管理 API＋分析金標優先覆寫（done）；另見策略阻擋：App MVP 完成前不開工

**Status:** deferred

## Deferral（2026-08 策略）

**App-first：** 優先完成 Android App MVP。本票（策展 Blazor UI）延後至 App 功能閉環（約 07–10）完成後再恢復 frontier。  
過渡：策展者可暫用 04 的管理 API（工具／腳本）維護已驗證空耳；App 分析仍可吃到金標覆寫與標示。

## Parent

[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md) · ADR-0003、0005、0006

## What to build

新建並分開部署的策展端 Blazor 應用（宿主 Server／WASM／Unified 本票實作前選定即可，不影響「獨立站＋單一 API」決策）。流程：Google 登入 → email ∈ 允許清單才進入維護 UI；對 04 的管理 API 做最小 CRUD 畫面（語言、原詞、displayText、notationText、explanation、啟用／下架、列表／搜尋）。非允許清單不得使用策展 UI。不開社群投稿／審核佇列。驗證上架後，學習者側分析同鍵命中行為已由 04 保證；本票以策展者操作路徑可 demo 為驗收。

## Acceptance criteria

- [ ] 允許清單內 Google 帳號可登入策展站並看到維護介面。
- [ ] 非允許清單帳號無法進入維護面（即使 Google 登入成功）。
- [ ] 策展者可於 UI 新增、編輯、啟用／下架已驗證空耳，並列表／搜尋。
- [ ] UI 寫入走共用 API；不另起第二業務後端。
- [ ] 獨立於 Web 學習端部署／專案邊界清楚（非同站隱藏管理路由）。
- [ ] 煙測或手動腳本可示範：UI 上架一條 → 學習者分析同鍵拿到已驗證空耳（依賴 04 行為）。

## Blocked by

- 04 — 已驗證空耳管理 API＋分析金標優先覆寫（done）
- App MVP 功能閉環（07–10）完成後才恢復本票為 frontier
