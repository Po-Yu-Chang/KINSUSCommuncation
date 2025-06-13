using NUnit.Framework;
using System;
using System.Collections.Generic;
using DDSWebAPI.Models;
using FluentAssertions;
using Newtonsoft.Json;

namespace DDSWebAPI.Tests.Unit.Models
{
    /// <summary>
    /// BaseRequest 基礎請求類別的單元測試
    /// </summary>
    [TestFixture]
    public class BaseRequestTests
    {
        [Test]
        public void BaseRequest_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var request = new BaseRequest<string>();

            // Assert
            request.Should().NotBeNull();
            request.RequestID.Should().BeNull();
            request.ServiceName.Should().BeNull();
            request.TimeStamp.Should().BeNull();
            request.DevCode.Should().BeNull();
            request.Operator.Should().BeNull();
            request.Data.Should().BeNull();
        }

        [Test]
        public void BaseRequest_SetProperties_ShouldSetCorrectly()
        {            // Arrange
            var request = new BaseRequest<string>();
            var testData = new List<string> { "data1", "data2" };

            // Act
            request.RequestID = "req-123";
            request.ServiceName = "TEST_SERVICE";
            request.TimeStamp = "2025-06-14 10:30:00";
            request.DevCode = "KINSUS001";
            request.Operator = "TestOperator";
            request.Data = testData;

            // Assert
            request.RequestID.Should().Be("req-123");
            request.ServiceName.Should().Be("TEST_SERVICE");
            request.TimeStamp.Should().Be("2025-06-14 10:30:00");
            request.DevCode.Should().Be("KINSUS001");
            request.Operator.Should().Be("TestOperator");
            request.Data.Should().BeEquivalentTo(testData);
        }

        [Test]
        public void BaseRequest_JsonSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var request = new BaseRequest<SendMessageData>
            {
                RequestID = "msg-789",
                ServiceName = "SEND_MESSAGE_COMMAND",
                TimeStamp = "2025-06-14 15:30:00",
                DevCode = "KINSUS002",
                Operator = "SYSTEM",
                Data = new List<SendMessageData>
                {
                    new SendMessageData
                    {
                        Message = "測試訊息",
                        Level = "info",
                        Priority = "normal",
                        ActionType = 1,
                        IntervalSecondTime = 5
                    }
                }
            };

            // Act
            var json = JsonConvert.SerializeObject(request, Formatting.Indented);
            var deserializedRequest = JsonConvert.DeserializeObject<BaseRequest<SendMessageData>>(json);

            // Assert
            json.Should().Contain("\"requestID\": \"msg-789\"");
            json.Should().Contain("\"serviceName\": \"SEND_MESSAGE_COMMAND\"");
            json.Should().Contain("\"devCode\": \"KINSUS002\"");
              deserializedRequest!.RequestID.Should().Be(request.RequestID);
            deserializedRequest.ServiceName.Should().Be(request.ServiceName);
            deserializedRequest.DevCode.Should().Be(request.DevCode);
            deserializedRequest.Data.Should().HaveCount(1);
            deserializedRequest.Data[0].Message.Should().Be("測試訊息");
        }

        [Test]
        public void BaseRequest_ExtendData_ShouldHandleComplexObjects()
        {
            // Arrange
            var request = new BaseRequest<object>();
            var extendData = new 
            { 
                customField1 = "value1",
                customField2 = 123,
                customField3 = new[] { "item1", "item2" }
            };

            // Act
            request.ExtendData = extendData;

            // Assert
            request.ExtendData.Should().NotBeNull();
            request.ExtendData.Should().BeEquivalentTo(extendData);
        }

        [Test]
        public void BaseRequest_Validation_ShouldValidateRequiredFields()
        {
            // Arrange
            var request = new BaseRequest<string>();

            // Act & Assert - 檢查必要欄位
            Action setEmptyRequestId = () => request.RequestID = "";
            Action setNullServiceName = () => request.ServiceName = null;

            // 這些操作應該不會拋出例外，但可以用於未來的驗證邏輯
            setEmptyRequestId.Should().NotThrow();
            setNullServiceName.Should().NotThrow();

            // 驗證設定的值
            request.RequestID.Should().Be("");
            request.ServiceName.Should().BeNull();
        }

        [Test]
        public void BaseRequest_MultipleDataTypes_ShouldHandleDifferentTypes()
        {
            // Arrange & Act
            var stringRequest = new BaseRequest<string> { Data = new List<string> { "test" } };
            var intRequest = new BaseRequest<int> { Data = new List<int> { 1, 2, 3 } };
            var objectRequest = new BaseRequest<object> { Data = new List<object> { new { id = 1 }, "string", 123 } };

            // Assert
            stringRequest.Data.Should().HaveCount(1);
            stringRequest.Data[0].Should().Be("test");

            intRequest.Data.Should().HaveCount(3);
            intRequest.Data.Should().Contain(new[] { 1, 2, 3 });

            objectRequest.Data.Should().HaveCount(3);
            objectRequest.Data[1].Should().Be("string");
            objectRequest.Data[2].Should().Be(123);
        }
    }
}
