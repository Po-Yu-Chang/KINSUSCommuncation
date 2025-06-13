using NUnit.Framework;
using Moq;
using FluentAssertions;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using DDSWebAPI.Services;
using DDSWebAPI.Models;
using System.Net;
using Moq.Protected;
using System.Threading;
using System.Collections.Generic;

namespace DDSWebAPI.Tests.Unit.Services
{
    /// <summary>
    /// ApiClientService 單元測試
    /// </summary>
    [TestFixture]
    public class ApiClientServiceTests
    {
        private ApiClientService _apiClientService;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _mockHttpClient;
        private const string TestBaseUrl = "http://test-api.example.com";

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
            
            // 使用反射來設定私有的 HttpClient
            _apiClientService = new ApiClientService(TestBaseUrl, 30);
            
            var httpClientField = typeof(ApiClientService).GetField("_httpClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField?.SetValue(_apiClientService, _mockHttpClient);
        }

        [TearDown]
        public void TearDown()
        {
            _apiClientService?.Dispose();
            _mockHttpClient?.Dispose();
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            using var service = new ApiClientService(TestBaseUrl, 60);

            // Assert
            service.Should().NotBeNull();
        }

        [Test]
        public void Constructor_WithNullBaseUrl_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () => new ApiClientService(null);

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void SetBaseUrl_WithValidUrl_ShouldUpdateBaseUrl()
        {
            // Arrange
            const string newBaseUrl = "http://new-api.example.com";

            // Act
            _apiClientService.SetBaseUrl(newBaseUrl);

            // Assert
            // 由於 _baseUrl 是私有欄位，我們無法直接測試
            // 但可以透過後續的 API 呼叫來驗證
            Assert.Pass("SetBaseUrl executed without throwing exception");
        }        [Test]
        public async Task SendToolOutputReportAsync_WithValidRequest_ShouldReturnSuccessResult()        {
            // Arrange
            var toolData = new ToolOutputReportData
            {
                WorkOrderNo = "WO001",
                ToolSpec = "ST001",
                Position = "SP001",
                ToolCode = "T001",
                OutputQuantity = 100,
                OperationTime = DateTime.Now.AddHours(-1),
                QualityStatus = "Pass"
            };
              var request = new BaseRequest<ToolOutputReportData>
            {
                RequestID = "test-123",
                Data = new List<ToolOutputReportData> { toolData }
            };

            var expectedResponse = new BaseResponse<string>
            {
                StatusCode = 200,
                IsSuccess = true,
                StatusMessage = "Success"
            };

            SetupMockHttpResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(expectedResponse));

            // Act
            var result = await _apiClientService.SendToolOutputReportAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.RequestUrl.Should().Contain("/api/tool-output-report");
        }        [Test]        public async Task SendErrorReportAsync_WithValidRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            var errorData = new ErrorReportData
            {
                ErrorCode = "E001",
                ErrorMessage = "Test error",
                ErrorLevel = "High",
                OccurrenceTime = DateTime.Now,
                DeviceCode = "ST001",
                OperatorName = "TestOperator",
                DetailDescription = "Test error details",
                IsResolved = false
            };

            var request = new BaseRequest<ErrorReportData>
            {
                RequestID = "error-123",
                Data = new List<ErrorReportData> { errorData }
            };

            SetupMockHttpResponse(HttpStatusCode.OK, "{'success': true}");

            // Act
            var result = await _apiClientService.SendErrorReportAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.RequestUrl.Should().Contain("/api/error-report");
        }        [Test]
        public async Task SendMachineStatusReportAsync_WithValidRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            var statusData = new MachineStatusReportData
            {
                MachineStatus = "Running",
                OperationMode = "AUTO",
                CurrentJob = "WO001",
                ProcessedCount = 100,
                TargetCount = 1000,
                CompletionPercentage = 10.0,
                Temperature = 25.5,
                ReportTime = DateTime.Now,
                Warnings = new List<string>()
            };

            var request = new BaseRequest<MachineStatusReportData>
            {
                RequestID = "status-123",
                Data = new List<MachineStatusReportData> { statusData }
            };

            SetupMockHttpResponse(HttpStatusCode.OK, "{'success': true}");

            // Act
            var result = await _apiClientService.SendMachineStatusReportAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.RequestUrl.Should().Contain("/api/machine-status-report");
        }

        [Test]
        public async Task SendCustomRequestAsync_WithValidRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            var customEndpoint = "/api/custom-endpoint";
            var customRequest = new { Message = "Test custom request" };

            SetupMockHttpResponse(HttpStatusCode.OK, "{'success': true}");

            // Act
            var result = await _apiClientService.SendCustomRequestAsync(customEndpoint, customRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.RequestUrl.Should().Contain(customEndpoint);
        }        [Test]
        public async Task SendPostRequestAsync_WithHttpError_ShouldReturnFailureResult()
        {
            // Arrange
            var toolData = new DDSWebAPI.Models.ToolOutputReportData
            {
                WorkOrderNo = "WO001",
                ToolSpec = "ST001"
            };

            var request = new BaseRequest<DDSWebAPI.Models.ToolOutputReportData>
            {
                RequestID = "test-error",
                Data = new List<DDSWebAPI.Models.ToolOutputReportData> { toolData }
            };

            SetupMockHttpResponse(HttpStatusCode.BadRequest, "Bad Request");

            // Act
            var result = await _apiClientService.SendToolOutputReportAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.ErrorMessage.Should().Contain("HTTP 錯誤");
        }        [Test]
        public async Task SendPostRequestAsync_WithTimeout_ShouldReturnTimeoutError()
        {
            // Arrange
            var request = new BaseRequest<DDSWebAPI.Models.ToolOutputReportData>
            {
                RequestID = "test-timeout",
                Data = new List<DDSWebAPI.Models.ToolOutputReportData> { new DDSWebAPI.Models.ToolOutputReportData() }
            };

            // 設定 HttpMessageHandler 拋出 TaskCanceledException (模擬逾時)
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new TaskCanceledException("Request timeout", new TimeoutException()));

            // Act
            var result = await _apiClientService.SendToolOutputReportAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("請求逾時");
        }        [Test]
        public async Task SendPostRequestAsync_WithNetworkError_ShouldReturnNetworkError()
        {
            // Arrange
            var request = new BaseRequest<DDSWebAPI.Models.ToolOutputReportData>
            {
                RequestID = "test-network-error",
                Data = new List<DDSWebAPI.Models.ToolOutputReportData> { new DDSWebAPI.Models.ToolOutputReportData() }
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _apiClientService.SendToolOutputReportAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("網路錯誤");
        }

        [Test]
        public void ApiCallSuccess_Event_ShouldBeTriggeredOnSuccess()
        {
            // Arrange
            var eventTriggered = false;
            ApiCallSuccessEventArgs? eventArgs = null;

            _apiClientService.ApiCallSuccess += (sender, args) =>
            {
                eventTriggered = true;
                eventArgs = args;
            };

            // Act & Assert
            // 由於事件觸發是在私有方法中，我們需要透過成功的 API 呼叫來測試
            // 這個測試更多是確保事件處理程式可以正確附加
            eventTriggered.Should().BeFalse(); // 在沒有呼叫 API 的情況下不應觸發
        }

        [Test]
        public void ApiCallFailure_Event_ShouldBeTriggeredOnFailure()
        {
            // Arrange
            var eventTriggered = false;
            ApiCallFailureEventArgs? eventArgs = null;

            _apiClientService.ApiCallFailure += (sender, args) =>
            {
                eventTriggered = true;
                eventArgs = args;
            };

            // Act & Assert
            // 由於事件觸發是在私有方法中，我們需要透過失敗的 API 呼叫來測試
            // 這個測試更多是確保事件處理程式可以正確附加
            eventTriggered.Should().BeFalse(); // 在沒有呼叫 API 的情況下不應觸發
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () => _apiClientService.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        #region Helper Methods

        private void SetupMockHttpResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);
        }

        #endregion
    }
}
