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
                string markdownPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Postman_測試範例.md");
                
                if (!File.Exists(markdownPath))
                {
                    // 嘗試其他可能的路徑
                    markdownPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Postman_測試範例.md");
                    
                    if (!File.Exists(markdownPath))
                    {
                        // 最後嘗試絕對路徑
                        markdownPath = @"c:\Users\qoose\Desktop\OThin\Postman_測試範例.md";
                    }
                }

                if (File.Exists(markdownPath))
                {
                    // 指定 UTF-8 編碼讀取檔案
                    string content = File.ReadAllText(markdownPath, System.Text.Encoding.UTF8);
                    DisplayMarkdown(content);
                }
                else
                {
                    MessageBox.Show("找不到 Postman 測試範例文件。", "檔案不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入文件時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
