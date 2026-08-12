# Tickets — App-first MVP，其後再恢復 Web／策展

Parent（App MVP）: [`docs/AI 空耳外語學習 APP－MVP 系統規劃書/Cursor-MVP App 規劃書.md`](../AI%20空耳外語學習%20APP－MVP%20系統規劃書/Cursor-MVP%20App%20規劃書.md)  
Parent（信任閘／Web／策展，已交付子切片＋延後前端）: [`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md)  
Parent（Client-first 單字本）: [`docs/specs/client-first-wordcards-sync.md`](../specs/client-first-wordcards-sync.md) · ADR-0007  
Parent（同字再查／本機短路）: [`docs/specs/local-notebook-lookup-short-circuit.md`](../specs/local-notebook-lookup-short-circuit.md) · ADR-0008  
Glossary: [`docs/glossary.md`](../glossary.md) · ADRs: [`docs/adr/`](../adr/)（0001–0008）

## 總覽（2026-08-12）

| 指標 | 數量 |
|---|---|
| **done** | 13 |
| **in-progress（WIP）** | 1（18） |
| **ready-for-agent** | 1（11） |
| **blocked** | 1（12） |
| **deferred** | 2（05、06） |
| **完成率** | 13／18＝72%（不含 deferred：13／16＝81%） |

**Frontier（無 blocker／可立刻開工）：**

- **[18](18-app-local-notebook-lookup-short-circuit.md)** — 同字再查本機短路＋詳情重新分析 — **in-progress（WIP）**（AC 全綠；殘餘實機煙測）
- **[11](11-app-closed-testing.md)** — 封閉測試就緒與缺陷收斂（blocker 07–10 已全 done；與 18 平行）

## 策略（2026-08）

**先完成 Android App MVP**，再回頭做策展 Blazor（05）與 Web 學習端（06）。  
04 的已驗證空耳 API 保留為 done（App 不經 Blazor 也能受益；策展暫可用 API）。  
單字本長期形狀以 **ADR-0007 Client-first＋可選雲端同步** 為準（票 13–18；同字再查見 ADR-0008／票 18）；03 為歷史雲端鏡像基礎（done），不再視為「Server 擁有 App 卡」的終局。  
明確不在本輪：iOS、Billing UI、完整 SRS、雲端 OCR、多 Agent、即時協同／CRDT。

## 工單進度主表

| # | 分組 | 標題 | 狀態 | 目前狀況 | code-review-dual | 檢測方式 | Blocker／前置 | 下一步 | 殘餘風險 | 近期工時 |
|---|---|---|---|---|---|---|---|---|---|---|
| [01](01-application-tests-prefactor.md) | 信任閘／基礎 | Application 測試骨架（prefactor） | done | 測試宿主與 fake seam 就緒，紅綠循環可跑 | 未跑 | 自動（`dotnet test`） | — | — | — | 0.25h（08-10） |
| [02](02-hard-gate-llm-draft.md) | 信任閘／基礎 | 後處理硬閘＋LLM 草稿標示 | done | `MnemonicHardGate`＋App 草稿橫幅已交付 | 未跑 | 自動（Application.Tests） | 01 | — | — | 0.5h（08-10） |
| [03](03-notebook-end-to-end.md) | 信任閘／基礎 | 單字本端到端可存可查（歷史雲端鏡像） | done | 雲端鏡像 CRUD 基礎；角色已由 13–15 重釋 | 未跑 | 自動+手動 | — | — | 語意已過渡至 Client-first | 0.75h（08-10） |
| [04](04-verified-override-and-api.md) | 信任閘／基礎 | 已驗證空耳管理 API＋分析金標優先覆寫 | done | 策展 CRUD＋分析命中金標跳過 LLM | 未跑 | 自動（Application.Tests） | 02 | — | — | 0.5h（08-11） |
| [05](05-curator-blazor-crud.md) | Web／策展 | 策展端 Blazor：允許清單登入＋最小 CRUD | **deferred** | App-first 策略延後；過渡用 04 API | 不適用 | 手動（UI 煙測） | 04 done；App 07–10 | App MVP 後恢復 | — | — |
| [06](06-web-learner-mvp1.md) | Web／策展 | Web 學習端薄 MVP1 | **deferred** | App-first 延後；單字本＝雲端鏡像過渡 | 不適用 | 手動（Web 煙測） | 02、03；App 07–10 | App MVP 後恢復 | — | — |
| [07](07-app-ocr-select-one.md) | App MVP | 裝置端 OCR 選一字進分析 | done | MediaPicker＋裝置 OCR＋單選解析已接線 | 未跑 | 自動+手動（實機 OCR 品質） | — | — | CJK／泰文 on-device 腳本限制 | 0.5h（08-11） |
| [08](08-app-tts-formal-reading.md) | App MVP | 播放正式發音（系統 TTS） | done | 結果／詳情／列表系統 TTS 已接 | 未跑 | 自動+手動（缺語音包提示） | — | — | — | 1.5h（08-12） |
| [09](09-app-regenerate-cap-and-errors.md) | App MVP | 同字重產 ≤3 與分析錯誤態 | done | `REGENERATION_LIMIT_EXCEEDED`＋App 錯誤態 | 未跑 | 自動（App+API TDD） | — | — | — | 1.5h（08-12） |
| [10](10-app-privacy-settings-polish.md) | App MVP | 隱私／AI 聲明與設定收尾 | done | 應用內 LegalDocument 頁＋設定入口 | 未跑 | 自動+手動 | — | — | 商店託管 URL 留票 12 | 1.25h（08-12） |
| [11](11-app-closed-testing.md) | App 上架 | 封閉測試就緒與缺陷收斂 | **ready-for-agent** | 07–10 全 done；可開封閉測試輪次 | 未跑 | 手動（§15 檢核表） | 07–10（皆 done） | 產測試建置＋回歸清單 | 13–18 單字本驗收對齊 ADR-0007 | — |
| [12](12-app-play-store-submit.md) | App 上架 | 商店素材與送審 | **blocked** | 等 11 封閉測試通過 | 不適用 | 手動（Play Console） | 11 | 11 完成後準備素材 | — | — |
| [13](13-app-local-wordcard-store.md) | Client-first | 本機單字卡儲存與列表／存／刪 | done | `LocalNotebookService` 本機 SoT；刪帳／session 清庫 | 未跑 | 自動+手動（煙測） | — | — | 離線 JWT 過期偵測；UsageDaily 孤兒列 | ~4.25h（08-11） |
| [14](14-app-sync-protocol-lww.md) | Client-first | 可選同步協定（LWW／tombstone／換帳） | done | Merger＋Coordinator＋前景觸發；假鏡像單測綠 | 未跑 | 自動（ClientLogic.Tests） | 13 | — | 端到端多裝置待 15 現場驗 | 1.5h（08-12） |
| [15](15-server-notebook-mirror-api.md) | Client-first | Server Notebook API 雲端鏡像語意 | done | `GET/PUT mirror`＋`HttpCloudWordCardMirror` 已接 | 未跑 | 自動+手動（雙端 demo 待驗） | 14 | 現場雙端推拉 demo | 雙端 demo AC 未勾；Id 衝突 CONFLICT | 2.5h（08-12） |
| [16](16-app-edit-personal-mnemonic.md) | Client-first | 詳情頁隨時編修個人空耳 | done | `UpdateSelectedMnemonicAsync`＋詳情 UI | 未跑 | 自動+手動 | 13 | — | — | 1h（08-12） |
| [17](17-verified-mnemonic-no-overwrite-saved-card.md) | Client-first | 金標不覆蓋已存卡個人空耳 | done | Save 同鍵回傳既有卡；TDD 回歸鎖住 | 未跑 | 自動（Application+ClientLogic） | 13；04 | — | — | 0.75h（08-12） |
| [18](18-app-local-notebook-lookup-short-circuit.md) | Client-first | 同字再查本機短路＋詳情重新分析 | **in-progress（WIP）** | ClientLogic Gate＋App 短路／重新分析已接；AC 全綠 | 未跑 | 自動+手動（實機煙測待做） | 13（done） | 實機煙測（命中／未登入／計額） | 他機未同步卡仍會分析（ADR-0008 接受） | 1.5h（08-12） |

> **code-review-dual 語意**：`未跑`＝尚無 Station 5 雙軸審查紀錄；`不適用`＝未開工或延後；勿將 `未跑` 視為通過。

## 依賴圖

```text
01 (prefactor tests) ──► 02 (硬閘 + LLM 草稿標示) ──► 04 (已驗證 API + 覆寫) ──┐
                                                                              │
03 (單字本端到端) ✓ done＝雲端鏡像歷史基礎 ──────────────────────────────────┤
                                                                              │
                         ┌── 07 (OCR 選一字)     ✓ done                     │
                         ├── 08 (TTS 正式發音)   ✓ done                     │
                         ├── 09 (同字重產≤3＋錯誤態) ✓ done                 │
                         ├── 10 (隱私／設定收尾) ✓ done                     │
                         │                                                  │
                         └──────────► 11 (封閉測試) ◄─ frontier ──► 12 (商店送審) │
                                                                              │
05 (策展 Blazor) ── deferred（App 07–10 後） ◄───────────────────────────────┘
06 (Web 薄 MVP1) ── deferred；單字本＝過渡期打雲端鏡像（ADR-0007） ◄── 02、03

Client-first 單字本（App 主切片；與 08–10 平行安全）:

13 (本機 SoT 列表／存／刪) ✓ done
 │
 ├──► 14 (同步協定 LWW／tombstone／換帳) ✓ done ──► 15 (Server Notebook＝鏡像推拉) ✓ done
 │
 ├──► 16 (詳情頁編修個人空耳) ✓ done
 ├──► 17 (金標不覆蓋已存卡個人空耳) ✓ done
 └──► 18 (本機短路／詳情重新分析) ← **in-progress（WIP）**；重產 ≤3 銜接 09
```

## 與規劃書對照（缺口 → 票）

| 規劃 | 現況粗判 | 票 |
|---|---|---|
| W1 Auth | 大致完成 | 歷史 |
| W2–W3 分析＋單字本＋信任標示 | 01–04 done；單字本改 Client-first | **13–18** |
| W4 OCR | 07 done（實機品質待驗） | 歷史 |
| W4 TTS | 系統 TTS 已接（結果／詳情／列表） | **08** done |
| 同字重產 ≤3／錯誤態 | 後端上限＋App 錯誤態 | **09** done |
| 同字再查／本機短路 | App 本機查鍵短路＋詳情重新分析 — in-progress（WIP） | **18** |
| W5 設定／隱私／聲明 | 應用內隱私＋AI 聲明入口 | **10** done |
| W6–W8 封閉測試／上架 | 未開始；單字本驗收對齊 ADR-0007 | **11–12** |
| Web／策展 UI | 刻意延後；Web 單字本＝鏡像過渡 | **05–06 deferred** |

## Handoff → Station 4

每次新 session：**一張** frontier 票 + 對應 parent（App 規劃書、parallel-web spec、client-first-wordcards-sync、或 local-notebook-lookup-short-circuit）+ Testing Decisions／規劃驗收 seam + 先紅測再實作。  
  
- **08–10**：08／09／10 done。  
- **封閉測試（11）**：blocker 07–10 已全 done → frontier。  
- **Client-first 串**：**13–17 done**；**18 in-progress（WIP）**；與 11 平行。  
- **本機短路（18）**：in-progress（WIP）（ClientLogic 查鍵＋AnalyzeEntryGate；App WordInput／OCR／詳情重新分析；`ForceRefresh` 銜接 09；殘餘見票 Notes）。  
- 封閉測試（11）驗單字本時以 ADR-0007／13–18 為準，勿假定 Server 為 App SoT；同字再查以 ADR-0008／票 18 為準。
