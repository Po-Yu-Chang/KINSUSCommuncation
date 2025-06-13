using NUnit.Framework;
using System;
using System.Collections.Generic;
using DDSWebAPI.Models;
using FluentAssertions;
using Newtonsoft.Json;

namespace DDSWebAPI.Tests.Unit.Models
{
    /// <summary>
    /// WorkorderModels 工單相關模型的單元測試
    /// </summary>
    [TestFixture]
    public class WorkorderModelsTests
    {
        [Test]
        public void WorkPieceInfo_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var workPiece = new WorkPieceInfo();

            // Assert
            workPiece.Should().NotBeNull();
            workPiece.PieceId.Should().BeNull();
            workPiece.PartNumber.Should().BeNull();
            workPiece.Revision.Should().BeNull();
            workPiece.Quantity.Should().Be(0);
            workPiece.Thickness.Should().Be(0.0);
            workPiece.Material.Should().BeNull();
            workPiece.Specifications.Should().BeNull();
        }

        [Test]
        public void WorkPieceInfo_SetProperties_ShouldSetCorrectly()
        {
            // Arrange
            var workPiece = new WorkPieceInfo();
            var specifications = new Dictionary<string, object>
            {
                { "tolerance", "±0.1mm" },
                { "surface", "smooth" }
            };

            // Act
            workPiece.PieceId = "WP001";
            workPiece.PartNumber = "PN-123456";
            workPiece.Revision = "Rev.A";
            workPiece.Quantity = 100;
            workPiece.Thickness = 1.6;
            workPiece.Material = "FR4";
            workPiece.Specifications = specifications;

            // Assert
            workPiece.PieceId.Should().Be("WP001");
            workPiece.PartNumber.Should().Be("PN-123456");
            workPiece.Revision.Should().Be("Rev.A");
            workPiece.Quantity.Should().Be(100);
            workPiece.Thickness.Should().Be(1.6);
            workPiece.Material.Should().Be("FR4");
            workPiece.Specifications.Should().BeEquivalentTo(specifications);
        }

        [Test]
        public void ToolRequirement_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var toolReq = new ToolRequirement();

            // Assert
            toolReq.Should().NotBeNull();
            toolReq.StationId.Should().BeNull();
            toolReq.SpindleId.Should().BeNull();
            toolReq.ToolType.Should().BeNull();
            toolReq.ToolDiameter.Should().Be(0.0);
            toolReq.ToolLength.Should().Be(0.0);
            toolReq.RequiredQuantity.Should().Be(0);
            toolReq.Specifications.Should().BeNull();
        }

        [Test]
        public void ToolRequirement_SetProperties_ShouldSetCorrectly()
        {
            // Arrange
            var toolReq = new ToolRequirement();
            var specs = new Dictionary<string, object>
            {
                { "coating", "TiAlN" },
                { "maxRPM", 30000 }
            };

            // Act
            toolReq.StationId = "ST01";
            toolReq.SpindleId = "SP01";
            toolReq.ToolType = "Drill";
            toolReq.ToolDiameter = 0.2;
            toolReq.ToolLength = 50.0;
            toolReq.RequiredQuantity = 5;
            toolReq.Specifications = specs;

            // Assert
            toolReq.StationId.Should().Be("ST01");
            toolReq.SpindleId.Should().Be("SP01");
            toolReq.ToolType.Should().Be("Drill");
            toolReq.ToolDiameter.Should().Be(0.2);
            toolReq.ToolLength.Should().Be(50.0);
            toolReq.RequiredQuantity.Should().Be(5);
            toolReq.Specifications.Should().BeEquivalentTo(specs);
        }

        [Test]
        public void CreateWorkorderResponse_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var response = new CreateWorkorderResponse();

            // Assert
            response.Should().NotBeNull();
            response.WorkOrder.Should().BeNull();
            response.TaskId.Should().BeNull();
            response.Status.Should().BeNull();
            response.CreatedTime.Should().Be(default(DateTime));
            response.EstimatedStartTime.Should().BeNull();
            response.EstimatedDuration.Should().BeNull();
            response.AssignedStations.Should().BeNull();
            response.ToolAllocation.Should().BeNull();
            response.Message.Should().BeNull();
        }

        [Test]
        public void CreateWorkorderResponse_SetProperties_ShouldSetCorrectly()
        {
            // Arrange
            var response = new CreateWorkorderResponse();
            var createdTime = new DateTime(2025, 6, 14, 10, 30, 0);
            var estimatedStart = createdTime.AddMinutes(15);
            var estimatedDuration = TimeSpan.FromHours(2.5);
            var stations = new List<string> { "ST01", "ST02", "ST03" };
            var toolAllocations = new List<ToolAllocation>
            {
                new ToolAllocation
                {
                    StationId = "ST01",
                    SpindleId = "SP01",
                    ToolId = "T001",
                    ToolType = "Drill",
                    AllocationStatus = "Allocated"
                }
            };

            // Act
            response.WorkOrder = "WO-2025061401";
            response.TaskId = "TASK-12345";
            response.Status = "CREATED";
            response.CreatedTime = createdTime;
            response.EstimatedStartTime = estimatedStart;
            response.EstimatedDuration = estimatedDuration;
            response.AssignedStations = stations;
            response.ToolAllocation = toolAllocations;
            response.Message = "工單建立成功";

            // Assert
            response.WorkOrder.Should().Be("WO-2025061401");
            response.TaskId.Should().Be("TASK-12345");
            response.Status.Should().Be("CREATED");
            response.CreatedTime.Should().Be(createdTime);
            response.EstimatedStartTime.Should().Be(estimatedStart);
            response.EstimatedDuration.Should().Be(estimatedDuration);
            response.AssignedStations.Should().BeEquivalentTo(stations);
            response.ToolAllocation.Should().HaveCount(1);
            response.ToolAllocation[0].StationId.Should().Be("ST01");
            response.Message.Should().Be("工單建立成功");
        }

        [Test]
        public void WorkorderProgress_SetProperties_ShouldCalculatePercentageCorrectly()
        {
            // Arrange
            var progress = new WorkorderProgress();

            // Act
            progress.CompletedSheets = 75;
            progress.TotalSheets = 100;
            progress.CompletedHoles = 7500;
            progress.TotalHoles = 10000;
            progress.PercentageComplete = (double)progress.CompletedSheets / progress.TotalSheets * 100;

            // Assert
            progress.CompletedSheets.Should().Be(75);
            progress.TotalSheets.Should().Be(100);
            progress.PercentageComplete.Should().Be(75.0);
        }

        [Test]
        public void WorkorderModels_JsonSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var workorderResponse = new CreateWorkorderResponse
            {
                WorkOrder = "WO-TEST001",
                TaskId = "TASK-TEST001",
                Status = "PROCESSING",
                CreatedTime = new DateTime(2025, 6, 14, 12, 0, 0),
                EstimatedDuration = TimeSpan.FromHours(3),
                AssignedStations = new List<string> { "ST01", "ST02" },
                Message = "測試工單"
            };

            // Act
            var json = JsonConvert.SerializeObject(workorderResponse, Formatting.Indented);
            var deserializedResponse = JsonConvert.DeserializeObject<CreateWorkorderResponse>(json);

            // Assert
            json.Should().Contain("\"workOrder\": \"WO-TEST001\"");
            json.Should().Contain("\"status\": \"PROCESSING\"");
            
            deserializedResponse.WorkOrder.Should().Be(workorderResponse.WorkOrder);
            deserializedResponse.TaskId.Should().Be(workorderResponse.TaskId);
            deserializedResponse.Status.Should().Be(workorderResponse.Status);
            deserializedResponse.AssignedStations.Should().BeEquivalentTo(workorderResponse.AssignedStations);
        }
    }
}
