using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DDSWebAPI.Services
{
    /// <summary>
    /// 效能控制器，處理請求頻率限制、平行處理控制和資料大小限制
    /// </summary>
    public class PerformanceController
    {
        private readonly int _maxRequestsPerMinute;
        private readonly int _maxConcurrentConnections;
        private readonly long _maxDataSizeMB;
        private readonly int _requestTimeoutSeconds;

        // 請求頻率追蹤
        private readonly ConcurrentDictionary<string, ClientRequestTracker> _clientRequestTrackers;
        
        // 平行連線追蹤
        private readonly SemaphoreSlim _connectionSemaphore;
        private int _currentConnections;

        /// <summary>
        /// 初始化效能控制器
        /// </summary>
        /// <param name="maxRequestsPerMinute">每分鐘最大請求數，預設100</param>
        /// <param name="maxConcurrentConnections">最大平行連線數，預設10</param>
        /// <param name="maxDataSizeMB">最大資料大小（MB），預設10</param>
        /// <param name="requestTimeoutSeconds">請求逾時秒數，預設30</param>
        public PerformanceController(
            int maxRequestsPerMinute = 100,
            int maxConcurrentConnections = 10,
            long maxDataSizeMB = 10,
            int requestTimeoutSeconds = 30)
        {
            _maxRequestsPerMinute = maxRequestsPerMinute;
            _maxConcurrentConnections = maxConcurrentConnections;
            _maxDataSizeMB = maxDataSizeMB;
            _requestTimeoutSeconds = requestTimeoutSeconds;

            _clientRequestTrackers = new ConcurrentDictionary<string, ClientRequestTracker>();
            _connectionSemaphore = new SemaphoreSlim(maxConcurrentConnections, maxConcurrentConnections);
            _currentConnections = 0;

            // 啟動清理任務
            StartCleanupTask();
        }

        /// <summary>
        /// 檢查請求頻率限制
        /// </summary>
        /// <param name="clientId">用戶端識別碼（通常使用 IP 位址）</param>
        /// <returns>檢查結果</returns>
        public PerformanceCheckResult CheckRateLimit(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return new PerformanceCheckResult
                {
                    IsAllowed = false,
                    ErrorCode = "RATE_001",
                    ErrorMessage = "無效的用戶端識別碼"
                };
            }

            var tracker = _clientRequestTrackers.GetOrAdd(clientId, _ => new ClientRequestTracker());
            var now = DateTime.UtcNow;

            lock (tracker)
            {
                // 清理超過1分鐘的舊記錄
                tracker.RequestTimes.RemoveAll(time => (now - time).TotalMinutes > 1);

                // 檢查是否超過限制
                if (tracker.RequestTimes.Count >= _maxRequestsPerMinute)
                {
                    return new PerformanceCheckResult
                    {
                        IsAllowed = false,
                        ErrorCode = "RATE_002",
                        ErrorMessage = $"超過請求頻率限制，每分鐘最多 {_maxRequestsPerMinute} 次請求",
                        RetryAfterSeconds = 60
                    };
                }

                // 記錄這次請求
                tracker.RequestTimes.Add(now);
            }

            return new PerformanceCheckResult { IsAllowed = true };
        }        /// <summary>
        /// 檢查平行處理能力限制
        /// </summary>
        /// <returns>檢查結果</returns>
        public async Task<PerformanceCheckResult> CheckConcurrencyLimitAsync()
        {
            var currentCount = _currentConnections;
            
            if (currentCount >= _maxConcurrentConnections)
            {
                return new PerformanceCheckResult
                {
                    IsAllowed = false,
                    ErrorCode = "CONC_001",
                    ErrorMessage = $"超過平行處理能力限制，最多同時處理 {_maxConcurrentConnections} 個請求"
                };
            }

            // 嘗試取得連線許可
            var acquired = await _connectionSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
            if (!acquired)
            {
                return new PerformanceCheckResult
                {
                    IsAllowed = false,
                    ErrorCode = "CONC_002",
                    ErrorMessage = "系統忙碌中，請稍後再試",
                    RetryAfterSeconds = 5
                };
            }

            Interlocked.Increment(ref _currentConnections);
            
            return new PerformanceCheckResult 
            { 
                IsAllowed = true,
                ConnectionToken = Guid.NewGuid().ToString() // 用於釋放連線
            };
        }

        /// <summary>
        /// 釋放平行處理能力
        /// </summary>
        /// <param name="connectionToken">連線權杖</param>
        public void ReleaseConcurrency(string connectionToken)
        {
            if (!string.IsNullOrEmpty(connectionToken))
            {
                Interlocked.Decrement(ref _currentConnections);
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// 檢查資料大小限制
        /// </summary>
        /// <param name="requestBody">請求內容</param>
        /// <returns>檢查結果</returns>
        public PerformanceCheckResult CheckDataSizeLimit(string requestBody)
        {
            if (string.IsNullOrEmpty(requestBody))
            {
                return new PerformanceCheckResult { IsAllowed = true };
            }

            var dataSizeBytes = System.Text.Encoding.UTF8.GetByteCount(requestBody);
            var dataSizeMB = dataSizeBytes / (1024.0 * 1024.0);

            if (dataSizeMB > _maxDataSizeMB)
            {
                return new PerformanceCheckResult
                {
                    IsAllowed = false,
                    ErrorCode = "SIZE_001",
                    ErrorMessage = $"請求資料過大，最大允許 {_maxDataSizeMB}MB，實際大小 {dataSizeMB:F2}MB"
                };
            }

            return new PerformanceCheckResult { IsAllowed = true };
        }

        /// <summary>
        /// 執行完整的效能檢查
        /// </summary>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="requestBody">請求內容</param>
        /// <returns>檢查結果</returns>
        public async Task<PerformanceCheckResult> ValidateRequestAsync(string clientId, string requestBody)
        {
            // 1. 檢查資料大小
            var sizeResult = CheckDataSizeLimit(requestBody);
            if (!sizeResult.IsAllowed)
            {
                return sizeResult;
            }

            // 2. 檢查請求頻率
            var rateResult = CheckRateLimit(clientId);
            if (!rateResult.IsAllowed)
            {
                return rateResult;
            }

            // 3. 檢查平行處理能力
            var concurrencyResult = await CheckConcurrencyLimitAsync();
            if (!concurrencyResult.IsAllowed)
            {
                return concurrencyResult;
            }

            return concurrencyResult; // 包含 ConnectionToken
        }        /// <summary>
        /// 取得效能統計資訊
        /// </summary>
        /// <returns>效能統計</returns>
        public PerformanceStatistics GetStatistics()
        {
            var currentConnections = _currentConnections;
            var totalClients = _clientRequestTrackers.Count;
            
            var totalRequests = 0;
            foreach (var tracker in _clientRequestTrackers.Values)
            {
                lock (tracker)
                {
                    totalRequests += tracker.RequestTimes.Count;
                }
            }

            return new PerformanceStatistics
            {
                CurrentConnections = currentConnections,
                MaxConcurrentConnections = _maxConcurrentConnections,
                TotalActiveClients = totalClients,
                TotalRequestsInLastMinute = totalRequests,
                MaxRequestsPerMinute = _maxRequestsPerMinute,
                MaxDataSizeMB = _maxDataSizeMB,
                RequestTimeoutSeconds = _requestTimeoutSeconds
            };
        }

        /// <summary>
        /// 啟動清理任務，定期清理過期的追蹤記錄
        /// </summary>
        private void StartCleanupTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5)); // 每5分鐘清理一次
                        
                        var now = DateTime.UtcNow;
                        var clientsToRemove = new List<string>();

                        foreach (var kvp in _clientRequestTrackers)
                        {
                            var tracker = kvp.Value;
                            lock (tracker)
                            {
                                // 清理超過1分鐘的舊記錄
                                tracker.RequestTimes.RemoveAll(time => (now - time).TotalMinutes > 1);
                                
                                // 如果沒有任何記錄，標記為可移除
                                if (tracker.RequestTimes.Count == 0)
                                {
                                    clientsToRemove.Add(kvp.Key);
                                }
                            }
                        }

                        // 移除空的追蹤器
                        foreach (var clientId in clientsToRemove)
                        {
                            _clientRequestTrackers.TryRemove(clientId, out _);
                        }
                    }
                    catch (Exception)
                    {
                        // 忽略清理過程中的錯誤
                    }
                }
            });
        }
    }

    /// <summary>
    /// 用戶端請求追蹤器
    /// </summary>
    internal class ClientRequestTracker
    {
        public List<DateTime> RequestTimes { get; } = new List<DateTime>();
    }

    /// <summary>
    /// 效能檢查結果
    /// </summary>
    public class PerformanceCheckResult
    {
        /// <summary>
        /// 是否允許請求
        /// </summary>
        public bool IsAllowed { get; set; }

        /// <summary>
        /// 錯誤代碼
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 建議重試等待秒數
        /// </summary>
        public int? RetryAfterSeconds { get; set; }

        /// <summary>
        /// 連線權杖（用於釋放平行處理能力）
        /// </summary>
        public string ConnectionToken { get; set; }
    }

    /// <summary>
    /// 效能統計資訊
    /// </summary>
    public class PerformanceStatistics
    {
        /// <summary>
        /// 目前連線數
        /// </summary>
        public int CurrentConnections { get; set; }

        /// <summary>
        /// 最大平行連線數
        /// </summary>
        public int MaxConcurrentConnections { get; set; }

        /// <summary>
        /// 總活躍用戶端數
        /// </summary>
        public int TotalActiveClients { get; set; }

        /// <summary>
        /// 最近一分鐘總請求數
        /// </summary>
        public int TotalRequestsInLastMinute { get; set; }

        /// <summary>
        /// 每分鐘最大請求數
        /// </summary>
        public int MaxRequestsPerMinute { get; set; }

        /// <summary>
        /// 最大資料大小（MB）
        /// </summary>
        public long MaxDataSizeMB { get; set; }

        /// <summary>
        /// 請求逾時秒數
        /// </summary>
        public int RequestTimeoutSeconds { get; set; }
    }
}
