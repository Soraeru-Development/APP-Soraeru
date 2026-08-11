# 03 — 單字本端到端可存可查



**What to build:** 已登入學習者能將選定空耳候選存成單字卡，並在單字本列表查看與刪除；API 與至少一條學習端（優先既有 App，或同等 API 驗收）可獨立 demo。



**Blocked by:** None — can start immediately



**Status:** done



## Parent



[`docs/specs/parallel-web-curator-trust.md`](../specs/parallel-web-curator-trust.md)（User Stories 4、21）



## What to build



把單字本從「端點存在但應用層未實作」接到可驗收行為：登入後依帳號儲存選定候選（原詞、語言、詞義、讀音、選定空耳等既有契約），列表與刪除可用。持久化實作可先採已有可替換倉庫策略，但行為對學習者完整。本票不做 OCR、不做完整 SRS。供後續 Web 學習端薄頁直接消費同一語意。



## Acceptance criteria



- [x] 已登入學習者可儲存一張含選定空耳的單字卡（綁帳號）。

- [x] 可列出自己的單字卡；可刪除指定卡。

- [x] 未登入／他人資源受授權保護（不可讀寫他人單字本）。

- [x] 行為可經 API 合約測試（及／或 App 煙測）獨立驗證；不再回「未實作」類成功假象。

- [x] 額度語意不因存卡而被繞過或雙算（存卡本身不替代分析額度規則）。



## Blocked by



- None — can start immediately



## Done notes



- Application：`NotebookService`＋Application.Tests。

- 持久化：預設 Sqlite（與帳號／額度同一檔案庫）；`Provider=InMemory` 時仍用記憶體倉儲。重啟服務後單字卡仍在。

- Migration：`AddWordCards`。啟動 API 後會套用遷移（既有 DB migrate 路徑）。

- 驗證：`dotnet test tests/Soraeru.Application.Tests --filter FullyQualifiedName~Notebook`；`dotnet test tests/Soraeru.Infrastructure.Tests`

## ADR-0007 註記（歷史角色）

本票交付的 Server 帳號綁定單字卡儲存，在 ADR-0007 下**重釋為雲端鏡像基礎**（非 App 長期 SoT）。後續語意演進見票 **13–15**；本票維持 **done**，勿刪除覆蓋為「Server 永遠擁有 App 卡」。

