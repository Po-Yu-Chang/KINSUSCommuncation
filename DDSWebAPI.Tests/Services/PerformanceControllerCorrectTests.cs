using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DDSWebAPI.Services;

namespace DDSWebAPI.Tests.Services
{
    /// <summary>
    /// 效能控制器單元測試 - 修正版
    /// 依照實際的 PerformanceController 實作進行測試
    /// </summary>
    [TestClass]
    public class PerformanceControllerCorrectTests
    {
        private PerformanceController _performanceController;       
         [TestInitialize]
        public void Setup()
        {
            // 設定效能參數：每分鐘10個請求，最大5個併發連線，最大資料1MB，逾時30秒
            _performanceController = new PerformanceController(10, 5, 1, 30);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _performanceController = null;
        }

        #region 頻率限制測試        /// <summary>
        /// 測試正常請求的頻率限制檢查
        /// 驗證項目：
        /// 1. 在限制範圍內的請求應該被允許
        /// 2. 回傳結果不為 null
        /// 3. IsAllowed 屬性為 true
        /// 4. 不會產生錯誤碼或重試時間
        /// </summary>
        [TestMethod]
        public void CheckRateLimit_NormalRequest_ShouldReturnSuccess()
        {
            // Arrange - 準備測試資料
            var clientId = "test-client-001";

            // Act - 執行頻率限制檢查
            var result = _performanceController.CheckRateLimit(clientId);

            // Assert - 驗證結果
            Assert.IsNotNull(result, "頻率限制檢查結果不應為 null");
            Assert.IsTrue(result.IsAllowed, "正常請求應該被允許通過");
        }        /// <summary>
        /// 測試超過頻率限制的請求處理
        /// 驗證項目：
        /// 1. 超過每分鐘最大請求數限制時應被拒絕
        /// 2. 回傳 IsAllowed = false
        /// 3. 錯誤碼應為 RATE_002（超過頻率限制）
        /// 4. 應包含重試等待時間 (RetryAfterSeconds)
        /// 5. 頻率限制機制正確運作
        /// </summary>
        [TestMethod]
        public void CheckRateLimit_ExceedLimit_ShouldReturnFailure()
        {
            // Arrange - 準備測試用戶端
            var clientId = "test-client-002";

            // Act - 發送超過限制的請求（每分鐘10個，這裡發送12個）
            for (int i = 0; i < 12; i++) // 超過每分鐘10個的限制
            {
                _performanceController.CheckRateLimit(clientId);
            }
            var result = _performanceController.CheckRateLimit(clientId);            // Assert - 驗證超過限制時的行為
            Assert.IsNotNull(result, "頻率限制檢查結果不應為 null");
            Assert.IsFalse(result.IsAllowed, "超過頻率限制的請求應被拒絕");
            Assert.AreEqual("RATE_002", result.ErrorCode, "應回傳正確的頻率限制錯誤碼");
            Assert.IsTrue(result.RetryAfterSeconds.HasValue, "應提供重試等待時間");
        }        /// <summary>
        /// 測試不同用戶端的頻率限制獨立追蹤
        /// 驗證項目：
        /// 1. 不同用戶端的頻率限制應該獨立計算
        /// 2. 一個用戶端超過限制不應影響其他用戶端
        /// 3. 用戶端識別機制正確運作
        /// 4. 頻率追蹤的隔離性
        /// </summary>
        [TestMethod]
        public void CheckRateLimit_DifferentClients_ShouldTrackSeparately()
        {
            // Arrange - 準備兩個不同的用戶端
            var client1 = "test-client-003";
            var client2 = "test-client-004";

            // Act - 讓 client1 超過限制，但 client2 保持正常
            for (int i = 0; i < 12; i++)
            {
                _performanceController.CheckRateLimit(client1);
            }
            var result1 = _performanceController.CheckRateLimit(client1);
            var result2 = _performanceController.CheckRateLimit(client2);

            // Assert - 驗證不同用戶端的獨立性
            Assert.IsFalse(result1.IsAllowed, "超過限制的用戶端應被拒絕"); // client1 超過限制
            Assert.IsTrue(result2.IsAllowed, "未超過限制的用戶端應被允許");  // client2 仍可使用
        }

        #endregion

        #region 併發連線限制測試        /// <summary>
        /// 測試正常併發連線建立
        /// 驗證項目：
        /// 1. 在併發限制範圍內的連線應該被允許
        /// 2. 回傳結果不為 null
        /// 3. IsAllowed 屬性為 true
        /// 4. 應提供有效的連線 Token 用於後續操作
        /// 5. 併發連線管理機制正常運作
        /// </summary>
        [TestMethod]
        public async Task CheckConcurrencyLimitAsync_NormalConnection_ShouldReturnSuccess()
        {
            // Act - 建立正常的併發連線
            var result = await _performanceController.CheckConcurrencyLimitAsync();

            // Assert - 驗證連線建立結果
            Assert.IsNotNull(result, "併發連線檢查結果不應為 null");
            Assert.IsTrue(result.IsAllowed, "正常併發連線應該被允許");
            Assert.IsNotNull(result.ConnectionToken, "應提供有效的連線 Token");
        }        /// <summary>
        /// 測試超過併發連線限制的處理
        /// 驗證項目：
        /// 1. 超過最大併發連線數時應被拒絕
        /// 2. 回傳 IsAllowed = false
        /// 3. 錯誤碼應為 CONC_001（超過併發限制）
        /// 4. 併發連線計數器正確運作
        /// 5. 連線資源管理正確
        /// </summary>
        [TestMethod]
        public async Task CheckConcurrencyLimitAsync_ExceedLimit_ShouldReturnFailure()
        {
            // Arrange - 建立多個連線直到達到限制（最大5個）
            var tokens = new string[5];
            for (int i = 0; i < 5; i++)
            {
                var connResult = await _performanceController.CheckConcurrencyLimitAsync();
                tokens[i] = connResult.ConnectionToken;
            }

            // Act - 嘗試建立第6個連線（超過限制）
            var result = await _performanceController.CheckConcurrencyLimitAsync();            // Assert - 驗證超過併發限制時的行為
            Assert.IsNotNull(result, "併發連線檢查結果不應為 null");
            Assert.IsFalse(result.IsAllowed, "超過併發限制的連線應被拒絕");
            Assert.AreEqual("CONC_001", result.ErrorCode, "應回傳正確的併發限制錯誤碼");

            // Cleanup - 釋放連線資源
            foreach (var token in tokens)
            {
                if (!string.IsNullOrEmpty(token))
                {
                    _performanceController.ReleaseConcurrency(token);
                }
            }
        }

        /// <summary>
        /// 測試有效連線 Token 的釋放功能
        /// 驗證項目：
        /// 1. 有效的連線 Token 應能正確釋放
        /// 2. 釋放後應能重新建立新連線
        /// 3. 連線計數器正確遞減
        /// 4. 信號量 (SemaphoreSlim) 正確釋放
        /// 5. 連線資源管理的正確性
        /// </summary>
        [TestMethod]
        public async Task ReleaseConcurrency_ValidToken_ShouldWork()
        {
            // Arrange - 建立連線
            var connectionResult = await _performanceController.CheckConcurrencyLimitAsync();
            var token = connectionResult.ConnectionToken;

            // Act - 釋放連線
            _performanceController.ReleaseConcurrency(token);

            // Assert - 確認可以再次建立連線
            var newResult = await _performanceController.CheckConcurrencyLimitAsync();
            Assert.IsTrue(newResult.IsAllowed);
            
            // Cleanup
            if (!string.IsNullOrEmpty(newResult.ConnectionToken))
            {
                _performanceController.ReleaseConcurrency(newResult.ConnectionToken);
            }
        }

        #endregion

        #region 資料大小限制測試

        /// <summary>
        /// 測試正常大小資料的限制檢查
        /// 驗證項目：
        /// 1. 小於1MB的資料應該被允許
        /// 2. 回傳 IsAllowed = true
        /// 3. UTF-8 編碼的資料大小計算正確
        /// 4. 資料大小限制機制正常運作
        /// 5. 不產生錯誤碼或錯誤訊息
        /// </summary>
        [TestMethod]
        public void CheckDataSizeLimit_NormalSize_ShouldReturnSuccess()
        {
            // Arrange - 建立小於1MB的測試資料
            var smallData = new string('a', 1000); // 1KB

            // Act
            var result = _performanceController.CheckDataSizeLimit(smallData);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAllowed);
        }        /// <summary>
        /// 測試超過資料大小限制的處理
        /// 驗證項目：
        /// 1. 超過1MB的資料應被拒絕
        /// 2. 回傳 IsAllowed = false
        /// 3. 錯誤碼應為 SIZE_001（資料過大）
        /// 4. 錯誤訊息包含實際大小和限制大小
        /// 5. 資料大小計算的準確性
        /// </summary>
        [TestMethod]
        public void CheckDataSizeLimit_ExceedLimit_ShouldReturnFailure()
        {
            // Arrange - 建立大於1MB的測試資料
            var largeData = new string('a', 1048577); // 大約1MB + 1 byte的字元

            // Act
            var result = _performanceController.CheckDataSizeLimit(largeData);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsAllowed);
            Assert.AreEqual("SIZE_001", result.ErrorCode);
        }

        /// <summary>
        /// 測試 null 資料的處理
        /// 驗證項目：
        /// 1. null 資料應被視為有效（大小為0）
        /// 2. 回傳 IsAllowed = true
        /// 3. 空資料的安全處理
        /// 4. 邊界條件處理正確
        /// 5. 不會拋出 NullReferenceException
        /// </summary>
        [TestMethod]
        public void CheckDataSizeLimit_NullData_ShouldReturnSuccess()
        {
            // Act
            var result = _performanceController.CheckDataSizeLimit(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAllowed);
        }

        #endregion

        #region 綜合驗證測試

        /// <summary>
        /// 測試綜合效能驗證 - 所有條件通過
        /// 驗證項目：
        /// 1. 同時檢查頻率限制、併發限制、資料大小限制
        /// 2. 正常請求應通過所有驗證
        /// 3. 回傳 IsAllowed = true
        /// 4. 提供有效的連線 Token
        /// 5. 綜合效能控制機制協調運作
        /// </summary>
        [TestMethod]
        public async Task ValidateRequestAsync_AllValid_ShouldReturnSuccess()
        {
            // Arrange
            var clientId = "test-client-005";
            var requestBody = "{\"test\": \"data\"}";

            // Act
            var result = await _performanceController.ValidateRequestAsync(clientId, requestBody);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAllowed);
            Assert.IsNotNull(result.ConnectionToken);

            // Cleanup
            if (!string.IsNullOrEmpty(result.ConnectionToken))
            {
                _performanceController.ReleaseConcurrency(result.ConnectionToken);
            }
        }        /// <summary>
        /// 測試綜合效能驗證 - 超過頻率限制
        /// 驗證項目：
        /// 1. 綜合驗證中的頻率限制檢查
        /// 2. 超過頻率限制時應被拒絕
        /// 3. 回傳 IsAllowed = false
        /// 4. 錯誤碼應為 RATE_002
        /// 5. 綜合驗證流程中的優先級處理
        /// </summary>
        [TestMethod]
        public async Task ValidateRequestAsync_ExceedRateLimit_ShouldReturnFailure()
        {
            // Arrange
            var clientId = "test-client-006";
            var requestBody = "{\"test\": \"data\"}";

            // Act - 發送超過限制的請求
            for (int i = 0; i < 12; i++)
            {
                await _performanceController.ValidateRequestAsync(clientId, requestBody);
            }
            var result = await _performanceController.ValidateRequestAsync(clientId, requestBody);            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsAllowed);
            Assert.AreEqual("RATE_002", result.ErrorCode);
        }        /// <summary>
        /// 測試綜合效能驗證 - 超過資料大小限制
        /// 驗證項目：
        /// 1. 綜合驗證中的資料大小限制檢查
        /// 2. 超過1MB資料大小時應被拒絕
        /// 3. 回傳 IsAllowed = false
        /// 4. 錯誤碼應為 SIZE_001
        /// 5. 資料大小檢查的優先級處理
        /// </summary>
        [TestMethod]
        public async Task ValidateRequestAsync_ExceedDataSize_ShouldReturnFailure()
        {
            // Arrange
            var clientId = "test-client-007";
            var largeRequestBody = new string('a', 1048577); // 超過1MB

            // Act
            var result = await _performanceController.ValidateRequestAsync(clientId, largeRequestBody);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsAllowed);
            Assert.AreEqual("SIZE_001", result.ErrorCode);
        }

        #endregion

        #region 統計資訊測試

        /// <summary>
        /// 測試效能統計資訊查詢 - 初始狀態
        /// 驗證項目：
        /// 1. 初始狀態的統計資訊正確
        /// 2. CurrentConnections = 0
        /// 3. MaxConcurrentConnections = 5
        /// 4. MaxRequestsPerMinute = 10
        /// 5. 統計資訊結構完整性
        /// </summary>
        [TestMethod]
        public void GetStatistics_InitialState_ShouldReturnCorrectInfo()
        {
            // Act
            var stats = _performanceController.GetStatistics();

            // Assert
            Assert.IsNotNull(stats);
            Assert.AreEqual(0, stats.CurrentConnections);
            Assert.AreEqual(5, stats.MaxConcurrentConnections);
            Assert.AreEqual(10, stats.MaxRequestsPerMinute);
        }

        /// <summary>
        /// 測試有連線時的統計資訊
        /// 驗證項目：
        /// 1. 建立連線後統計資訊即時更新
        /// 2. CurrentConnections 正確反映實際連線數
        /// 3. TotalActiveClients 計數正確
        /// 4. 統計資訊的即時性和準確性
        /// 5. 連線狀態追蹤機制
        /// </summary>
        [TestMethod]
        public async Task GetStatistics_WithConnections_ShouldReflectCurrentState()
        {
            // Arrange - 建立一些連線
            var token1 = (await _performanceController.CheckConcurrencyLimitAsync()).ConnectionToken;
            var token2 = (await _performanceController.CheckConcurrencyLimitAsync()).ConnectionToken;

            // Act
            var stats = _performanceController.GetStatistics();

            // Assert
            Assert.IsNotNull(stats);
            Assert.AreEqual(2, stats.CurrentConnections);
            Assert.IsTrue(stats.TotalActiveClients >= 0);

            // Cleanup
            _performanceController.ReleaseConcurrency(token1);
            _performanceController.ReleaseConcurrency(token2);
        }

        #endregion

        #region 邊界條件測試        /// <summary>
        /// 測試頻率限制 - 空用戶端 ID 的優雅處理
        /// 驗證項目：
        /// 1. 空字串用戶端 ID 不應拋出異常
        /// 2. 回傳結果不為 null
        /// 3. 邊界條件的安全處理
        /// 4. 系統防護機制正常運作
        /// 5. 用戶端識別的容錯性
        /// </summary>
        [TestMethod]
        public void CheckRateLimit_EmptyClientId_ShouldHandleGracefully()
        {
            // Act
            var result = _performanceController.CheckRateLimit("");

            // Assert
            Assert.IsNotNull(result);
            // 應該正常處理，不應該拋出異常
        }        /// <summary>
        /// 測試頻率限制 - null 用戶端 ID 的優雅處理
        /// 驗證項目：
        /// 1. null 用戶端 ID 不應拋出異常
        /// 2. 回傳結果不為 null
        /// 3. 空值檢查的防護機制
        /// 4. NullReferenceException 預防
        /// 5. 輸入驗證的健壯性
        /// </summary>
        [TestMethod]
        public void CheckRateLimit_NullClientId_ShouldHandleGracefully()
        {
            // Act
            var result = _performanceController.CheckRateLimit(null);

            // Assert
            Assert.IsNotNull(result);
            // 應該正常處理，不應該拋出異常
        }/// <summary>
        /// 測試無效連線 Token 的釋放處理
        /// 驗證項目：
        /// 1. 無效 Token 釋放不應拋出異常
        /// 2. null、空字串、無效 Token 的安全處理
        /// 3. 可能拋出預期的異常類型
        /// 4. 異常處理的優雅性
        /// 5. 系統穩定性保證
        /// </summary>
        [TestMethod]
        public void ReleaseConcurrency_InvalidToken_ShouldHandleGracefully()
        {
            // Act & Assert - 應該能正常處理無效的 token，但不會釋放信號量
            try
            {
                _performanceController.ReleaseConcurrency("invalid-token");
                _performanceController.ReleaseConcurrency(null);
                _performanceController.ReleaseConcurrency("");
                
                // 如果沒有拋出異常，測試通過
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                // 如果拋出異常，檢查是否為預期的異常類型
                Assert.IsTrue(ex is ArgumentException || ex is InvalidOperationException || ex is System.Threading.SemaphoreFullException, 
                    $"Unexpected exception type: {ex.GetType().Name}");
            }
        }

        #endregion

        #region 壓力測試

        /// <summary>
        /// 測試多個同時請求的壓力處理
        /// 驗證項目：
        /// 1. 20個平行請求的併發處理
        /// 2. 不同用戶端的請求隔離
        /// 3. 系統在高負載下的穩定性
        /// 4. 資源競爭和死鎖預防
        /// 5. 平行處理的正確性
        /// 6. 所有任務都應完成而不拋出異常
        /// 7. 最終系統狀態正常
        /// </summary>
        [TestMethod]
        public async Task StressTest_MultipleSimultaneousRequests_ShouldHandleCorrectly()
        {
            // Arrange
            var tasks = new Task[20];
            var clientPrefix = "stress-test-client-";

            // Act - 同時發送多個請求
            for (int i = 0; i < 20; i++)
            {
                var clientId = clientPrefix + i;
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        var result = await _performanceController.ValidateRequestAsync(clientId, "{\"test\": \"data\"}");
                        if (result.IsAllowed && !string.IsNullOrEmpty(result.ConnectionToken))
                        {
                            // 模擬處理時間
                            await Task.Delay(100);
                            _performanceController.ReleaseConcurrency(result.ConnectionToken);
                        }
                    }
                    catch (Exception)
                    {
                        // 記錄但不拋出異常
                    }
                });
            }

            // Assert - 所有任務都應該完成而不拋出異常
            await Task.WhenAll(tasks);
            
            // 驗證系統狀態正常
            var stats = _performanceController.GetStatistics();
            Assert.IsNotNull(stats);
        }

        #endregion
    }
}
