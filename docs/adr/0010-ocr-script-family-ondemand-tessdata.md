# ADR 0010 — Script-family routing + on-demand tessdata ≠ all languages in APK

- Status: accepted
- Date: 2026-08-25

## Context

「全世界語言」OCR 不能靠把全部 tessdata 打進 APK（體積爆炸）。ML Kit 也只覆蓋部分腳本模組。

## Decision

1. **腳本族路由（soft hint）**：ImagePick 選 Auto／拉丁／西里爾／CJK／阿拉伯／天城文／東南亞／其他；`OcrEngineRouter` 決定 SkipMlKit 與 Tesseract primary 語言串。
2. **核心包隨 APK**：既有 tessdata_fast 核心集合仍打包。
3. **按需下載**：`ITessdataPackStore` 從 tessdata_fast（GitHub raw）下載 allowlist 缺包（proof：`ara` 檢查、`deu`／`fra` 拉丁族），快取於 app data；原圖不上雲。
4. **來源語可搜尋**：ClientLogic curated ISO 清單 + Search；UI 用 SearchBar／CollectionView，不是固定 8 chips。

## Consequences

- 「全語言」是路由 + 目錄 + 按需包的產品承諾，不是 APK 內建全集。
- 首次選拉丁等族可能需下載模型；離線時僅能用已快取／已打包包。
- 仍不做雲端 Vision／原圖上傳；P2 文字 LLM 輔助需確認、不靜默覆寫。
