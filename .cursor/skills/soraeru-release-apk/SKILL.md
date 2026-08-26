---
name: soraeru-release-apk
description: >-
  After any source code change in the Soraeru .NET MAUI app, automatically builds
  a Release APK for device sideload verification. Use when finishing implementation
  or bugfix work that modified app/source code, or when the user mentions Release
  APK, 打 APK, 實機驗證, 側載, or 改程式後打包. Do not wait for the user to ask again
  unless they explicitly say not to package.
---

# Soraeru Release APK

## 原則（verbatim）

以後這個專案中，只要有修改程式的部分，就需要自動release new APK。

## When to run

**Must auto-build** at the end of the same turn / same task after work that changed app or source code (features, bugfixes, refactors that touch code).

**Skip** when:
- 純文件／純討論／未改程式
- 使用者明確說不要打包

**Configuration**: 一律 **Release**。Only build Debug if the user explicitly asks for Debug.

## Build (Windows PowerShell)

```powershell
cd D:\VS\Soraeru
$env:NUGET_PACKAGES = "D:\.nuget\packages"
dotnet build src\Soraeru.App\Soraeru.App.csproj `
  -f net10.0-android `
  -c Release `
  -p:RestorePackagesPath=D:\.nuget\packages `
  -p:AndroidPackageFormat=apk `
  -p:AndroidAapt2DaemonMaxInstanceCount=1 `
  -m:1
```

- Project: `src/Soraeru.App`（TFM `net10.0-android`）
- NuGet short path: `D:\.nuget\packages`（避免 sandbox 長路徑污染）

## Output / sideload

| File | Use |
|------|-----|
| `src/Soraeru.App/bin/Release/net10.0-android/com.soraeru.app-Signed.apk` | **實機側載**（Signed） |
| `src/Soraeru.App/bin/Release/net10.0-android/com.soraeru.app.apk` | 未簽章 — **不要**給實機 |

目前無正式 Play keystore 時，用 debug 簽章側載即可。回報時給 **Signed APK 絕對路徑**：

`D:\VS\Soraeru\src\Soraeru.App\bin\Release\net10.0-android\com.soraeru.app-Signed.apk`

## On failure

1. 清相關 `bin`／`obj`（至少 `src/Soraeru.App`）
2. 固定 `NUGET_PACKAGES` / `RestorePackagesPath` 為 `D:\.nuget\packages`
3. 維持單執行緒（`-m:1`）與 `AndroidAapt2DaemonMaxInstanceCount=1`
4. 可再跑 `.\scripts\fix-nuget-path.ps1 -Build`（詳見 `docs/dev-setup-build.md`）
5. 重試上述 Release 建置

## Agent checklist

```
- [ ] Source changes done (or user asked for APK)
- [ ] Release build (not Debug unless asked)
- [ ] Report Signed APK absolute path on success
- [ ] If failed: clean + NuGet path + retry; summarize error
```
