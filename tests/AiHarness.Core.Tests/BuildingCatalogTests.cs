using System;
using System.Linq;
using LouisPaulet.AiHarness;
using Xunit;

namespace AiHarness.Core.Tests;

public sealed class BuildingCatalogTests {

  [Fact]
  public void Resolve_PicksSmallWaterTankForWaterTankAlias() {
    AiHarnessBuildingTemplate resolved = AiHarnessBuildingCatalog.Resolve(Templates(), "water_tank");

    Assert.Equal("SmallTank.Folktails", resolved.TemplateName);
  }

  [Fact]
  public void Resolve_MapsRoadAliasToPathTemplate() {
    AiHarnessBuildingTemplate resolved = AiHarnessBuildingCatalog.Resolve(Templates(), "road");

    Assert.Equal("Path.Folktails", resolved.TemplateName);
  }

  [Fact]
  public void Resolve_ReportsAmbiguousTextMatches() {
    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        AiHarnessBuildingCatalog.Resolve(Templates(), "storage"));

    Assert.Contains("Building template query is ambiguous: storage.", exception.Message);
    Assert.Contains("SmallTank.Folktails", exception.Message);
    Assert.Contains("MediumTank.Folktails", exception.Message);
  }

  [Fact]
  public void Filter_SearchesNormalizedTemplateText() {
    AiHarnessBuildingTemplate[] matches = AiHarnessBuildingCatalog.Filter(Templates(), "tank").ToArray();

    Assert.Equal(new[] { "MediumTank.Folktails", "SmallTank.Folktails" }, matches.Select(template => template.TemplateName).ToArray());
  }

  private static AiHarnessBuildingTemplate[] Templates() {
    return new[] {
      new AiHarnessBuildingTemplate("MediumTank.Folktails", "MediumTank", "WaterStorageMedium.DisplayName", "stores water", "Storage", 20),
      new AiHarnessBuildingTemplate("Path.Folktails", "Path", "Path.DisplayName", "basic path", "Paths", 1),
      new AiHarnessBuildingTemplate("SmallTank.Folktails", "SmallTank", "WaterTankSmall.DisplayName", "small water storage", "Storage", 10),
      new AiHarnessBuildingTemplate("Platform.Folktails", "Platform", "Platform.DisplayName", "platform", "Platforms", 30)
    };
  }

}
