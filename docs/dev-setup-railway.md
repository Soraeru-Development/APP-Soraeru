# 生產環境：Railway 部署 Soraeru.Api

讓 API 以 Docker 跑在 Railway，SQLite 放 Volume。**密鑰只放 Railway Variables，不要寫進 git／`appsettings.json`。**

決策見 [ADR-0009](adr/0009-railway-sqlite-volume.md)。本機建置路徑見 [dev-setup-build.md](dev-setup-build.md)。

## 1. 映像怎麼建

- 方案**根目錄** `Dockerfile`：restore／publish **只** `src/Soraeru.Api`（會帶入 Application、Infrastructure）。
- 不要把 `Soraeru.slnx` 整包丟進 SDK 映像——方案含 MAUI，官方映像沒有 Android workload。
- Railpack 0.37+ 雖列出 Dotnet，但**只在倉庫根目錄找 `*.csproj`**。本 repo 的 API 在 `src/Soraeru.Api/`，根目錄又是 `.slnx`（含 MAUI），Railpack 會失敗（`could not determine how to build`／`start.sh not found`）。**必須用 Dockerfile**，不要讓服務走 Railpack。
- 可選根目錄 `railway.toml`（builder=DOCKERFILE、healthcheck `/health`）。若該服務是 Railway **新服務**且 Config as Code 已被忽略，請在 Dashboard 手動設：
  - **Builder = Dockerfile**（根目錄 `Dockerfile`；不要選 Railpack）
  - Healthcheck Path = `/health`
  - Replicas = **1**

## 2. Volume

| 項目 | 值 |
|------|-----|
| Mount path | `/app/data` |
| 連線字串 | `Data Source=/app/data/soraeru.db` |

掛上 Volume 後目錄是空的；API 會 `CreateDirectory`，再 `Migrate()` 建檔。掛完請 **Redeploy**。

**Replicas 必須為 1。** 多副本會互搶同一個 SQLite 檔。

## 3. Variables（密鑰只放這裡）

在 Railway 服務 Variables 設定（雙底線 = 巢狀設定）：

| 變數 | 說明 |
|------|------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:${{PORT}}`（與 `Program.cs` 讀 `PORT` 雙保險） |
| `ConnectionStrings__Default` | `Data Source=/app/data/soraeru.db` |
| `Jwt__SigningKey` | ≥32 字；**與本機 User Secrets 不同**的正式密鑰 |
| `GoogleAuth__ClientIds__0` | 現有 Web Client ID |
| `GoogleAuth__ClientIds__1` | 現有 Android Client ID（若有） |
| `Llm__ApiKey` | Gemini／相容端點金鑰 |
| `Llm__Model` | 可選覆寫 |
| `Llm__BaseUrl` | 可選覆寫 |
| `Cors__AllowedOrigins__0` | **先不要設**。Web 上線後再填 `https://<web>.up.railway.app` |

`Jwt:Issuer`／`Jwt:Audience` 沿用 `appsettings.json`（`Soraeru`／`Soraeru.App`）即可。

產生 JWT 密鑰（本機、不要貼進聊天紀錄或 commit）：

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 } | ForEach-Object { [byte]$_ }))
```

## 4. Dashboard 步驟（同一 GitHub repo、一個 API 服務）

1. 將含 Dockerfile 的 commit 推到 GitHub（`origin`：`APP-Soraeru`）。
2. [Railway](https://railway.com/) → New Project → Deploy from GitHub repo。
3. 確認建置用根目錄 Dockerfile（不要讓它嘗試還原整個 solution）。
4. 新增 Volume，mount `/app/data`，Attach 到 API 服務 → Redeploy。
5. Settings → Replicas = 1。
6. Generate Domain。
7. Healthcheck：`/health`。
8. 填上節 Variables → Redeploy。

驗證：`GET https://<api>.up.railway.app/health` 應為 200。再註冊一筆帳、Redeploy，確認 Volume 上的 `soraeru.db` 還在。

## 4.1 常見錯誤：Railpack `could not determine how to build`

Railway 預設用 **Railpack**。若 GitHub 還沒有根目錄 `Dockerfile`，或服務 Builder 仍是 Railpack，日誌會類似：

- `⚠ Script start.sh not found`
- `✖ Railpack could not determine how to build the app`
- 分析到的檔案只有 `Soraeru.slnx`、`src/`、`nuget.config`，**沒有** `Dockerfile`／根目錄 `.csproj`

處理：

1. 確認 `Dockerfile`、`.dockerignore` 已 **commit 並 push** 到 Railway 所接的 GitHub 分支（通常是 `main`）。
2. Railway 服務 → Settings → Build → **Builder = Dockerfile**（Dockerfile path 留空或 `Dockerfile`）。
3. Redeploy。成功的建置日誌應出現 `FROM mcr.microsoft.com/dotnet/sdk:10.0`，而不是 `Railpack 0.x`。

## 5. 已知限制（上線知情）

- 忘記密碼：token 在行程記憶體，信只進 log；封閉測試請用 Email 註冊或 Google。
- 分析快取是行程內記憶體：redeploy 後清空（可能再扣額度）。
- SQLite = MVP 單實例；要多區／多副本再遷 Postgres（另開 ADR）。
- App 目前仍打 `http://10.0.2.2:5080/`。實機／封閉測試連雲端須另改基底 URL（不要把正式 URL 寫死進會進 git 的 Release 設定，除非有意）。

## 6. CORS（現在空、Web 之後）

`Cors:AllowedOrigins` 預設空陣列 → **不加**跨域中介，Android App 不受影響。Web 學習端上線後再設 `Cors__AllowedOrigins__0`。
