using System;
using System.Collections.Generic;

namespace LouisPaulet.AiHarness {
  public sealed class AiHarnessPopulation {

    public AiHarnessPopulation(int adults, int children, int bots) {
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

  public sealed class AiHarnessResourceSnapshot {

    public AiHarnessResourceSnapshot(
        int availableStock,
        int allStock,
        int totalCapacity,
        float fillRate,
        int stockpiledStock,
        int bufferedOutputStock,
        int carriedToStockpilesStock,
        int carriedToProcessors,
        int stockUnderProcessing,
        int bufferedInput) {
      AvailableStock = availableStock;
      AllStock = allStock;
      TotalCapacity = totalCapacity;
      FillRate = fillRate;
      StockpiledStock = stockpiledStock;
      BufferedOutputStock = bufferedOutputStock;
      CarriedToStockpilesStock = carriedToStockpilesStock;
      CarriedToProcessors = carriedToProcessors;
      StockUnderProcessing = stockUnderProcessing;
      BufferedInput = bufferedInput;
    }

    public int AvailableStock { get; }
    public int AllStock { get; }
    public int TotalCapacity { get; }
    public float FillRate { get; }
    public int StockpiledStock { get; }
    public int BufferedOutputStock { get; }
    public int CarriedToStockpilesStock { get; }
    public int CarriedToProcessors { get; }
    public int StockUnderProcessing { get; }
    public int BufferedInput { get; }

    public object ToResourceSummaryData(string requestedGood, string goodId) {
      return new Dictionary<string, object> {
        { "requestedGood", requestedGood },
        { "goodId", goodId },
        { "availableStock", AvailableStock },
        { "allStock", AllStock },
        { "totalCapacity", TotalCapacity },
        { "fillRate", FillRate },
        { "stockpiledStock", StockpiledStock },
        { "bufferedOutputStock", BufferedOutputStock },
        { "carriedToStockpilesStock", CarriedToStockpilesStock },
        { "carriedToProcessors", CarriedToProcessors },
        { "stockUnderProcessing", StockUnderProcessing },
        { "bufferedInput", BufferedInput }
      };
    }

  }

  public static class AiHarnessWaterReadiness {

    public const string Rule = "daysOfWater = availableWater / max(1, beavers * 2)";

    public static object Calculate(string goodId, AiHarnessResourceSnapshot count, AiHarnessPopulation population) {
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
        { "rule", Rule },
        { "recommendation", Recommendation(count.AvailableStock, count.TotalCapacity, population.Beavers) }
      };
    }

    public static string Recommendation(int availableWater, int capacity, int beavers) {
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

  }
}
