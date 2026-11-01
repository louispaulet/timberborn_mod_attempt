using Timberborn.MapStateSystem;
using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed class RcCarSpawnFinder {
    private readonly MapSize _mapSize;
    private readonly RcCarWorldQuery _worldQuery;
    public RcCarSpawnFinder(MapSize mapSize, RcCarWorldQuery worldQuery) {
      _mapSize = mapSize; _worldQuery = worldQuery;
    }
    public bool TryFindSpawnPosition(Quaternion rotation, out Vector3 spawnPosition) {
      Vector2Int mapSize = _mapSize.TerrainSize2D;
      Vector2Int center = new Vector2Int(Mathf.RoundToInt(mapSize.x * 0.5f), Mathf.RoundToInt(mapSize.y * 0.5f));
      int maxRadius = Mathf.Max(mapSize.x, mapSize.y);

      for (int radius = RcCarMovementSettings.FlatGroundSearchStartRadius; radius <= maxRadius; radius++) {
        for (int xOffset = -radius; xOffset <= radius; xOffset++) {
          for (int zOffset = -radius; zOffset <= radius; zOffset++) {
            if (radius > 0 && Mathf.Abs(xOffset) != radius && Mathf.Abs(zOffset) != radius) {
              continue;
            }

            Vector2Int cell = new Vector2Int(center.x + xOffset, center.y + zOffset);
            if (cell.x < 1 || cell.x >= mapSize.x - 1 || cell.y < 1 || cell.y >= mapSize.y - 1) {
              continue;
            }

            if (!_worldQuery.TryGetFlatDryTerrainHeight(cell, out int height)) {
              continue;
            }

            Vector3 candidate = new Vector3(cell.x + 0.5f, height + RcCarMovementSettings.CarHalfExtents.y + RcCarMovementSettings.GroundClearance, cell.y + 0.5f);
            if (!IsSafeSpawnPosition(candidate, rotation, height, mapSize)) {
              continue;
            }

            spawnPosition = candidate;
            return true;
          }
        }
      }

      spawnPosition = default;
      return false;
    }

    private bool IsSafeSpawnPosition(Vector3 position, Quaternion rotation, int supportHeight, Vector2Int mapSize) {
      for (int probeIndex = 0; probeIndex < 5; probeIndex++) {
        Vector3 probe = GetFootprintProbe(position, rotation, probeIndex);
        Vector2Int cell = new Vector2Int(Mathf.FloorToInt(probe.x), Mathf.FloorToInt(probe.z));
        if (!IsCellInsideMap(cell, mapSize)
          || !_worldQuery.TryGetTopTerrainHeight(cell, out int cellHeight)
          || cellHeight != supportHeight
          || _worldQuery.IsUnderwaterAtSurface(cell, cellHeight)
          || _worldQuery.IsBlockedByNonPathObject(cell, supportHeight, supportHeight + 1)) {
          return false;
        }
      }

      Collider[] overlaps = Physics.OverlapBox(
        position,
        RcCarMovementSettings.CarHalfExtents,
        rotation,
        Physics.DefaultRaycastLayers,
        QueryTriggerInteraction.Ignore);
      foreach (Collider collider in overlaps) {
        if (collider != null && !_worldQuery.IsDrivableSurfaceCollider(collider)) {
          return false;
        }
      }

      return true;
    }

    private static bool IsCellInsideMap(Vector2Int cell, Vector2Int mapSize) {
      return cell.x >= 0 && cell.x < mapSize.x && cell.y >= 0 && cell.y < mapSize.y;
    }

    private static Vector3 GetFootprintProbe(Vector3 position, Quaternion rotation, int index) {
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
