# 17 — 回歸：已驗證空耳不得覆蓋已存卡個人空耳

**What to build:** 確認並鎖住邊界——`VerifiedMnemonics`／金標優先**只影響分析結果候選**，**永不**強蓋使用者單字卡上已保存的個人空耳（含本機存檔與之後同步會合之卡）。

**Blocked by:** 13（done；需有「已存卡」本機路徑可回歸）；04 done（金標分析覆寫已存在）

**Status:** done

## Parent

[`docs/specs/client-first-wordcards-sync.md`](../specs/client-first-wordcards-sync.md)（User Story 5、Testing Decisions）· ADR-0007 · Glossary：金標優先覆寫、個人空耳

## What to build

以垂直回歸鎖住易混邊界：同一原詞＋語言在分析命中啟用中已驗證空耳時，結果頁候選可走金標；但使用者稍早（或他裝置會合後）已存進單字卡的個人空耳欄位不得被分析管線或任何「同步金標」路徑覆寫。覆蓋誤實作（若有）須移除並加防回帰測試。本票不擴張策展 CRUD、不改額度規則。

## Acceptance criteria

- [x] 已存單字卡含自訂個人空耳；其後對同詞分析命中金標時，該卡個人空耳仍為原值。
- [x] 分析結果頁仍正確呈現金標／已核定語意（04 行為不回退）。
- [x] 有自動化測試鎖定「金標不回寫／不覆蓋已存卡個人空耳」（優先 seams：分析覆寫不回寫卡；必要時含本機讀卡斷言）。
- [x] 文件／實作用語不暗示 Server 金標擁有或覆蓋使用者卡上助記。

## Blocked by

- 13 — App：本機單字卡儲存與列表／存／刪（Client-first）（done）
- 04 — 已驗證空耳管理 API＋分析金標優先覆寫（done）

## Delivered (impl notes)

- **誤實作移除**：`LocalNotebookService.SaveAsync` 同鍵再存曾覆寫 `SelectedMnemonic`（結果頁再存金標候選會強蓋個人空耳）。改為與 Server `NotebookService.SaveAsync` 一致——回傳既有卡、不寫入。個人空耳編修仍走票 16 `UpdateSelectedMnemonicAsync`。
- **分析管線**：`AnalyzeWordService` 不依賴 `IWordCardRepository`；金標只進結果候選（04 行為保留）。
- **測試**：
  - `LocalNotebookServiceTests.SaveAsync_same_normalized_key_returns_existing_without_overwriting_personal_mnemonic`
  - `AnalyzeWordServiceTests.AnalyzeAsync_verified_hit_only_affects_result_candidates_and_does_not_depend_on_word_cards`
  - 既有 verified 命中／信任策展測＋ `NotebookServiceTests` 同鍵不覆寫鏡像列
- **用語**：Analyze／VerifiedMnemonic／Notebook 註解標明金標不回寫個人空耳。
