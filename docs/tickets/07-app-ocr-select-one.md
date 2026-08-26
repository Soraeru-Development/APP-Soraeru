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
  `eng, jpn, kor, tha, mya, lao, khm, ara, bod, hin, nep, chi_tra, chi_sim, fil, vie, rus, spa`  
  （MauiAsset LogicalName 扁平化為 `*.traineddata` 供 `TesseractOcrMaui` 載入）
- **路由**：泰／緬／寮／柬／阿／藏／俄／尼泊爾等無 ML Kit 模組的腳本 → Tesseract primary；必要時再 broad fallback（含 CJK／拉丁包）。
- **2026-08-21 體驗收斂**：
  - **Session 清空**：選圖後再進「圖片取字」不再殘留上一張舊圖／舊 OCR 結果。
  - **來源語自動預選**：OCR 後依 Unicode 腳本推斷來源語言（韓／日／泰／越等）並預選，減少手動改語。
- **2026-08-24 同圖重選 UX（Q1=C／Q2=A）**：
  - **保留 session**：分析結果頁、本機短路詳情、儲存後詳情 **不再** `Clear` OCR session；僅 **Home 根**、**手動輸入**、**新選圖** 清空。
  - **返回同列表**：結果頁／詳情頁 **返回** 與 CTA「**繼續選同圖其他字**」→ `GoToContinueOcrSelectAsync`（`//main/HomePage/ImagePickPage/OcrSelectPage`）。
  - **Analyzing 導航**：先 `..` pop Analyzing 再 push Result，避免 `../AnalysisResult` 把 OcrSelect 彈掉。
  - **登入恢復**：OCR 流程中登入後若 session 仍有效 → 回 OcrSelect（`SuppressHomeRootResetOnce`）。
  - **可測 helper**：`Soraeru.ClientLogic.Ocr.OcrSessionRetention`＋14 項單元測試。
  - **未驗證**：實機 Android 同圖連選多字、儲存後返回、本機短路後 CTA。
- **殘餘風險**：實機品質／首次模型下載時間；多腳本序掃可能較慢；超大 APK（語言包）。
- **中期亞洲語言包**：08-13 僅討論；08-21 已落地 **tessdata_fast 混合包**（見上）。進一步體積／品質取捨仍可後續優化，不阻擋本票 done。

## Notes（OCR 路由／ru·es／腳本族・2026-08-25）

- **P0 裝置路由**：`OcrEngineRouter`／`OcrScriptQuality`（ClientLogic）— Latin ML Kit「腳本幻覺」（如 `weHUAMHa.`）**不得短路**，改走 Tesseract（含已打包 `rus`）。`HybridDeviceOcrService` 依 hint 接線。
- **P0′ 來源語**：catalog／OCR・WordInput picker 加 **`ru`／`es`**；推斷 Cyrillic→`ru`；西語僅 `ñ¿¡`（不依共享重音過擬合）。
- **P1 軟腳本族**：ImagePick `自動｜拉丁｜西里爾｜CJK`（預設自動）綁引擎路由；西里爾跳過 ML Kit → `rus`。
- **P2 文字 LLM 輔助**：可疑品質時 OcrSelect 提示；**需確認、不靜默覆寫、不上傳原圖**。獨立 suggest-fix API **尚未接**（stub 說明）；避免雙倍額度。
- **spa tessdata**：已加入 `spa.traineddata`（tessdata_fast）＋ catalog／broad fallback。
- **未驗證**：實機「Я женщина.」／西語圖品質。
- **APK**：2026-08-25 已重建 Release Signed：`src/Soraeru.App/bin/Release/net10.0-android/com.soraeru.app-Signed.apk`。

## Notes（腳本族擴充／可搜尋來源語／按需 tessdata・2026-08-25 續）

- **腳本族**：Auto｜拉丁｜西里爾｜CJK｜阿拉伯｜天城文｜東南亞｜其他；`OcrEngineRouter`＋`DetectDominantScriptFamily`／`ResolveEffectiveHint`；Auto 拒拉丁幻覺後偏好 rus。
- **來源語**：ClientLogic curated ≥30 ISO；OCR／WordInput 改 SearchBar＋CollectionView（收藏夾＋搜尋）；推斷加 ar／hi／my／km／lo。
- **按需包**：`ITessdataPackStore`／`TessdataPackStore`（tessdata_fast raw）；拉丁族 Ensure `deu`＋`fra` 下載證明；阿拉伯族 Ensure `ara`（多半已打包）。進度寫入 ImagePick StatusLabel。
- **ADR**：[`docs/adr/0010-ocr-script-family-ondemand-tessdata.md`](../adr/0010-ocr-script-family-ondemand-tessdata.md) — 腳本族＋按需 ≠ APK 全語言。
- **P2 banner**：OcrSelect 文案更明確（需確認、不上傳圖、不靜默覆寫）；suggest-fix API 仍 stub。
- **未驗證**：實機下載 deu／fra、阿拉伯／SEA 族品質。
- **APK（續）**：2026-08-25 再重建 Release Signed：`src/Soraeru.App/bin/Release/net10.0-android/com.soraeru.app-Signed.apk`。

## Notes（語系別文案／阿語翻拍／西里爾短詞・2026-08-25 晚＋08-26）

- **語系別 UI（08-25 晚）**：ImagePick「腳本族」改「**語系別（選填）**」；選項改文字長相＋代表語言（拉丁字母英／法／德／西…、西里爾俄／烏…、漢字體系中／日／韓、阿拉伯字母阿／波…、天城文印地／尼泊爾…、東南亞泰／緬／寮／柬）。底層 `OcrScriptFamilyHint` 路由不變。
- **阿語低品質螢幕翻拍（08-25 晚）**：本機預處理（暗底反相／放大／對比）；空結果引導「螢幕翻拍、發光或點陣字較難辨識；請改拍或手動輸入」。此類圖仍不保證成功。
- **西里爾三按鈕短詞（08-26）**：實機圖 `Девочка | ест | яблоко` 歷程＝漏中間詞 → 誤識 `токо` → 誤識 `ОКО`。
  - **Review 後策略**：刪單詞映射表（`токо`/`ОКО`→`ест`）；**有** `ect`/`ест`（或同等 remap）才校正中間短詞；單獨 `ОКО` **不得發明** `ест`。
  - 保留：拉丁同形 remap、多 PSM／切塊合併、Long–Short–Long 評分（垃圾中間詞輸給更高品質短詞）。
  - **殘餘**：使用者 08-26 晚間判定此例「似乎無法解決」；裝置端 OCR 對稀疏按鈕列短詞仍可能失敗，不阻擋本票 done。
- **APK**：08-26 多輪 Release Signed 側載（產物不進 git）。
