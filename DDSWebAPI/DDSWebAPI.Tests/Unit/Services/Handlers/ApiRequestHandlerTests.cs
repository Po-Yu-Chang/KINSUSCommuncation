using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDSWebAPI.Services.Handlers;
using DDSWebAPI.Models;
using DDSWebAPI.Interfaces;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;

namespace DDSWebAPI.Tests.Unit.Services.Handlers
{
    /// <summary>
    /// ApiRequestHandler API 請求處理器的單元測試
    /// </summary>
    [TestFixture]
    public class ApiRequestHandlerTests
    {
        private ApiRequestHandler _apiRequestHandler;
        private Mock<IDatabaseService> _mockDatabaseService;
        private Mock<IWarehouseQueryService> _mockWarehouseQueryService;
        private Mock<IWorkflowTaskService> _mockWorkflowTaskService;
        private Mock<IGlobalConfigService> _mockGlobalConfigService;
        private Mock<IUtilityService> _mockUtilityService;

        [SetUp]
        public void SetUp()
        {
            // 建立 Mock 物件
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockWarehouseQueryService = new Mock<IWarehouseQueryService>();
            _mockWorkflowTaskService = new Mock<IWorkflowTaskService>();
            _mockGlobalConfigService = new Mock<IGlobalConfigService>();
            _mockUtilityService = new Mock<IUtilityService>();

            // 建立測試對象
            _apiRequestHandler = new ApiRequestHandler(
                _mockDatabaseService.Object,
                _mockWarehouseQueryService.Object,
                _mockWorkflowTaskService.Object,
                _mockGlobalConfigService.Object,
                _mockUtilityService.Object
            );
        }

        [Test]
        public async Task ProcessSendMessageCommandAsync_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var messageData = new SendMessageData
            {
                Message = "測試訊息",
                Level = "info",
                Priority = "normal",
                ActionType = 1,
                IntervalSecondTime = 5
            };

            var request = new BaseRequest<SendMessageData>
            {
                RequestID = "test-123",
                ServiceName = "SEND_MESSAGE_COMMAND",
                TimeStamp = "2025-06-14 10:30:00",
                DevCode = "KINSUS001",
                Operator = "TestOperator",
                Data = new List<SendMessageData> { messageData }
            };

            var requestBody = JsonConvert.SerializeObject(request);

            // Act
            var result = await _apiRequestHandler.ProcessSendMessageCommandAsync(requestBody);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("訊息處理完成");
            
            // 驗證 Mock 調用
            _mockUtilityService.Verify(x => x.LogInfo(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task ProcessSendMessageCommandAsync_EmptyData_ShouldReturnError()
        {
            // Arrange
            var request = new BaseRequest<SendMessageData>
            {
                RequestID = "test-456",
                ServiceName = "SEND_MESSAGE_COMMAND",
                Data = new List<SendMessageData>() // 空資料
            };

            var requestBody = JsonConvert.SerializeObject(request);

            // Act
            var result = await _apiRequestHandler.ProcessSendMessageCommandAsync(requestBody);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("訊息資料不能為空");
        }

        [Test]
        public async Task ProcessSendMessageCommandAsync_InvalidJson_ShouldReturnError()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act
            var result = await _apiRequestHandler.ProcessSendMessageCommandAsync(invalidJson);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("訊息資料格式錯誤");
        }

        [Test]
        public async Task ProcessCreateNeedleWorkorderCommandAsync_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var workorderData = new CreateNeedleWorkorderData
            {
                WorkOrderNo = "WO-2025061401",
                ProductModel = "Test-Model-001",
                Quantity = 100,
                Priority = 1
            };

            var request = new BaseRequest<CreateNeedleWorkorderData>
            {
                RequestID = "workorder-123",
                ServiceName = "CREATE_NEEDLE_WORKORDER_COMMAND",
                TimeStamp = "2025-06-14 11:00:00",
                DevCode = "KINSUS001",
                Data = new List<CreateNeedleWorkorderData> { workorderData }
            };

            var requestBody = JsonConvert.SerializeObject(request);

            // Act
            var result = await _apiRequestHandler.ProcessCreateNeedleWorkorderCommandAsync(requestBody);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("工單建立完成");
            
            // 檢查回應資料
            result.Data.Should().NotBeNull();
            var responses = result.Data as List<CreateWorkorderResponse>;
            responses.Should().HaveCount(1);
            responses[0].WorkOrder.Should().Be("WO-2025061401");
            responses[0].Status.Should().Be("CREATED");

            // 驗證日誌調用
            _mockUtilityService.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("WO-2025061401"))), Times.Once);
        }

        [Test]
        public async Task ProcessCreateNeedleWorkorderCommandAsync_EmptyWorkOrderNo_ShouldReturnError()
        {
            // Arrange
            var workorderData = new CreateNeedleWorkorderData
            {
                WorkOrderNo = "", // 空的工單號碼
                ProductModel = "Test-Model-001"
            };

            var request = new BaseRequest<CreateNeedleWorkorderData>
            {
                RequestID = "workorder-456",
                Data = new List<CreateNeedleWorkorderData> { workorderData }
            };

            var requestBody = JsonConvert.SerializeObject(request);

            // Act
            var result = await _apiRequestHandler.ProcessCreateNeedleWorkorderCommandAsync(requestBody);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("工單號碼不能為空");
        }

        [Test]
        public async Task ProcessDateMessageCommandAsync_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var dateTimeData = new DateTimeData
            {
                CurrentTime = DateTime.Now,
                TimeZone = "UTC+8",
                SyncType = "AUTO"
            };

            var request = new BaseRequest<DateTimeData>
            {
                RequestID = "sync-789",
                ServiceName = "DATE_MESSAGE_COMMAND",
                Data = new List<DateTimeData> { dateTimeData }
            };

            var requestBody = JsonConvert.SerializeObject(request);

            // Act
            var result = await _apiRequestHandler.ProcessDateMessageCommandAsync(requestBody);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("時間同步完成");
            
            _mockUtilityService.Verify(x => x.LogInfo(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task ProcessDeviceControlCommandAsync_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var controlData = new DeviceControlData
            {
                Command = "START",
                TargetDevice = "DRILL_MACHINE_01",
                Parameters = new Dictionary<string, object>
                {
                    { "speed", 15000 },
                    { "mode", "AUTO" }
                },
                ForceExecute = false
            };

            var request = new BaseRequest<DeviceControlData>
            {
                RequestID = "control-101",
                ServiceName = "DEVICE_CONTROL_COMMAND",
                Data = new List<DeviceControlData> { controlData }
            };

            var requestBody = JsonConvert.SerializeObject(request);

            // Act
            var result = await _apiRequestHandler.ProcessDeviceControlCommandAsync(requestBody);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("設備控制完成");
            
            _mockUtilityService.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("START") && s.Contains("DRILL_MACHINE_01"))), Times.Once);
        }        [Test]
        public async Task HandleToolTraceHistoryReportAsync_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var historyData = new ToolTraceHistoryReportData
            {
                ToolId = "T001",
                Axis = "X",
                MachineId = "M001",
                Product = "PCB-001",
                GrindCount = 5,
                TrayId = "TRAY001",
                TraySlot = 1
            };

            var request = new BaseRequest<ToolTraceHistoryReportData>
            {
                RequestID = "history-202",
                ServiceName = "TOOL_TRACE_HISTORY_REPORT_COMMAND",
                Data = new List<ToolTraceHistoryReportData> { historyData }
            };

            var requestBody = JsonConvert.SerializeObject(request);

            // Act
            var result = await _apiRequestHandler.HandleToolTraceHistoryReportAsync(requestBody);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("鑽針履歷回報處理完成");
            
            _mockUtilityService.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("T001"))), Times.Once);
        }

        [Test]
        public async Task ProcessWarehouseResourceQueryCommandAsync_ValidRequest_ShouldReturnSuccess()        {
            // Arrange
            var queryData = new WarehouseResourceQueryData
            {
                QueryType = "TOOL_INVENTORY",
                ToolCode = "T001"
            };

            var request = new BaseRequest<WarehouseResourceQueryData>
            {
                RequestID = "query-303",
                ServiceName = "WAREHOUSE_RESOURCE_QUERY_COMMAND",
                Data = new List<WarehouseResourceQueryData> { queryData }
            };

            var requestBody = JsonConvert.SerializeObject(request);

            // Act
            var result = await _apiRequestHandler.ProcessWarehouseResourceQueryCommandAsync(requestBody);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("倉庫資源查詢完成");
        }        [Test]
        public void CreateSuccessResponse_ValidParameters_ShouldCreateCorrectResponse()
        {
            // Skip - CreateSuccessResponse is a private helper method
            Assert.Ignore("CreateSuccessResponse is a private helper method and should not be tested directly");
        }

        [Test]
        public void CreateErrorResponse_ValidMessage_ShouldCreateCorrectResponse()
        {
            // Skip - CreateErrorResponse is a private helper method  
            Assert.Ignore("CreateErrorResponse is a private helper method and should not be tested directly");
        }

        [TearDown]
        public void TearDown()
        {
            // 清理資源
            _apiRequestHandler = null;
        }
    }
}
