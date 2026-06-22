using System;
using System.Collections.Generic;
using System.Globalization;
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
      ResourceCount count = _resourceCountingService.GetGlobalResourceCount(goodId);
      return new Dictionary<string, object> {
        { "requestedGood", requestedGood },
        { "goodId", goodId },
        { "availableStock", count.AvailableStock },
        { "allStock", count.AllStock },
        { "totalCapacity", count.TotalCapacity },
        { "fillRate", count.FillRate },
        { "stockpiledStock", count.StockpiledStock },
        { "bufferedOutputStock", count.BufferedOutputStock },
        { "carriedToStockpilesStock", count.CarriedToStockpilesStock },
        { "carriedToProcessors", count.CarriedToProcessors },
        { "stockUnderProcessing", count.StockUnderProcessing },
        { "bufferedInput", count.BufferedInput }
      };
    }

    public object WaterReadiness() {
      string goodId = ResolveGoodId("water");
      ResourceCount count = _resourceCountingService.GetGlobalResourceCount(goodId);
      Population population = GlobalPopulation();
      int divisor = Math.Max(1, population.Beavers);
      double waterPerBeaver = (double) count.AvailableStock / divisor;
      double waterCapacityPerBeaver = (double) count.TotalCapacity / divisor;
      double daysOfWater = (double) count.AvailableStock / Math.Max(1, population.Beavers * 2);

      return new Dictionary<string, object> {
        { "goodId", goodId },
        { "population", population.ToData() },
        { "availableWater", count.AvailableStock },
        { "allWater", count.AllStock },
        { "waterCapacity", count.TotalCapacity },
        { "waterFillRate", count.FillRate },
        { "waterPerBeaver", Math.Round(waterPerBeaver, 2) },
        { "waterCapacityPerBeaver", Math.Round(waterCapacityPerBeaver, 2) },
        { "daysOfWater", Math.Round(daysOfWater, 2) },
        { "rule", "daysOfWater = availableWater / max(1, beavers * 2)" },
        { "recommendation", WaterRecommendation(count.AvailableStock, count.TotalCapacity, population.Beavers) }
      };
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
      return selectedDistrict == null ? GlobalPopulation().ToData() : DistrictPopulation(selectedDistrict).ToData();
    }

    private object DistrictData(DistrictCenter districtCenter) {
      return new Dictionary<string, object> {
        { "population", DistrictPopulation(districtCenter).ToData() }
      };
    }

    private Population GlobalPopulation() {
      int adults = 0;
      int children = 0;
      int bots = 0;
      foreach (DistrictCenter districtCenter in _districtCenterRegistry.FinishedDistrictCenters) {
        Population population = DistrictPopulation(districtCenter);
        adults += population.Adults;
        children += population.Children;
        bots += population.Bots;
      }

      return new Population(adults, children, bots);
    }

    private static Population DistrictPopulation(DistrictCenter districtCenter) {
      DistrictPopulation population = districtCenter.GetComponent<DistrictPopulation>();
      if (population == null) {
        return new Population(0, 0, 0);
      }

      return new Population(population.NumberOfAdults, population.NumberOfChildren, population.NumberOfBots);
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

    private static string WaterRecommendation(int availableWater, int capacity, int beavers) {
      if (beavers <= 0) {
        return "No beaver population detected yet.";
      }

      double days = (double) availableWater / Math.Max(1, beavers * 2);
      double capacityPerBeaver = (double) capacity / beavers;
      if (days < 1.5) {
        return "Build more water storage or pumps soon.";
      }

      if (capacityPerBeaver < 6) {
        return "Water stock is acceptable, but storage capacity is thin.";
      }

      return "Water storage looks acceptable for the current population.";
    }

    private static string Normalize(string value) {
      return string.IsNullOrWhiteSpace(value)
          ? ""
          : new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }

    private sealed class Population {

      public Population(int adults, int children, int bots) {
        Adults = adults;
        Children = children;
        Bots = bots;
      }

      public int Adults { get; }
      public int Children { get; }
      public int Bots { get; }
      public int Beavers { get { return Adults + Children; } }
      public int Total { get { return Adults + Children + Bots; } }

      public object ToData() {
        return new Dictionary<string, object> {
          { "adults", Adults },
          { "children", Children },
          { "beavers", Beavers },
          { "bots", Bots },
          { "total", Total }
        };
      }

    }

  }
}
