# 10 — App：隱私／AI 聲明與設定收尾

**What to build:** 學習者可從登入／設定開啟真實可用的隱私權與 AI 內容聲明入口；設定頁完整呈現額度、方案欄位預留與登出；與首次說明再閱路徑一致可用。

**Blocked by:** None — can start immediately（設定／onboarding 骨架已有）

**Status:** done

## Parent

[`docs/AI 空耳外語學習 APP－MVP 系統規劃書/Cursor-MVP App 規劃書.md`](../AI%20空耳外語學習%20APP－MVP%20系統規劃書/Cursor-MVP%20App%20規劃書.md)（F16、F17、F20、W5）  
策略補充：[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md) Further Notes（App-first）

## What to build

把現行「示範 alert」升級為可驗收的知情同意入口：隱私權政策與「近似音僅供記憶／AI 可能有誤／多語品質不一」等 AI 內容聲明可從登入與設定開啟（URL 或應用內頁皆可，須可穩定開啟）。設定維持登出、查看今日額度、方案／未來升級欄位預留（不做 Play Billing UI）。首次使用說明可於設定再次查看。視覺全面 Stitch 對齊不在本票強制範圍（可極小修正阻擋閱讀的缺陷）。

## Acceptance criteria

- [x] 登入與設定可開啟隱私權政策內容（非「稍後再補」空殼告警）。
- [x] 有 AI 內容／空耳免責聲明入口，語意含「僅供記憶、請以正式發音為準」。
- [x] 設定可查看額度、方案預留欄位、登出；已登入會話行為正確。
- [x] 設定可再次開啟首次使用說明。
- [x] 不做實際金流／訂閱升級購買流程（Phase 2）。

## Blocked by

- None — can start immediately

## Notes（Station 4）

- Seam：`Soraeru.ClientLogic.Legal.LegalDocuments`（文案 oracle）＋ `LegalDocumentPage`（應用內頁，`?doc=privacy|ai`）。
- 登入／設定／註冊入口改導應用內頁；商店託管 URL 留給票 12。
- 驗證：`dotnet test …LegalDocumentsTests`（3 綠）；App Windows TFM 建置成功。額度／方案預留／登出／再看 onboarding 為既有行為。
