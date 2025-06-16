using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace KINSUS
{
    /// <summary>
    /// ApiGuideWindow.xaml 的互動邏輯
    /// </summary>
    public partial class ApiGuideWindow : Window
    {
        public ApiGuideWindow()
        {
            InitializeComponent();
            LoadMarkdownContent();
        }        
        private void LoadMarkdownContent()
        {
            try
            {
                // 直接載入 KINSUS 通訊規格文件
                string docPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "配針機通訊規範.md");
                

                if (File.Exists(docPath))
                {
                    // 指定 UTF-8 編碼讀取檔案
                    string content = File.ReadAllText(docPath, System.Text.Encoding.UTF8);
                    DisplayMarkdown(content);
                }
                else
                {
                    DisplayBuiltInApiGuide();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入文件時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                DisplayBuiltInApiGuide();
            }
        }

        private void DisplayBuiltInApiGuide()
        {
            string apiGuideContent = @"# KINSUS 配針機 API 測試指南

## 📚 API 功能總覽

### 🔧 伺服端角色 API（接收 MES/IoT 系統指令）

#### 1. 遠程資訊下發指令 (SEND_MESSAGE_COMMAND)
- **功能**: 接收來自 MES/IoT 系統的訊息
- **HTTP 方法**: POST
- **端點**: http://localhost:8085/api/send-message
- **範例請求**:
```json
{
  ""requestID"": ""MSG_CMD_001"",
  ""serviceName"": ""SEND_MESSAGE_COMMAND"",
  ""timeStamp"": ""2025-06-14 16:00:00"",
  ""devCode"": ""KINSUS001"",
  ""operator"": ""OP001"",
  ""data"": [
    {
      ""message"": ""請補充刀具庫存！"",
      ""level"": ""warning"",
      ""priority"": ""high"",
      ""actionType"": 1,
      ""intervalSecondTime"": 30
    }
  ]
}
```

#### 2. 派針工單建立指令 (CREATE_NEEDLE_WORKORDER_COMMAND)
- **功能**: 建立新的配針工單
- **HTTP 方法**: POST
- **端點**: http://localhost:8085/api/create-workorder
- **範例請求**:
```json
{
  ""requestID"": ""WO_CREATE_001"",
  ""serviceName"": ""CREATE_NEEDLE_WORKORDER_COMMAND"",
  ""timeStamp"": ""2025-06-14 16:00:00"",
  ""devCode"": ""KINSUS001"",
  ""AllPlate"": 300,
  ""Pressplatens"": 2,
  ""operator"": ""OP001"",
  ""data"": [
    {
      ""taskID"": ""TaskID1"",
      ""workOrder"": ""Workorder1"",
      ""tPackage"": ""TPackage1"",
      ""stackCount"": 1,
      ""totalSheets"": 300,
      ""startTime"": ""2025-06-14 16:00:00"",
      ""endTime"": ""2025-06-14 20:00:00"",
      ""priority"": ""normal"",
      ""isUrgent"": false
    }
  ]
}
```

#### 3. 設備時間同步指令 (DATE_MESSAGE_COMMAND)
- **功能**: 同步設備時間
- **HTTP 方法**: POST
- **端點**: http://localhost:8085/api/sync-time

#### 4. 刀具工鑽袋檔發送指令 (SWITCH_RECIPE_COMMAND)
- **功能**: 更新刀具配方
- **HTTP 方法**: POST
- **端點**: http://localhost:8085/api/switch-recipe

#### 5. 設備啟停控制指令 (DEVICE_CONTROL_COMMAND)
- **功能**: 控制設備啟動/停止
- **HTTP 方法**: POST
- **端點**: http://localhost:8085/api/device-control

#### 6. 倉庫資源查詢指令 (WAREHOUSE_RESOURCE_QUERY_COMMAND)
- **功能**: 查詢倉庫資源狀態
- **HTTP 方法**: POST
- **端點**: http://localhost:8085/api/warehouse-query

#### 7. 鑽針履歷查詢指令 (TOOL_TRACE_HISTORY_QUERY_COMMAND)
- **功能**: 查詢鑽針使用履歷
- **HTTP 方法**: POST
- **端點**: http://localhost:8085/api/tool-history

### 📤 用戶端角色 API（主動上報至 MES/IoT 系統）

#### 1. 配針回報上傳 (TOOL_OUTPUT_REPORT_MESSAGE)
- **功能**: 上報配針完成資訊
- **HTTP 方法**: POST
- **目標**: MES/IoT 系統端點

#### 2. 錯誤回報上傳 (ERROR_REPORT_MESSAGE)
- **功能**: 上報系統錯誤資訊
- **HTTP 方法**: POST
- **目標**: MES/IoT 系統端點

#### 3. 機臺狀態上報 (MACHINE_STATUS_REPORT_MESSAGE)
- **功能**: 上報機臺運行狀態
- **HTTP 方法**: POST
- **目標**: MES/IoT 系統端點

## 🧪 Postman 測試步驟

### 1. 環境設定
- 建立新的 Postman Collection: ""KINSUS API Tests""
- 設定基礎 URL 變量: {{baseURL}} = http://localhost:8085

### 2. 伺服端 API 測試
1. 確保 KINSUS 應用程式已啟動伺服器模式
2. 伺服器監聽位址: http://localhost:8085/
3. 選擇對應的 API 端點進行測試
4. 設定 Content-Type: application/json
5. 在 Body 中貼上對應的 JSON 範例

### 3. 用戶端 API 測試
1. 設定目標 MES/IoT 系統端點
2. 在 KINSUS 應用程式中設定 IoT 端點位址
3. 選擇對應的 API 功能並發送請求

## 📋 測試檢查清單

✅ 伺服端模式啟動成功
✅ 端口 8085 正常監聽
✅ JSON 格式驗證通過
✅ 所有必要欄位已填寫
✅ 時間戳記格式正確
✅ 設備代碼 (devCode) 設定
✅ 操作員 (operator) 資訊填寫
✅ 回應狀態碼檢查 (200 OK)
✅ 錯誤處理測試

## 🔍 故障排除

### 常見問題
1. **連線失敗**: 檢查伺服器是否啟動
2. **JSON 格式錯誤**: 驗證 JSON 語法
3. **端口占用**: 更改監聽端口
4. **編碼問題**: 確保使用 UTF-8 編碼

### 錯誤代碼
- 400: 請求格式錯誤
- 404: API 端點不存在
- 500: 伺服器內部錯誤
- 503: 服務暫時無法使用
";
            DisplayMarkdown(apiGuideContent);
        }

        private void DisplayMarkdown(string content)
        {
            var document = new FlowDocument();
            
            // 將 Markdown 內容處理成簡易格式的文字
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            foreach (string line in lines)
            {
                Paragraph para = new Paragraph();
                
                if (line.StartsWith("# "))
                {
                    // 一級標題
                    para.FontSize = 24;
                    para.FontWeight = FontWeights.Bold;
                    para.Foreground = Brushes.DarkBlue;
                    para.Margin = new Thickness(0, 10, 0, 5);
                    para.Inlines.Add(line.Substring(2));
                }
                else if (line.StartsWith("## "))
                {
                    // 二級標題
                    para.FontSize = 20;
                    para.FontWeight = FontWeights.Bold;
                    para.Foreground = Brushes.DarkGreen;
                    para.Margin = new Thickness(0, 8, 0, 4);
                    para.Inlines.Add(line.Substring(3));
                }
                else if (line.StartsWith("**") && line.EndsWith("**"))
                {
                    // 粗體文字
                    para.FontWeight = FontWeights.Bold;
                    para.Margin = new Thickness(0, 5, 0, 2);
                    para.Inlines.Add(line.Substring(2, line.Length - 4));
                }
                else if (line.StartsWith("```") && !line.EndsWith("```"))
                {
                    // 程式碼區塊開始
                    para.Background = Brushes.LightGray;
                    para.FontFamily = new FontFamily("Consolas");
                    para.Margin = new Thickness(10, 5, 10, 0);
                    if (line.Length > 3)
                    {
                        para.Foreground = Brushes.DarkRed;
                        para.Inlines.Add(line.Substring(3));
                    }
                }
                else if (line.StartsWith("```"))
                {
                    // 程式碼區塊結束
                    para.Background = Brushes.LightGray;
                    para.FontFamily = new FontFamily("Consolas");
                    para.Margin = new Thickness(10, 0, 10, 5);
                }
                else if (line.StartsWith("- "))
                {
                    // 列表項目
                    para.Margin = new Thickness(20, 2, 0, 2);
                    para.Inlines.Add("• " + line.Substring(2));
                }
                else if (line.StartsWith("  - "))
                {
                    // 縮排列表項目
                    para.Margin = new Thickness(40, 2, 0, 2);
                    para.Inlines.Add("◦ " + line.Substring(4));
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // 空行
                    para.Margin = new Thickness(0, 4, 0, 4);
                }
                else
                {
                    // 普通文字
                    if (line.StartsWith("```") || para.Background == Brushes.LightGray)
                    {
                        para.Background = Brushes.LightGray;
                        para.FontFamily = new FontFamily("Consolas");
                        para.Margin = new Thickness(10, 0, 10, 0);
                        if (!line.EndsWith("```"))
                        {
                            para.Inlines.Add(line);
                        }
                    }
                    else
                    {
                        para.Inlines.Add(line);
                    }
                }

                document.Blocks.Add(para);
            }
            
            rtbContent.Document = document;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
