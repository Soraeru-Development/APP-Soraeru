# 07 — App：裝置端 OCR 選一字進分析

**What to build:** 學習者在 Android App 用相機拍照或相簿選圖，於**裝置端** OCR，校正並每次選一個字／短語後進入既有分析流程；原圖不上雲。

**Blocked by:** None — can start immediately（分析／登入閉環已在 01–04）

**Status:** done

## Parent

[`docs/AI 空耳外語學習 APP－MVP 系統規劃書/Cursor-MVP App 規劃書.md`](../AI%20空耳外語學習%20APP－MVP%20系統規劃書/Cursor-MVP%20App%20規劃書.md)（F02–F05、W4）  
策略補充：[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md) Further Notes（App-first）

## What to build

補齊現行骨架頁（圖片來源 → OCR 預覽／選字 → 分析）的端到端行為：相機、相簿取圖；裝置端辨識文字；使用者校正並**一次選一個**目標字串，再走既有手動輸入後的分析管線（含額度、硬閘、草稿／已核定標示）。腳本不支援或 OCR 失敗時，明確引導改手動輸入，且不得把原圖上傳到後端。雲端 OCR 不在範圍。

## Acceptance criteria

- [x] 可從相機或相簿取得圖片並在裝置上 OCR。
- [x] OCR 結果可校正；每次只選一個字／短語進入分析。
- [x] 選字後可完成與手動輸入同等的分析→結果流程。
- [x] 原圖不上傳；失敗或不支援時可改手動輸入且有清楚提示。
- [x] 與登入／額度語意一致（有額度才進分析等既有規則）。

## Blocked by

- None — can start immediately

## Done notes（2026-08）

- App L07／L08：`MediaPicker` 相機／相簿＋裝置端 OCR；原圖只進本機 OCR，分析 request 僅文字。
- 分詞／單選解析：`Soraeru.ClientLogic`（`OcrTextTokenizer`、`OcrAnalyzeSelection`）＋單元測試。
- 失敗／弱腳本：alert 引導改手動輸入（WordInput）。
- 未驗證：實機 Android OCR 品質（CLI Android 建置刻意避開 VS lock）。

## Notes（混合 OCR・2026-08-13／08-21）

- **引擎**：`HybridDeviceOcrService`＝**ML Kit on-device 多腳本**（Latin／中／日／韓／天城文）優先，失敗或空結果再走 **Tesseract + tessdata_fast**（圖不上雲）。
- **ML Kit Manifest prefetch**：`ocr,ocr_chinese,ocr_japanese,ocr_korean,ocr_devanagari`（`AndroidManifest.xml`）。
- **Tesseract 語言包**（約 37 MB）：`Resources/Raw/tessdata/*.traineddata`  
  `eng, jpn, kor, tha, mya, lao, khm, ara, bod, hin, nep, chi_tra, chi_sim, fil, vie, rus`  
  （MauiAsset LogicalName 扁平化為 `*.traineddata` 供 `TesseractOcrMaui` 載入）
- **路由**：泰／緬／寮／柬／阿／藏／俄／尼泊爾等無 ML Kit 模組的腳本 → Tesseract primary；必要時再 broad fallback（含 CJK／拉丁包）。
- **2026-08-21 體驗收斂**：
  - **Session 清空**：選圖後再進「圖片取字」不再殘留上一張舊圖／舊 OCR 結果。
  - **來源語自動預選**：OCR 後依 Unicode 腳本推斷來源語言（韓／日／泰／越等）並預選，減少手動改語。
- **殘餘風險**：實機品質／首次模型下載時間；多腳本序掃可能較慢；超大 APK（語言包）。
- **中期亞洲語言包**：08-13 僅討論；08-21 已落地 **tessdata_fast 混合包**（見上）。進一步體積／品質取捨仍可後續優化，不阻擋本票 done。
