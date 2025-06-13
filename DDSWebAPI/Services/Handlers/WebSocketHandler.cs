///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: WebSocketHandler.cs
// 檔案描述: WebSocket 訊息處理器
// 功能概述: 處理 WebSocket 連接和訊息的核心邏輯
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DDSWebAPI.Interfaces;
using DDSWebAPI.Events;
using DDSWebAPI.Enums;

namespace DDSWebAPI.Services.Handlers
{
    /// <summary>
    /// WebSocket 處理器類別
    /// 包含 WebSocket 連接和訊息處理的核心邏輯
    /// </summary>
    public class WebSocketHandler
    {
        #region 私有欄位

        private readonly IUtilityService _utilityService;

        #endregion

        #region 事件定義

        /// <summary>
        /// WebSocket 訊息接收事件
        /// </summary>
        public event EventHandler<WebSocketMessageEventArgs> MessageReceived;

        /// <summary>
        /// 狀態變更事件
        /// </summary>
        public event EventHandler<ServerStatusChangedEventArgs> StatusChanged;

        #endregion

        #region 建構函式

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="utilityService">公用程式服務</param>
        public WebSocketHandler(IUtilityService utilityService = null)
        {
            _utilityService = utilityService;
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 處理 WebSocket 用戶端連接的完整生命週期
        /// </summary>
        /// <param name="webSocket">WebSocket 連接物件</param>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="cancellationToken">取消標記</param>
        /// <returns>處理任務</returns>
        public async Task HandleWebSocketClientAsync(WebSocket webSocket, string clientId, CancellationToken cancellationToken)
        {
            // 設定接收緩衝區大小 (4KB)
            var buffer = new byte[4096];

            try
            {
                // 持續監聽 WebSocket 訊息，直到連接關閉
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // 接收 WebSocket 訊息
                        var result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), 
                            cancellationToken);

                        // 處理不同類型的訊息
                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                // 處理文字訊息
                                await HandleTextMessageAsync(webSocket, buffer, result, clientId);
                                break;

                            case WebSocketMessageType.Binary:
                                // 處理二進位訊息
                                await HandleBinaryMessageAsync(webSocket, buffer, result, clientId);
                                break;

                            case WebSocketMessageType.Close:
                                // 處理關閉訊息
                                await HandleCloseMessageAsync(webSocket, result, clientId);
                                return;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 操作被取消，通常是伺服器正在關閉
                        break;
                    }
                    catch (WebSocketException wsEx)
                    {
                        // WebSocket 特定錯誤
                        OnStatusChanged(ServerStatus.Warning, 
                            $"WebSocket 連接錯誤 (用戶端: {clientId}): {wsEx.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged(ServerStatus.Error, 
                    $"WebSocket 用戶端處理過程中發生未預期錯誤 (用戶端: {clientId}): {ex.Message}");
            }
            finally
            {
                // 確保 WebSocket 連接被正確關閉
                await CloseWebSocketSafelyAsync(webSocket, clientId);
            }
        }

        /// <summary>
        /// 發送文字訊息到 WebSocket
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="message">要發送的訊息</param>
        /// <returns>發送任務</returns>
        public async Task SendTextAsync(WebSocket webSocket, string message)
        {
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged(ServerStatus.Warning, $"發送 WebSocket 文字訊息時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 發送二進位資料到 WebSocket
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="data">要發送的二進位資料</param>
        /// <returns>發送任務</returns>
        public async Task SendBinaryAsync(WebSocket webSocket, byte[] data)
        {
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(data),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged(ServerStatus.Warning, $"發送 WebSocket 二進位資料時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 處理 WebSocket 文字訊息
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="buffer">接收緩衝區</param>
        /// <param name="result">接收結果</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleTextMessageAsync(WebSocket webSocket, byte[] buffer, 
            WebSocketReceiveResult result, string clientId)
        {
            try
            {
                // 將接收到的位元組轉換為文字
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                // 觸發 WebSocket 訊息接收事件
                OnMessageReceived(message, webSocket, clientId);

                // 處理特殊指令 (可選)
                if (message.StartsWith("{") && message.EndsWith("}"))
                {
                    // 嘗試解析 JSON 指令
                    try
                    {
                        var jsonMessage = JsonConvert.DeserializeObject<dynamic>(message);
                        await ProcessWebSocketCommandAsync(webSocket, jsonMessage, clientId);
                    }
                    catch (JsonException)
                    {
                        // 不是有效的 JSON，當作普通文字處理
                        await SendTextAsync(webSocket, $"收到訊息: {message}");
                    }
                }
                else
                {
                    // 簡單的回音服務
                    await SendTextAsync(webSocket, $"伺服器收到: {message}");
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged(ServerStatus.Warning, 
                    $"處理 WebSocket 文字訊息時發生錯誤 (用戶端: {clientId}): {ex.Message}");
            }
        }

        /// <summary>
        /// 處理 WebSocket 二進位訊息
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="buffer">接收緩衝區</param>
        /// <param name="result">接收結果</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleBinaryMessageAsync(WebSocket webSocket, byte[] buffer, 
            WebSocketReceiveResult result, string clientId)
        {
            try
            {
                // 取得二進位資料
                var binaryData = new byte[result.Count];
                Array.Copy(buffer, binaryData, result.Count);

                // 觸發 WebSocket 訊息接收事件
                var binaryMessage = Convert.ToBase64String(binaryData);
                OnMessageReceived($"[BINARY:{binaryData.Length} bytes] {binaryMessage}", webSocket, clientId);

                // 回傳確認訊息
                await SendTextAsync(webSocket, $"已接收 {binaryData.Length} 位元組的二進位資料");
            }
            catch (Exception ex)
            {
                OnStatusChanged(ServerStatus.Warning, 
                    $"處理 WebSocket 二進位訊息時發生錯誤 (用戶端: {clientId}): {ex.Message}");
            }
        }

        /// <summary>
        /// 處理 WebSocket 關閉訊息
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="result">接收結果</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task HandleCloseMessageAsync(WebSocket webSocket, 
            WebSocketReceiveResult result, string clientId)
        {
            try
            {
                // 取得關閉原因
                var closeStatus = result.CloseStatus ?? WebSocketCloseStatus.NormalClosure;
                var closeDescription = result.CloseStatusDescription ?? "正常關閉";

                // 記錄關閉事件
                OnStatusChanged(ServerStatus.Info, 
                    $"WebSocket 連接關閉 (用戶端: {clientId}, 原因: {closeDescription})");
            }
            catch (Exception ex)
            {
                OnStatusChanged(ServerStatus.Warning, 
                    $"處理 WebSocket 關閉訊息時發生錯誤 (用戶端: {clientId}): {ex.Message}");
            }
            finally
            {
                // 確保 WebSocket 連接被關閉
                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "正常關閉", CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged(ServerStatus.Warning, 
                        $"關閉 WebSocket 連接時發生錯誤 (用戶端: {clientId}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 處理 WebSocket JSON 指令
        /// </summary>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="jsonMessage">JSON 訊息物件</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task ProcessWebSocketCommandAsync(WebSocket webSocket, dynamic jsonMessage, string clientId)
        {
            try
            {
                string command = jsonMessage.command?.ToString()?.ToLower();
                
                switch (command)
                {
                    case "ping":
                        await SendTextAsync(webSocket, JsonConvert.SerializeObject(new
                        {
                            command = "pong",
                            timestamp = DateTime.Now,
                            clientId = clientId
                        }));
                        break;

                    case "get_time":
                        await SendTextAsync(webSocket, JsonConvert.SerializeObject(new
                        {
                            command = "time_response",
                            serverTime = DateTime.Now,
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        }));
                        break;

                    case "get_status":
                        await SendTextAsync(webSocket, JsonConvert.SerializeObject(new
                        {
                            command = "status_response",
                            status = "ONLINE",
                            uptime = TimeSpan.FromMilliseconds(Environment.TickCount),
                            timestamp = DateTime.Now
                        }));
                        break;

                    default:
                        await SendTextAsync(webSocket, JsonConvert.SerializeObject(new
                        {
                            command = "error",
                            message = $"未知的指令: {command}",
                            timestamp = DateTime.Now
                        }));
                        break;
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged(ServerStatus.Warning, 
                    $"處理 WebSocket 指令時發生錯誤 (用戶端: {clientId}): {ex.Message}");
                
                await SendTextAsync(webSocket, JsonConvert.SerializeObject(new
                {
                    command = "error",
                    message = "指令處理錯誤",
                    timestamp = DateTime.Now
                }));
            }
        }

        /// <summary>
        /// 安全關閉 WebSocket 連接
        /// </summary>
        /// <param name="webSocket">WebSocket 連接物件</param>
        /// <param name="clientId">用戶端識別碼</param>
        private async Task CloseWebSocketSafelyAsync(WebSocket webSocket, string clientId)
        {
            try
            {
                // 檢查 WebSocket 狀態
                if (webSocket.State == WebSocketState.Open)
                {
                    // 發送關閉訊息 (可選)
                    await SendTextAsync(webSocket, "伺服器正在關閉連接...");

                    // 等待一段時間以確保訊息發送完成
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged(ServerStatus.Warning, 
                    $"安全關閉 WebSocket 連接時發生錯誤 (用戶端: {clientId}): {ex.Message}");
            }
            finally
            {
                try
                {
                    // 確保 WebSocket 連接被正確關閉
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "正常關閉", CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged(ServerStatus.Warning, 
                        $"關閉 WebSocket 連接時發生錯誤 (用戶端: {clientId}): {ex.Message}");
                }
            }
        }

        #endregion

        #region 事件觸發方法

        /// <summary>
        /// 觸發 WebSocket 訊息接收事件
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <param name="webSocket">WebSocket 連接</param>
        /// <param name="clientId">用戶端識別碼</param>
        protected virtual void OnMessageReceived(string message, WebSocket webSocket, string clientId)
        {
            MessageReceived?.Invoke(this, new WebSocketMessageEventArgs(message, webSocket, clientId));
        }

        /// <summary>
        /// 觸發狀態變更事件
        /// </summary>
        /// <param name="status">狀態</param>
        /// <param name="message">訊息</param>
        protected virtual void OnStatusChanged(ServerStatus status, string message)
        {
            StatusChanged?.Invoke(this, new ServerStatusChangedEventArgs(status, message));
        }

        #endregion
    }
}
