# 19 — App：品牌圖示與 L00／L01／L04 對齊

**What to build:** 以 Soraeru Deep Teal（耳／聲波主題）取代 .NET MAUI 預設紫色 placeholder 圖示；App 啟動圖、登入／引導頁品牌標誌與 Stitch 原型 L00／L01／L04 視覺一致。

**Blocked by:** None

**Status:** done

## Parent

[`docs/AI 空耳外語學習 APP－MVP 系統規劃書/Cursor-MVP App 規劃書.md`](../AI%20空耳外語學習%20APP－MVP%20系統規劃書/Cursor-MVP%20App%20規劃書.md)（視覺識別、W6–W8 商店素材前置）  
關聯：票 **12**（商店素材定稿仍 blocked by 11）；本票為封閉測試與上架前的品牌基線。

## What to build

1. 研究 Soraeru 品牌語意，產出 AI 圖示生成 prompt（Deep Teal、耳／聲波）。
2. 使用者選定生成圖後，落地 App 圖示與 splash：`appicon.png`、`logo.png`；更新 `Soraeru.App.csproj` 的 `MauiIcon`／`MauiSplashScreen`／`MauiImage`；`splash.svg` 底色改 `#004d64`；修正 `SplashPage` `BrandMarkFrame` → `BrandMark`。
3. 同步 MAUI `SplashPage`、`LoginPage`、`OnboardingPage` 與 Stitch 原型 `L00_code.html`、`L01_code.html`、`l04_onboarding_screen_mvp_rev.html` 及 `Stitch assets/logo.png`。

## Acceptance criteria

- [x] Android／MAUI 不再顯示預設紫色 .NET placeholder 圖示；launcher icon 與 splash 使用新 logo。
- [x] 登入／引導／Splash 頁面品牌標誌一致（`BrandMark`）。
- [x] Stitch L00／L01／L04 原型與 App 使用同一 `logo.png` 資產。
- [x] Release APK 可成功建置（圖示資產不阻擋打包）。

## Blocked by

- None

## Notes（2026-08-28）

- 圖示 prompt 共 5 版（Deep Teal、耳／聲波）；最終採使用者選定之生成圖。
- `splash.svg` 實色 `#004d64`；`Soraeru.App.csproj` 已接 `MauiIcon`／`MauiSplashScreen`／`MauiImage`。
- 商店截圖／Play 列表圖仍留票 **12**；本票僅 App 與 Stitch 品牌基線。
