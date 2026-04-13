# Bugzilla Dumper

**v0.12** — .NET 10 WPF Windows App

Bugzilla REST API 瀏覽與資料匯出工具，支援 Bug 清單瀏覽、完整內文查看，以及匯出為 JSON / Excel。

---

## 功能

### 連線設定
- 輸入 Bugzilla URL 與 API Key
- 設定自動儲存至 `%AppData%\BugzillaDumper\settings.json`，下次開啟自動載入

### Bug 搜尋
| 篩選欄位 | 說明 |
|---------|------|
| Product | 產品名稱 |
| Component | 元件名稱 |
| Status | 勾選 CONFIRMED / IN_PROGRESS / RESOLVED / REOPENED |
| Assigned To | 指派人員 email |
| Summary Keyword | 標題關鍵字 |
| Limit | 筆數上限，填 `0` 自動分頁抓取全部 |

> Limit = 0 時每頁抓 500 筆，自動循環至最後一頁。

### Bug 清單
- DataGrid 顯示，狀態色彩標籤（NEW=藍、ASSIGNED=黃、RESOLVED=綠、REOPENED=紅）
- 點選任一行自動載入完整 Bug Detail（含 Comments）

### Bug Detail
- 完整欄位（ID、Summary、Status、Priority、Severity、Component、Product、Assigned To、Creator、Whiteboard 等）
- 依序顯示所有 Comments（作者、時間、內文）

### 匯出

#### List Only（Bug 清單）
| 格式 | 內容 |
|------|------|
| JSON | Bug 清單陣列 |
| Excel | 單一 Sheet，含 AutoFilter |

#### Full Dump（清單 + 完整內文 + Comments）
| 格式 | 內容 |
|------|------|
| JSON | `BugDetail[]`，每筆含 comments 陣列，中文直接輸出（不 escape） |
| Excel | 3 個 Sheet：`Bugs`（清單）/ `Details`（完整欄位）/ `Comments`（所有 comments，含 Bug ID 欄） |

> Full Dump 會逐筆呼叫 API，執行中顯示進度條，可隨時按 Cancel 中止。

#### Detail（單筆）
點選 Bug 後，Detail 區右上角可單獨匯出該筆 Bug：
- JSON：完整欄位 + Comments
- Excel：2 個 Sheet（`Bug Info` / `Comments`）

---

## 建置與部署

### 環境需求
- .NET 10 SDK
- Windows x64

### 建置
```bash
dotnet build
```

### 發佈（Single-file exe + 自動部署）
```bash
dotnet publish -c Release
```

Publish 完成後自動複製至：
```
\\j-nas01\部門_研發二部\QA\tools\BugzillaDumper\BugzillaDumper.exe
```

輸出的 exe 為 single-file self-contained，無需安裝 .NET Runtime 即可執行，約 144 MB。

---

## 專案結構

```
BugzillaDumper/
├── Models/
│   └── BugzillaModels.cs       # BugSummary, BugDetail, BugComment, SearchCriteria 等
├── Services/
│   ├── BugzillaService.cs      # Bugzilla REST API 呼叫（含自動分頁）
│   ├── ExportService.cs        # JSON / Excel 匯出
│   └── SettingsService.cs      # 連線設定讀寫（%AppData%）
├── ViewModels/
│   └── MainViewModel.cs        # MVVM ViewModel
├── Converters/
│   └── Converters.cs           # WPF Value Converters
├── AppVersion.cs               # 版號集中管理
├── MainWindow.xaml             # 主畫面 UI
└── App.xaml.cs                 # 啟動 + 全域 Exception Handler
```

---

## 錯誤處理

程式啟動時自動掛載三層 exception handler，crash 時：
1. 彈出對話框顯示錯誤訊息
2. 寫入完整 stack trace 至 `%AppData%\BugzillaDumper\crash.log`

---

## 版本記錄

| 版本 | 變更 |
|------|------|
| 0.12 | Status 改為 CheckBox 勾選（CONFIRMED / IN_PROGRESS / RESOLVED / REOPENED） |
| 0.11 | 修正 Status 多值 filter 失效（NameValueCollection 合併 key 問題） |
| 0.10 | 初始版本：Bug 搜尋、Detail、JSON/Excel 匯出、Full Dump、自動分頁、部署 target |
