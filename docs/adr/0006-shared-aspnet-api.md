# App／Web 學習端／策展端共用同一 ASP.NET API

Android App、Web 學習端、策展端皆呼叫**同一** ASP.NET 後端。學習者 API 與策展管理 API 以身分與授權區分，不另起第二個業務後端。

**Status**: accepted  
**Decided in**: Station 1 close C3=yes

## Considered Options

- **單一 API（選定）**：分析、帳號、額度、已驗證空耳儲存與覆寫語意一致；平行切片可一次驗通。  
- **策展／Web 另起後端**：易漂移、雙倍維運，已拒。

## Consequences

- 策展端需要受保護的管理端點（允許清單／策展者授權）；不得讓一般學習者寫入已驗證空耳。  
- CORS、Google audience、JWT 需同時服務多個前端來源。  
- Blazor 宿主型態（Server／WASM）未鎖（見 ADR-0002），但不改變「單一 API」邊界。
