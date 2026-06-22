using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessInteractionState {

    private const string ModVersion = "0.1.0";

    private readonly object _lock = new object();

    private string _status = "idle";
    private string _interactionId = "";
    private string _menuId = "";
    private string _question = "Press Ask AI to request contextual help.";
    private string _topic = "";
    private string _source = "";
    private string _skillId = "general";
    private string _menuPath = "root";
    private string _contextHash = "none";
    private string _lastError = "";
    private int _lastButton;
    private string _lastTool = "";
    private bool _lastToolOk;
    private string _lastToolSummary = "";
    private int _refreshRequests;
    private int _revision;
    private AiHarnessInteractionOption[] _options = DefaultOptions();

    public int Revision {
      get {
        lock (_lock) {
          return _revision;
        }
      }
    }

    public object SnapshotData() {
      lock (_lock) {
        return SnapshotDataLocked();
      }
    }

    public object RequestInteraction(string topic, string source) {
      lock (_lock) {
        _status = "requested";
        _interactionId = "interaction-" + Guid.NewGuid().ToString("N");
        _menuId = "";
        _question = string.IsNullOrWhiteSpace(topic)
            ? "AI help requested. Waiting for Pi."
            : "AI help requested: " + topic;
        _topic = topic ?? "";
        _source = source ?? "";
        _skillId = "general";
        _menuPath = "root";
        _contextHash = "none";
        _lastError = "";
        _lastButton = 0;
        _lastTool = "";
        _lastToolOk = false;
        _lastToolSummary = "";
        _refreshRequests = 0;
        _options = RequestedOptions();
        BumpRevisionLocked();
        Debug.Log("[LouisPaulet.AiHarness] Pi interaction requested: " + _interactionId + " topic=" + _topic);
        return SnapshotDataLocked();
      }
    }

    public object RequestChoiceRefresh() {
      lock (_lock) {
        _status = "refreshRequested";
        if (string.IsNullOrWhiteSpace(_interactionId)) {
          _interactionId = "interaction-" + Guid.NewGuid().ToString("N");
        }

        _menuId = "";
        _question = "Choice refresh requested. Waiting for Pi to offer a different path.";
        _lastError = "";
        _lastButton = 0;
        _lastTool = "";
        _lastToolOk = false;
        _lastToolSummary = "";
        _refreshRequests++;
        _options = RefreshRequestedOptions();
        BumpRevisionLocked();
        Debug.Log("[LouisPaulet.AiHarness] Pi interaction refresh requested: " + _interactionId
            + " refresh=" + _refreshRequests.ToString(CultureInfo.InvariantCulture));
        return SnapshotDataLocked();
      }
    }

    public object ShowMenu(
        string interactionId,
        string menuId,
        string question,
        AiHarnessInteractionOption[] options,
        string skillId,
        string menuPath,
        string contextHash) {
      lock (_lock) {
        string validationError = ValidateMenu(question, options);
        if (!string.IsNullOrEmpty(validationError)) {
          _lastError = validationError;
          BumpRevisionLocked();
          throw new ArgumentException(validationError);
        }

        if (string.IsNullOrWhiteSpace(interactionId)) {
          interactionId = string.IsNullOrWhiteSpace(_interactionId)
              ? "interaction-" + Guid.NewGuid().ToString("N")
              : _interactionId;
        }

        _status = "menuShown";
        _interactionId = interactionId;
        _menuId = string.IsNullOrWhiteSpace(menuId) ? "menu-" + Guid.NewGuid().ToString("N") : menuId;
        _question = question;
        _options = options;
        _skillId = string.IsNullOrWhiteSpace(skillId) ? "general" : skillId;
        _menuPath = string.IsNullOrWhiteSpace(menuPath) ? "root" : menuPath;
        _contextHash = string.IsNullOrWhiteSpace(contextHash) ? "none" : contextHash;
        _lastError = "";
        _lastButton = 0;
        _lastTool = "";
        _lastToolOk = false;
        _lastToolSummary = "";
        BumpRevisionLocked();
        WriteReplayLocked();
        Debug.Log("[LouisPaulet.AiHarness] Pi interaction menu shown: " + _interactionId + " menu=" + _menuId);
        return SnapshotDataLocked();
      }
    }

    public object SubmitAnswer(int button) {
      lock (_lock) {
        if (button < 1 || button > 4) {
          throw new ArgumentException("Button must be one of 1, 2, 3, or 4.");
        }

        AiHarnessInteractionOption option = _options[button - 1];
        _lastButton = button;
        _lastError = "";
        _status = string.Equals(option.Kind, "tool", StringComparison.OrdinalIgnoreCase)
            ? "toolRequested"
            : "answerSubmitted";
        BumpRevisionLocked();
        Debug.Log("[LouisPaulet.AiHarness] Pi interaction answer: button=" + button.ToString(CultureInfo.InvariantCulture)
            + " kind=" + option.Kind + " label=" + option.Label);
        return SnapshotDataLocked();
      }
    }

    public object RecordToolResult(string tool, bool ok, string summary) {
      lock (_lock) {
        _status = "toolCompleted";
        _lastTool = tool ?? "";
        _lastToolOk = ok;
        _lastToolSummary = summary ?? "";
        _question = string.IsNullOrWhiteSpace(_lastToolSummary)
            ? "Tool completed: " + _lastTool
            : _lastToolSummary;
        _lastError = ok ? "" : _lastToolSummary;
        BumpRevisionLocked();
        Debug.Log("[LouisPaulet.AiHarness] Pi interaction tool result: tool=" + _lastTool
            + " ok=" + ok.ToString(CultureInfo.InvariantCulture));
        return SnapshotDataLocked();
      }
    }

    public object Clear() {
      lock (_lock) {
        _status = "idle";
        _interactionId = "";
        _menuId = "";
        _question = "Press Ask AI to request contextual help.";
        _topic = "";
        _source = "";
        _skillId = "general";
        _menuPath = "root";
        _contextHash = "none";
        _lastError = "";
        _lastButton = 0;
        _lastTool = "";
        _lastToolOk = false;
        _lastToolSummary = "";
        _refreshRequests = 0;
        _options = DefaultOptions();
        BumpRevisionLocked();
        Debug.Log("[LouisPaulet.AiHarness] Pi interaction cleared.");
        return SnapshotDataLocked();
      }
    }

    private object SnapshotDataLocked() {
      return new Dictionary<string, object> {
        { "status", _status },
        { "interactionId", _interactionId },
        { "menuId", _menuId },
        { "question", _question },
        { "topic", _topic },
        { "source", _source },
        { "skillId", _skillId },
        { "menuPath", _menuPath },
        { "contextHash", _contextHash },
        { "replayKey", ReplayKeyLocked() },
        { "revision", _revision },
        { "refreshRequests", _refreshRequests },
        { "lastButton", _lastButton },
        { "lastAnswer", LastAnswerDataLocked() },
        { "lastTool", new Dictionary<string, object> {
          { "name", _lastTool },
          { "ok", _lastToolOk },
          { "summary", _lastToolSummary }
        } },
        { "lastError", _lastError },
        { "options", _options.Select(option => option.ToData()).ToArray() }
      };
    }

    private object LastAnswerDataLocked() {
      if (_lastButton < 1 || _lastButton > 4) {
        return new Dictionary<string, object>();
      }

      AiHarnessInteractionOption option = _options[_lastButton - 1];
      return new Dictionary<string, object> {
        { "button", _lastButton },
        { "label", option.Label },
        { "kind", option.Kind },
        { "payload", option.Payload }
      };
    }

    private void BumpRevisionLocked() {
      _revision++;
    }

    private string ReplayKeyLocked() {
      return SanitizeReplayPart(ModVersion)
          + "-" + SanitizeReplayPart(_skillId)
          + "-" + SanitizeReplayPart(_menuPath)
          + "-" + SanitizeReplayPart(_contextHash);
    }

    private void WriteReplayLocked() {
      try {
        string replayKey = ReplayKeyLocked();
        string path = AiHarnessPaths.GetGeneratedFilePath("interactions", replayKey, ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var payload = new Dictionary<string, object> {
          { "gameVersion", "runtime" },
          { "modVersion", ModVersion },
          { "skillId", _skillId },
          { "menuPath", _menuPath },
          { "contextHash", _contextHash },
          { "replayKey", replayKey },
          { "createdAtUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
          { "interaction", SnapshotDataLocked() }
        };
        File.WriteAllText(path, AiHarnessJson.SerializeObject(payload));
      } catch (Exception exception) {
        Debug.LogError("[LouisPaulet.AiHarness] Failed to write Pi interaction replay: " + exception.Message);
      }
    }

    private static string ValidateMenu(string question, AiHarnessInteractionOption[] options) {
      if (string.IsNullOrWhiteSpace(question)) {
        return "Interaction menu requires a question.";
      }

      if (options.Length != 4) {
        return "Interaction menu must provide exactly four options.";
      }

      bool hasTool = false;
      bool hasNavigation = false;
      bool hasBackOrNo = false;
      bool hasYes = false;
      bool hasNo = false;
      foreach (AiHarnessInteractionOption option in options) {
        if (string.IsNullOrWhiteSpace(option.Label)) {
          return "Interaction menu option labels cannot be empty.";
        }

        string kind = Normalize(option.Kind);
        string label = Normalize(option.Label);
        hasTool = hasTool || kind == "tool";
        hasNavigation = hasNavigation || kind == "menu" || kind == "nav" || kind == "navigate";
        hasBackOrNo = hasBackOrNo || kind == "back" || kind == "cancel" || kind == "no"
            || label.Contains("back") || label.Contains("cancel") || label == "no";
        hasYes = hasYes || kind == "yes" || label == "yes" || label.StartsWith("yes");
        hasNo = hasNo || kind == "no" || label == "no" || label.StartsWith("no");
      }

      bool confirmation = options.Any(option => Normalize(option.Kind) == "confirm");
      if (confirmation && (!hasYes || !hasNo)) {
        return "Confirmation menus must include yes and no choices.";
      }

      if (!confirmation && (!hasTool || !hasNavigation || !hasBackOrNo)) {
        return "Non-confirmation menus must include at least one tool option, one menu/navigation option, and one back/cancel/no option.";
      }

      return "";
    }

    private static AiHarnessInteractionOption[] DefaultOptions() {
      return new[] {
        new AiHarnessInteractionOption(1, "Ask AI", "menu", "root"),
        new AiHarnessInteractionOption(2, "Water Check", "tool", "timberborn_water_readiness"),
        new AiHarnessInteractionOption(3, "Build Tips", "menu", "building.pathing"),
        new AiHarnessInteractionOption(4, "No", "cancel", "")
      };
    }

    private static AiHarnessInteractionOption[] RequestedOptions() {
      return new[] {
        new AiHarnessInteractionOption(1, "Waiting", "menu", "waiting"),
        new AiHarnessInteractionOption(2, "Status", "tool", "timberborn_status"),
        new AiHarnessInteractionOption(3, "Context", "tool", "timberborn_game_context"),
        new AiHarnessInteractionOption(4, "Cancel", "cancel", "")
      };
    }

    private static AiHarnessInteractionOption[] RefreshRequestedOptions() {
      return new[] {
        new AiHarnessInteractionOption(1, "Refreshing", "menu", "refreshing"),
        new AiHarnessInteractionOption(2, "Status", "tool", "timberborn_status"),
        new AiHarnessInteractionOption(3, "Context", "tool", "timberborn_game_context"),
        new AiHarnessInteractionOption(4, "Cancel", "cancel", "")
      };
    }

    private static string SanitizeReplayPart(string value) {
      if (string.IsNullOrWhiteSpace(value)) {
        return "none";
      }

      var characters = value.Trim().ToLowerInvariant()
          .Select(character => char.IsLetterOrDigit(character) ? character : '-')
          .ToArray();
      string sanitized = new string(characters).Trim('-');
      return string.IsNullOrWhiteSpace(sanitized) ? "none" : sanitized;
    }

    private static string Normalize(string value) {
      if (string.IsNullOrWhiteSpace(value)) {
        return "";
      }

      return new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }

  }

  public sealed class AiHarnessInteractionOption {

    public AiHarnessInteractionOption(int button, string label, string kind, string payload) {
      Button = button;
      Label = label ?? "";
      Kind = string.IsNullOrWhiteSpace(kind) ? "menu" : kind;
      Payload = payload ?? "";
    }

    public int Button { get; }
    public string Label { get; }
    public string Kind { get; }
    public string Payload { get; }

    public object ToData() {
      return new Dictionary<string, object> {
        { "button", Button },
        { "label", Label },
        { "kind", Kind },
        { "payload", Payload }
      };
    }

  }
}
