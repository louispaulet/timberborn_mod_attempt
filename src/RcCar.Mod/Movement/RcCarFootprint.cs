using UnityEngine;

namespace LouisPaulet.RcCar {
  internal static class RcCarFootprint {
    public static bool IsCellInsideMap(Vector2Int cell, Vector2Int mapSize) {
      return cell.x >= 0 && cell.x < mapSize.x && cell.y >= 0 && cell.y < mapSize.y;
    }

    public static Vector3 GetFootprintProbe(Vector3 position, Quaternion rotation, int index) {
      Vector3 offset = index switch {
        1 => new Vector3(-RcCarMovementSettings.CarHalfExtents.x, 0f, -RcCarMovementSettings.CarHalfExtents.z),
        2 => new Vector3(-RcCarMovementSettings.CarHalfExtents.x, 0f, RcCarMovementSettings.CarHalfExtents.z),
        3 => new Vector3(RcCarMovementSettings.CarHalfExtents.x, 0f, -RcCarMovementSettings.CarHalfExtents.z),
        4 => new Vector3(RcCarMovementSettings.CarHalfExtents.x, 0f, RcCarMovementSettings.CarHalfExtents.z),
        _ => Vector3.zero,
      };
      return position + rotation * offset;
    }
  }
}
