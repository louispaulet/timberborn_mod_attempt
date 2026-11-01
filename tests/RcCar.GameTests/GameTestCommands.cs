// Compiled only with -p:RcCarGameTests=true. Commands execute on Unity's main thread,
// through the real movement/support/collision code; normal mod builds have no test bridge.
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace LouisPaulet.RcCar {
  public partial class RcCarController {
    private readonly System.Collections.Generic.List<GameObject> _gameTestWalls = new System.Collections.Generic.List<GameObject>();
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
              _car.transform.rotation, new Vector3(Number(1), 0f, Number(2)),
              args.Length > 3 ? Number(3) : 0f);
            Physics.SyncTransforms();
            break;
          case "wall":
            var wall = new GameObject("RC Car game test wall");
            wall.transform.position = new Vector3(Number(1), Number(2), Number(3));
            wall.AddComponent<BoxCollider>().size = new Vector3(Number(4), Number(5), Number(6));
            _gameTestWalls.Add(wall);
            Physics.SyncTransforms();
            break;
          case "clear-walls":
            foreach (GameObject testWall in _gameTestWalls) {
              testWall.SetActive(false);
              UnityEngine.Object.Destroy(testWall);
            }
            _gameTestWalls.Clear();
            Physics.SyncTransforms();
            break;
          case "status": break;
          case "probe":
            WriteGameTestResponse(directory,
              _movement.GameTestProbe(_car.transform.position, _car.transform.rotation,
                new Vector3(Number(1), 0f, Number(2))));
            return;
          default: throw new ArgumentException("Unknown game test command.");
        }
        _cameraService.MoveTargetTo(_car.transform.position);
        Vector3 p = _car.transform.position;
        WriteGameTestResponse(directory,
          string.Format(CultureInfo.InvariantCulture, "{0:R} {1:R} {2:R}", p.x, p.y, p.z));
      } catch (Exception e) {
        WriteGameTestResponse(directory, "ERROR " + e.Message);
      }
    }

    private static void WriteGameTestResponse(string directory, string contents) {
      string pending = Path.Combine(directory, "response.pending");
      string response = Path.Combine(directory, "response.txt");
      File.WriteAllText(pending, contents);
      if (File.Exists(response)) File.Delete(response);
      File.Move(pending, response);
    }
  }

  internal sealed partial class RcCarMovement {
    internal Vector3 GameTestMove(Vector3 position, Quaternion rotation, Vector3 displacement, float steering) {
      _steering = steering;
      return Move(position, rotation, displacement, FloorHeight(position), _mapSize.TerrainSize2D);
    }

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
