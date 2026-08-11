# 02 — 後處理硬閘＋LLM 草稿標示（含 App）

**What to build:** 學習者分析未命中已驗證空耳時，LLM 空耳候選必須通過後處理硬閘才算成功，並以「AI 草稿／未經聽感核定」顯示；違規（兒化／拉丁殘渣等）拒收並有限次重試，耗盡則清楚錯誤、不得當成功結果。

**Blocked by:** 01 — Application 測試骨架（prefactor）

**Status:** done

## Parent

[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md) · ADR-0004

## What to build

在共用分析管線上，於 schema 驗證之後、對學習端回傳之前，對**將顯示的每一則** LLM 空耳候選套用後處理硬閘（至少：不當兒化／「兒」「爾」禁則、孤立拉丁或非許可腳本殘渣）。不合格則拒收並依政策自動重試（有上限）；耗盡則回傳明確錯誤碼／訊息。成功的 LLM 路徑在 API 與 App 結果面標示為 `llm_draft`（文案可調，語義＝未經聽感核定）。開放多語不變。本票不實作已驗證覆寫；持續 Prompt 迭代仍是信任槓桿，但 **v1.3 已套用，除非硬閘案例證明需再改 analyze Prompt，否則不開新 Prompt 任務**。

## Acceptance criteria

- [x] 合規 LLM 空耳可成功回傳，且客戶端能辨識為 LLM 草稿（非已驗證）。
- [x] 含兒化／拉丁殘渣等違規、本應顯示的候選：不得以成功分析回傳；觸發有限次自動重試。
- [x] 重試耗盡：學習者看到清楚失敗（可稍後再試），而非空轉或半套壞候選。
- [x] App 分析結果 UI 顯著標示「AI 草稿／未經聽感核定」（或同等語義）。
- [x] 自動化測試覆蓋：硬閘純規則（違規拒／合規過）＋未命中路徑硬閘失敗耗盡（優先 seams 見 parent Testing Decisions）。
- [x] 語種不以允許清單封鎖分析。

## Blocked by

- 01 — Application 測試骨架（prefactor）

## Done notes

- `MnemonicHardGate` 純規則＋`AnalyzeWordService` 接入（schema 後、耗額度前）；錯誤碼 `HARD_GATE_FAILED`。
- API／DTO 回傳 `mnemonicSource: llm_draft`；App 結果頁顯示草稿橫幅。
- 驗證：`dotnet test tests/Soraeru.Application.Tests --filter FullyQualifiedName~Analyze`
