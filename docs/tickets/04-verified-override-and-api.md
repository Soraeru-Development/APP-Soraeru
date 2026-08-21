# 04 — 已驗證空耳管理 API＋分析金標優先覆寫

**What to build:** 策展授權身分可對已驗證空耳做最小 CRUD；學習者分析命中「啟用中」條目時，空耳欄位取自金標且不呼叫 LLM 產空耳，結果標為已驗證；詞義／正式讀音仍可取得；App 可區分已核定與草稿。

**Blocked by:** 02 — 後處理硬閘＋LLM 草稿標示（含 App）

**Status:** done

## Parent

[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md) · ADR-0001、0005、0006

## What to build

在**共用 API**上交付已驗證空耳的持久化與查詢鍵（語言＋正規化原詞），以及僅策展者（Google email 允許清單，可與既有 developer 政策對齊）可呼叫的管理端點：新增／編輯／啟用／下架，並可列表或搜尋以便管理與測試。分析管線：額度與登入檢查後先查啟用中已驗證條目；**命中**則空耳相關欄位（displayText、notationText、explanation 等）來自條目並**跳過 LLM 產空耳**，回應標記 `verified`；詞義與正式讀音仍可走既有 LLM／其他來源。未命中仍走 02 的草稿＋硬閘路徑。一般學習者 JWT 呼叫管理寫入必須 403。本票以 API（＋App 標示）可獨立驗收；不要求策展 Blazor UI（見 05）。社群投稿不做。

## Acceptance criteria

- [x] 策展授權可新增已驗證空耳（語言、原詞、displayText、notationText、explanation，預設或可設啟用狀態）。
- [x] 策展授權可編輯條目並啟用／下架；下架後不可被分析命中。
- [x] 策展授權可列表／搜尋既有條目。
- [x] 一般學習者呼叫管理寫入／完整管理列表 → 403。
- [x] 同鍵啟用中條目存在時：分析回傳該空耳欄位、標記已驗證、**不**呼叫產空耳 LLM；詞義與／或正式讀音仍可取得。
- [x] App 結果面可顯著區分「聽感已核定／策展」與 LLM 草稿（語義不混用）。
- [x] 已驗證路徑預設信任策展內容；不得因防衛掃描失敗而默默改回 LLM 空耳。
- [x] 自動化測試：命中不產空耳 LLM＋verified 旗標；CRUD 後學習者分析同鍵可命中；非允許清單 403。

## Blocked by

- 02 — 後處理硬閘＋LLM 草稿標示（含 App）

## Delivered (impl notes)

- 表 `VerifiedMnemonics`（非 WordCards）；唯一鍵 `(Language, NormalizedSource)`。
- API：`/api/v1/curator/verified-mnemonics`（CRUD + enable）；允許清單＝`DeveloperAccounts`／`IDeveloperAccountPolicy`；FORBIDDEN→403。
- 分析：命中 → `SkipMnemonics` 詞義／讀音 LLM + 金標空耳 + `mnemonicSource=verified`。
- App：verified Info banner vs llm_draft Warning banner。

## Notes（手動驗證／金標來源）

- **金標 ≠ 使用者自建**：僅策展授權身分透過 `/api/v1/curator/verified-mnemonics` 寫入 `VerifiedMnemonics`；一般學習者 JWT 寫入 → 403。App 端**不能**自己建金標。
- **MVP 現況**：策展 Blazor（票 05）仍 deferred；過渡以 API／腳本建立。若庫中尚無啟用中條目，分析不會出現「聽感已核定」——屬預期，非 App bug。
- 學習者可改的是單字卡**個人空耳**（票 16），與金標庫分離（票 17：金標不覆蓋已存卡）。
