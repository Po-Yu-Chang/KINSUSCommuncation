using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DDSWebAPI.Services
{
    /// <summary>
    /// INI 設定檔讀取類別
    /// 提供從 INI 格式設定檔讀取設定參數的功能
    /// </summary>
    public class ConfigurationManager
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sections;
        private readonly string _configFilePath;

        /// <summary>
        /// 初始化設定管理器
        /// </summary>
        /// <param name="configFilePath">設定檔路徑</param>
        public ConfigurationManager(string configFilePath = "config.ini")
        {
            _configFilePath = configFilePath;
            _sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            LoadConfiguration();
        }

        /// <summary>
        /// 載入設定檔
        /// </summary>
        private void LoadConfiguration()
        {
            if (!File.Exists(_configFilePath))
            {
                throw new FileNotFoundException($"設定檔案不存在: {_configFilePath}");
            }

            try
            {
                string[] lines = File.ReadAllLines(_configFilePath);
                string currentSection = "";

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    // 跳過空行和註解行
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                        continue;

                    // 處理區段標題 [Section]
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        if (!_sections.ContainsKey(currentSection))
                        {
                            _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                        continue;
                    }

                    // 處理鍵值對
                    int equalIndex = trimmedLine.IndexOf('=');
                    if (equalIndex > 0 && !string.IsNullOrEmpty(currentSection))
                    {
                        string key = trimmedLine.Substring(0, equalIndex).Trim();
                        string value = trimmedLine.Substring(equalIndex + 1).Trim();
                        _sections[currentSection][key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"載入設定檔失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 取得字串設定值
        /// </summary>
        /// <param name="section">區段名稱</param>
        /// <param name="key">鍵名</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns>設定值</returns>
        public string GetString(string section, string key, string defaultValue = "")
        {
            if (_sections.ContainsKey(section) && _sections[section].ContainsKey(key))
            {
                return _sections[section][key];
            }
            return defaultValue;
        }

        /// <summary>
        /// 取得整數設定值
        /// </summary>
        /// <param name="section">區段名稱</param>
        /// <param name="key">鍵名</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns>設定值</returns>
        public int GetInt(string section, string key, int defaultValue = 0)
        {
            string value = GetString(section, key);
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 取得長整數設定值
        /// </summary>
        /// <param name="section">區段名稱</param>
        /// <param name="key">鍵名</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns>設定值</returns>
        public long GetLong(string section, string key, long defaultValue = 0)
        {
            string value = GetString(section, key);
            if (long.TryParse(value, out long result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 取得布林設定值
        /// </summary>
        /// <param name="section">區段名稱</param>
        /// <param name="key">鍵名</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns>設定值</returns>
        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            string value = GetString(section, key).ToLower();
            if (value == "true" || value == "1" || value == "yes" || value == "on")
            {
                return true;
            }
            if (value == "false" || value == "0" || value == "no" || value == "off")
            {
                return false;
            }
            return defaultValue;
        }

        /// <summary>
        /// 取得雙精度浮點數設定值
        /// </summary>
        /// <param name="section">區段名稱</param>
        /// <param name="key">鍵名</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns>設定值</returns>
        public double GetDouble(string section, string key, double defaultValue = 0.0)
        {
            string value = GetString(section, key);
            if (double.TryParse(value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 取得字串陣列設定值（用逗號分隔）
        /// </summary>
        /// <param name="section">區段名稱</param>
        /// <param name="key">鍵名</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns>設定值陣列</returns>
        public string[] GetStringArray(string section, string key, string[] defaultValue = null)
        {
            string value = GetString(section, key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue ?? new string[0];
            }
            return value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        /// <summary>
        /// 檢查設定是否存在
        /// </summary>
        /// <param name="section">區段名稱</param>
        /// <param name="key">鍵名</param>
        /// <returns>是否存在</returns>
        public bool HasSetting(string section, string key)
        {
            return _sections.ContainsKey(section) && _sections[section].ContainsKey(key);
        }

        /// <summary>
        /// 取得所有區段名稱
        /// </summary>
        /// <returns>區段名稱陣列</returns>
        public string[] GetSections()
        {
            return _sections.Keys.ToArray();
        }

        /// <summary>
        /// 取得指定區段的所有鍵名
        /// </summary>
        /// <param name="section">區段名稱</param>
        /// <returns>鍵名陣列</returns>
        public string[] GetKeys(string section)
        {
            if (_sections.ContainsKey(section))
            {
                return _sections[section].Keys.ToArray();
            }
            return new string[0];
        }

        /// <summary>
        /// 重新載入設定檔
        /// </summary>
        public void Reload()
        {
            _sections.Clear();
            LoadConfiguration();
        }

        /// <summary>
        /// 取得設定檔路徑
        /// </summary>
        public string ConfigFilePath => _configFilePath;
    }

    /// <summary>
    /// 應用程式設定類別
    /// 封裝應用程式所需的設定參數
    /// </summary>
    public class AppConfiguration
    {
        public ServerConfiguration Server { get; set; }
        public SecurityConfiguration Security { get; set; }
        public PerformanceConfiguration Performance { get; set; }
        public LoggingConfiguration Logging { get; set; }

        /// <summary>
        /// 從設定管理器載入設定
        /// </summary>
        /// <param name="configManager">設定管理器</param>
        /// <returns>應用程式設定</returns>
        public static AppConfiguration LoadFromConfigManager(ConfigurationManager configManager)
        {
            return new AppConfiguration
            {
                Server = new ServerConfiguration
                {
                    ServerUrl = configManager.GetString("Server", "ServerUrl", "http://localhost:8085/"),
                    RemoteApiUrl = configManager.GetString("Server", "RemoteApiUrl", "http://localhost:8086/"),
                    DeviceCode = configManager.GetString("Server", "DeviceCode", "KINSUS_DEFAULT"),
                    OperatorName = configManager.GetString("Server", "OperatorName", "DEFAULT_USER")
                },
                Security = new SecurityConfiguration
                {
                    ApiKey = configManager.GetString("Security", "ApiKey", ""),
                    SignatureSecret = configManager.GetString("Security", "SignatureSecret", ""),
                    EnableApiKeyValidation = configManager.GetBool("Security", "EnableApiKeyValidation", false),
                    EnableSignatureValidation = configManager.GetBool("Security", "EnableSignatureValidation", false),
                    EnableIpWhitelist = configManager.GetBool("Security", "EnableIpWhitelist", false),
                    AllowedIpAddresses = configManager.GetStringArray("Security", "AllowedIpAddresses", new string[0])
                },
                Performance = new PerformanceConfiguration
                {
                    MaxRequestsPerMinute = configManager.GetInt("Performance", "MaxRequestsPerMinute", 100),
                    MaxConcurrentConnections = configManager.GetInt("Performance", "MaxConcurrentConnections", 50),
                    MaxRequestSizeBytes = configManager.GetLong("Performance", "MaxRequestSizeBytes", 10485760),
                    RequestTimeoutSeconds = configManager.GetInt("Performance", "RequestTimeoutSeconds", 30),
                    MaxResponseSizeBytes = configManager.GetLong("Performance", "MaxResponseSizeBytes", 52428800)
                },
                Logging = new LoggingConfiguration
                {
                    LogLevel = configManager.GetString("Logging", "LogLevel", "INFO"),
                    EnableFileLogging = configManager.GetBool("Logging", "EnableFileLogging", false),
                    LogFilePath = configManager.GetString("Logging", "LogFilePath", "logs/dds_api.log"),
                    MaxLogFileSizeMB = configManager.GetInt("Logging", "MaxLogFileSizeMB", 10),
                    MaxLogFiles = configManager.GetInt("Logging", "MaxLogFiles", 5)
                }
            };
        }
    }

    /// <summary>
    /// 伺服器設定
    /// </summary>
    public class ServerConfiguration
    {
        public string ServerUrl { get; set; }
        public string RemoteApiUrl { get; set; }
        public string DeviceCode { get; set; }
        public string OperatorName { get; set; }
    }

    /// <summary>
    /// 安全性設定
    /// </summary>
    public class SecurityConfiguration
    {
        public string ApiKey { get; set; }
        public string SignatureSecret { get; set; }
        public bool EnableApiKeyValidation { get; set; }
        public bool EnableSignatureValidation { get; set; }
        public bool EnableIpWhitelist { get; set; }
        public string[] AllowedIpAddresses { get; set; }
    }

    /// <summary>
    /// 效能設定
    /// </summary>
    public class PerformanceConfiguration
    {
        public int MaxRequestsPerMinute { get; set; }
        public int MaxConcurrentConnections { get; set; }
        public long MaxRequestSizeBytes { get; set; }
        public int RequestTimeoutSeconds { get; set; }
        public long MaxResponseSizeBytes { get; set; }
    }

    /// <summary>
    /// 日誌設定
    /// </summary>
    public class LoggingConfiguration
    {
        public string LogLevel { get; set; }
        public bool EnableFileLogging { get; set; }
        public string LogFilePath { get; set; }
        public int MaxLogFileSizeMB { get; set; }
        public int MaxLogFiles { get; set; }
    }
}
