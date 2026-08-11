# Tickets — App-first MVP，其後再恢復 Web／策展

Parent（App MVP）: [`docs/AI 空耳外語學習 APP－MVP 系統規劃書/Cursor-MVP App 規劃書.md`](../AI%20空耳外語學習%20APP－MVP%20系統規劃書/Cursor-MVP%20App%20規劃書.md)  
Parent（信任閘／Web／策展，已交付子切片＋延後前端）: [`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md)  
Parent（Client-first 單字本）: [`docs/specs/client-first-wordcards-sync.md`](../specs/client-first-wordcards-sync.md) · ADR-0007  
Glossary: [`docs/glossary.md`](../glossary.md) · ADRs: [`docs/adr/`](../adr/)（0001–0007）

## 策略（2026-08）

**先完成 Android App MVP**，再回頭做策展 Blazor（05）與 Web 學習端（06）。  
04 的已驗證空耳 API 保留為 done（App 不經 Blazor 也能受益；策展暫可用 API）。  
單字本長期形狀以 **ADR-0007 Client-first＋可選雲端同步** 為準（票 13–17）；03 為歷史雲端鏡像基礎（done），不再視為「Server 擁有 App 卡」的終局。  
明確不在本輪：iOS、Billing UI、完整 SRS、雲端 OCR、多 Agent、即時協同／CRDT。

## 依賴圖

```text
01 (prefactor tests) ──► 02 (硬閘 + LLM 草稿標示) ──► 04 (已驗證 API + 覆寫) ──┐
                                                                              │
03 (單字本端到端) ✓ done＝雲端鏡像歷史基礎 ──────────────────────────────────┤
                                                                              │
                         ┌── 07 (OCR 選一字)     ✓ done                     │
                         ├── 08 (TTS 正式發音)   ◄─ frontier                │
                         ├── 09 (同字重產≤3＋錯誤態) ◄─ frontier            │
                         ├── 10 (隱私／設定收尾) ◄─ frontier                │
                         │                                                  │
                         └──────────► 11 (封閉測試) ──► 12 (商店送審)         │
                                                                              │
05 (策展 Blazor) ── deferred（App 07–10 後） ◄───────────────────────────────┘
06 (Web 薄 MVP1) ── deferred；單字本＝過渡期打雲端鏡像（ADR-0007） ◄── 02、03

Client-first 單字本（App 主切片；與 08–10 平行安全）:

13 (本機 SoT 列表／存／刪) ✓ done
 │
 ├──► 14 (同步協定 LWW／tombstone／換帳) ◄─ frontier ──► 15 (Server Notebook＝鏡像推拉)
 │
 ├──► 16 (詳情頁編修個人空耳) ◄─ frontier
 └──► 17 (金標不覆蓋已存卡個人空耳) ◄─ frontier
```

## Frontier（無 blocker／可立刻開工）

| 票 | 說明 |
|---|---|
| [08](08-app-tts-formal-reading.md) | App：結果頁／單字卡系統 TTS（規劃書 W4 建議優先之一） |
| [09](09-app-regenerate-cap-and-errors.md) | 同字重產 ≤3＋分析錯誤／額度態 |
| [10](10-app-privacy-settings-polish.md) | 隱私／AI 聲明＋設定收尾 |
| [14](14-app-sync-protocol-lww.md) | App：可選同步協定（整卡 LWW／tombstone／換帳）；建議 Client-first 下一刀 |
| [16](16-app-edit-personal-mnemonic.md) | App：詳情頁隨時編修個人空耳（可與 14 平行） |
| [17](17-verified-mnemonic-no-overwrite-saved-card.md) | 回歸：已驗證空耳不得覆蓋已存卡個人空耳（可與 14 平行） |

## 清單

| # | Title | Blocked by | Status |
|---|---|---|---|
| 01 | Application 測試骨架（prefactor） | None | done |
| 02 | 後處理硬閘＋LLM 草稿標示（含 App） | 01 | done |
| 03 | 單字本端到端可存可查（歷史→雲端鏡像基礎） | None | done |
| 04 | 已驗證空耳管理 API＋分析金標優先覆寫 | 02 | done |
| 05 | 策展端 Blazor：允許清單登入＋最小 CRUD | 04；App 07–10 | **deferred**（App-first） |
| 06 | Web 學習端薄 MVP1（單字本＝雲端鏡像過渡） | 02、03；App 07–10 | **deferred**（App-first） |
| 07 | App：裝置端 OCR 選一字進分析 | None | done |
| 08 | App：播放正式發音（系統 TTS） | None | ready-for-agent |
| 09 | App／API：同字重產 ≤3 與分析錯誤態 | None | ready-for-agent |
| 10 | App：隱私／AI 聲明與設定收尾 | None | ready-for-agent |
| 11 | App：封閉測試就緒與缺陷收斂 | 07–10 | blocked |
| 12 | App：商店素材與送審 | 11 | blocked |
| 13 | App：本機單字卡儲存與列表／存／刪（Client-first） | None | done |
| 14 | App：可選同步協定（整卡 LWW／tombstone／換帳隔離） | 13 | ready-for-agent |
| 15 | Server：Notebook API 雲端鏡像語意（推拉） | 14 | blocked |
| 16 | App：詳情頁隨時編修個人空耳 | 13 | ready-for-agent |
| 17 | 回歸：已驗證空耳不得覆蓋已存卡個人空耳 | 13；04 done | ready-for-agent |

## 與規劃書對照（缺口 → 票）

| 規劃 | 現況粗判 | 票 |
|---|---|---|
| W1 Auth | 大致完成 | 歷史 |
| W2–W3 分析＋單字本＋信任標示 | 01–04 done；單字本改 Client-first | **13–17** |
| W4 OCR | 07 done（實機品質待驗） | 歷史 |
| W4 TTS | 按鈕為 stub | **08** |
| 同字重產 ≤3／錯誤態 | 有重產 UI，無上限與完善錯誤流 | **09** |
| W5 設定／隱私／聲明 | 設定可用；隱私為示範 alert | **10** |
| W6–W8 封閉測試／上架 | 未開始；單字本驗收對齊 ADR-0007 | **11–12** |
| Web／策展 UI | 刻意延後；Web 單字本＝鏡像過渡 | **05–06 deferred** |

## Handoff → Station 4

每次新 session：**一張** frontier 票 + 對應 parent（App 規劃書、parallel-web spec、或 client-first-wordcards-sync）+ Testing Decisions／規劃驗收 seam + 先紅測再實作。  

- **08–10** 彼此無硬依賴，可平行給多 agent；與 **14／16／17** 亦平行安全。  
- **Client-first 串**：**13 done** → 建議優先 **14→15**；**16**／**17** 可與 14 平行。  
- 封閉測試（11）驗單字本時以 ADR-0007／13–17 為準，勿假定 Server 為 App SoT。
