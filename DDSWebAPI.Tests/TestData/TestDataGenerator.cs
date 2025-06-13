using System;
using System.Collections.Generic;
using DDSWebAPI.Models;

namespace DDSWebAPI.Tests.TestData
{
    /// <summary>
    /// 測試資料產生器 - 依照 KINSUS 通訊規格建立測試資料
    /// 涵蓋所有 API 指令的測試資料模型
    /// </summary>
    public static class TestDataGenerator
    {
        #region 伺服端角色 API 測試資料

        /// <summary>
        /// 建立遠程資訊下發指令測試資料
        /// </summary>
        public static SendMessageData CreateSendMessageData()
        {
            return new SendMessageData
            {
                Message = "請補充刀具庫存！",
                Level = "warning",
                Priority = "high",
                ActionType = 1,
                IntervalSecondTime = 30,
                ExtendData = null
            };
        }        /// <summary>
        /// 建立派針工單建立指令測試資料
        /// </summary>
        public static CreateNeedleWorkorderData CreateWorkorderData()
        {
            return new CreateNeedleWorkorderData
            {
                WorkOrderNo = "WO20250425001",
                ProductModel = "PM001",
                Quantity = 300,
                Priority = 1,
                ScheduledStartTime = DateTime.Now,
                ScheduledEndTime = DateTime.Now.AddHours(4),
                ToolSpecs = new List<ToolSpecData>
                {
                    new ToolSpecData
                    {
                        ToolCode = "T06",
                        ToolSpec = "0.15mm drill",
                        RequiredQuantity = 10,
                        Position = "A1"
                    }
                }
            };
        }

        /// <summary>
        /// 建立設備時間同步指令測試資料
        /// </summary>
        public static DateSyncData CreateDateSyncData()
        {
            return new DateSyncData
            {
                SyncDateTime = DateTime.Now,
                TimeZone = "+08:00",
                NtpServer = "time.windows.com"
            };
        }        /// <summary>
        /// 建立刀具工鑽袋檔發送指令測試資料
        /// </summary>
        public static SwitchRecipeData CreateSwitchRecipeData()
        {
            return new SwitchRecipeData
            {
                RecipeFileName = "RECIPE_001.xml",
                RecipeVersion = "1.0.0",
                RecipeContent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("<recipe><tool>T06</tool><size>0.15</size></recipe>")),
                WorkMode = "AUTO",
                RecipeType = "DRILL"
            };
        }

        /// <summary>
        /// 建立設備啟停控制指令測試資料
        /// </summary>
        public static DeviceControlData CreateDeviceControlData()
        {
            return new DeviceControlData
            {
                Command = "START",
                TargetDevice = "ALL",
                Parameters = new Dictionary<string, object>
                {
                    { "mode", "auto" },
                    { "speed", 100 }
                },
                ForceExecute = false
            };
        }        /// <summary>
        /// 建立倉庫資源查詢指令測試資料
        /// </summary>
        public static WarehouseResourceQueryData CreateWarehouseResourceQueryData()
        {
            return new WarehouseResourceQueryData
            {
                QueryType = "BY_TOOL_CODE",
                ToolCode = "T06",
                PositionCode = null,
                StatusFilter = "AVAILABLE"
            };
        }        /// <summary>
        /// 建立鑽針履歷查詢指令測試資料
        /// </summary>
        public static ToolTraceHistoryQueryData CreateToolTraceHistoryQueryData()
        {
            return new ToolTraceHistoryQueryData
            {
                ToolCode = "T06",
                StartTime = DateTime.Now.AddDays(-7),
                EndTime = DateTime.Now,
                QueryType = "ALL",
                PageNumber = 1,
                PageSize = 20
            };
        }

        /// <summary>
        /// 建立鑽針履歷回報指令測試資料
        /// </summary>
        public static ToolTraceHistoryReportData CreateToolTraceHistoryReportData()
        {
            return new ToolTraceHistoryReportData
            {
                ToolId = "T123456",
                Axis = "A",
                MachineId = "MC01",
                Product = "P20250425A",
                GrindCount = 6,
                TrayId = "TRAY01",
                TraySlot = 3,
                History = new List<ToolHistoryItem>
                {
                    new ToolHistoryItem
                    {
                        UseTime = DateTime.Now.AddHours(-1),
                        MachineId = "MC01",
                        Axis = "A",
                        Product = "P20250425A",
                        GrindCount = 5,
                        TrayId = "TRAY01",
                        TraySlot = 2
                    }
                },
                ExtendData = null
            };
        }

        #endregion

        #region 用戶端角色 API 測試資料        /// <summary>
        /// 建立配針回報上傳測試資料
        /// </summary>
        public static ToolOutputReportData CreateToolOutputReportData()
        {
            return new ToolOutputReportData
            {
                WorkOrderNo = "WO20250425001",
                ToolCode = "T06",
                ToolSpec = "0.15mm",
                OutputQuantity = 100,
                OperationTime = DateTime.Now,
                OperationType = "DRILL",
                Position = "A1",
                QualityStatus = "PASS",
                Remarks = "正常作業"
            };
        }        /// <summary>
        /// 建立錯誤回報上傳測試資料
        /// </summary>
        public static ErrorReportData CreateErrorReportData()
        {
            return new ErrorReportData
            {
                ErrorCode = "E001",
                ErrorMessage = "刀具庫存不足",
                ErrorLevel = "WARNING",
                OccurrenceTime = DateTime.Now,
                DeviceCode = "MC01",
                OperatorName = "TEST_OP",
                DetailDescription = "T06 刀具庫存低於安全庫存量",
                IsResolved = false
            };
        }        /// <summary>
        /// 建立機臺狀態上報測試資料
        /// </summary>
        public static MachineStatusReportData CreateMachineStatusReportData()
        {
            return new MachineStatusReportData
            {
                MachineStatus = "RUNNING",
                OperationMode = "AUTO",
                CurrentJob = "WO20250425001",
                ProcessedCount = 150,
                TargetCount = 300,
                CompletionPercentage = 50.0,
                Temperature = 25.5,
                Pressure = 1.2,
                Vibration = 0.1,
                ReportTime = DateTime.Now,
                Warnings = new List<string> { "溫度稍高" }
            };
        }

        #endregion

        #region 安全性測試資料

        /// <summary>
        /// 建立有效的 API 金鑰測試資料
        /// </summary>
        public static string CreateValidApiKey()
        {
            return "test-api-key-123456";
        }

        /// <summary>
        /// 建立無效的 API 金鑰測試資料
        /// </summary>
        public static string CreateInvalidApiKey()
        {
            return "invalid-api-key";
        }

        /// <summary>
        /// 建立測試請求簽章
        /// </summary>
        public static string CreateTestSignature(string data)
        {
            // 簡化的測試簽章，實際應該使用真實的簽章演算法
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"signature-{data}"));
        }

        /// <summary>
        /// 建立測試 IP 位址 - 白名單內
        /// </summary>
        public static string CreateWhitelistIP()
        {
            return "192.168.1.100";
        }

        /// <summary>
        /// 建立測試 IP 位址 - 白名單外
        /// </summary>
        public static string CreateBlacklistIP()
        {
            return "10.0.0.1";
        }

        #endregion

        #region 效能測試資料

        /// <summary>
        /// 建立大量測試資料
        /// </summary>
        public static List<SendMessageData> CreateBulkMessageData(int count)
        {
            var messages = new List<SendMessageData>();
            for (int i = 0; i < count; i++)
            {
                messages.Add(new SendMessageData
                {
                    Message = $"測試訊息 {i + 1}",
                    Level = i % 2 == 0 ? "info" : "warning",
                    Priority = i % 3 == 0 ? "high" : "normal",
                    ActionType = 1,
                    IntervalSecondTime = 30,
                    ExtendData = null
                });
            }
            return messages;
        }

        /// <summary>
        /// 建立超大資料測試
        /// </summary>
        public static SendMessageData CreateLargeMessageData()
        {
            return new SendMessageData
            {
                Message = new string('A', 10000), // 10KB 的訊息
                Level = "info",
                Priority = "normal",
                ActionType = 1,
                IntervalSecondTime = 30,
                ExtendData = new string('B', 50000) // 50KB 的擴充資料
            };
        }

        #endregion
    }
}
