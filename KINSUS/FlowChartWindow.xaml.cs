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
{
    /// <summary>
    /// FlowChartWindow.xaml 的互動邏輯
    /// </summary>
    public partial class FlowChartWindow : Window
    {

        private bool isImageVisible = false;        
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
        }


        private void LoadMarkdownContent()
        {
           
            // 載入 Mermaid 流程圖
            LoadMermaidFlowChart();

            // 預設顯示 Markdown 格式
            txtFlowChartMarkdown.Visibility = Visibility.Collapsed;
            imgFlowChart.Visibility = Visibility.Collapsed;
            webViewMermaid.Visibility = Visibility.Visible;
            isImageVisible = false;
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

        private DisplayMode currentMode = DisplayMode.Markdown;

        private void btnShowFlowChart_Click(object sender, RoutedEventArgs e)
        {
            // 在三種顯示模式之間循環切換：Markdown -> Mermaid 渲染 -> 靜態圖片 -> Markdown
            switch (currentMode)
            {
                case DisplayMode.Markdown:
                    // 切換到 Mermaid 渲染模式
                    txtFlowChartMarkdown.Visibility = Visibility.Collapsed;
                    webViewMermaid.Visibility = Visibility.Visible;
                    imgFlowChart.Visibility = Visibility.Collapsed;
                    btnShowFlowChart.Content = "顯示靜態圖片";
                    currentMode = DisplayMode.MermaidRendered;
                    break;

                case DisplayMode.MermaidRendered:
                    // 切換到靜態圖片模式
                    txtFlowChartMarkdown.Visibility = Visibility.Collapsed;
                    webViewMermaid.Visibility = Visibility.Collapsed;
                    imgFlowChart.Visibility = Visibility.Visible;
                    btnShowFlowChart.Content = "顯示 Markdown";
                    currentMode = DisplayMode.StaticImage;
                    break;

                case DisplayMode.StaticImage:
                    // 切換到 Markdown 模式
                    txtFlowChartMarkdown.Visibility = Visibility.Visible;
                    webViewMermaid.Visibility = Visibility.Collapsed;
                    imgFlowChart.Visibility = Visibility.Collapsed;
                    btnShowFlowChart.Content = "顯示流程圖";
                    currentMode = DisplayMode.Markdown;
                    break;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}