using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Timberborn.BlockObjectTools;
using Timberborn.BlockSystem;
using Timberborn.CameraSystem;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.TemplateSystem;
using Timberborn.UndoSystem;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessBuildingPlacement {

    private const int DefaultSearchRadius = 16;

    private static readonly string[] WaterTankAliases = {
      "watertank",
      "waterstorage",
      "waterbarrel",
      "tank"
    };

    private readonly BlockObjectPlacerService _blockObjectPlacerService;
    private readonly CameraService _cameraService;
    private readonly PreviewPlacerFactory _previewPlacerFactory;
    private readonly TemplateService _templateService;
    private readonly IUndoRegistry _undoRegistry;

    public AiHarnessBuildingPlacement(
        TemplateService templateService,
        BlockObjectPlacerService blockObjectPlacerService,
        PreviewPlacerFactory previewPlacerFactory,
        CameraService cameraService,
        IUndoRegistry undoRegistry) {
      _templateService = templateService;
      _blockObjectPlacerService = blockObjectPlacerService;
      _previewPlacerFactory = previewPlacerFactory;
      _cameraService = cameraService;
      _undoRegistry = undoRegistry;
    }

    public object ListBuildings(string? query) {
      string normalizedQuery = Normalize(query ?? "");
      IEnumerable<PlaceableBlockObjectSpec> templates = GetPlaceableTemplates();
      if (!string.IsNullOrWhiteSpace(normalizedQuery)) {
        templates = templates.Where(template => SearchText(template).Contains(normalizedQuery));
      }

      return new Dictionary<string, object> {
        { "query", query ?? "" },
        { "buildings", templates.Take(100).Select(BuildingData).ToArray() }
      };
    }

    public object PlaceBuilding(
        string templateQuery,
        int? x,
        int? y,
        int? z,
        Orientation orientation,
        bool flipped,
        int searchRadius) {
      PlaceableBlockObjectSpec template = ResolveTemplate(templateQuery);
      Placement placement = FindBuildablePlacement(
          template,
          x,
          y,
          z,
          orientation,
          flipped ? FlipMode.Flipped : FlipMode.Unflipped,
          searchRadius <= 0 ? DefaultSearchRadius : searchRadius);

      IBlockObjectPlacer placer = _blockObjectPlacerService.GetMatchingPlacer(template.GetSpec<BlockObjectSpec>());
      placer.Place(new EntitySetup.Builder(template.Blueprint), placement);
      _undoRegistry.CommitStack();

      Vector3 target = new Vector3(placement.Coordinates.x, placement.Coordinates.z, placement.Coordinates.y);
      _cameraService.MoveTargetTo(target);

      Debug.Log("[LouisPaulet.AiHarness] Placed building " + TemplateName(template) + " at " + FormatPlacement(placement));

      return new Dictionary<string, object> {
        { "template", BuildingData(template) },
        { "placement", PlacementData(placement) },
        { "placedAs", "construction-site" },
        { "searched", !(x.HasValue && y.HasValue && z.HasValue) },
        { "searchRadius", searchRadius <= 0 ? DefaultSearchRadius : searchRadius }
      };
    }

    private Placement FindBuildablePlacement(
        PlaceableBlockObjectSpec template,
        int? x,
        int? y,
        int? z,
        Orientation orientation,
        FlipMode flipMode,
        int searchRadius) {
      var previewPlacer = _previewPlacerFactory.Create(template);
      try {
        foreach (Vector3Int coordinates in CandidateCoordinates(x, y, z, searchRadius)) {
          var candidate = new Placement(coordinates, orientation, flipMode);
          if (TryGetBuildablePlacement(previewPlacer, candidate, out Placement placement)) {
            return placement;
          }
        }
      } finally {
        previewPlacer.HideAllPreviews();
      }

      string templateName = TemplateName(template);
      if (x.HasValue && y.HasValue && z.HasValue) {
        throw new InvalidOperationException("Could not place " + templateName + " at x=" + x.Value.ToString(CultureInfo.InvariantCulture)
            + ", y=" + y.Value.ToString(CultureInfo.InvariantCulture)
            + ", z=" + z.Value.ToString(CultureInfo.InvariantCulture) + ".");
      }

      throw new InvalidOperationException("Could not find a valid placement for " + templateName + " near the camera.");
    }

    private bool TryGetBuildablePlacement(PreviewPlacer previewPlacer, Placement candidate, out Placement placement) {
      List<Placement> buildablePlacements = previewPlacer.GetBuildableCoordinates(new[] { candidate }).ToList();
      if (buildablePlacements.Count == 1) {
        placement = buildablePlacements[0];
        return true;
      }

      placement = Placement.Zero;
      return false;
    }

    private IEnumerable<Vector3Int> CandidateCoordinates(int? x, int? y, int? z, int searchRadius) {
      bool hasAnyCoordinate = x.HasValue || y.HasValue || z.HasValue;
      if (hasAnyCoordinate) {
        if (!x.HasValue || !y.HasValue || !z.HasValue) {
          throw new ArgumentException("Placement coordinates must include x, y, and z together.");
        }

        yield return new Vector3Int(x.Value, y.Value, z.Value);
        yield break;
      }

      Vector3 target = _cameraService.Target;
      int centerX = Mathf.RoundToInt(target.x);
      int centerY = Mathf.RoundToInt(target.z);

      for (int radius = 0; radius <= searchRadius; radius++) {
        for (int dx = -radius; dx <= radius; dx++) {
          for (int dy = -radius; dy <= radius; dy++) {
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) {
              continue;
            }

            for (int cz = 0; cz <= 32; cz++) {
              yield return new Vector3Int(centerX + dx, centerY + dy, cz);
            }
          }
        }
      }
    }

    private PlaceableBlockObjectSpec ResolveTemplate(string templateQuery) {
      string normalizedQuery = Normalize(templateQuery);
      if (string.IsNullOrWhiteSpace(normalizedQuery)) {
        normalizedQuery = "watertank";
      }

      List<PlaceableBlockObjectSpec> templates = GetPlaceableTemplates().ToList();
      PlaceableBlockObjectSpec? exactMatch = templates.FirstOrDefault(template => NormalizedNames(template).Any(name => name == normalizedQuery));
      if (exactMatch != null) {
        return exactMatch;
      }

      if (WaterTankAliases.Contains(normalizedQuery)) {
        PlaceableBlockObjectSpec? waterTank = templates
            .Where(IsWaterTankCandidate)
            .OrderBy(WaterTankSortKey)
            .ThenBy(template => template.ToolOrder)
            .FirstOrDefault();
        if (waterTank != null) {
          return waterTank;
        }
      }

      List<PlaceableBlockObjectSpec> matches = templates
          .Where(template => SearchText(template).Contains(normalizedQuery))
          .OrderBy(template => template.ToolOrder)
          .Take(5)
          .ToList();
      if (matches.Count == 1) {
        return matches[0];
      }

      if (matches.Count > 1) {
        throw new InvalidOperationException("Building template query is ambiguous: " + templateQuery
            + ". Matches: " + string.Join(", ", matches.Select(TemplateName).ToArray()));
      }

      throw new InvalidOperationException("Building template not found: " + templateQuery);
    }

    private IEnumerable<PlaceableBlockObjectSpec> GetPlaceableTemplates() {
      return _templateService.GetAll<PlaceableBlockObjectSpec>()
          .Where(template => template.UsableWithCurrentFeatureToggles)
          .Where(template => template.GetSpec<BlockObjectSpec>() != null)
          .OrderBy(template => template.ToolGroupId)
          .ThenBy(template => template.ToolOrder)
          .ThenBy(TemplateName);
    }

    private static bool IsWaterTankCandidate(PlaceableBlockObjectSpec template) {
      string searchText = SearchText(template);
      return (searchText.Contains("water") && (searchText.Contains("tank") || searchText.Contains("barrel") || searchText.Contains("storage")))
          || (Normalize(template.ToolGroupId ?? "") == "storage" && searchText.Contains("tank"));
    }

    private static int WaterTankSortKey(PlaceableBlockObjectSpec template) {
      string searchText = SearchText(template);
      if (searchText.Contains("small")) {
        return 0;
      }

      if (searchText.Contains("medium")) {
        return 1;
      }

      return 2;
    }

    private static Dictionary<string, object> BuildingData(PlaceableBlockObjectSpec template) {
      LabeledEntitySpec? label = template.GetSpec<LabeledEntitySpec>();
      return new Dictionary<string, object> {
        { "templateName", TemplateName(template) },
        { "blueprintName", template.Blueprint.Name ?? "" },
        { "displayNameLocKey", label?.DisplayNameLocKey ?? "" },
        { "descriptionLocKey", label?.DescriptionLocKey ?? "" },
        { "toolGroupId", template.ToolGroupId ?? "" },
        { "toolOrder", template.ToolOrder }
      };
    }

    private static Dictionary<string, object> PlacementData(Placement placement) {
      return new Dictionary<string, object> {
        { "x", placement.Coordinates.x },
        { "y", placement.Coordinates.y },
        { "z", placement.Coordinates.z },
        { "orientation", placement.Orientation.ToString() },
        { "flipped", placement.FlipMode.IsFlipped }
      };
    }

    private static string SearchText(PlaceableBlockObjectSpec template) {
      var builder = new StringBuilder();
      foreach (string name in NormalizedNames(template)) {
        builder.Append(name);
        builder.Append(' ');
      }

      builder.Append(Normalize(template.ToolGroupId ?? ""));
      return builder.ToString();
    }

    private static IEnumerable<string> NormalizedNames(PlaceableBlockObjectSpec template) {
      LabeledEntitySpec? label = template.GetSpec<LabeledEntitySpec>();
      yield return Normalize(TemplateName(template));
      yield return Normalize(template.Blueprint.Name ?? "");
      yield return Normalize(label?.DisplayNameLocKey ?? "");
      yield return Normalize(label?.DescriptionLocKey ?? "");
    }

    private static string TemplateName(PlaceableBlockObjectSpec template) {
      return template.GetSpec<TemplateSpec>()?.TemplateName ?? template.Blueprint.Name ?? "";
    }

    private static string FormatPlacement(Placement placement) {
      return "x=" + placement.Coordinates.x.ToString(CultureInfo.InvariantCulture)
          + ", y=" + placement.Coordinates.y.ToString(CultureInfo.InvariantCulture)
          + ", z=" + placement.Coordinates.z.ToString(CultureInfo.InvariantCulture)
          + ", orientation=" + placement.Orientation
          + ", flipped=" + placement.FlipMode.IsFlipped;
    }

    private static string Normalize(string value) {
      if (string.IsNullOrWhiteSpace(value)) {
        return "";
      }

      var builder = new StringBuilder(value.Length);
      foreach (char character in value.ToLowerInvariant()) {
        if (char.IsLetterOrDigit(character)) {
          builder.Append(character);
        }
      }

      return builder.ToString();
    }

  }
}
