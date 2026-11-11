using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarMovement {
    private bool TryGetSupportedPosition(Vector3 position, Quaternion rotation, float height, Vector2Int mapSize, bool allowStairHeightChange, out Vector3 groundedPosition) =>
      _support.TryGetSupportedPosition(position, rotation, height, mapSize, allowStairHeightChange, out groundedPosition);
    private bool IsPartOfCar(Transform candidate) =>
      _car != null && (candidate == _car.transform || candidate.IsChildOf(_car.transform));
  }
}
