// Compiled only with -p:RcCarGameTests=true. Commands execute on Unity's main thread,
// through the real movement/support/collision code; normal mod builds have no test bridge.
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace LouisPaulet.RcCar {
  public partial class RcCarController {
    private void ProcessGameTestCommand() {
      string directory = Environment.GetEnvironmentVariable("RC_CAR_GAME_TEST_DIR")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
          "Documents", "Timberborn", "Mods", "RcCar", "generated", "game-tests");
      if (_car == null) return;
      string request = Path.Combine(directory, "request.txt");
      if (!File.Exists(request)) return;
      string[] args = File.ReadAllText(request).Trim().Split(' ');
      File.Delete(request);
      try {
        float Number(int i) => float.Parse(args[i], CultureInfo.InvariantCulture);
        switch (args[0]) {
          case "position":
            _movement.Stop();
            _car.transform.SetPositionAndRotation(new Vector3(Number(1), Number(2), Number(3)),
              Quaternion.Euler(0f, Number(4), 0f));
            Physics.SyncTransforms();
            break;
          case "move":
            _car.transform.position = _movement.GameTestMove(_car.transform.position,
              _car.transform.rotation, new Vector3(Number(1), 0f, Number(2)));
            Physics.SyncTransforms();
            break;
          case "status": break;
          case "probe":
            File.WriteAllText(Path.Combine(directory, "response.txt"),
              _movement.GameTestProbe(_car.transform.position, _car.transform.rotation,
                new Vector3(Number(1), 0f, Number(2))));
            return;
          default: throw new ArgumentException("Unknown game test command.");
        }
        _cameraService.MoveTargetTo(_car.transform.position);
        Vector3 p = _car.transform.position;
        File.WriteAllText(Path.Combine(directory, "response.txt"),
          string.Format(CultureInfo.InvariantCulture, "{0:R} {1:R} {2:R}", p.x, p.y, p.z));
      } catch (Exception e) {
        File.WriteAllText(Path.Combine(directory, "response.txt"), "ERROR " + e.Message);
      }
    }
  }

  internal sealed partial class RcCarMovement {
    internal Vector3 GameTestMove(Vector3 position, Quaternion rotation, Vector3 displacement) =>
      Move(position, rotation, displacement, FloorHeight(position), _mapSize.TerrainSize2D);

    internal string GameTestProbe(Vector3 p, Quaternion r, Vector3 d) {
      float h = FloorHeight(p);
      bool supported = TryGetSupportedPosition(p + d, r, h, _mapSize.TerrainSize2D, true, out Vector3 target);
      float step = target.y - p.y;
      var overlaps = Physics.OverlapBox(target + Vector3.up * RcCarMovementSettings.CollisionSkinWidth,
        RcCarMovementSettings.CarHalfExtents, r, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
      string blockers = "";
      foreach (Collider c in overlaps) {
        if (!IsPartOfCar(c.transform) && !_worldQuery.IsDrivableSurfaceCollider(c)) blockers += c.name + c.bounds + ";";
      }
      return $"supported={supported} target={target} step={step} aligned={_worldQuery.IsAlignedStairTraversal(p, p+d,h,r)} "
        + $"stairs={_worldQuery.CanTraverseByStairs(p,p+d,h,h+step)} vertical={HasVerticalStepClearance(p,r,step)} "
        + $"clear={HasClearanceAt(target,r)} blockers={blockers}";
    }
  }
}
