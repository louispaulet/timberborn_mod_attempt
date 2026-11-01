using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarMovement {
    private bool HasBridgeDeckSupport(Vector3 probe, int bridgeHeight) {
      Vector3 rayOrigin = new Vector3(probe.x, bridgeHeight + RcCarMovementSettings.BridgeSupportRayStartOffset, probe.z);
      if (!Physics.Raycast(
        rayOrigin,
        Vector3.down,
        out RaycastHit hit,
        RcCarMovementSettings.BridgeSupportRayDistance,
        Physics.DefaultRaycastLayers,
        QueryTriggerInteraction.Ignore)) {
        return false;
      }

      if (hit.collider == null || _car != null && IsPartOfCar(hit.collider.transform)) {
        return false;
      }

      return Vector3.Dot(hit.normal, Vector3.up) > 0.65f
        && Mathf.Abs(hit.point.y - bridgeHeight) <= RcCarMovementSettings.BridgeSupportHeightTolerance;
    }

    private bool IsPartOfCar(Transform candidate) {
      return _car != null && (candidate == _car.transform || candidate.IsChildOf(_car.transform));
    }

    private static Vector3 GetFootprintProbe(Vector3 position, Quaternion rotation, int probeIndex) {
      Vector3 localOffset = probeIndex switch {
        1 => new Vector3(-RcCarMovementSettings.CarHalfExtents.x, 0f, -RcCarMovementSettings.CarHalfExtents.z),
        2 => new Vector3(-RcCarMovementSettings.CarHalfExtents.x, 0f, RcCarMovementSettings.CarHalfExtents.z),
        3 => new Vector3(RcCarMovementSettings.CarHalfExtents.x, 0f, -RcCarMovementSettings.CarHalfExtents.z),
        4 => new Vector3(RcCarMovementSettings.CarHalfExtents.x, 0f, RcCarMovementSettings.CarHalfExtents.z),
        _ => Vector3.zero,
      };
      return position + rotation * localOffset;
    }
    private static bool IsCellInsideMap(Vector2Int cell, Vector2Int mapSize) {
      return cell.x >= 0 && cell.x < mapSize.x && cell.y >= 0 && cell.y < mapSize.y;
    }
  }
}
