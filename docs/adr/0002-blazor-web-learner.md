# Web 學習端採用 Blazor

非 App 使用者的 Web 學習端以 **Blazor** 實作，與既有 ASP.NET／.NET 生態同棧。

**Status**: accepted（框架）；宿主型態（Server／WASM／Unified）**刻意未鎖**（Round 2 Q15=D）  
**Decided in**: Station 1 Round 1 Q6=B；Q15 延後至實作前

## Considered Options

- **Blazor（選定）**：與 API／MAUI 同 .NET。  
- **獨立 SPA**：已拒。  
- **宿主偏好**：尚無；不阻塞产品范围规格。

## Consequences

- Web 功能子集：登入＋手動輸入＋結果＋單字本（見 ADR-0003）。  
- 策展端亦為獨立 Blazor 站（見 ADR-0005），可與 Web 學習端分開選宿主。  
- 實作 ticket 開始前再鎖宿主，避免空想部署約束。
