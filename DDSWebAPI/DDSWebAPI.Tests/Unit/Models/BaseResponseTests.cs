using NUnit.Framework;
using System;
using System.Collections.Generic;
using DDSWebAPI.Models;
using FluentAssertions;
using Newtonsoft.Json;

namespace DDSWebAPI.Tests.Unit.Models
{
    /// <summary>
    /// BaseResponse 基礎回應類別的單元測試
    /// </summary>
    [TestFixture]
    public class BaseResponseTests
    {
        [Test]
        public void BaseResponse_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var response = new BaseResponse();

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.IsSuccess.Should().BeTrue(); // 相容性屬性預設為 true
            response.RequestId.Should().BeNull();
            response.Message.Should().BeNull();
            response.Data.Should().BeNull();
            response.StatusCode.Should().Be(200); // 預設為 200
        }

        [Test]
        public void BaseResponse_SetProperties_ShouldSetCorrectly()
        {
            // Arrange
            var response = new BaseResponse();
            var testRequestId = "test-request-123";
            var testMessage = "測試訊息";
            var testData = new { test = "data" };
            var testTimestamp = DateTime.Now;

            // Act
            response.RequestId = testRequestId;
            response.Success = true;
            response.Message = testMessage;
            response.Data = testData;
            response.Timestamp = testTimestamp;
            response.StatusCode = 201;

            // Assert
            response.RequestId.Should().Be(testRequestId);
            response.Success.Should().BeTrue();
            response.StatusCode.Should().Be(201);
            response.Message.Should().Be(testMessage);
            response.Data.Should().Be(testData);
            response.Timestamp.Should().Be(testTimestamp);
        }

        [Test]
        public void BaseResponse_JsonSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var response = new BaseResponse
            {
                RequestId = "test-123",
                Success = true,
                Message = "操作成功",
                Data = new { count = 5, items = new[] { "item1", "item2" } },
                Timestamp = new DateTime(2025, 6, 14, 10, 30, 0)
            };

            // Act
            var json = JsonConvert.SerializeObject(response, Formatting.Indented);
            var deserializedResponse = JsonConvert.DeserializeObject<BaseResponse>(json);

            // Assert
            json.Should().Contain("\"requestId\": \"test-123\"");
            json.Should().Contain("\"success\": true");
            json.Should().Contain("\"message\": \"操作成功\"");
            
            deserializedResponse.RequestId.Should().Be(response.RequestId);
            deserializedResponse.Success.Should().Be(response.Success);
            deserializedResponse.Message.Should().Be(response.Message);
        }

        [Test]
        public void BaseResponseGeneric_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var response = new BaseResponse<string>();

            // Assert
            response.Should().NotBeNull();
            response.ResponseID.Should().BeNull();
            response.ServiceName.Should().BeNull();
            response.TimeStamp.Should().BeNull();
            response.DevCode.Should().BeNull();
            response.StatusCode.Should().Be(0);
            response.IsSuccess.Should().BeTrue();
            response.Data.Should().BeNull();
        }        [Test]
        public void BaseResponseGeneric_SetProperties_ShouldSetCorrectly()
        {
            // Arrange
            var response = new BaseResponse<string>();
            var testData = new List<string> { "item1", "item2", "item3" };

            // Act
            response.ResponseID = "response-123";
            response.ServiceName = "TestService";
            response.TimeStamp = "2025-06-14T10:30:00";
            response.DevCode = "DEV001";
            response.StatusCode = 200;
            response.IsSuccess = true;
            response.StatusMessage = "操作成功";
            response.Data = testData;

            // Assert
            response.ResponseID.Should().Be("response-123");
            response.ServiceName.Should().Be("TestService");
            response.TimeStamp.Should().Be("2025-06-14T10:30:00");
            response.DevCode.Should().Be("DEV001");
            response.StatusCode.Should().Be(200);
            response.IsSuccess.Should().BeTrue();
            response.StatusMessage.Should().Be("操作成功");
            response.Data.Should().BeEquivalentTo(testData);
        }

        [Test]
        public void BaseResponseGeneric_JsonSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var testData = new List<string> { "test1", "test2" };
            var response = new BaseResponse<string>
            {
                ResponseID = "resp-456",
                ServiceName = "DataService",
                StatusCode = 200,
                IsSuccess = true,
                Data = testData
            };

            // Act
            var json = JsonConvert.SerializeObject(response, Formatting.Indented);
            var deserializedResponse = JsonConvert.DeserializeObject<BaseResponse<string>>(json);

            // Assert
            json.Should().Contain("\"responseID\": \"resp-456\"");
            json.Should().Contain("\"serviceName\": \"DataService\"");
            json.Should().Contain("\"statusCode\": 200");
            json.Should().Contain("\"isSuccess\": true");
            
            deserializedResponse!.ResponseID.Should().Be(response.ResponseID);
            deserializedResponse.ServiceName.Should().Be(response.ServiceName);
            deserializedResponse.StatusCode.Should().Be(response.StatusCode);
            deserializedResponse.IsSuccess.Should().Be(response.IsSuccess);
            deserializedResponse.Data.Should().BeEquivalentTo(testData);
        }

        [Test]
        public void BaseSingleResponse_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var response = new BaseSingleResponse<string>();

            // Assert
            response.Should().NotBeNull();
            response.ResponseID.Should().BeNull();
            response.ServiceName.Should().BeNull();
            response.TimeStamp.Should().BeNull();
            response.DevCode.Should().BeNull();
            response.StatusCode.Should().Be(0);
            response.StatusMessage.Should().BeNull();
            response.Data.Should().BeNull();
        }        [Test]
        public void BaseSingleResponse_SetProperties_ShouldSetCorrectly()
        {
            // Arrange
            var response = new BaseSingleResponse<int>();
            var testData = 42;

            // Act
            response.ResponseID = "single-789";
            response.ServiceName = "SingleService";
            response.TimeStamp = "2025-06-14T11:00:00";
            response.DevCode = "DEV002";
            response.StatusCode = 201;
            response.StatusMessage = "建立成功";
            response.Data = testData;

            // Assert
            response.ResponseID.Should().Be("single-789");
            response.ServiceName.Should().Be("SingleService");
            response.TimeStamp.Should().Be("2025-06-14T11:00:00");
            response.DevCode.Should().Be("DEV002");
            response.StatusCode.Should().Be(201);
            response.StatusMessage.Should().Be("建立成功");
            response.Data.Should().Be(testData);
        }

        [Test]
        public void ResponseStatusCode_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            ResponseStatusCode.Success.Should().Be(200);
            ResponseStatusCode.BadRequest.Should().Be(400);
            ResponseStatusCode.Unauthorized.Should().Be(401);
            ResponseStatusCode.Forbidden.Should().Be(403);
            ResponseStatusCode.NotFound.Should().Be(404);
            ResponseStatusCode.InternalServerError.Should().Be(500);
            ResponseStatusCode.ServiceUnavailable.Should().Be(503);
        }
    }
}
