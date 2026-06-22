using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Timberborn.FactionSystem;
using Timberborn.GameSceneLoading;
using Timberborn.MapRepositorySystem;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessMainMenuEndpoint : IAiHarnessRequestHandler {

    private const string ApiPrefix = "/api/ai-harness";

    private readonly AiHarnessCommandQueue _commandQueue;
    private readonly FactionSpecService _factionSpecService;
    private readonly GameSceneLoader _gameSceneLoader;
    private readonly MapRepository _mapRepository;

    public AiHarnessMainMenuEndpoint(
        AiHarnessCommandQueue commandQueue,
        FactionSpecService factionSpecService,
        GameSceneLoader gameSceneLoader,
        MapRepository mapRepository) {
      _commandQueue = commandQueue;
      _factionSpecService = factionSpecService;
      _gameSceneLoader = gameSceneLoader;
      _mapRepository = mapRepository;
    }

    public AiHarnessResponse HandleRequest(string method, string path, QueryReader query) {
      if (!MethodIsAllowed(method)) {
        return AiHarnessResponse.Failure("unknown", NewCommandId("unknown"), "Only GET and POST are supported.");
      }

      string command = GetCommand(path);
      switch (command) {
        case "":
        case "status":
          return AiHarnessResponse.Success("status", NewCommandId("status"), Status());
        case "commands":
          return AiHarnessResponse.Success("commands", NewCommandId("commands"), Commands());
        case "new-game":
          return HandleNewGame(query);
        default:
          return AiHarnessResponse.Failure(command, NewCommandId(command), "Unknown AI Harness command.");
      }
    }

    private AiHarnessResponse HandleNewGame(QueryReader query) {
      string settlementName = query("settlement") ?? "AiHarnessTest-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
      string? mapName = query("map");
      string? factionId = query("faction");

      return _commandQueue.Run("new-game", () => {
        string selectedMap = SelectMapName(mapName);
        string selectedFaction = SelectFactionId(factionId);
        _gameSceneLoader.StartNewGameInstantly(selectedFaction, MapFileReference.FromResource(selectedMap), settlementName);
        Debug.Log("[LouisPaulet.AiHarness] Started throwaway new game: " + settlementName + " map=" + selectedMap + " faction=" + selectedFaction);
        return new Dictionary<string, object> {
          { "settlement", settlementName },
          { "map", selectedMap },
          { "faction", selectedFaction }
        };
      });
    }

    private object Status() {
      return new Dictionary<string, object> {
        { "mod", "AI Harness" },
        { "id", "LouisPaulet.AiHarness" },
        { "context", "MainMenu" },
        { "api", ApiPrefix },
        { "modPath", AiHarnessPaths.ModPath },
        { "pendingCommands", _commandQueue.PendingCount },
        { "executedCommands", _commandQueue.ExecutedCommands },
        { "maps", _mapRepository.GetBuiltinMapNames().OrderBy(name => name).Take(10).ToArray() },
        { "factions", _factionSpecService.Factions.OrderBy(faction => faction.Order).Select(faction => faction.Id).ToArray() }
      };
    }

    private static object Commands() {
      return new Dictionary<string, object> {
        { "basePath", ApiPrefix },
        { "commands", new[] {
          "GET /api/ai-harness/status",
          "GET /api/ai-harness/commands",
          "GET|POST /api/ai-harness/new-game?settlement=...&map=...&faction=..."
        } }
      };
    }

    private string SelectMapName(string? requestedMapName) {
      string[] maps = _mapRepository.GetBuiltinMapNames().OrderBy(name => name).ToArray();
      if (maps.Length == 0) {
        throw new InvalidOperationException("No built-in maps are available.");
      }

      if (string.IsNullOrWhiteSpace(requestedMapName)) {
        return maps[0];
      }

      return maps.FirstOrDefault(name => string.Equals(name, requestedMapName, StringComparison.OrdinalIgnoreCase))
          ?? throw new ArgumentException("Built-in map not found: " + requestedMapName);
    }

    private string SelectFactionId(string? requestedFactionId) {
      FactionSpec[] factions = _factionSpecService.Factions.OrderBy(faction => faction.Order).ToArray();
      if (factions.Length == 0) {
        throw new InvalidOperationException("No factions are available.");
      }

      if (string.IsNullOrWhiteSpace(requestedFactionId)) {
        return factions[0].Id;
      }

      return factions.FirstOrDefault(faction => string.Equals(faction.Id, requestedFactionId, StringComparison.OrdinalIgnoreCase))?.Id
          ?? throw new ArgumentException("Faction not found: " + requestedFactionId);
    }

    private static bool MethodIsAllowed(string method) {
      return string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
          || string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCommand(string path) {
      if (path.Length <= ApiPrefix.Length) {
        return "";
      }

      return path.Substring(ApiPrefix.Length).Trim('/').ToLowerInvariant();
    }

    private static string NewCommandId(string command) {
      return command + "-" + Guid.NewGuid().ToString("N");
    }

  }
}
