# 排查筆記：西里爾三按鈕列 OCR（Девочка | ест | яблоко）

**日期：** 2026-08-26  
**狀態：** 開放排查（最新版實機仍失敗）  
**關聯票：** [`docs/tickets/07-app-ocr-select-one.md`](../tickets/07-app-ocr-select-one.md)  
**關聯 ADR：** [`docs/adr/0010-ocr-script-family-ondemand-tessdata.md`](../adr/0010-ocr-script-family-ondemand-tessdata.md)

---

## 1. 問題現象（實機）

| 項目 | 內容 |
|------|------|
| 期望圖文 | 三顆按鈕／chip：`Девочка` · `ест` · `яблоко`（「女孩吃蘋果」） |
| 最新版實際 OCR | `Девочка токо яблоко`（中間短詞誤識為 `токо`） |
| 畫面 | ImagePick → OCR → **選擇單字**（`OcrSelectPage`）：候選 radio 為三詞；來源語推成俄語 |
| UI 標示 | 「已選圖片（僅本機 OCR）」— 無雲端 Vision |
| 歷程（08-26） | 漏中間詞 → 誤識 `токо` → 曾誤識 `ОКО`；code review 後**禁止**用單詞表把 `токо`/`ОКО` 發明成 `ест` |

**設計約束（現行）：** 只有觀測到 `ect`／`ест`（或拉丁同形 remap 結果）才可把 Long–Short–Long 中間詞校正為 `ест`；單獨 `токо`／`ОКО` **不得發明** `ест`。因此若引擎整輪都沒產出 `ест`/`ect`，UI 會停在 `токо`——這與「看起來校正失敗」一致，可能是**策略刻意不發明**＋**引擎未辨出正確短詞**的疊加。

---

## 2. 端到端流程（依呼叫順序）

```
HomePage
  └─ 進「圖片取字」
ImagePickPage
  ├─ OnCameraClicked / OnGalleryClicked → IImageCaptureService
  │     └─ OcrSessionStore.LocalImagePath = 本機路徑（Clear 視 NewImagePick）
  ├─ ScriptFamilyPicker → OcrScriptFamilyHint（Auto｜Latin｜Cyrillic｜…）
  └─ OnOcrClicked
        └─ IDeviceOcrService.RecognizeAsync(path, hint, progress)
              = HybridDeviceOcrService.RecognizeAsync
                    ├─ OcrEngineRouter.Plan(hint)
                    ├─ [可選] IOnDeviceMlKitOcr.RecognizeBestAsync  → AndroidMlKitMultiScriptOcr
                    │     ※ 若結果含西里爾 → demote，強制改 Cyrillic plan（SkipMlKit=true, rus）
                    ├─ ITessdataPackStore.EnsurePacksAsync（rus 等）
                    ├─ IOcrImagePreprocessor.PrepareForTesseractAsync
                    ├─ RecognizeWithTesseractAsync（primary / broad）
                    └─ FinalizeTesseractResultAsync
                          ├─ 多 PSM（SparseText / SingleLine / RawLine / SingleWord）
                          ├─ CreateVerticalStripsAsync(3) + RecognizeStripBestAsync
                          ├─ PreferRicherCyrillic / UnionMissingLookalikeTokens / ReconcileButtonRowMiddle
                          └─ RebuildOkResult → StripNoiseTokens + Tokenize
        └─ session.RecognizedText = FullText
        └─ Routes.GoAsync(OcrSelect)
OcrSelectPage
  ├─ OcrEditor ← RecognizedText（可編輯）
  ├─ RebuildTokenRadios → OcrTextTokenizer.Tokenize
  ├─ OcrSourceLanguageInference.Infer → 來源語（ru）
  └─ OnAnalyzeClicked → OcrAnalyzeSelection.TryResolve → AnalyzeEntryFlow
```

**導航常數：** `src/Soraeru.App/Routes.cs` — `ImagePick` / `OcrSelect`；同圖續選：`GoToContinueOcrSelectAsync`。

---

## 3. DI 接線（MauiProgram）

檔案：`src/Soraeru.App/MauiProgram.cs`

| 介面 | 實作 | 備註 |
|------|------|------|
| `IOcrSessionStore` | `OcrSessionStore` | 圖路徑＋辨識文字；不上傳圖 |
| `IOnDeviceMlKitOcr` | `AndroidMlKitMultiScriptOcr` / `UnsupportedOnDeviceMlKitOcr` | 僅 Android |
| `IDeviceOcrService` | `HybridDeviceOcrService` | 混合 OCR 主入口 |
| `IOcrImagePreprocessor` | `AndroidOcrImagePreprocessor` / `Passthrough…` | 反相／放大／三條直切 |
| `ITessdataPackStore` | `TessdataPackStore` | 按需下載；ru 多半已打包 |
| Tesseract | `TesseractOcrMaui` + `TessdataCatalog` | tessdata_fast |

---

## 4. 程式碼／函式清單（排查用）

### 4.1 UI 頁面

| 檔案 | 重點符號 | 職責 |
|------|----------|------|
| `src/Soraeru.App/Pages/ImagePickPage.xaml` | ScriptFamilyPicker、StartOcrButton | 語系別 hint、預覽、開始辨識 |
| `src/Soraeru.App/Pages/ImagePickPage.xaml.cs` | `OnOcrClicked`, `ResolveScriptFamilyHint`, `MergeRecognizedText` | 呼叫 OCR、寫 session、導向選字 |
| `src/Soraeru.App/Pages/OcrSelectPage.xaml` | OcrEditor、TokenList、來源語 Search | 「選擇單字」畫面 |
| `src/Soraeru.App/Pages/OcrSelectPage.xaml.cs` | `RebuildTokenRadios`, `OnAnalyzeClicked`, `MaybeApplyInferredSourceLanguage`, `OnAssistClicked` | 分詞 radio、分析、LLM 輔助 stub |
| `src/Soraeru.App/Services/Interfaces/IOcrSessionStore.cs` | `OcrSessionStore` | `LocalImagePath` / `RecognizedText` / `StatusMessage` / `Clear` |
| `src/Soraeru.App/Routes.cs` | `ImagePick`, `OcrSelect`, `GoToContinueOcrSelectAsync` | Shell 路由 |

### 4.2 混合 OCR 引擎（失敗最可能落點）

| 檔案 | 重點符號 | 職責 |
|------|----------|------|
| `src/Soraeru.App/Services/Local/HybridDeviceOcrService.cs` | `RecognizeAsync` | 總控：ML Kit → demote → Tess → finalize |
| 同上 | `FinalizeTesseractResultAsync` | 西里爾路徑：多 PSM + 直切合併 + Homoglyph |
| 同上 | `RecognizeVerticalStripTextsAsync` | 三條垂直切塊 OCR |
| 同上 | `RecognizeStripBestAsync` | 單條多 PSM；短 chip 用 `PreferBestShortToken` |
| 同上 | `LooksLikeShortChipOnly` | 判斷是否全為短詞候選 |
| 同上 | `ApplyCyrillicHomoglyphPass` | ML Kit 最終 fallback 時仍跑 union |
| 同上 | `RecognizeWithTesseractAsync` | 設 `PageSegmentationMode`、swap tessdata subset |
| 同上 | `RebuildOkResult` | `StripNoiseTokens` + element token 列表 |
| 同上 | `CyrillicAltPsmModes` | SparseText / SingleLine / RawLine / SingleWord |
| `src/Soraeru.App/Services/Interfaces/IDeviceOcrService.cs` | `DeviceOcrResult`, `RecognizeAsync` 多載 | 契約 |
| `src/Soraeru.App/Services/Interfaces/IOnDeviceMlKitOcr.cs` | `RecognizeBestAsync` | ML Kit 抽象 |
| `src/Soraeru.App/Platforms/Android/AndroidMlKitMultiScriptOcr.cs` | `RecognizeBestAsync`, `CreateRecognizers`, `MapResult` | Latin/CJK/Devanagari 等；**無專用 Cyrillic 模組** |
| `src/Soraeru.App/Services/Local/UnsupportedOnDeviceMlKitOcr.cs` | — | 非 Android stub |

**西里爾特殊行為（Hybrid 內註解）：**  
ML Kit 常回不完整西里爾（漏中間 `ест`）→ **永不 early-accept**；改 demote + `OcrScriptFamilyHint.Cyrillic`（SkipMlKit）+ rus Tesseract + strip。

### 4.3 影像預處理／切塊

| 檔案 | 重點符號 | 職責 |
|------|----------|------|
| `src/Soraeru.App/Services/Interfaces/IOcrImagePreprocessor.cs` | `PrepareForTesseractAsync`, `CreateVerticalStripsAsync` | 契約 |
| `src/Soraeru.App/Platforms/Android/AndroidOcrImagePreprocessor.cs` | `PrepareCore`, `CreateVerticalStripsCore` | 放大／反相／對比；**三直條**（中條 inset、邊條 overlap） |
| `src/Soraeru.App/Services/Local/PassthroughOcrImagePreprocessor.cs` | — | 非 Android |
| `src/Soraeru.ClientLogic/Ocr/OcrImageEnhanceHints.cs` | `ShouldInvertForOcr`, `ShouldBoostContrastForOcr`, `ShouldUpscale`, `EmptyResultGuidance` | 純決策 |

### 4.4 路由／品質／分詞（ClientLogic，可單測）

| 檔案 | 重點符號 | 職責 |
|------|----------|------|
| `src/Soraeru.ClientLogic/Ocr/OcrEngineRouter.cs` | `Plan`, `ShouldAcceptMlKitResult`, `DetectDominantScriptFamily`, `ResolveEffectiveHint`, `RequiredTessPacks`, `CyrillicPrimaryLanguages="rus"` | 引擎路由 |
| `src/Soraeru.ClientLogic/Ocr/OcrScriptFamilyHint.cs` | enum | Auto／Latin／Cyrillic／… |
| `src/Soraeru.ClientLogic/Ocr/OcrScriptQuality.cs` | `ContainsCyrillic`, `LooksLikeCyrillicScriptHallucination`, `IsSuspiciousLatinOcr`, `IsCyrillicScript`, `IsLatinLetter` | 腳本品質 |
| `src/Soraeru.ClientLogic/Ocr/OcrCyrillicHomoglyphNormalizer.cs` | 見下節 | **中間短詞校正核心** |
| `src/Soraeru.ClientLogic/Ocr/OcrTextTokenizer.cs` | `Tokenize`, `StripNoiseTokens`, `IsLikelyVocabularyToken` | 候選 radio 分詞 |
| `src/Soraeru.ClientLogic/Ocr/OcrAnalyzeSelection.cs` | `TryResolve` | 單選進分析 |
| `src/Soraeru.ClientLogic/Ocr/OcrSourceLanguageInference.cs` | `Infer` | Cyrillic → `ru` 等 |
| `src/Soraeru.ClientLogic/Ocr/OcrTextAssistGate.cs` | `ShouldSuggestAssist`, `BuildEditableSuggestionStub` | 可疑品質 banner；**API 未接** |
| `src/Soraeru.ClientLogic/Ocr/OcrSessionRetention.cs` | `ShouldClearOn`, `ResolvePostLoginDestination` | session 何時 Clear／登入恢復 |
| `src/Soraeru.App/Services/Local/TessdataCatalog.cs` | 打包語言清單 | 含 `rus` |
| `src/Soraeru.App/Services/Local/TessdataPackStore.cs` | `EnsurePacksAsync` | 按需包 |

### 4.5 `OcrCyrillicHomoglyphNormalizer` 公開 API（必讀）

檔案：`src/Soraeru.ClientLogic/Ocr/OcrCyrillicHomoglyphNormalizer.cs`

| 函式 | 行為摘要 |
|------|----------|
| `NormalizeMixedScript` | 純拉丁同形 token remap（`ect`→`ест`, `ili`→`или`） |
| `TryRemapPureLookalikeToken` | 逐字 Latin→Cyrillic lookalike；擋英文 stopword |
| `UnionMissingLookalikeTokens` | 多路 secondary 合併後 `ReconcileButtonRowMiddle` |
| `MergeMissingLookalikeTokens` | 把 secondary 的短詞插入 primary（錨點對齊） |
| `ReconcileButtonRowMiddle` | Long–Short–Long（三詞、兩邊 ≥4 字母、中間 ≤5）挑最佳中間詞 |
| `PreferBestShortToken` | 多候選短詞評分選優（strip 用） |
| `PreferRicherCyrillic` | 兩條全文比 line quality／西里爾密度 |
| `ScoreMiddleShortCandidate` | `ест`/`или` 高分；garbage（全大寫 2–4、混腳本）扣分 |
| `IsHighConfusionMiddleGarbage` | 全大寫短詞／混腳本＝垃圾；**preferred 永不 garbage** |
| `IsPreferredMiddleShort` | 僅 `ест`／`или`（及 remap 後同等） |
| `TryNormalizeSecondaryShortToken` | 短詞是否可當 secondary |
| 常數 | `MaxLookalikeTokenLength=5`, `MinSideTokenLengthForButtonRow=4` |

**刻意不做：** `токо`→`ест`、`ОКО`→`ест` 的單詞映射表（08-26 review 刪除）。

**單元測試已覆蓋、但「僅 primary=`… токо …` 且無更好 secondary」時：**  
`ReconcileButtonRowMiddle_does_not_invent_est_without_better_candidate` → **保留 `токо`**。這與實機現況對齊。

### 4.6 測試檔

| 檔案 | 對應 |
|------|------|
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrCyrillicHomoglyphNormalizerTests.cs` | `токо`/`ect`/`ест`/`ОКО`/`OKO` 全套 |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrEngineRouterTests.cs` | 路由／Accept ML Kit |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrScriptQualityTests.cs` | 腳本品質 |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrTextTokenizerTests.cs` | 分詞 |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrAnalyzeSelectionTests.cs` | 選字 |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrImageEnhanceHintsTests.cs` | 預處理決策 |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrTextAssistGateTests.cs` | 輔助門檻 |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrSessionRetentionTests.cs` | session |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrSourceLanguageInferenceTests.cs` | 來源語 |
| `tests/Soraeru.ClientLogic.Tests/Ocr/OcrTessPackRequirementsTests.cs` | tess 包 |

**缺口：** 無針對 `HybridDeviceOcrService` / Android 預處理／ML Kit 的裝置整合測；實機品質無法用現有 ClientLogic 測試保證。

---

## 5. 此例失敗路徑（假設樹）

依現行程式，實機得到 `Девочка токо яблоко` 時，合理推論：

1. **Tesseract（或合併後）主結果中間詞就是 `токо`**（或等價垃圾），且  
2. **所有 secondary**（多 PSM、三直切、demoted ML Kit）**都沒有**產出可勝出的 `ест`／`ect`（或 remap 後的 preferred），因此  
3. `ReconcileButtonRowMiddle` / `PreferBestShortToken` **依策略留下 `токо`**，再  
4. `OcrTextTokenizer.Tokenize` 原樣顯示三個 radio。

排查時應先驗證「引擎原始多路輸出」是否曾出現 `ест`/`ect`，再談校正邏輯。

### 建議儀器化／除錯點（另案實作）

在不改產品策略前，可暫時 log（僅 debug／內部建置）：

| 位置 | 記錄什麼 |
|------|----------|
| `HybridDeviceOcrService.RecognizeAsync` | `scriptHint`、是否 demote ML Kit、`mlKit.FullText` |
| `FinalizeTesseractResultAsync` | primary Tess 全文、每個 PSM `extras[]`、每個 strip 文字 |
| `RecognizeStripBestAsync` | `shortCandidates`、`PreferBestShortToken` 結果 |
| `UnionMissingLookalikeTokens` 前後 | primary vs final |
| `ImagePickPage.OnOcrClicked` | 最終寫入 session 的字串 |

對照同一張實機圖：手動選「西里爾」hint vs「自動」。

---

## 6. 策略與產品邊界（排查時勿踩雷）

| 決策 | 來源 |
|------|------|
| 原圖不上雲；雲端 OCR 不在範圍 | 票 07、ADR 0010 |
| 西里爾 SkipMlKit → `rus` tessdata_fast | `OcrEngineRouter.Plan(Cyrillic)` |
| 刪單詞映射；不發明 `ест` | 票 07 Notes 08-26、Homoglyph 註解 |
| P2 文字 LLM 輔助：需確認、不靜默覆寫；API **未接**（stub） | `OcrSelectPage.OnAssistClicked` |
| 使用者仍可手動改 OcrEditor 後再分析 | `OcrSelectPage` 可編輯 |

若另案要「解決此例」，需先選策略分支（產品問題，非單純 bug）：

- **A.** 放寬校正：允許在特定圖案下把高混淆短詞換成 `ест`（回退單詞映射／更高風險誤傷）  
- **B.** 加強引擎：更好預處理／切塊／PSM／模型，讓 secondary **真實產出** `ест`/`ect`  
- **C.** 產品繞過：可疑三按鈕列強制提示手動改／接 P2 suggest-fix  
- **D.** 接受裝置端極限：此類螢幕翻拍／稀疏短詞不保證（票 07 已記殘餘）

---

## 7. 相關文件索引

- 票：`docs/tickets/07-app-ocr-select-one.md`（含 08-26 西里爾三按鈕歷程）  
- 票總表：`docs/tickets/README.md`（07 列「三按鈕例仍可能失敗」）  
- ADR：`docs/adr/0010-ocr-script-family-ondemand-tessdata.md`  
- 工時：`docs/time-log/2026-W35.md`（08-26 OCR 多輪）  
- MVP 選字流程示意：`docs/AI 空耳外語學習 APP－MVP 系統規劃書/ChatGPT-MVP系統規劃書.md`（選擇單字／候選單字）

---

## 8. 本檔用途

供**另開聊天／另案排查**時一次載入：流程、檔案、函式、策略約束、失敗假設樹與除錯插入點。  
**本檔不修改產品程式；不宣稱問題已修復。**
