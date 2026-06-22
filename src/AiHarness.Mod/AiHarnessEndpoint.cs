using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Timberborn.CameraSystem;
using Timberborn.Coordinates;
using Timberborn.CoreUI;
using Timberborn.HttpApiSystem;
using Timberborn.TimeSystem;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessEndpoint : IHttpApiEndpoint, IAiHarnessRequestHandler {

    private const string ApiPrefix = "/api/ai-harness";

    private readonly AiHarnessCommandQueue _commandQueue;
    private readonly AiHarnessBuildingPlacement _buildingPlacement;
    private readonly CameraService _cameraService;
    private readonly IDayNightCycle _dayNightCycle;
    private readonly DialogBoxShower _dialogBoxShower;
    private readonly SpeedManager _speedManager;

    public AiHarnessEndpoint(
        AiHarnessCommandQueue commandQueue,
        AiHarnessBuildingPlacement buildingPlacement,
        DialogBoxShower dialogBoxShower,
        SpeedManager speedManager,
        CameraService cameraService,
        IDayNightCycle dayNightCycle) {
      _commandQueue = commandQueue;
      _buildingPlacement = buildingPlacement;
      _dialogBoxShower = dialogBoxShower;
      _speedManager = speedManager;
      _cameraService = cameraService;
      _dayNightCycle = dayNightCycle;
    }

    public async Task<bool> TryHandle(HttpListenerContext context) {
      string path = context.Request.Url.AbsolutePath.TrimEnd('/');
      if (!path.StartsWith(ApiPrefix, StringComparison.OrdinalIgnoreCase)) {
        return false;
      }

      AiHarnessResponse response = HandleRequest(
          context.Request.HttpMethod,
          path,
          name => context.Request.QueryString[name]);
      if (!response.ok) {
        context.Response.StatusCode = AiHarnessResponse.StatusCodeFor(response);
      }

      await WriteJson(context, response);
      return true;
    }

    public AiHarnessResponse HandleRequest(string method, string path, QueryReader query) {
      if (!MethodIsAllowed(method)) {
        return AiHarnessResponse.Failure("unknown", NewCommandId("unknown"), "Only GET and POST are supported.");
      }

      string command = GetCommand(path);
      switch (command) {
        case "":
        case "status":
          return _commandQueue.Run("status", Status);
        case "commands":
          return AiHarnessResponse.Success("commands", NewCommandId("commands"), Commands());
        case "log":
          return HandleLog(query);
        case "popup":
          return HandlePopup(query);
        case "screenshot":
          return HandleScreenshot(query);
        case "speed":
          return HandleSpeed(query);
        case "camera":
          return HandleCamera(query);
        case "buildings":
          return HandleBuildings(query);
        case "place-building":
          return HandlePlaceBuilding(query);
        default:
          return AiHarnessResponse.Failure(command, NewCommandId(command), "Unknown AI Harness command.");
      }
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

    private AiHarnessResponse HandleLog(QueryReader query) {
      string? message = query("message");
      if (string.IsNullOrWhiteSpace(message)) {
        return AiHarnessResponse.Failure("log", NewCommandId("log"), "Missing required query parameter: message.");
      }

      return _commandQueue.Run("log", () => {
        Debug.Log("[LouisPaulet.AiHarness] " + message);
        return new Dictionary<string, object> {
          { "message", message }
        };
      });
    }

    private AiHarnessResponse HandlePopup(QueryReader query) {
      string? message = query("message");
      if (string.IsNullOrWhiteSpace(message)) {
        return AiHarnessResponse.Failure("popup", NewCommandId("popup"), "Missing required query parameter: message.");
      }

      return _commandQueue.Run("popup", () => {
        Debug.Log("[LouisPaulet.AiHarness] Showing popup from AI Harness: " + message);
        _dialogBoxShower.Create()
            .SetMessage(message)
            .Show();
        return new Dictionary<string, object> {
          { "message", message }
        };
      });
    }

    private AiHarnessResponse HandleScreenshot(QueryReader query) {
      string name = query("name") ?? "ai-harness-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
      return _commandQueue.Run("screenshot", () => {
        string path = AiHarnessPaths.GetGeneratedFilePath("screenshots", name, ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[LouisPaulet.AiHarness] Screenshot requested: " + path);
        return new Dictionary<string, object> {
          { "path", path }
        };
      });
    }

    private AiHarnessResponse HandleSpeed(QueryReader query) {
      string? valueText = query("value");
      if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int speed) || speed < 0 || speed > 3) {
        return AiHarnessResponse.Failure("speed", NewCommandId("speed"), "Speed value must be one of 0, 1, 2, or 3.");
      }

      return _commandQueue.Run("speed", () => {
        float currentSpeedBefore = _speedManager.CurrentSpeed;
        _speedManager.UnlockSpeed();
        _speedManager.ChangeSpeed(speed);
        Debug.Log("[LouisPaulet.AiHarness] Speed changed to " + speed.ToString(CultureInfo.InvariantCulture));
        return new Dictionary<string, object> {
          { "requestedSpeed", speed },
          { "currentSpeedBefore", currentSpeedBefore },
          { "currentSpeed", _speedManager.CurrentSpeed }
        };
      });
    }

    private AiHarnessResponse HandleCamera(QueryReader query) {
      string? xText = query("x");
      string? yText = query("y");
      string? zText = query("z");
      string? zoomText = query("zoom");

      bool hasTarget = xText != null || yText != null || zText != null;
      if (hasTarget && (!TryParseFloat(xText, out float x) || !TryParseFloat(yText, out float y) || !TryParseFloat(zText, out float z))) {
        return AiHarnessResponse.Failure("camera", NewCommandId("camera"), "Camera target requires numeric x, y, and z query parameters.");
      }

      bool hasZoom = zoomText != null;
      if (hasZoom && !TryParseFloat(zoomText, out float zoom)) {
        return AiHarnessResponse.Failure("camera", NewCommandId("camera"), "Camera zoom must be numeric.");
      }

      return _commandQueue.Run("camera", () => {
        if (hasTarget) {
          _cameraService.MoveTargetTo(new Vector3(
              ParseFloat(xText),
              ParseFloat(yText),
              ParseFloat(zText)));
        }

        if (hasZoom) {
          _cameraService.ZoomLevel = ParseFloat(zoomText);
        }

        Vector3 target = _cameraService.Target;
        return new Dictionary<string, object> {
          { "target", VectorData(target) },
          { "zoom", _cameraService.ZoomLevel }
        };
      });
    }

    private AiHarnessResponse HandleBuildings(QueryReader query) {
      return _commandQueue.Run("buildings", () => _buildingPlacement.ListBuildings(query("query")));
    }

    private AiHarnessResponse HandlePlaceBuilding(QueryReader query) {
      string template = query("template") ?? "water_tank";
      if (!TryParseOptionalInt(query("x"), out int? x)
          || !TryParseOptionalInt(query("y"), out int? y)
          || !TryParseOptionalInt(query("z"), out int? z)) {
        return AiHarnessResponse.Failure("place-building", NewCommandId("place-building"), "Coordinates must be integers.");
      }

      if (!TryParseOrientation(query("orientation") ?? "Cw0", out Orientation orientation)) {
        return AiHarnessResponse.Failure("place-building", NewCommandId("place-building"), "Orientation must be one of Cw0, Cw90, Cw180, or Cw270.");
      }

      if (!TryParseOptionalBool(query("flipped"), out bool flipped)) {
        return AiHarnessResponse.Failure("place-building", NewCommandId("place-building"), "Flipped must be true or false.");
      }

      int searchRadius = 16;
      string? searchRadiusText = query("searchRadius");
      if (searchRadiusText != null && (!int.TryParse(searchRadiusText, NumberStyles.Integer, CultureInfo.InvariantCulture, out searchRadius) || searchRadius < 1 || searchRadius > 64)) {
        return AiHarnessResponse.Failure("place-building", NewCommandId("place-building"), "searchRadius must be an integer from 1 to 64.");
      }

      return _commandQueue.Run("place-building", () => _buildingPlacement.PlaceBuilding(template, x, y, z, orientation, flipped, searchRadius));
    }

    private object Status() {
      Vector3 target = _cameraService.Target;
      return new Dictionary<string, object> {
        { "mod", "AI Harness" },
        { "id", "LouisPaulet.AiHarness" },
        { "api", ApiPrefix },
        { "modPath", AiHarnessPaths.ModPath },
        { "pendingCommands", _commandQueue.PendingCount },
        { "executedCommands", _commandQueue.ExecutedCommands },
        { "speed", _speedManager.CurrentSpeed },
        { "dayNumber", _dayNightCycle.DayNumber },
        { "hoursPassedToday", _dayNightCycle.HoursPassedToday },
        { "camera", new Dictionary<string, object> {
          { "target", VectorData(target) },
          { "zoom", _cameraService.ZoomLevel }
        } }
      };
    }

    private static object Commands() {
      return new Dictionary<string, object> {
        { "basePath", ApiPrefix },
        { "commands", new[] {
          "GET /api/ai-harness/status",
          "GET /api/ai-harness/commands",
          "GET|POST /api/ai-harness/log?message=...",
          "GET|POST /api/ai-harness/popup?message=...",
          "GET|POST /api/ai-harness/screenshot?name=...",
          "GET|POST /api/ai-harness/speed?value=0|1|2|3",
          "GET|POST /api/ai-harness/camera?x=...&y=...&z=...&zoom=...",
          "GET /api/ai-harness/buildings?query=...",
          "GET|POST /api/ai-harness/place-building?template=water_tank&x=...&y=...&z=...&orientation=Cw0&flipped=false"
        } }
      };
    }

    private static Dictionary<string, object> VectorData(Vector3 vector) {
      return new Dictionary<string, object> {
        { "x", vector.x },
        { "y", vector.y },
        { "z", vector.z }
      };
    }

    private static bool TryParseFloat(string? value, out float parsed) {
      return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    private static float ParseFloat(string? value) {
      return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static bool TryParseOptionalInt(string? value, out int? parsed) {
      if (value == null) {
        parsed = null;
        return true;
      }

      if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)) {
        parsed = result;
        return true;
      }

      parsed = null;
      return false;
    }

    private static bool TryParseOptionalBool(string? value, out bool parsed) {
      if (value == null) {
        parsed = false;
        return true;
      }

      if (bool.TryParse(value, out parsed)) {
        return true;
      }

      if (value == "1") {
        parsed = true;
        return true;
      }

      if (value == "0") {
        parsed = false;
        return true;
      }

      parsed = false;
      return false;
    }

    private static bool TryParseOrientation(string value, out Orientation orientation) {
      if (Enum.TryParse(value, ignoreCase: true, out orientation)) {
        return true;
      }

      switch (value) {
        case "0":
          orientation = Orientation.Cw0;
          return true;
        case "90":
          orientation = Orientation.Cw90;
          return true;
        case "180":
          orientation = Orientation.Cw180;
          return true;
        case "270":
          orientation = Orientation.Cw270;
          return true;
        default:
          orientation = Orientation.Cw0;
          return false;
      }
    }

    private static string NewCommandId(string command) {
      return command + "-" + Guid.NewGuid().ToString("N");
    }

    private static async Task WriteJson(HttpListenerContext context, AiHarnessResponse response) {
      string json = AiHarnessJson.Serialize(response);
      byte[] bytes = Encoding.UTF8.GetBytes(json);
      context.Response.ContentType = "application/json; charset=utf-8";
      context.Response.ContentLength64 = bytes.Length;
      await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
      context.Response.Close();
    }

  }

  public delegate string? QueryReader(string name);
}
