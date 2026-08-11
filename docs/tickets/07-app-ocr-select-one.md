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

- App L07／L08：`MediaPicker` 相機／相簿＋`Plugin.Maui.OCR`（`TryHard=false` 裝置端）；原圖只進本機 OCR，分析 request 僅文字。
- 分詞／單選解析：`Soraeru.ClientLogic`（`OcrTextTokenizer`、`OcrAnalyzeSelection`）＋單元測試。
- 失敗／弱腳本：alert 引導改手動輸入（WordInput）。
- 未驗證：實機 Android OCR 品質（CLI Android 建置刻意避開 VS lock）；CJK／泰文 on-device 腳本涵蓋度依 ML Kit Latin 模組限制。
