using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace DDSWebAPI.Services
{    /// <summary>
    /// 安全性中介軟體，處理 API 金鑰驗證、請求簽章驗證和 IP 白名單檢查
    /// </summary>
    public class SecurityMiddleware
    {
        private readonly HashSet<string> _validApiKeys;
        private readonly HashSet<string> _ipWhitelist;
        private readonly string _secretKey;

        /// <summary>
        /// 是否啟用 API 金鑰驗證
        /// </summary>
        public bool EnableApiKeyValidation { get; set; } = true;

        /// <summary>
        /// 是否啟用簽章驗證
        /// </summary>
        public bool EnableSignatureValidation { get; set; } = true;

        /// <summary>
        /// 是否啟用 IP 白名單
        /// </summary>
        public bool EnableIpWhitelist { get; set; } = true;

        /// <summary>
        /// 初始化安全性中介軟體
        /// </summary>
        /// <param name="validApiKeys">有效的 API 金鑰列表</param>
        /// <param name="ipWhitelist">IP 白名單</param>
        /// <param name="secretKey">簽章驗證密鑰</param>
        public SecurityMiddleware(IEnumerable<string> validApiKeys, IEnumerable<string> ipWhitelist, string secretKey)
        {
            _validApiKeys = new HashSet<string>(validApiKeys ?? new string[0]);
            _ipWhitelist = new HashSet<string>(ipWhitelist ?? new string[0]);
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
        }        /// <summary>
        /// 驗證 API 金鑰
        /// </summary>
        /// <param name="authorization">Authorization 標頭值</param>
        /// <returns>驗證結果</returns>
        public SecurityValidationResult ValidateApiKey(string authorization)
        {
            // 如果未啟用 API 金鑰驗證，直接通過
            if (!EnableApiKeyValidation)
            {
                return new SecurityValidationResult { IsValid = true };
            }

            if (string.IsNullOrEmpty(authorization))
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "AUTH_001",
                    ErrorMessage = "缺少 Authorization 標頭"
                };
            }

            // 檢查 Bearer Token 格式
            if (!authorization.StartsWith("Bearer "))
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "AUTH_002",
                    ErrorMessage = "Authorization 標頭格式錯誤，應為 'Bearer {token}'"
                };
            }

            var apiKey = authorization.Substring(7); // 移除 "Bearer " 前綴
            //KINSUS-API-KEY-2024
            if (!_validApiKeys.Contains(apiKey))
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "AUTH_003",
                    ErrorMessage = "無效的 API 金鑰"
                };
            }

            return new SecurityValidationResult { IsValid = true };
        }        /// <summary>
        /// 驗證請求簽章
        /// </summary>
        /// <param name="signature">請求簽章</param>
        /// <param name="content">請求內容</param>
        /// <param name="timestamp">時間戳記</param>
        /// <returns>驗證結果</returns>
        public SecurityValidationResult ValidateSignature(string signature, string content, string timestamp)
        {
            // 如果未啟用簽章驗證，直接通過
            if (!EnableSignatureValidation)
            {
                return new SecurityValidationResult { IsValid = true };
            }

            if (string.IsNullOrEmpty(signature))
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "SIG_001",
                    ErrorMessage = "缺少簽章"
                };
            }

            if (string.IsNullOrEmpty(timestamp))
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "SIG_002",
                    ErrorMessage = "缺少時間戳記"
                };
            }

            // 檢查時間戳記是否在有效範圍內（5分鐘內）
            if (!long.TryParse(timestamp, out long timestampValue))
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "SIG_003",
                    ErrorMessage = "時間戳記格式錯誤"
                };
            }

            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timestampDiff = Math.Abs(currentTimestamp - timestampValue);
            
            if (timestampDiff > 300) // 5分鐘 = 300秒
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "SIG_004",
                    ErrorMessage = "請求已過期"
                };
            }

            // 產生預期的簽章
            var expectedSignature = GenerateSignature(content, timestamp);

            if (signature != expectedSignature)
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "SIG_005",
                    ErrorMessage = "簽章驗證失敗"
                };
            }

            return new SecurityValidationResult { IsValid = true };
        }        /// <summary>
        /// 驗證 IP 白名單
        /// </summary>
        /// <param name="clientIp">用戶端 IP 位址</param>
        /// <returns>驗證結果</returns>
        public SecurityValidationResult ValidateIPWhitelist(string clientIp)
        {
            // 如果未啟用 IP 白名單，直接通過
            if (!EnableIpWhitelist)
            {
                return new SecurityValidationResult { IsValid = true };
            }

            if (string.IsNullOrEmpty(clientIp))
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "IP_001",
                    ErrorMessage = "無法取得用戶端 IP 位址"
                };
            }

            // 如果白名單為空，則允許所有 IP
            if (_ipWhitelist.Count == 0)
            {
                return new SecurityValidationResult { IsValid = true };
            }

            // 檢查是否在白名單中
            if (!_ipWhitelist.Contains(clientIp))
            {
                // 檢查是否為本機 IP
                if (IsLocalIP(clientIp))
                {
                    return new SecurityValidationResult { IsValid = true };
                }

                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorCode = "IP_002",
                    ErrorMessage = $"IP 位址 {clientIp} 不在白名單中"
                };
            }

            return new SecurityValidationResult { IsValid = true };
        }

        /// <summary>
        /// 執行完整的安全性驗證
        /// </summary>
        /// <param name="authorization">Authorization 標頭</param>
        /// <param name="signature">簽章</param>
        /// <param name="content">請求內容</param>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="clientIp">用戶端 IP</param>
        /// <returns>驗證結果</returns>
        public SecurityValidationResult ValidateRequest(string authorization, string signature, string content, string timestamp, string clientIp)
        {
            // 1. 驗證 IP 白名單
            var ipResult = ValidateIPWhitelist(clientIp);
            if (!ipResult.IsValid)
            {
                return ipResult;
            }

            // 2. 驗證 API 金鑰
            var apiKeyResult = ValidateApiKey(authorization);
            if (!apiKeyResult.IsValid)
            {
                return apiKeyResult;
            }

            // 3. 驗證簽章
            var signatureResult = ValidateSignature(signature, content, timestamp);
            if (!signatureResult.IsValid)
            {
                return signatureResult;
            }

            return new SecurityValidationResult { IsValid = true };
        }

        /// <summary>
        /// 產生 HMAC-SHA256 簽章
        /// </summary>
        /// <param name="content">內容</param>
        /// <param name="timestamp">時間戳記</param>
        /// <returns>簽章</returns>
        private string GenerateSignature(string content, string timestamp)
        {
            var message = $"{content}{timestamp}";
            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// 檢查是否為本機 IP
        /// </summary>
        /// <param name="ip">IP 位址</param>
        /// <returns>是否為本機 IP</returns>
        private bool IsLocalIP(string ip)
        {
            if (ip == "127.0.0.1" || ip == "::1" || ip == "localhost")
            {
                return true;
            }

            // 檢查是否為內網 IP
            if (IPAddress.TryParse(ip, out IPAddress address))
            {
                var bytes = address.GetAddressBytes();
                
                // 10.0.0.0/8
                if (bytes[0] == 10)
                    return true;
                
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return true;
                
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 新增 API 金鑰
        /// </summary>
        /// <param name="apiKey">API 金鑰</param>
        public void AddApiKey(string apiKey)
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                _validApiKeys.Add(apiKey);
            }
        }

        /// <summary>
        /// 移除 API 金鑰
        /// </summary>
        /// <param name="apiKey">API 金鑰</param>
        public void RemoveApiKey(string apiKey)
        {
            _validApiKeys.Remove(apiKey);
        }

        /// <summary>
        /// 新增 IP 到白名單
        /// </summary>
        /// <param name="ip">IP 位址</param>
        public void AddIPToWhitelist(string ip)
        {
            if (!string.IsNullOrEmpty(ip))
            {
                _ipWhitelist.Add(ip);
            }
        }

        /// <summary>
        /// 從白名單移除 IP
        /// </summary>
        /// <param name="ip">IP 位址</param>
        public void RemoveIPFromWhitelist(string ip)
        {
            _ipWhitelist.Remove(ip);
        }
    }

    /// <summary>
    /// 安全性驗證結果
    /// </summary>
    public class SecurityValidationResult
    {
        /// <summary>
        /// 是否驗證通過
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 錯誤代碼
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
