using Timberborn.BlockSystem;
using Timberborn.PathSystem;
using LouisPaulet.RcCar.Core;
using UnityEngine;
namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarWorldQuery {
    public RcCarSurface GetSurfaceAt(Vector2Int cell, int height) {
      RcCarSurface surface = new RcCarTerrainSurface(height);
      if (TryGetTopTerrainHeight(cell, out int terrainHeight)
        && TryGetFinishedPlatformTopHeight(cell, terrainHeight, out int platformHeight)
        && platformHeight == height) surface = new RcCarPlatformSurface(height);
      return IsPathAt(cell, height) ? new RcCarPathSurface(surface) : surface;
    }

    public bool TryGetFinishedPlatformTopHeight(Vector2Int cell, int terrainHeight, out int height) {
      height = int.MinValue;
      for (int blockHeight = terrainHeight - 1; blockHeight <= terrainHeight + 1; blockHeight++) {
        Vector3Int coordinates = new Vector3Int(cell.x, cell.y, blockHeight);
        if (!_blockService.Contains(coordinates)) {
          continue;
        }

        foreach (BlockObject blockObject in _blockService.GetObjectsAt(coordinates)) {
          if (!blockObject.IsFinished || !HasBlockName(blockObject, "Platform")) {
            continue;
          }

          height = Mathf.Max(height, blockObject.Coordinates.z + 1);
        }
      }

      return height != int.MinValue;
    }

    public bool IsBlockedByNonPathObject(Vector2Int cell, int supportHeight, int carTopHeight) {
      for (int height = supportHeight; height <= carTopHeight; height++) {
        Vector3Int coordinates = new Vector3Int(cell.x, cell.y, height);
        if (_blockService.Contains(coordinates)
          && _blockService.AnyObjectAt(coordinates)
          && !_pathService.IsPath(coordinates)
          && !HasFinishedDrivableBlockAt(coordinates)) {
          return true;
        }
      }

      return false;
    }

    private bool HasFinishedDrivableBlockAt(Vector3Int coordinates) {
      bool foundObject = false;
      foreach (BlockObject blockObject in _blockService.GetObjectsAt(coordinates)) {
        foundObject = true;
        if (!blockObject.IsFinished
          || !HasBlockName(blockObject, "Stairs") && !HasBlockName(blockObject, "Platform")) {
          return false;
        }
      }

      return foundObject;
    }

    private static bool HasBlockName(BlockObject blockObject, string namePart) {
      string gameObjectName = blockObject.GameObject != null ? blockObject.GameObject.name : string.Empty;
      string displayName = blockObject.Name ?? string.Empty;
      return gameObjectName.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0
        || displayName.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
  }
}
