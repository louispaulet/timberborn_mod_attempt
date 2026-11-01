using UnityEngine;
namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarMovement {
    private bool TryGetSupportedPosition(
      Vector3 position,
      Quaternion rotation,
      float referenceSupportHeight,
      Vector2Int mapSize,
      bool allowStairHeightChange,
      out Vector3 groundedPosition) {
      float lowestSupportHeight = float.PositiveInfinity;
      float highestSupportHeight = float.NegativeInfinity;
      bool hasSupportHeight = false;

      for (int probeIndex = 0; probeIndex < 5; probeIndex++) {
        Vector3 probe = GetFootprintProbe(position, rotation, probeIndex);
        Vector2Int cell = new Vector2Int(Mathf.FloorToInt(probe.x), Mathf.FloorToInt(probe.z));
        if (!IsCellInsideMap(cell, mapSize)) {
          groundedPosition = default;
          return false;
        }

        float cellSupportHeight = 0f;
        bool hasTerrainSupport = _worldQuery.TryGetTopTerrainHeight(cell, out int terrainHeight);
        if (hasTerrainSupport) {
          cellSupportHeight = terrainHeight;
          if (_worldQuery.TryGetFinishedPlatformTopHeight(cell, terrainHeight, out int platformTopHeight)) {
            cellSupportHeight = Mathf.Max(cellSupportHeight, platformTopHeight);
          }
          hasTerrainSupport = !_worldQuery.IsUnderwaterAtSurface(cell, cellSupportHeight);
        }

        if (!hasTerrainSupport) {
          int bridgeHeight = Mathf.RoundToInt(referenceSupportHeight);
          if (!_worldQuery.IsPathAt(cell, bridgeHeight)
            || _worldQuery.IsUnderwaterAtSurface(cell, bridgeHeight)
            || !HasBridgeDeckSupport(probe, bridgeHeight)) {
            groundedPosition = default;
            return false;
          }

          cellSupportHeight = bridgeHeight;
        }

        if (Mathf.Abs(cellSupportHeight - referenceSupportHeight) > RcCarMovementSettings.MaxStairHeightChange
          || !allowStairHeightChange
            && Mathf.Abs(cellSupportHeight - referenceSupportHeight) > RcCarMovementSettings.SupportHeightTolerance
          || _worldQuery.IsBlockedByNonPathObject(cell, Mathf.FloorToInt(cellSupportHeight), Mathf.CeilToInt(cellSupportHeight + RcCarMovementSettings.CarBlockHeight))) {
          groundedPosition = default;
          return false;
        }

        lowestSupportHeight = Mathf.Min(lowestSupportHeight, cellSupportHeight);
        highestSupportHeight = Mathf.Max(highestSupportHeight, cellSupportHeight);
        hasSupportHeight = true;
      }

      float allowedSupportSpread = allowStairHeightChange ? RcCarMovementSettings.MaxStairHeightChange : RcCarMovementSettings.SupportHeightTolerance;
      if (!hasSupportHeight || highestSupportHeight - lowestSupportHeight > allowedSupportSpread) {
        groundedPosition = default;
        return false;
      }

      groundedPosition = new Vector3(position.x, highestSupportHeight + RcCarMovementSettings.CarHalfExtents.y + RcCarMovementSettings.GroundClearance, position.z);
      return hasSupportHeight;
    }
  }
}
