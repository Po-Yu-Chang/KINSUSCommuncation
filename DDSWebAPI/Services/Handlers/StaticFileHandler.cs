///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: StaticFileHandler.cs
// 檔案描述: 靜態檔案處理器
// 功能概述: 處理靜態檔案請求的核心邏輯
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DDSWebAPI.Services.Handlers
{
    /// <summary>
    /// 靜態檔案處理器類別
    /// 包含靜態檔案服務的核心邏輯
    /// </summary>
    public class StaticFileHandler
    {
        #region 私有欄位

        private readonly string _staticFilesPath;
        private readonly Dictionary<string, string> _mimeTypes;

        #endregion

        #region 建構函式

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="staticFilesPath">靜態檔案根目錄路徑</param>
        /// <param name="mimeTypes">MIME 類型對應表</param>
        public StaticFileHandler(string staticFilesPath, Dictionary<string, string> mimeTypes)
        {
            _staticFilesPath = staticFilesPath ?? throw new ArgumentNullException(nameof(staticFilesPath));
            _mimeTypes = mimeTypes ?? throw new ArgumentNullException(nameof(mimeTypes));
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 處理靜態檔案請求
        /// </summary>
        /// <param name="context">HTTP 請求上下文</param>
        /// <param name="requestPath">請求路徑</param>
        /// <returns>處理任務</returns>
        public async Task HandleStaticFileRequestAsync(HttpListenerContext context, string requestPath)
        {
            try
            {
                // 清理和驗證請求路徑
                var cleanPath = CleanPath(requestPath);
                
                // 如果是根路徑，預設為 index.html
                if (cleanPath == "/" || string.IsNullOrEmpty(cleanPath))
                {
                    cleanPath = "/index.html";
                }

                // 建構完整的檔案路徑
                var filePath = Path.Combine(_staticFilesPath, cleanPath.TrimStart('/'));
                
                // 正規化路徑並檢查安全性
                filePath = Path.GetFullPath(filePath);
                var rootPath = Path.GetFullPath(_staticFilesPath);
                
                // 確保檔案路徑在允許的根目錄內 (防止路徑遍歷攻擊)
                if (!filePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    await SendNotFoundResponseAsync(context.Response, "存取被拒絕");
                    return;
                }

                // 檢查檔案是否存在
                if (!File.Exists(filePath))
                {
                    await SendNotFoundResponseAsync(context.Response, "檔案不存在");
                    return;
                }

                // 檢查檔案大小 (限制最大 50MB)
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 50 * 1024 * 1024)
                {
                    await SendErrorResponseAsync(context.Response, HttpStatusCode.RequestEntityTooLarge, "檔案過大");
                    return;
                }

                // 設定 Content-Type
                var extension = Path.GetExtension(filePath).ToLower();
                var contentType = GetContentType(extension);
                context.Response.ContentType = contentType;

                // 設定快取標頭 (可選)
                SetCacheHeaders(context.Response, extension);

                // 設定內容長度
                context.Response.ContentLength64 = fileInfo.Length;

                // 設定狀態碼
                context.Response.StatusCode = (int)HttpStatusCode.OK;

                // 讀取並發送檔案內容
                using (var fileStream = File.OpenRead(filePath))
                {
                    await fileStream.CopyToAsync(context.Response.OutputStream);
                }
            }
            catch (UnauthorizedAccessException)
            {
                await SendErrorResponseAsync(context.Response, HttpStatusCode.Forbidden, "存取被拒絕");
            }
            catch (DirectoryNotFoundException)
            {
                await SendNotFoundResponseAsync(context.Response, "目錄不存在");
            }
            catch (FileNotFoundException)
            {
                await SendNotFoundResponseAsync(context.Response, "檔案不存在");
            }
            catch (IOException ex)
            {
                await SendErrorResponseAsync(context.Response, HttpStatusCode.InternalServerError, $"讀取檔案時發生 I/O 錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                await SendErrorResponseAsync(context.Response, HttpStatusCode.InternalServerError, $"處理靜態檔案時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理目錄瀏覽請求
        /// </summary>
        /// <param name="context">HTTP 請求上下文</param>
        /// <param name="requestPath">請求路徑</param>
        /// <returns>處理任務</returns>
        public async Task HandleDirectoryBrowsingAsync(HttpListenerContext context, string requestPath)
        {
            try
            {
                // 清理和驗證請求路徑
                var cleanPath = CleanPath(requestPath);
                
                // 建構完整的目錄路徑
                var directoryPath = Path.Combine(_staticFilesPath, cleanPath.TrimStart('/'));
                
                // 正規化路徑並檢查安全性
                directoryPath = Path.GetFullPath(directoryPath);
                var rootPath = Path.GetFullPath(_staticFilesPath);
                
                // 確保目錄路徑在允許的根目錄內
                if (!directoryPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    await SendErrorResponseAsync(context.Response, HttpStatusCode.Forbidden, "存取被拒絕");
                    return;
                }

                // 檢查目錄是否存在
                if (!Directory.Exists(directoryPath))
                {
                    await SendNotFoundResponseAsync(context.Response, "目錄不存在");
                    return;
                }

                // 產生目錄清單 HTML
                var html = GenerateDirectoryListingHtml(directoryPath, cleanPath);
                
                // 設定回應
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                
                var bytes = System.Text.Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength64 = bytes.Length;
                
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                await SendErrorResponseAsync(context.Response, HttpStatusCode.InternalServerError, $"處理目錄瀏覽時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 清理請求路徑
        /// </summary>
        /// <param name="path">原始路徑</param>
        /// <returns>清理後的路徑</returns>
        private string CleanPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "/";

            // URL 解碼
            path = Uri.UnescapeDataString(path);
            
            // 移除查詢字串
            var queryIndex = path.IndexOf('?');
            if (queryIndex >= 0)
            {
                path = path.Substring(0, queryIndex);
            }

            // 標準化路徑分隔符
            path = path.Replace('\\', '/');
            
            // 確保以斜線開頭
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            return path;
        }

        /// <summary>
        /// 根據檔案副檔名取得 Content-Type
        /// </summary>
        /// <param name="extension">檔案副檔名</param>
        /// <returns>Content-Type 字串</returns>
        private string GetContentType(string extension)
        {
            if (_mimeTypes.TryGetValue(extension, out string contentType))
            {
                return contentType;
            }

            // 預設為二進位檔案
            return "application/octet-stream";
        }

        /// <summary>
        /// 設定快取標頭
        /// </summary>
        /// <param name="response">HTTP 回應物件</param>
        /// <param name="extension">檔案副檔名</param>
        private void SetCacheHeaders(HttpListenerResponse response, string extension)
        {
            // 根據檔案類型設定不同的快取策略
            switch (extension)
            {
                case ".html":
                case ".htm":
                    // HTML 檔案不快取或短時間快取
                    response.Headers.Add("Cache-Control", "no-cache, must-revalidate");
                    response.Headers.Add("Expires", "0");
                    break;

                case ".css":
                case ".js":
                    // CSS 和 JS 檔案快取 1 天
                    response.Headers.Add("Cache-Control", "public, max-age=86400");
                    response.Headers.Add("Expires", DateTime.UtcNow.AddDays(1).ToString("R"));
                    break;

                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".ico":
                case ".svg":
                    // 圖片檔案快取 7 天
                    response.Headers.Add("Cache-Control", "public, max-age=604800");
                    response.Headers.Add("Expires", DateTime.UtcNow.AddDays(7).ToString("R"));
                    break;

                default:
                    // 其他檔案快取 1 小時
                    response.Headers.Add("Cache-Control", "public, max-age=3600");
                    response.Headers.Add("Expires", DateTime.UtcNow.AddHours(1).ToString("R"));
                    break;
            }

            // 設定 ETag (簡單版本，使用檔案修改時間)
            try
            {
                var lastModified = File.GetLastWriteTimeUtc(response.Headers["X-File-Path"]);
                var etag = $"\"{lastModified.Ticks:x}\"";
                response.Headers.Add("ETag", etag);
                response.Headers.Add("Last-Modified", lastModified.ToString("R"));
            }
            catch
            {
                // 忽略 ETag 設定錯誤
            }
        }

        /// <summary>
        /// 產生目錄清單 HTML
        /// </summary>
        /// <param name="directoryPath">目錄路徑</param>
        /// <param name="urlPath">URL 路徑</param>
        /// <returns>HTML 內容</returns>
        private string GenerateDirectoryListingHtml(string directoryPath, string urlPath)
        {
            var html = new System.Text.StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"utf-8\">");
            html.AppendLine($"    <title>目錄清單 - {urlPath}</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("        h1 { color: #333; }");
            html.AppendLine("        table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("        th, td { text-align: left; padding: 8px; border-bottom: 1px solid #ddd; }");
            html.AppendLine("        th { background-color: #f2f2f2; }");
            html.AppendLine("        a { text-decoration: none; color: #0066cc; }");
            html.AppendLine("        a:hover { text-decoration: underline; }");
            html.AppendLine("        .directory { font-weight: bold; }");
            html.AppendLine("        .file-size { text-align: right; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine($"    <h1>目錄清單 - {urlPath}</h1>");
            
            // 上層目錄連結
            if (urlPath != "/")
            {
                var parentPath = Path.GetDirectoryName(urlPath.Replace('/', Path.DirectorySeparatorChar))?.Replace(Path.DirectorySeparatorChar, '/') ?? "/";
                html.AppendLine($"    <p><a href=\"{parentPath}\">[上層目錄]</a></p>");
            }
            
            html.AppendLine("    <table>");
            html.AppendLine("        <thead>");
            html.AppendLine("            <tr>");
            html.AppendLine("                <th>名稱</th>");
            html.AppendLine("                <th>類型</th>");
            html.AppendLine("                <th>大小</th>");
            html.AppendLine("                <th>修改時間</th>");
            html.AppendLine("            </tr>");
            html.AppendLine("        </thead>");
            html.AppendLine("        <tbody>");

            try
            {
                // 列出目錄
                var directories = Directory.GetDirectories(directoryPath);
                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    var dirUrl = urlPath.TrimEnd('/') + "/" + dirName;
                    var lastWrite = Directory.GetLastWriteTime(dir);
                    
                    html.AppendLine("            <tr>");
                    html.AppendLine($"                <td><a href=\"{dirUrl}/\" class=\"directory\">{dirName}/</a></td>");
                    html.AppendLine("                <td>目錄</td>");
                    html.AppendLine("                <td>-</td>");
                    html.AppendLine($"                <td>{lastWrite:yyyy-MM-dd HH:mm:ss}</td>");
                    html.AppendLine("            </tr>");
                }

                // 列出檔案
                var files = Directory.GetFiles(directoryPath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var fileUrl = urlPath.TrimEnd('/') + "/" + fileName;
                    var fileInfo = new FileInfo(file);
                    var fileSize = FormatFileSize(fileInfo.Length);
                    
                    html.AppendLine("            <tr>");
                    html.AppendLine($"                <td><a href=\"{fileUrl}\">{fileName}</a></td>");
                    html.AppendLine("                <td>檔案</td>");
                    html.AppendLine($"                <td class=\"file-size\">{fileSize}</td>");
                    html.AppendLine($"                <td>{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td>");
                    html.AppendLine("            </tr>");
                }
            }
            catch (Exception ex)
            {
                html.AppendLine("            <tr>");
                html.AppendLine($"                <td colspan=\"4\">錯誤: {ex.Message}</td>");
                html.AppendLine("            </tr>");
            }

            html.AppendLine("        </tbody>");
            html.AppendLine("    </table>");
            html.AppendLine("    <hr>");
            html.AppendLine($"    <p><small>產生時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</small></p>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        /// <summary>
        /// 格式化檔案大小
        /// </summary>
        /// <param name="bytes">位元組數</param>
        /// <returns>格式化的檔案大小字串</returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 發送 404 Not Found 回應
        /// </summary>
        /// <param name="response">HTTP 回應物件</param>
        /// <param name="message">錯誤訊息</param>
        private async Task SendNotFoundResponseAsync(HttpListenerResponse response, string message)
        {
            await SendErrorResponseAsync(response, HttpStatusCode.NotFound, message);
        }

        /// <summary>
        /// 發送錯誤回應
        /// </summary>
        /// <param name="response">HTTP 回應物件</param>
        /// <param name="statusCode">HTTP 狀態碼</param>
        /// <param name="message">錯誤訊息</param>
        private async Task SendErrorResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, string message)
        {
            try
            {
                response.StatusCode = (int)statusCode;
                response.ContentType = "text/html; charset=utf-8";
                
                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>錯誤 {(int)statusCode}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 50px; text-align: center; }}
        h1 {{ color: #d32f2f; }}
        p {{ color: #666; }}
    </style>
</head>
<body>
    <h1>錯誤 {(int)statusCode}</h1>
    <p>{message}</p>
    <hr>
    <p><small>HTTP 伺服器</small></p>
</body>
</html>";

                var bytes = System.Text.Encoding.UTF8.GetBytes(html);
                response.ContentLength64 = bytes.Length;
                
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch
            {
                // 忽略發送錯誤回應時的異常
            }
        }

        #endregion
    }
}
