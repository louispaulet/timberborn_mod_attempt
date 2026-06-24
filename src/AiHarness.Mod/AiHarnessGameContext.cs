using System;
using System.Collections.Generic;
using System.Linq;
using Timberborn.CameraSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.ResourceCountingSystem;
using Timberborn.TimeSystem;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessGameContext {

    private readonly CameraService _cameraService;
    private readonly DistrictCenterRegistry _districtCenterRegistry;
    private readonly DistrictContextService _districtContextService;
    private readonly IDayNightCycle _dayNightCycle;
    private readonly IGoodService _goodService;
    private readonly ResourceCountingService _resourceCountingService;
    private readonly SpeedManager _speedManager;

    public AiHarnessGameContext(
        CameraService cameraService,
        DistrictCenterRegistry districtCenterRegistry,
        DistrictContextService districtContextService,
        IDayNightCycle dayNightCycle,
        IGoodService goodService,
        ResourceCountingService resourceCountingService,
        SpeedManager speedManager) {
      _cameraService = cameraService;
      _districtCenterRegistry = districtCenterRegistry;
      _districtContextService = districtContextService;
      _dayNightCycle = dayNightCycle;
      _goodService = goodService;
      _resourceCountingService = resourceCountingService;
      _speedManager = speedManager;
    }

    public object GameContext() {
      Vector3 target = _cameraService.Target;
      DistrictCenter? selectedDistrict = _districtContextService.SelectedDistrict;
      return new Dictionary<string, object> {
        { "scope", selectedDistrict == null ? "global" : "selectedDistrict" },
        { "dayNumber", _dayNightCycle.DayNumber },
        { "hoursPassedToday", _dayNightCycle.HoursPassedToday },
        { "speed", _speedManager.CurrentSpeed },
        { "camera", VectorData(target, _cameraService.ZoomLevel) },
        { "population", PopulationData(selectedDistrict) },
        { "districts", _districtCenterRegistry.FinishedDistrictCenters.Select(DistrictData).ToArray() }
      };
    }

    public object ResourceSummary(string requestedGood) {
      string goodId = ResolveGoodId(requestedGood);
      return ResourceSnapshot(goodId).ToResourceSummaryData(requestedGood, goodId);
    }

    public object WaterReadiness() {
      string goodId = ResolveGoodId("water");
      return AiHarnessWaterReadiness.Calculate(goodId, ResourceSnapshot(goodId), GlobalPopulation());
    }

    private string ResolveGoodId(string requestedGood) {
      string normalized = Normalize(requestedGood);
      string? exact = _goodService.Goods.FirstOrDefault(good => Normalize(good) == normalized);
      if (!string.IsNullOrWhiteSpace(exact)) {
        return exact;
      }

      string? contains = _goodService.Goods.FirstOrDefault(good => Normalize(good).Contains(normalized));
      return string.IsNullOrWhiteSpace(contains) ? requestedGood : contains;
    }

    private object PopulationData(DistrictCenter? selectedDistrict) {
      return selectedDistrict == null ? GlobalPopulation().ToData() : ReadDistrictPopulation(selectedDistrict).ToData();
    }

    private object DistrictData(DistrictCenter districtCenter) {
      return new Dictionary<string, object> {
        { "population", ReadDistrictPopulation(districtCenter).ToData() }
      };
    }

    private AiHarnessPopulation GlobalPopulation() {
      int adults = 0;
      int children = 0;
      int bots = 0;
      foreach (DistrictCenter districtCenter in _districtCenterRegistry.FinishedDistrictCenters) {
        AiHarnessPopulation population = ReadDistrictPopulation(districtCenter);
        adults += population.Adults;
        children += population.Children;
        bots += population.Bots;
      }

      return new AiHarnessPopulation(adults, children, bots);
    }

    private static AiHarnessPopulation ReadDistrictPopulation(DistrictCenter districtCenter) {
      DistrictPopulation population = districtCenter.GetComponent<DistrictPopulation>();
      if (population == null) {
        return new AiHarnessPopulation(0, 0, 0);
      }

      return new AiHarnessPopulation(population.NumberOfAdults, population.NumberOfChildren, population.NumberOfBots);
    }

    private AiHarnessResourceSnapshot ResourceSnapshot(string goodId) {
      ResourceCount count = _resourceCountingService.GetGlobalResourceCount(goodId);
      return new AiHarnessResourceSnapshot(
          count.AvailableStock,
          count.AllStock,
          count.TotalCapacity,
          count.FillRate,
          count.StockpiledStock,
          count.BufferedOutputStock,
          count.CarriedToStockpilesStock,
          count.CarriedToProcessors,
          count.StockUnderProcessing,
          count.BufferedInput);
    }

    private static object VectorData(Vector3 target, float zoom) {
      return new Dictionary<string, object> {
        { "target", new Dictionary<string, object> {
          { "x", target.x },
          { "y", target.y },
          { "z", target.z }
        } },
        { "zoom", zoom }
      };
    }

    private static string Normalize(string value) {
      return AiHarnessText.Normalize(value);
    }

  }
}
