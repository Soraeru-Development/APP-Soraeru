# 策展端為獨立 Blazor 站＋Google 允許清單

策展端與 Web 學習端**分開部署**（各自 Blazor 應用）。策展認證＝同一套 Google 身分，但僅 **email 允許清單**可進入。社群投稿與審核佇列本階段不做。

**Status**: accepted  
**Decided in**: Station 1 Round 2 Q8=A、Q9=C、Q14=D

## Considered Options

- **獨立站＋Google 允許清單（選定）**：學習者面與策展面隔離、權限邊界清楚。  
- **同站隱藏管理路由**：部署單純但誤曝風險較高。  
- **完全獨立帳密**：一人維運成本較高，未採。  
- **公網策展但本機工具**：未採（要平行驗「上架後可維護金標」）。

## Consequences

- 允許清單可對齊既有開發者允許清單／`IsDeveloper` 思路；建議重用同一 email 清單作為策展授權來源（實作細節見規格）。  
- 兩套 Blazor 前端＝兩份部署與 CORS／Auth audience 設定；後端仍為單一 API（ADR-0006）。  
- 無投稿狀態機；日後開放社群需新 ADR。
