using Timberborn.BlockSystem;
using Timberborn.PathSystem;
using UnityEngine;
namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarWorldQuery {
    public bool IsDrivableSurfaceCollider(Collider collider) {
      Bounds bounds = collider.bounds;
      if (bounds.size.x > 1.5f || bounds.size.y > 1.5f || bounds.size.z > 1.5f) {
        return false;
      }

      int minX = Mathf.FloorToInt(bounds.min.x + 0.001f);
      int maxX = Mathf.FloorToInt(bounds.max.x - 0.001f);
      int minY = Mathf.FloorToInt(bounds.min.z + 0.001f);
      int maxY = Mathf.FloorToInt(bounds.max.z - 0.001f);
      int minHeight = Mathf.FloorToInt(bounds.min.y + 0.001f);
      int maxHeight = Mathf.FloorToInt(bounds.max.y - 0.001f);

      if (maxX < minX) {
        minX = maxX = Mathf.FloorToInt(bounds.center.x);
      }
      if (maxY < minY) {
        minY = maxY = Mathf.FloorToInt(bounds.center.z);
      }
      if (maxHeight < minHeight) {
        minHeight = maxHeight = Mathf.FloorToInt(bounds.center.y);
      }

      for (int x = minX; x <= maxX; x++) {
        for (int y = minY; y <= maxY; y++) {
          for (int height = minHeight; height <= maxHeight; height++) {
            Vector2Int cell = new Vector2Int(x, y);
            if (IsPathAt(cell, height) || HasFinishedStairsAt(new Vector3Int(x, y, height))) {
              return true;
            }
          }
        }
      }

      return false;
    }

    private bool HasFinishedStairsAt(Vector3Int coordinates) {
      if (!_blockService.Contains(coordinates)) {
        return false;
      }

      foreach (BlockObject blockObject in _blockService.GetObjectsAt(coordinates)) {
        if (blockObject.IsFinished && HasBlockName(blockObject, "Stairs")) {
          return true;
        }
      }

      return false;
    }
  }
}
