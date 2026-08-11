# 已驗證空耳命中則不呼叫 LLM（僅空耳欄位）

分析時若以「原詞＋語言」（正規化後）命中啟用中的已驗證空耳，則 **displayText／notationText／explanation** 等空耳欄位直接來自該條目，**不呼叫 LLM 產空耳**。詞義與正式讀音仍可走 LLM（或其他既有來源）。

**Status**: accepted  
**Decided in**: Station 1 Round 1 Q3=A；Round 2 Q11=B、Q12=A

## Considered Options

- **命中只覆寫空耳（選定）**：聽感 determinism；詞義／讀音管線可重用。  
- **整包靜態／必須齊欄才上架**：零 LLM，但策展成本高、本切片不做。  
- **混合置頂／金標僅測評**：已拒。

## Consequences

- 查詢鍵與正規化規則成為契約；錯誤已驗證條目會系統性誤導命中使用者。  
- 一次「命中」分析仍可能產生 LLM 成本（詞義／讀音）。  
- 回應需能區分「空耳已驗證」vs「詞義／讀音仍為 AI」。  
- LLM 未命中路徑另受後處理硬閘約束（ADR-0004）；命中路徑預設信任策展內容。
