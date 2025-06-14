using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.WinForms;

namespace KINSUS
{    /// <summary>
    /// FlowChartWindow.xaml 的互動邏輯
    /// </summary>
    public partial class FlowChartWindow : Window
    {
        public FlowChartWindow()
        {
            InitializeComponent();
            InitializeWebView2Async();
            LoadMarkdownContent();
        }

        private async void InitializeWebView2Async()
        {
            // 初始化 WebView2 環境
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null, Path.GetTempPath(), null);
                await webViewMermaid.EnsureCoreWebView2Async(env);
                
                // 設定 WebView2 的安全策略
                webViewMermaid.CoreWebView2.Settings.IsScriptEnabled = true;
                webViewMermaid.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化 WebView2 元件時發生錯誤：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        private void LoadMarkdownContent()
        {
            try
            {
                // 載入內嵌的 KINSUS 流程圖說明
                string flowChartDescription = @"# KINSUS 配針機系統流程圖

## 系統架構說明

KINSUS 配針機系統支援三種操作模式：

### 1. 伺服端模式（接收 MES/IoT 指令）
- 啟動 HTTP 伺服器，監聽端口 8085
- 接收並處理來自 MES/IoT 系統的各種指令
- 支援的 API 指令：
  - SEND_MESSAGE_COMMAND（遠程資訊下發）
  - CREATE_NEEDLE_WORKORDER_COMMAND（派針工單建立）
  - DATE_MESSAGE_COMMAND（設備時間同步）
  - SWITCH_RECIPE_COMMAND（刀具工鑽袋檔發送）
  - DEVICE_CONTROL_COMMAND（設備啟停控制）
  - WAREHOUSE_RESOURCE_QUERY_COMMAND（倉庫資源查詢）
  - TOOL_TRACE_HISTORY_QUERY_COMMAND（鑽針履歷查詢）

### 2. 用戶端模式（主動上報至 MES/IoT）
- 主動向 MES/IoT 系統發送狀態與資訊
- 支援的上報功能：
  - TOOL_OUTPUT_REPORT_MESSAGE（配針回報上傳）
  - ERROR_REPORT_MESSAGE（錯誤回報上傳）
  - MACHINE_STATUS_REPORT_MESSAGE（機臺狀態上報）

### 3. 雙向模式
- 同時支援伺服端和用戶端功能
- 可接收指令並主動上報狀態

## 工作流程

1. **系統啟動** → 選擇操作模式
2. **伺服端流程**：啟動伺服器 → 等待指令 → 處理指令 → 回應結果
3. **用戶端流程**：設定端點 → 選擇功能 → 發送請求 → 接收回應
4. **資料交換**：所有通訊採用 JSON 格式，支援擴充欄位

## 技術特色

- **高延展性**：支援平行處理多組作業
- **彈性配置**：可依需求調整操作模式
- **完整記錄**：支援履歷追溯與狀態監控
- **標準介面**：採用 RESTful API 設計
";
                
                txtFlowChartMarkdown.Text = flowChartDescription;
                
                // 載入 Mermaid 流程圖
                LoadMermaidFlowChart();                // 預設顯示 Markdown 文字說明
                txtFlowChartMarkdown.Visibility = Visibility.Visible;
                imgFlowChart.Visibility = Visibility.Collapsed;
                webViewMermaid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入流程圖說明時發生錯誤：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void LoadMermaidFlowChart()
        {
            try
            {
                // 從 Markdown 文字中提取 Mermaid 語法
                string htmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Html", "MainFlow.html");
               
                // 建立 HTML 內容，包含 Mermaid.js 函式庫和流程圖程式碼
              
                // 檢查 WebView2 是否已初始化完成，如果沒有，等待初始化完成
                if (webViewMermaid.CoreWebView2 == null)
                {
                    // 等待 WebView2 初始化完成 (應該是在 InitializeWebView2Async 方法中已經進行)
                    await webViewMermaid.EnsureCoreWebView2Async();

                    // 如果仍未初始化，顯示錯誤訊息
                    if (webViewMermaid.CoreWebView2 == null)
                    {
                        MessageBox.Show("WebView2 元件尚未初始化完成，無法顯示流程圖。請稍後重試。", "初始化錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                webViewMermaid.CoreWebView2.Navigate(new Uri(htmlFilePath).AbsoluteUri);
               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入 Mermaid 流程圖時發生錯誤：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
 

        private enum DisplayMode
        {
            Markdown,
            StaticImage,
            MermaidRendered
        }

        private DisplayMode currentMode = DisplayMode.Markdown;        private void btnShowFlowChart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 在文字說明和視覺化流程圖之間切換
                switch (currentMode)
                {
                    case DisplayMode.Markdown:
                        // 切換到 Mermaid 視覺化流程圖
                        txtFlowChartMarkdown.Visibility = Visibility.Collapsed;
                        webViewMermaid.Visibility = Visibility.Visible;
                        imgFlowChart.Visibility = Visibility.Collapsed;
                        btnShowFlowChart.Content = "顯示文字說明";
                        currentMode = DisplayMode.MermaidRendered;
                        break;

                    case DisplayMode.MermaidRendered:
                        // 切換回文字說明
                        txtFlowChartMarkdown.Visibility = Visibility.Visible;
                        webViewMermaid.Visibility = Visibility.Collapsed;
                        imgFlowChart.Visibility = Visibility.Collapsed;
                        btnShowFlowChart.Content = "顯示視覺化流程圖";
                        currentMode = DisplayMode.Markdown;
                        break;

                    case DisplayMode.StaticImage:
                        // 如果目前是靜態圖片模式，切換到 Markdown
                        txtFlowChartMarkdown.Visibility = Visibility.Visible;
                        webViewMermaid.Visibility = Visibility.Collapsed;
                        imgFlowChart.Visibility = Visibility.Collapsed;
                        btnShowFlowChart.Content = "顯示視覺化流程圖";
                        currentMode = DisplayMode.Markdown;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切換顯示模式時發生錯誤：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}