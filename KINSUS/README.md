# KINSUS 通訊系統

> 配針機通訊整合系統 - 用於配針機與 MES/IoT 系統之間的通訊協調

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2019+-purple.svg)](https://visualstudio.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 📋 專案概述

KINSUS 通訊系統是一個基於 WPF 的桌面應用程式，專門用於管理配針機與 MES/IoT 系統之間的通訊協調。系統提供完整的 API 管理、流程圖展示、以及設備狀態監控功能。

### 🎯 主要功能

- **API 通訊管理** - 支援雙向 API 通訊（伺服端/用戶端角色）
- **視覺化流程圖** - 使用 Mermaid.js 展示工作流程
- **設備狀態監控** - 即時監控配針機狀態
- **工單管理** - 配針工單建立與追蹤
- **倉庫資源查詢** - 查詢倉庫資源狀態
- **鑽針履歷追蹤** - 完整的鑽針使用履歷記錄

## 🏗️ 系統架構

```text
┌─────────────────┐    HTTP API    ┌─────────────────┐
│   MES/IoT 系統   │ ◄─────────────► │  KINSUS 通訊系統  │
└─────────────────┘                └─────────────────┘
                                           │
                                           ▼
                                   ┌─────────────────┐
                                   │     配針機      │
                                   └─────────────────┘
```

### 🔧 技術棧

- **前端框架**: WPF (Windows Presentation Foundation)
- **程式語言**: C# (.NET Framework 4.8)
- **HTTP 用戶端**: HttpClient
- **JSON 處理**: Newtonsoft.Json
- **Web 檢視**: Microsoft WebView2
- **圖表展示**: Mermaid.js

## 📦 安裝與設定

### 前置需求

- Windows 10 或更高版本
- .NET Framework 4.8
- Visual Studio 2019 或更高版本（開發用）
- Microsoft Edge WebView2 Runtime

### 安裝步驟

1. **複製專案**

   ```bash
   git clone https://github.com/Po-Yu-Chang/KINSUSCommuncation.git
   cd KINSUSCommuncation
   ```

2. **還原 NuGet 套件**

   ```bash
   nuget restore KINSUS.sln
   ```

3. **建構專項**

   ```bash
   msbuild KINSUS.sln /p:Configuration=Release
   ```

4. **設定檔案**

   - 複製 `setting.ini.example` 為 `setting.ini`
   - 修改設定檔中的 API 端點和認證資訊

## 🚀 使用方法

### 啟動應用程式

1. 執行 `bin/Release/OthinCloud.exe`
2. 系統將自動載入設定檔案
3. 檢查 API 連線狀態

### 主要操作

#### 1. API 測試與管理

- 點選「API 指南」按鈕開啟 API 測試介面
- 選擇對應的 API 範本
- 填入參數並執行測試

#### 2. 流程圖檢視

- 點選「流程圖」按鈕檢視工作流程
- 支援互動式流程圖導覽

#### 3. 狀態監控

- 主介面即時顯示設備狀態
- 支援自動重新整理

## 📖 API 文件

### 伺服端角色 API（MES/IoT → 配針機）

| API | 功能 | 端點 |
|-----|------|------|
| `send_message_command` | 遠程資訊下發 | `/api/v1/send_message` |
| `create_needle_workorder_command` | 派針工單建立 | `/api/v1/create_workorder` |
| `date_message_command` | 設備時間同步 | `/api/v1/sync_time` |
| `switch_recipe_command` | 刀具工鑽袋檔發送 | `/api/v1/switch_recipe` |
| `device_control_command` | 設備啟停控制 | `/api/v1/device_control` |

### 用戶端角色 API（KINSUS → MES/IoT）

| API | 功能 | 端點 |
|-----|------|------|
| `tool_output_report_message` | 配針回報上傳 | `/api/v1/tool_report` |
| `error_report_message` | 錯誤回報上傳 | `/api/v1/error_report` |
| `machine_status_report_message` | 機臺狀態上報 | `/api/v1/status_report` |

詳細的 API 規格請參考：[KINSUS通訊_整理版.md](Document/KINSUS通訊_整理版.md)

## 📂 專案結構

```text
KINSUS/
├── API/                    # API 相關程式碼
│   ├── ApiClient.cs       # HTTP 用戶端
│   ├── HttpServer.cs      # HTTP 伺服器
│   └── IniManager.cs      # 設定檔管理
├── Model/                 # 資料模型
│   ├── ApiDataModels.cs   # API 資料模型
│   ├── BaseRequest.cs     # 基礎請求模型
│   └── BaseResponse.cs    # 基礎回應模型
├── Document/              # 文件資料
├── Html/                  # HTML 檔案
├── Templates/             # API 範本
├── Scripts/               # JavaScript 檔案
└── Image/                 # 圖片資源
```

## ⚙️ 設定檔說明

### setting.ini 設定項目

```ini
[API]
BaseUrl=http://localhost:8080
ApiKey=your_api_key_here
Timeout=30000

[Server]
Port=8081
EnableHttps=false

[Logging]
LogLevel=Info
LogPath=./logs/
```

## 🔧 開發指南

### 新增 API 端點

1. 在 `API/ApiClient.cs` 中新增對應方法
2. 在 `Model/` 目錄下建立相應的資料模型
3. 更新 `Templates/ApiTemplates.json` 範本檔案
4. 在主介面新增對應的 UI 控制項

### 自訂流程圖

1. 修改 `Html/MainFlow.html` 檔案
2. 使用 Mermaid.js 語法建立流程圖
3. 更新 `Scripts/mermaid.min.js` （如需要）

## 🐛 故障排除

### 常見問題

1. **API 連線失敗**
   - 檢查網路連線
   - 確認 API 端點設定正確
   - 檢查防火牆設定

2. **設定檔載入失敗**
   - 確認 `setting.ini` 檔案存在
   - 檢查檔案格式是否正確
   - 確認檔案權限

3. **WebView2 無法載入**
   - 安裝 Microsoft Edge WebView2 Runtime
   - 確認 .NET Framework 版本正確

### 記錄檔位置

- 應用程式記錄：`./logs/application.log`
- API 記錄：`./logs/api.log`
- 錯誤記錄：`./logs/error.log`

## 🤝 貢獻指南

歡迎提交 Pull Request 或回報問題！

1. Fork 此專案
2. 建立功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交變更 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 開啟 Pull Request

## 📄 授權條款

本專案採用 MIT 授權條款 - 詳見 [LICENSE](LICENSE) 檔案

## 📞 聯絡資訊

- 專案維護者：Po-Yu Chang
- GitHub：[@Po-Yu-Chang](https://github.com/Po-Yu-Chang)
- 專案連結：[https://github.com/Po-Yu-Chang/KINSUSCommuncation](https://github.com/Po-Yu-Chang/KINSUSCommuncation)

## 📈 版本紀錄

### v1.0.0 (2025-06-13)

- 初始版本發布
- 基本 API 通訊功能
- 流程圖展示功能
- 設備狀態監控

---

本文件最後更新：2025年6月13日
