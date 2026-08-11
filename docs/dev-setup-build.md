# 開發建置／NuGet 路徑（Windows）

若 Visual Studio 出現 **Import Project 路徑超過 260 字元**，或錯誤路徑含 `cursor-sandbox-cache`，代表 NuGet 曾還原到 Cursor sandbox 長路徑，並寫進 `obj\*.nuget.g.targets`。

## 正解：短路徑 global packages

本 repo 根目錄的 `nuget.config` 將套件放在：

`D:\.nuget\packages`

請勿使用 `cursor-sandbox-cache` 底下的路徑。

## 清掉 VS 錯誤後重新載入

1. 關閉 Visual Studio（或至少卸載 solution）。
2. 刪除專案／solution 的 `bin`、`obj`（至少 `src/Soraeru.App/bin` 與 `obj`）。
3. 在**本機一般終端機**（非 Cursor agent sandbox）確認：
   - 使用者／系統環境變數 **不要** 把 `NUGET_PACKAGES` 指到 sandbox。
   - 若需手動覆寫：`NUGET_PACKAGES=D:\.nuget\packages`
4. 在 repo 根目錄執行：
   ```powershell
   $env:NUGET_PACKAGES = "D:\.nuget\packages"   # 若 shell 裡仍殘留錯誤變數
   dotnet nuget locals global-packages -l       # 應顯示 D:\.nuget\packages
   dotnet restore Soraeru.slnx
   ```
5. 重新用 VS 開啟 solution，或：
   ```powershell
   dotnet build src/Soraeru.App/Soraeru.App.csproj -f net10.0-windows10.0.19041.0
   ```

還原後的 `*.nuget.g.targets` 應使用 `$(NuGetPackageRoot)\...`，且路徑不得含 `cursor-sandbox-cache`。

## 關於 NUGET_PACKAGES

Cursor Agent／sandbox 可能在程序內注入 `NUGET_PACKAGES` 指向 `%TEMP%\cursor-sandbox-cache\...\nuget`。這會覆寫 `nuget.config` 的 `globalPackagesFolder`。

- **Visual Studio／本機建置**：取消或改為 `D:\.nuget\packages`。
- 可在「系統內容 → 環境變數」檢查是否被寫成使用者層級永久變數；若有指向 sandbox，請刪除。

## Windows 長路徑（可選）

可啟用 Windows「長路徑」群組原則／登錄，但 **MAUI／AndroidX 仍建議以短 `globalPackagesFolder` 為主**，比只開長路徑更穩。

## Android 部署：「裝置無法辨識命令。(22)」

這是 Win32 `ERROR_BAD_COMMAND`（錯誤碼 22）的繁中訊息，常見於 Visual Studio 對模擬器／實機 **F5 部署**（Fast Deployment／ADB 同步）失敗，而不是 Google 登入邏輯錯誤。

專案已在 Debug Android 設定 `EmbedAssembliesIntoApk=true`（關閉 Fast Deployment，改部署完整 APK）。若仍失敗，依序做：

1. 確認 VS 工具列目標為 **net10.0-android**，裝置選到 **emulator-xxxx（device）**，不要選離線／錯誤裝置。
2. 重新啟動 ADB（PowerShell）：
   ```powershell
   & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" kill-server
   & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" start-server
   & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" devices -l
   ```
   應看到模擬器狀態為 `device`（不是 `offline`／`unauthorized`）。
3. 在模擬器／實機 **解除安裝** 既有 `空耳學單字`（`com.soraeru.app`），再於 VS 按 F5。
4. 模擬器異常時：Device Manager → **Cold Boot**（或 Wipe Data 後冷啟動），等完全開機再部署。
5. 本機建置抽樣：
   ```powershell
   $env:NUGET_PACKAGES = "D:\.nuget\packages"
   dotnet build src/Soraeru.App/Soraeru.App.csproj -f net10.0-android
   ```

仍失敗時請收集：VS「輸出」視窗（顯示來源選 Build／Xamarin／Deploy）完整錯誤列、`adb devices -l` 輸出、是否有多個 `adb.exe`。

## Android 大量 Java 錯誤：`package com.microsoft.maui does not exist`

若建置輸出出現例如：

- `package com.microsoft.maui does not exist`
- Glide / AndroidX 相關 `package ... does not exist`
- `cannot find symbol getClass()`（如 `ContentViewGroup`）

通常**不是業務程式碼錯誤**，而是 `obj` 中介損壞、與 Visual Studio **並行／設計時建置搶鎖**，或 NuGet 路徑不一致，導致 Java／aapt2 階段見到不完整的 library 匯入。

### 修復步驟（請先關閉 Visual Studio）

1. 關閉 Visual Studio（避免 `devenv`／MSBuild 鎖定或污染 `obj`）。
2. 在**一般本機終端**（非 Cursor sandbox）設定並確認 NuGet：
   ```powershell
   $env:NUGET_PACKAGES = "D:\.nuget\packages"
   dotnet nuget locals global-packages -l   # 應顯示 D:\.nuget\packages
   ```
3. 清理並徹底刪除 App（必要時整個 `src`）的 `bin`／`obj`：
   ```powershell
   cd D:\VS\Soraeru
   dotnet clean src/Soraeru.App/Soraeru.App.csproj -c Debug
   Remove-Item -Recurse -Force src\Soraeru.App\bin, src\Soraeru.App\obj -ErrorAction SilentlyContinue
   # 若仍異常，可刪除 src 下各專案的 bin/obj 後再還原
   ```
4. 還原並單目標建置（先 Android，必要時再驗 Windows）：
   ```powershell
   dotnet restore Soraeru.slnx
   dotnet build src/Soraeru.App/Soraeru.App.csproj -f net10.0-android -c Debug
   dotnet build src/Soraeru.App/Soraeru.App.csproj -f net10.0-windows10.0.19041.0 -c Debug
   ```
5. 再重新開啟 Visual Studio；啟動設定選 **net10.0-android** 後建置／部署。

### 在 VS 如何避免再發

- 清理重建前**先關 VS**，不要一邊開著方案一邊用 CLI／另一個 IDE 清 `obj`。
- 確認使用者／系統環境變數**沒有**把 `NUGET_PACKAGES` 指到 `cursor-sandbox-cache`；應依 repo `nuget.config` 使用 `D:\.nuget\packages`。
- 避免同時對同一 App 專案跑多個建置（VS 設計時建置 + 外部 `dotnet build`）。
- 若同時出現 `APT2000`／「裝置無法辨識命令 (22)」與 zip `Permission denied`，優先視為 **檔案鎖定／並行污染**，同樣以關 VS → 刪 `obj` → 單執行緒重建處理。

