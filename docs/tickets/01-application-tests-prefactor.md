# 01 — Application 測試骨架（prefactor）

**What to build:** 專案能對分析應用服務與純規則寫可執行的自動化測試（紅／綠循環就緒），而不改變學習者可見產品行為。

**Blocked by:** None — can start immediately

**Status:** done

## Parent

[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md)（Testing Decisions）

## What to build

在動硬閘、覆寫與策展之前，先讓 Application 層有標準測試宿主與最少 seam（可替換的 LLM／倉庫埠），證明「給定輸入與雙緒狀態 → 可觀察結果」的測法可跑通。不交付新用戶功能；`word-analysis.v1.3` 已套用，本票**不**要求再改 Prompt，除非為了把既有分析路徑接進可測縫。

## Acceptance criteria

- [x] 存在可在 CI／本機一鍵跑的 .NET 測試專案，至少覆蓋 Application 行為用例骨架。
- [x] 分析用例可在不打真實外部 LLM 的情況下被雙緒（fake agent／fake 倉庫）。
- [x] 既有分析成功路徑至少有一條「可綠」的契約樣例（作為後續硬閘／覆寫加案例的錨點）。
- [x] 不引入新的學習者／策展者產品能力；既有 App／API 行為無刻意回歸。

## Blocked by

- None — can start immediately

## Done notes

- `tests/Soraeru.Application.Tests`：`Analyze/AnalyzeWordServiceTests` 以 NSubstitute 假 LLM／額度／使用者；Notebook 用例亦在同專案。
- 驗證：`dotnet test tests/Soraeru.Application.Tests`
