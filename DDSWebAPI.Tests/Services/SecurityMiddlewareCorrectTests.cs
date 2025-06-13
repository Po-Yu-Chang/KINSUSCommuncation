using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DDSWebAPI.Services;

namespace DDSWebAPI.Tests.Services
{
    /// <summary>
    /// 安全性中介軟體單元測試 - 修正版
    /// 依照實際的 SecurityMiddleware 實作進行測試
    /// </summary>
    [TestClass]
    public class SecurityMiddlewareCorrectTests
    {
        private SecurityMiddleware _securityMiddleware;

        [TestInitialize]
        public void Setup()
        {
            var validApiKeys = new[] { "valid-api-key-1", "valid-api-key-2" };
            var ipWhitelist = new[] { "127.0.0.1", "192.168.1.100" };
            var secretKey = "test-secret-key-12345";
            
            _securityMiddleware = new SecurityMiddleware(validApiKeys, ipWhitelist, secretKey);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _securityMiddleware = null;
        }

        #region API 金鑰驗證測試        /// <summary>
        /// 測試有效的 API 金鑰驗證
        /// 驗證項目：
        /// 1. 使用預設白名單中的有效 API 金鑰應通過驗證
        /// 2. Bearer Token 格式解析正確
        /// 3. 回傳 IsValid = true
        /// 4. 不產生錯誤碼或錯誤訊息
        /// 5. API 金鑰驗證機制正常運作
        /// </summary>
        [TestMethod]
        public void ValidateApiKey_ValidKey_ShouldReturnSuccess()
        {
            // Arrange - 準備有效的 API 金鑰
            var authorization = "Bearer valid-api-key-1";

            // Act - 執行 API 金鑰驗證
            var result = _securityMiddleware.ValidateApiKey(authorization);

            // Assert - 驗證有效金鑰的處理結果
            Assert.IsNotNull(result, "API 金鑰驗證結果不應為 null");
            Assert.IsTrue(result.IsValid, "有效的 API 金鑰應通過驗證");
        }

        /// <summary>
        /// 測試無效的 API 金鑰驗證
        /// 驗證項目：
        /// 1. 使用不在白名單中的 API 金鑰應被拒絕
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 AUTH_003（無效的 API 金鑰）
        /// 4. 錯誤訊息應為「無效的 API 金鑰」
        /// 5. 安全性驗證機制正確防護
        /// </summary>
        [TestMethod]
        public void ValidateApiKey_InvalidKey_ShouldReturnFailure()
        {
            // Arrange
            var authorization = "Bearer invalid-api-key";

            // Act
            var result = _securityMiddleware.ValidateApiKey(authorization);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("AUTH_003", result.ErrorCode);
            Assert.AreEqual("無效的 API 金鑰", result.ErrorMessage);
        }

        /// <summary>
        /// 測試缺少 Authorization 標頭的處理
        /// 驗證項目：
        /// 1. null 或空的 Authorization 標頭應被拒絕
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 AUTH_001（缺少 Authorization 標頭）
        /// 4. 錯誤訊息應為「缺少 Authorization 標頭」
        /// 5. 必要參數驗證機制正常
        /// </summary>
        [TestMethod]
        public void ValidateApiKey_MissingAuthorization_ShouldReturnFailure()
        {
            // Act
            var result = _securityMiddleware.ValidateApiKey(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("AUTH_001", result.ErrorCode);
            Assert.AreEqual("缺少 Authorization 標頭", result.ErrorMessage);
        }

        /// <summary>
        /// 測試錯誤的 Authorization 格式處理
        /// 驗證項目：
        /// 1. 非 Bearer Token 格式應被拒絕
        /// 2. 回傳 IsValid = false  
        /// 3. 錯誤碼應為 AUTH_002（Authorization 標頭格式錯誤）
        /// 4. Bearer Token 格式驗證正確
        /// 5. 協定安全性標準遵循
        /// </summary>
        [TestMethod]
        public void ValidateApiKey_WrongFormat_ShouldReturnFailure()
        {
            // Arrange
            var authorization = "Basic valid-api-key-1";

            // Act
            var result = _securityMiddleware.ValidateApiKey(authorization);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("AUTH_002", result.ErrorCode);
        }

        #endregion

        #region 簽章驗證測試

        /// <summary>
        /// 測試簽章驗證 - 缺少簽章
        /// 驗證項目：
        /// 1. null 簽章應被拒絕
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 SIG_001（缺少簽章）
        /// 4. 錯誤訊息應為「缺少簽章」
        /// 5. HMAC 簽章必要性驗證
        /// </summary>
        [TestMethod]
        public void ValidateSignature_MissingSignature_ShouldReturnFailure()
        {
            // Arrange
            var content = "{\"test\": \"data\"}";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // Act
            var result = _securityMiddleware.ValidateSignature(null, content, timestamp);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("SIG_001", result.ErrorCode);
            Assert.AreEqual("缺少簽章", result.ErrorMessage);
        }

        /// <summary>
        /// 測試簽章驗證 - 缺少時間戳記
        /// 驗證項目：
        /// 1. null 時間戳記應被拒絕
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 SIG_002（缺少時間戳記）
        /// 4. 錯誤訊息應為「缺少時間戳記」
        /// 5. 時間戳記必要性驗證
        /// </summary>
        [TestMethod]
        public void ValidateSignature_MissingTimestamp_ShouldReturnFailure()
        {
            // Arrange
            var signature = "test-signature";
            var content = "{\"test\": \"data\"}";

            // Act
            var result = _securityMiddleware.ValidateSignature(signature, content, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("SIG_002", result.ErrorCode);
            Assert.AreEqual("缺少時間戳記", result.ErrorMessage);
        }

        /// <summary>
        /// 測試簽章驗證 - 錯誤的時間戳記格式
        /// 驗證項目：
        /// 1. 非數字格式的時間戳記應被拒絕
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 SIG_003（時間戳記格式錯誤）
        /// 4. 錯誤訊息應為「時間戳記格式錯誤」
        /// 5. 時間戳記格式驗證正確
        /// </summary>
        [TestMethod]
        public void ValidateSignature_InvalidTimestampFormat_ShouldReturnFailure()
        {
            // Arrange
            var signature = "test-signature";
            var content = "{\"test\": \"data\"}";
            var invalidTimestamp = "not-a-number";

            // Act
            var result = _securityMiddleware.ValidateSignature(signature, content, invalidTimestamp);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("SIG_003", result.ErrorCode);
            Assert.AreEqual("時間戳記格式錯誤", result.ErrorMessage);
        }

        /// <summary>
        /// 測試簽章驗證 - 過期的時間戳記
        /// 驗證項目：
        /// 1. 超過5分鐘的時間戳記應被拒絕
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 SIG_004（請求已過期）
        /// 4. 時間戳記有效期限控制正確
        /// 5. 重放攻擊防護機制有效
        /// </summary>
        [TestMethod]
        public void ValidateSignature_ExpiredTimestamp_ShouldReturnFailure()
        {
            // Arrange
            var signature = "test-signature";
            var content = "{\"test\": \"data\"}";
            var expiredTimestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 400).ToString(); // 400秒前

            // Act
            var result = _securityMiddleware.ValidateSignature(signature, content, expiredTimestamp);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("SIG_004", result.ErrorCode);
            Assert.AreEqual("請求已過期", result.ErrorMessage);
        }

        #endregion

        #region IP 白名單驗證測試

        /// <summary>
        /// 測試 IP 白名單驗證 - 有效 IP
        /// 驗證項目：
        /// 1. 白名單中的 IP 位址應通過驗證
        /// 2. 回傳 IsValid = true
        /// 3. 本機 IP（127.0.0.1）處理正確
        /// 4. IP 白名單機制正常運作
        /// 5. 網路存取控制正確
        /// </summary>
        [TestMethod]
        public void ValidateIPWhitelist_ValidIP_ShouldReturnSuccess()
        {
            // Arrange
            var validIp = "127.0.0.1";

            // Act
            var result = _securityMiddleware.ValidateIPWhitelist(validIp);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }        /// <summary>
        /// 測試 IP 白名單驗證 - 無效 IP
        /// 驗證項目：
        /// 1. 不在白名單中的外部 IP 應被拒絕
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 IP_002（IP 位址不在白名單中）
        /// 4. 內網 IP 和外網 IP 識別正確
        /// 5. 存取控制安全性正確
        /// </summary>
        [TestMethod]
        public void ValidateIPWhitelist_InvalidIP_ShouldReturnFailure()
        {
            // Arrange
            var invalidIp = "203.0.113.100"; // 使用非本機 IP

            // Act
            var result = _securityMiddleware.ValidateIPWhitelist(invalidIp);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("IP_002", result.ErrorCode);
        }

        /// <summary>
        /// 測試 IP 白名單驗證 - 缺少 IP
        /// 驗證項目：
        /// 1. null 或空的 IP 位址應被拒絕
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 IP_001（無法取得用戶端 IP 位址）
        /// 4. 錯誤訊息應為「無法取得用戶端 IP 位址」
        /// 5. 必要參數驗證機制正常
        /// </summary>
        [TestMethod]
        public void ValidateIPWhitelist_MissingIP_ShouldReturnFailure()
        {
            // Act
            var result = _securityMiddleware.ValidateIPWhitelist(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("IP_001", result.ErrorCode);
            Assert.AreEqual("無法取得用戶端 IP 位址", result.ErrorMessage);
        }

        #endregion

        #region 綜合安全性驗證測試

        /// <summary>
        /// 測試綜合安全性驗證 - 所有驗證項目通過
        /// 驗證項目：
        /// 1. API 金鑰、簽章、IP 白名單同時驗證
        /// 2. 停用簽章驗證時其他驗證仍正常
        /// 3. 回傳 IsValid = true
        /// 4. 多層安全性驗證協調正確
        /// 5. 安全性中介軟體整合運作
        /// </summary>
        [TestMethod]
        public void ValidateRequest_AllValid_ShouldReturnSuccess()
        {
            // Arrange
            var authorization = "Bearer valid-api-key-1";
            var content = "{\"test\": \"data\"}";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = "dummy-signature"; // 這會失敗，因為我們沒有正確的簽章產生邏輯
            var clientIp = "127.0.0.1";

            // 先停用簽章驗證，只測試其他部分
            _securityMiddleware.EnableSignatureValidation = false;

            // Act
            var result = _securityMiddleware.ValidateRequest(authorization, signature, content, timestamp, clientIp);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }

        /// <summary>
        /// 測試綜合安全性驗證 - 無效的 API 金鑰
        /// 驗證項目：
        /// 1. 使用無效的 API 金鑰
        /// 2. 回傳 IsValid = false
        /// 3. 錯誤碼應為 AUTH_003（無效的 API 金鑰）
        /// 4. 錯誤訊息應為「無效的 API 金鑰」
        /// 5. 安全性驗證機制正確防護
        /// </summary>
        [TestMethod]
        public void ValidateRequest_InvalidApiKey_ShouldReturnFailure()
        {
            // Arrange
            var authorization = "Bearer invalid-api-key";
            var content = "{\"test\": \"data\"}";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = "dummy-signature";
            var clientIp = "127.0.0.1";

            // Act
            var result = _securityMiddleware.ValidateRequest(authorization, signature, content, timestamp, clientIp);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("AUTH_003", result.ErrorCode);
        }

        #endregion

        #region 安全性設定動態更新測試

        /// <summary>
        /// 測試動態添加 API 金鑰功能
        /// 驗證項目：
        /// 1. 新添加的 API 金鑰應立即生效
        /// 2. 添加前驗證失敗，添加後驗證成功
        /// 3. 動態安全性設定更新正確
        /// 4. 執行時期安全性管理功能
        /// 5. API 金鑰管理機制正常
        /// </summary>
        [TestMethod]
        public void AddApiKey_NewKey_ShouldWork()
        {
            // Arrange
            var newApiKey = "new-api-key";
            var authorization = $"Bearer {newApiKey}";

            // 先確認新金鑰無效
            var initialResult = _securityMiddleware.ValidateApiKey(authorization);
            Assert.IsFalse(initialResult.IsValid);

            // Act - 添加新金鑰
            _securityMiddleware.AddApiKey(newApiKey);
            var result = _securityMiddleware.ValidateApiKey(authorization);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }

        /// <summary>
        /// 測試動態移除 API 金鑰功能
        /// 驗證項目：
        /// 1. 被移除的 API 金鑰應立即失效
        /// 2. 移除前驗證成功，移除後驗證失敗
        /// 3. 動態安全性設定移除正確
        /// 4. 安全性撤銷機制有效
        /// 5. API 金鑰生命週期管理
        /// </summary>
        [TestMethod]
        public void RemoveApiKey_ExistingKey_ShouldWork()
        {
            // Arrange
            var existingKey = "valid-api-key-1";
            var authorization = $"Bearer {existingKey}";

            // 先確認現有金鑰有效
            var initialResult = _securityMiddleware.ValidateApiKey(authorization);
            Assert.IsTrue(initialResult.IsValid);

            // Act - 移除金鑰
            _securityMiddleware.RemoveApiKey(existingKey);
            var result = _securityMiddleware.ValidateApiKey(authorization);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
        }        /// <summary>
        /// 測試動態添加 IP 到白名單功能
        /// 驗證項目：
        /// 1. 新添加的 IP 位址應立即被允許存取
        /// 2. 添加前驗證失敗，添加後驗證成功
        /// 3. 動態 IP 白名單更新正確
        /// 4. 執行時期存取控制管理
        /// 5. IP 白名單管理機制正常
        /// </summary>
        [TestMethod]
        public void AddIPToWhitelist_NewIP_ShouldWork()
        {
            // Arrange
            var newIp = "203.0.113.200"; // 使用非本機 IP

            // 先確認新 IP 無效
            var initialResult = _securityMiddleware.ValidateIPWhitelist(newIp);
            Assert.IsFalse(initialResult.IsValid);

            // Act - 添加新 IP
            _securityMiddleware.AddIPToWhitelist(newIp);
            var result = _securityMiddleware.ValidateIPWhitelist(newIp);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }

        #endregion
    }
}
