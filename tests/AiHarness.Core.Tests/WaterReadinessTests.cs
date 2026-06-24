using System.Collections.Generic;
using LouisPaulet.AiHarness;
using Xunit;

namespace AiHarness.Core.Tests;

public sealed class WaterReadinessTests {

  [Fact]
  public void Calculate_ComputesWaterMetricsFromPopulation() {
    var count = new AiHarnessResourceSnapshot(24, 30, 80, 0.5f, 24, 0, 0, 0, 0, 0);
    var population = new AiHarnessPopulation(adults: 3, children: 1, bots: 2);

    Dictionary<string, object> data = Snapshot(AiHarnessWaterReadiness.Calculate("Water", count, population));

    Assert.Equal("Water", data["goodId"]);
    Assert.Equal(24, data["availableWater"]);
    Assert.Equal(6.0, data["waterPerBeaver"]);
    Assert.Equal(20.0, data["waterCapacityPerBeaver"]);
    Assert.Equal(3.0, data["daysOfWater"]);
    Assert.Equal("daysOfWater = availableWater / max(1, beavers * 2)", data["rule"]);
    Assert.Equal("Water storage looks acceptable for the current population.", data["recommendation"]);
  }

  [Fact]
  public void Calculate_HandlesZeroPopulationWithoutDivisionByZero() {
    var count = new AiHarnessResourceSnapshot(12, 12, 0, 0f, 12, 0, 0, 0, 0, 0);
    var population = new AiHarnessPopulation(adults: 0, children: 0, bots: 0);

    Dictionary<string, object> data = Snapshot(AiHarnessWaterReadiness.Calculate("Water", count, population));

    Assert.Equal(12.0, data["waterPerBeaver"]);
    Assert.Equal(0.0, data["waterCapacityPerBeaver"]);
    Assert.Equal(12.0, data["daysOfWater"]);
    Assert.Equal("No beaver population detected yet.", data["recommendation"]);
  }

  private static Dictionary<string, object> Snapshot(object value) {
    return Assert.IsType<Dictionary<string, object>>(value);
  }

}
