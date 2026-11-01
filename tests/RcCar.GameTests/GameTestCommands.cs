// Compiled only with -p:RcCarGameTests=true. Commands execute on Unity's main thread,
// through the real movement/support/collision code; normal mod builds have no test bridge.
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace LouisPaulet.RcCar {
  public partial class RcCarController {
    private void ProcessGameTestCommand() {
      string? directory = Environment.GetEnvironmentVariable("RC_CAR_GAME_TEST_DIR");
      if (_car == null || string.IsNullOrEmpty(directory)) return;
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
  }
}
