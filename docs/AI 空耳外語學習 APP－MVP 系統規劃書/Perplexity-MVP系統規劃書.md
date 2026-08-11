
# Google Play 小型 APP MVP 規劃書 v1.1
> 目標：以最小成本上架 Google Play 的小型外語空耳學習 APP  
> 範圍：只做「單字輸入、圖片上傳、AI 候選空耳、收藏查詢」

---

## 1. 專案目標
建立一個輕量 APP，讓使用者輸入單字或上傳圖片後，系統可自動辨識文字、分析發音、產生空耳候選，讓使用者自行判斷是否好記並收藏。

---

## 2. MVP 功能清單

### 2.1 必要功能
- 單字輸入
- 圖片上傳
- OCR 文字辨識
- AI Agent 語言判定
- AI Agent 發音推論
- AI Agent 空耳候選生成
- 重複詞條檢查
- 顯示候選結果
- 使用者手動選擇
- 收藏詞條
- 查詢收藏詞條

### 2.2 暫不實作
- 文章匯入
- 音訊辨識
- 梗圖生成
- 社群功能
- 排行榜
- 測驗系統
- 熱門新番詞庫

---

## 3. 畫面設計（Mermaid）

### 3.1 畫面流程圖
```mermaid
flowchart TD
    A[首頁] --> B[單字輸入頁]
    A --> C[圖片上傳頁]

    B --> D[結果頁]
    C --> E[OCR 辨識中]
    E --> D

    D --> F[空耳候選顯示]
    F --> G[使用者選擇]
    G --> H[收藏成功]

    A --> I[收藏列表]
    I --> J[詞條詳情]
```

### 3.2 首頁畫面 Design
```mermaid
flowchart TD
    A[App 首頁] --> B[標題區]
    A --> C[主要功能區]
    A --> D[最近收藏區]

    B --> B1[APP 名稱]
    B --> B2[簡短說明]

    C --> C1[輸入單字按鈕]
    C --> C2[上傳圖片按鈕]
    C --> C3[收藏列表按鈕]

    D --> D1[最近新增詞條 1]
    D --> D2[最近新增詞條 2]
    D --> D3[最近新增詞條 3]
```

### 3.3 單字輸入頁 Design
```mermaid
flowchart TD
    A[單字輸入頁] --> B[輸入框]
    A --> C[語言選擇]
    A --> D[送出按鈕]
    A --> E[清除按鈕]

    B --> B1[使用者輸入單字]
    C --> C1[英文]
    C --> C2[日文]
    C --> C3[韓文]
    C --> C4[其他]
```

### 3.4 圖片上傳頁 Design
```mermaid
flowchart TD
    A[圖片上傳頁] --> B[相簿選擇]
    A --> C[相機拍照]
    A --> D[圖片預覽]
    A --> E[送出辨識]

    D --> F[確認圖片是否正確]
```

### 3.5 結果頁 Design
```mermaid
flowchart TD
    A[結果頁] --> B[原文顯示]
    A --> C[語言與發音]
    A --> D[空耳候選 A/B/C]
    A --> E[相似度或推薦分數]
    A --> F[收藏按鈕]
    A --> G[返回按鈕]

    D --> D1[候選 1]
    D --> D2[候選 2]
    D --> D3[候選 3]
```

---

## 4. 畫面流程圖（User Flow）

```mermaid
flowchart TD
    A[啟動 APP] --> B[首頁]
    B --> C[輸入單字]
    B --> D[上傳圖片]
    B --> E[查看收藏]

    C --> F[查重]
    D --> G[OCR 辨識]
    G --> F

    F --> H{是否已存在}
    H -- 是 --> I[顯示既有詞條]
    H -- 否 --> J[AI Agent 分析]

    J --> K[語言判定]
    J --> L[發音推論]
    J --> M[空耳候選生成]

    I --> N[結果頁]
    K --> N
    L --> N
    M --> N

    N --> O[使用者選擇候選]
    O --> P[收藏]
    P --> E
```

---

## 5. 系統架構圖

```mermaid
flowchart LR
    U[使用者手機 APP] --> API[後端 API]
    API --> DB[(MS SQL 資料庫)]
    API --> OCR[OCR 服務]
    API --> AI[AI Agent]
    API --> ST[物件儲存]
```

---

## 6. 資料表草案（ERD）

```mermaid
erDiagram
    USER ||--o{ VOCABULARY_ENTRY : creates
    VOCABULARY_ENTRY ||--o{ MISHEARING_CANDIDATE : has
    VOCABULARY_ENTRY ||--o{ UPLOAD_FILE : has
    VOCABULARY_ENTRY ||--o{ ENTRY_TAG : mapped
    TAG ||--o{ ENTRY_TAG : mapped
    VOCABULARY_ENTRY ||--o{ AGENT_LOG : generates

    USER {
        int UserId PK
        string Account
        string DisplayName
        datetime CreatedAt
    }

    VOCABULARY_ENTRY {
        int EntryId PK
        int UserId FK
        string SourceText
        string LanguageCode
        string Pronunciation
        string SourceType
        string Status
        datetime CreatedAt
        datetime UpdatedAt
        bit IsActive
    }

    MISHEARING_CANDIDATE {
        int CandidateId PK
        int EntryId FK
        string CandidateText
        string CandidateType
        decimal Score
        bit IsSelected
        datetime CreatedAt
    }

    UPLOAD_FILE {
        int FileId PK
        int EntryId FK
        string FilePath
        string FileType
        string OCRText
        datetime CreatedAt
    }

    TAG {
        int TagId PK
        string TagName
    }

    ENTRY_TAG {
        int EntryId FK
        int TagId FK
    }

    AGENT_LOG {
        int LogId PK
        int EntryId FK
        string StepName
        string RequestJson
        string ResponseJson
        datetime CreatedAt
    }
```

### 6.1 資料表重點
- `VOCABULARY_ENTRY`：詞條主檔
- `MISHEARING_CANDIDATE`：空耳候選
- `UPLOAD_FILE`：圖片與 OCR 結果
- `TAG` / `ENTRY_TAG`：標籤分類
- `AGENT_LOG`：AI 處理紀錄

---

## 7. 狀態轉移圖

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> TextReady: 單字輸入 / OCR 完成
    TextReady --> Analyzed: AI 完成分析
    Analyzed --> CandidateGenerated: 產生空耳候選
    CandidateGenerated --> Confirmed: 使用者選定
    Confirmed --> Saved: 寫入資料庫
    Saved --> Archived: 停用 / 刪除
```

---

## 8. 循序圖

```mermaid
sequenceDiagram
    actor User as 使用者
    participant App as APP
    participant API as 後端 API
    participant OCR as OCR 服務
    participant AI as AI Agent
    participant DB as 資料庫

    User->>App: 輸入單字 / 上傳圖片
    App->>API: 送出請求

    alt 圖片輸入
        API->>OCR: 辨識圖片文字
        OCR-->>API: 回傳文字
    end

    API->>DB: 查詢是否重複
    DB-->>API: 回傳結果

    alt 已存在
        API-->>App: 回傳既有詞條
    else 不存在
        API->>AI: 執行語言判定 / 發音推論 / 空耳生成
        AI-->>API: 回傳候選結果
        API-->>App: 顯示候選
        User->>App: 選擇候選
        App->>API: 儲存選擇
        API->>DB: 寫入詞條與候選
        DB-->>API: 儲存成功
        API-->>App: 完成
    end
```

---

## 9. 最小硬體支援列表

> 這是「小型 MVP、可上架」的最低建議，不是重型平台配置。

### 9.1 APP 端
- Android 手機
- Android 10 以上優先
- RAM 4GB 以上可順暢使用
- 儲存空間預留 200MB 以上
- 支援相機、相簿、網路連線

### 9.2 後端服務
- 4 vCPU
- 8 GB RAM
- 80 GB SSD
- Linux 或 Windows Server 皆可
- 角色：API、AI 流程協調、查重、紀錄寫入

### 9.3 資料庫服務
- 4 vCPU
- 8 GB RAM
- 100 GB SSD
- MS SQL Server
- 角色：詞條、候選、收藏、日誌

### 9.4 外部服務
- OCR API
- AI API
- 物件儲存空間
- 不建議 MVP 階段自架大型模型

---

## 10. 整體 Cost（MVP 估算）

> 以下為小型 MVP 的估算方向，目的是抓預算級距，不是精算報價。

### 10.1 一次性開發成本
| 項目 | 說明 | 估算區間 |
|---|---|---:|
| UI/UX | 4 個主要畫面 | 3 ~ 8 萬 |
| APP 前端開發 | Android App | 8 ~ 20 萬 |
| 後端 API | 查重、收藏、流程 | 8 ~ 18 萬 |
| OCR / AI 串接 | 圖片辨識、候選產生 | 6 ~ 15 萬 |
| DB 設計 | 資料表、索引、查重 | 3 ~ 8 萬 |
| 測試與修正 | 功能測試、上架修正 | 3 ~ 10 萬 |

**一次性總成本約：31 ~ 79 萬**

### 10.2 每月維運成本
| 項目 | 說明 | 估算區間 |
|---|---|---:|
| App Server | 4C/8G VM | 1,500 ~ 4,000 |
| DB Server | 4C/8G VM | 2,000 ~ 5,000 |
| 物件儲存 | 圖片存放 | 200 ~ 800 |
| OCR API | 依使用量 | 500 ~ 5,000 |
| AI API | 依使用量 | 1,000 ~ 8,000 |
| Log / 監控 | 基本監控 | 300 ~ 1,000 |

**每月總成本約：5,500 ~ 23,800**

### 10.3 Google Play 與其他費用
- Google Play 開發者帳號：一次性註冊費
- 網域名稱：每年費用
- 若需正式上線，需預留客服與維護人力成本

---

## 11. 部署圖

```mermaid
flowchart LR
    User[使用者手機] --> Play[Google Play 上架 APP]
    Play --> API[後端 API 伺服器]
    API --> DB[(MS SQL)]
    API --> ST[物件儲存]
    API --> OCR[外部 OCR]
    API --> AI[外部 AI]

    API --> LOG[監控 / Log]
    DB --> BK[備份]
```

---

## 12. 部署維運重點
- 使用 App Bundle 上架
- 控制 App 體積
- 圖片與 AI 分離處理
- 每日備份資料庫
- 記錄 AI 回應與 OCR 結果
- 對失敗請求做重試與提示
- 保留使用者選擇紀錄，方便後續優化

---

## 13. 上架注意事項
- 新 App 需符合 Google Play 的相容性要求
- 需注意 16 KB page size 支援
- 不要把大型模型包進 App
- 檔案上傳、OCR、AI 分離處理，避免安裝包過大
- 優先採用 Android App Bundle

---

## 14. MVP 驗收標準
- 可輸入單字
- 可上傳圖片
- 可成功 OCR
- 可產生空耳候選
- 可查重
- 可收藏
- 可查詢收藏
- 可穩定上架 Google Play

---

## 15. 結論
這版 MVP 的重點是：
- **小**
- **穩**
- **可上架**
- **可擴充**

先把主流程做通，再考慮多語言、社群、進階學習功能。

---
1. **再更精簡成 1 頁提案版**
2. **轉成 PRD 正式規格格式**
3. **補上 API 規格表**
4. **補上資料庫 SQL DDL**
5. **補上 Android 畫面 wireframe 清單**