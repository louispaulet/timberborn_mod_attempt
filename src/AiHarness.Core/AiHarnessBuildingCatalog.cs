using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LouisPaulet.AiHarness {
  public sealed class AiHarnessBuildingTemplate {

    public AiHarnessBuildingTemplate(
        string templateName,
        string blueprintName,
        string displayNameLocKey,
        string descriptionLocKey,
        string toolGroupId,
        int toolOrder) {
      TemplateName = templateName ?? "";
      BlueprintName = blueprintName ?? "";
      DisplayNameLocKey = displayNameLocKey ?? "";
      DescriptionLocKey = descriptionLocKey ?? "";
      ToolGroupId = toolGroupId ?? "";
      ToolOrder = toolOrder;
    }

    public string TemplateName { get; }
    public string BlueprintName { get; }
    public string DisplayNameLocKey { get; }
    public string DescriptionLocKey { get; }
    public string ToolGroupId { get; }
    public int ToolOrder { get; }

    public Dictionary<string, object> ToData() {
      return new Dictionary<string, object> {
        { "templateName", TemplateName },
        { "blueprintName", BlueprintName },
        { "displayNameLocKey", DisplayNameLocKey },
        { "descriptionLocKey", DescriptionLocKey },
        { "toolGroupId", ToolGroupId },
        { "toolOrder", ToolOrder }
      };
    }

  }

  public static class AiHarnessBuildingCatalog {

    private static readonly string[] WaterTankAliases = {
      "watertank",
      "waterstorage",
      "waterbarrel",
      "tank"
    };

    private static readonly Dictionary<string, string[]> BuildingAliasKeywords = new Dictionary<string, string[]> {
      { "path", new[] { "path" } },
      { "paths", new[] { "path" } },
      { "road", new[] { "path" } },
      { "stairs", new[] { "stair" } },
      { "stair", new[] { "stair" } },
      { "platform", new[] { "platform" } },
      { "platforms", new[] { "platform" } }
    };

    public static IEnumerable<AiHarnessBuildingTemplate> Filter(IEnumerable<AiHarnessBuildingTemplate> templates, string? query) {
      string normalizedQuery = AiHarnessText.Normalize(query ?? "");
      IEnumerable<AiHarnessBuildingTemplate> filtered = templates;
      if (!string.IsNullOrWhiteSpace(normalizedQuery)) {
        filtered = filtered.Where(template => SearchText(template).Contains(normalizedQuery));
      }

      return filtered.Take(100);
    }

    public static AiHarnessBuildingTemplate Resolve(IEnumerable<AiHarnessBuildingTemplate> templates, string templateQuery) {
      string normalizedQuery = AiHarnessText.Normalize(templateQuery);
      if (string.IsNullOrWhiteSpace(normalizedQuery)) {
        normalizedQuery = "watertank";
      }

      List<AiHarnessBuildingTemplate> templateList = templates.ToList();
      AiHarnessBuildingTemplate? exactMatch = templateList.FirstOrDefault(template => NormalizedNames(template).Any(name => name == normalizedQuery));
      if (exactMatch != null) {
        return exactMatch;
      }

      if (WaterTankAliases.Contains(normalizedQuery)) {
        AiHarnessBuildingTemplate? waterTank = templateList
            .Where(IsWaterTankCandidate)
            .OrderBy(WaterTankSortKey)
            .ThenBy(template => template.ToolOrder)
            .FirstOrDefault();
        if (waterTank != null) {
          return waterTank;
        }
      }

      if (BuildingAliasKeywords.TryGetValue(normalizedQuery, out string[] aliasKeywords)) {
        AiHarnessBuildingTemplate? aliasMatch = templateList
            .Where(template => MatchesAliasKeywords(template, aliasKeywords))
            .OrderBy(template => template.ToolOrder)
            .ThenBy(template => template.TemplateName)
            .FirstOrDefault();
        if (aliasMatch != null) {
          return aliasMatch;
        }
      }

      List<AiHarnessBuildingTemplate> matches = templateList
          .Where(template => SearchText(template).Contains(normalizedQuery))
          .OrderBy(template => template.ToolOrder)
          .Take(5)
          .ToList();
      if (matches.Count == 1) {
        return matches[0];
      }

      if (matches.Count > 1) {
        throw new InvalidOperationException("Building template query is ambiguous: " + templateQuery
            + ". Matches: " + string.Join(", ", matches.Select(template => template.TemplateName).ToArray()));
      }

      throw new InvalidOperationException("Building template not found: " + templateQuery);
    }

    public static string SearchText(AiHarnessBuildingTemplate template) {
      var builder = new StringBuilder();
      foreach (string name in NormalizedNames(template)) {
        builder.Append(name);
        builder.Append(' ');
      }

      builder.Append(AiHarnessText.Normalize(template.ToolGroupId));
      return builder.ToString();
    }

    private static IEnumerable<string> NormalizedNames(AiHarnessBuildingTemplate template) {
      yield return AiHarnessText.Normalize(template.TemplateName);
      yield return AiHarnessText.Normalize(template.BlueprintName);
      yield return AiHarnessText.Normalize(template.DisplayNameLocKey);
      yield return AiHarnessText.Normalize(template.DescriptionLocKey);
    }

    private static bool IsWaterTankCandidate(AiHarnessBuildingTemplate template) {
      string searchText = SearchText(template);
      return (searchText.Contains("water") && (searchText.Contains("tank") || searchText.Contains("barrel") || searchText.Contains("storage")))
          || (AiHarnessText.Normalize(template.ToolGroupId) == "storage" && searchText.Contains("tank"));
    }

    private static int WaterTankSortKey(AiHarnessBuildingTemplate template) {
      string searchText = SearchText(template);
      if (searchText.Contains("small")) {
        return 0;
      }

      if (searchText.Contains("medium")) {
        return 1;
      }

      return 2;
    }

    private static bool MatchesAliasKeywords(AiHarnessBuildingTemplate template, string[] keywords) {
      string searchText = SearchText(template);
      return keywords.All(keyword => searchText.Contains(keyword));
    }

  }
}
