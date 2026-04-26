# Bugzilla Dumper

**v0.19** — .NET 10 + Avalonia UI（跨平台：Windows / macOS / Linux）

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

### 匯出與匯入

#### 資料匯出 (Export)
| 格式 | 內容 |
|------|------|
| JSON | Bug 清單或詳細資料（含 Comments） |
| Excel | 格式化報表，支援 AutoFilter |
| 資料夾 | 下載所有附件並依 Bug ID 分類儲存 |

#### GitLab 匯入 (Import)
- 支援將選定的 Bug 批次匯入 GitLab Issues。
- 自動處理圖片附件：將 Bugzilla 圖片上傳至 GitLab 並嵌入描述。
- **Google Drive 影片整合**：
    - 偵測影片格式附件 (`.mp4`, `.avi`, `.mov` 等)。
    - 透過 OAuth 2.0 上傳至公司指定的 Google Drive 資料夾（支援 Google Workspace 共用雲端硬碟）。
    - 上傳成功後自動於 GitLab Issue 描述中插入影片分享連結。

---

## 設定指南

### GitLab & Google Drive 設定
1. **GitLab**:
   - 提供 GitLab Base URL (例如 `https://gitlab.com`)。
   - 建立 [Personal Access Token](https://gitlab.com/-/profile/personal_access_tokens) 並賦予 `api` 權限。
   - 輸入專案路徑 (例如 `namespace/project`)。

2. **Google Drive (影片上傳)**:
   - 到 [Google Cloud Console](https://console.cloud.google.com/) 建立專案並啟用 **Google Drive API**。
   - 建立 **OAuth 2.0 Client ID (桌面應用程式)**。
   - 將 Client ID 與 Client Secret 填入 App 設定中。
   - (選填) FOLDER ID: 指定上傳的資料夾 ID（支援共用雲端硬碟）。
   - **首次匯入含影片的 Bug 時**，App 會自動開啟瀏覽器要求 Google 帳號授權。

---

## 建置與部署

### 環境需求
- .NET 10 SDK
- Google Drive API 套件 (`Google.Apis.Drive.v3`)
- Windows x64 / macOS arm64 / macOS x64 / Linux x64

### 建置
```bash
dotnet build BugzillaDumper/BugzillaDumper.csproj
```

### 發佈（Single-file 自包式可執行檔）
```bash
# Windows x64（同時自動複製到部門 NAS）
dotnet publish BugzillaDumper/BugzillaDumper.csproj -c Release -r win-x64

# macOS Apple Silicon
dotnet publish BugzillaDumper/BugzillaDumper.csproj -c Release -r osx-arm64

# macOS Intel
dotnet publish BugzillaDumper/BugzillaDumper.csproj -c Release -r osx-x64
```

Windows publish 完成後自動複製至：
```
\\j-nas01\部門_研發二部\QA\tools\BugzillaDumper\BugzillaDumper.exe
```
（其他平台的 publish 不會觸發此步驟。）

輸出為 single-file self-contained，無需安裝 .NET Runtime 即可執行（Windows 約 110 MB、macOS 約 116 MB）。

### 平台差異
- **自動更新**：僅 Windows 支援（透過共用資料夾 `\\j-nas01\...`）。macOS / Linux 上會跳過更新檢查。
- **匯出對話框**：macOS 使用原生 NSSavePanel，其他平台使用各自原生對話框。
- **「含附件」匯出後開啟資料夾**：Windows 用 explorer、macOS 用 `open`、Linux 用 `xdg-open`。
- **Google 授權**：桌面版 OAuth 會在本地啟動監聽埠接收授權碼。

---

## 專案結構

```
BugzillaDumper/
├── Models/
│   ├── BugzillaModels.cs           # BugSummary, BugDetail, BugComment, SearchCriteria, AppSettings
│   └── GitLabModels.cs             # GitLab 匯入相關 model
├── Services/
│   ├── BugzillaService.cs          # Bugzilla REST API 呼叫（含自動分頁）
│   ├── GitLabService.cs            # GitLab 匯入 API 呼交
│   ├── GoogleDriveService.cs       # Google Drive OAuth 2.0 上傳（支援共用雲端硬碟）
│   ├── ExportService.cs            # JSON / Excel 匯出
│   ├── SettingsService.cs          # 連線設定讀寫（跨平台用 user app data 路徑）
│   ├── UpdateService.cs            # 自動更新（Windows-only，其他平台 no-op）
│   ├── IFilePickerService.cs       # 匯出檔對話框抽象
│   ├── AvaloniaFilePickerService.cs# Avalonia StorageProvider 實作
│   └── PlatformHelpers.cs          # 跨平台「開啟資料夾」
├── ViewModels/
│   └── MainViewModel.cs            # MVVM ViewModel，處理匯入與上傳邏輯
├── Converters/
│   └── Converters.cs               # Avalonia Value Converters
├── AppVersion.cs                   # 版號集中管理
├── Program.cs                      # Avalonia 進入點
├── App.axaml / App.axaml.cs        # Application 樣式 + 啟動邏輯
└── MainWindow.axaml / .axaml.cs    # 主畫面 UI
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
| 0.20 | 新增 GitLab 匯入功能與 Google Drive 影片整合（支援 OAuth 2.0 與共用雲端硬碟） |
| 0.12 | Status 改為 CheckBox 勾選（CONFIRMED / IN_PROGRESS / RESOLVED / REOPENED） |
| 0.11 | 修正 Status 多值 filter 失效（NameValueCollection 合併 key 問題） |
| 0.10 | 初始版本：Bug 搜尋、Detail、JSON/Excel 匯出、Full Dump、自動分頁、部署 target |
