# 08 — App：播放正式發音（系統 TTS）

**What to build:** 學習者在分析結果頁與單字卡詳情可播放**正式原文**發音（系統 TTS）；缺語音包時仍顯示讀音文字並引導安裝，不中斷其他流程。

**Blocked by:** None — can start immediately

**Status:** done

## Parent

[`docs/AI 空耳外語學習 APP－MVP 系統規劃書/Cursor-MVP App 規劃書.md`](../AI%20空耳外語學習%20APP－MVP%20系統規劃書/Cursor-MVP%20App%20規劃書.md)（F09、W4）  
策略補充：[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md) Further Notes（App-first）

## What to build

取代現行「尚未接 TTS」提示：依分析／單字卡的來源語與正式讀音／原文，請求系統 Text-to-speech **只播正式原文**（不播空耳候選）。無對應語音包或播放失敗時，保留讀音文字可見，並提示使用者可安裝語音包；分析與儲存流程不受阻。可與 07 平行。

## Acceptance criteria

- [x] 分析結果頁可播放正式原文發音。
- [x] 單字卡詳情頁可播放正式原文發音。
- [x] 不播放空耳候選作為「正式發音」。
- [x] 缺語音包／播放失敗時有可理解提示，且讀音文字仍可見。
- [x] 不新增雲端發音服務依賴（MVP 用系統 TTS）。

## Blocked by

- None — can start immediately

## Notes

- **2026-08-13 手動已驗證**：TTS 播放正式原文可用。
