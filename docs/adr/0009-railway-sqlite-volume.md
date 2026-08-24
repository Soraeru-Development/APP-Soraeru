# 生產 API：Railway Docker + Volume 上的 SQLite（單 replica）

Soraeru.Api 的第一個雲端環境跑在 **Railway**，以根目錄 **Dockerfile** 建置（Railpack 雖列出 Dotnet，但只偵測根目錄 `*.csproj`，且方案含 MAUI，故不用 Railpack）。資料庫維持 **SQLite 檔案**，掛在 Volume `/app/data`；**replicas = 1**。暫不遷 Postgres。

**Status**: accepted  
**Decided in**: Railway API 部署計畫（2026-08）  
**Depends on**: ADR-0006（單一 ASP.NET API）

## Considered Options

- **Railway Docker + Volume SQLite、單 replica（選定）**：與現有 `ConnectionStrings:Default` + 啟動時 `Migrate()` 相容；MVP 封閉測試夠用；無第二套 DB 維運。  
- **立刻遷 Postgres**：水平擴展／多區較穩，但要改 Persistence、連線字串、本機開發路徑；此刻沒有多副本需求，已拒為本輪。  
- **把 SQLite 路徑寫進已提交的 `appsettings.json`**：會弄壞本機 `dotnet run`；生產路徑只由 Railway 環境變數覆寫。  
- **同一容器塞 Blazor Web／策展**：違反 ADR-0005（學習端與策展端分開部署）與 ADR-0006 的「單一 API、獨立站」邊界；已拒。

## Locked policy

| 主題 | 決定 |
|------|------|
| 建置 | 方案根目錄 Dockerfile；只 publish `Soraeru.Api`（勿還原整個含 MAUI 的 solution） |
| 資料 | Volume mount `/app/data`；`ConnectionStrings__Default=Data Source=/app/data/soraeru.db` |
| 規模 | Replicas = 1（SQLite 不能多進程搶同一個檔） |
| 之後 Postgres | 另開 ADR；不在本次 |
| Web／策展 | 之後各一條 Dockerfile／服務；API 以空的 `Cors:AllowedOrigins` 預留 |

## Consequences

- Redeploy 後只要 Volume 還在，帳號與鏡像資料應還在；分析快取是行程內記憶體，會清空。  
- 忘記密碼 token 在記憶體、重設信只進 log：封閉測試請用 Email 註冊／Google。  
- App 本機仍打 `10.0.2.2:5080`；實機／封閉測試連雲端 API 須另改基底 URL（非本 ADR）。  
- 操作步驟與變數清單見 `docs/dev-setup-railway.md`。
