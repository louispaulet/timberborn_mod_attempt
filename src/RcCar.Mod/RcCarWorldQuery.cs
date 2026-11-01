using System.Collections.Generic;
using Timberborn.BlockSystem;
using Timberborn.PathSystem;
using Timberborn.TerrainSystem;
using Timberborn.WaterSystem;
using UnityEngine;

namespace LouisPaulet.RcCar {
  /// <summary>
  /// Reads Timberborn's terrain and water maps for safe ground movement and spawning.
  /// Map coordinates use x/y; Unity world coordinates use x/z for the same horizontal axes.
  /// </summary>
  internal sealed class RcCarWorldQuery {
    private readonly ITerrainService _terrainService;
    private readonly IThreadSafeWaterMap _waterMap;
    private readonly IBlockService _blockService;
    private readonly IPathService _pathService;

    public RcCarWorldQuery(
      ITerrainService terrainService,
      IThreadSafeWaterMap waterMap,
      IBlockService blockService,
      IPathService pathService) {
      _terrainService = terrainService;
      _waterMap = waterMap;
      _blockService = blockService;
      _pathService = pathService;
    }

    public bool TryGetTopTerrainHeight(Vector2Int cell, out int height) {
      height = int.MinValue;
      if (!_terrainService.Contains(cell)) {
        return false;
      }

      foreach (Vector3Int surface in _terrainService.GetAllHeightsInCell(cell)) {
        height = Mathf.Max(height, surface.z);
      }

      return height != int.MinValue;
    }

    /// <summary>
    /// Tests whether water occupies the Timberborn vertical cell at the support surface.
    /// Passing the actual support height lets bridges remain drivable when the water is
    /// below the bridge deck, while blocking the same cell at shoreline and water level.
    /// </summary>
    public bool IsUnderwaterAtSurface(Vector2Int cell, float supportHeight) {
      if (!_terrainService.Contains(cell)) {
        return true;
      }

      int height = Mathf.RoundToInt(supportHeight);
      return _waterMap.CellIsUnderwater(new Vector3Int(cell.x, cell.y, height));
    }

    public bool IsPathAt(Vector2Int cell, int height) {
      return _pathService.IsPath(new Vector3Int(cell.x, cell.y, height));
    }

    /// <summary>
    /// Identifies a single-cell Unity collider belonging to a path tile. This lets the car's
    /// collider sweeps pass over the path's own block collider without ignoring nearby buildings.
    /// </summary>
    public bool IsPathCollider(Collider collider) {
      Bounds bounds = collider.bounds;
      if (bounds.size.x > 1.5f || bounds.size.y > 1.5f || bounds.size.z > 1.5f) {
        return false;
      }

      int minX = Mathf.FloorToInt(bounds.min.x + 0.001f);
      int maxX = Mathf.FloorToInt(bounds.max.x - 0.001f);
      int minY = Mathf.FloorToInt(bounds.min.z + 0.001f);
      int maxY = Mathf.FloorToInt(bounds.max.z - 0.001f);
      int pathHeight = Mathf.FloorToInt(bounds.center.y);

      if (maxX < minX) {
        minX = maxX = Mathf.FloorToInt(bounds.center.x);
      }
      if (maxY < minY) {
        minY = maxY = Mathf.FloorToInt(bounds.center.z);
      }

      for (int x = minX; x <= maxX; x++) {
        for (int y = minY; y <= maxY; y++) {
          if (IsPathAt(new Vector2Int(x, y), pathHeight)) {
            return true;
          }
        }
      }

      return false;
    }

    /// <summary>
    /// Blocks buildings and natural resources while allowing paths, including bridge paths.
    /// Check the vertical cells occupied by the car body, starting at its support surface.
    /// </summary>
    public bool IsBlockedByNonPathObject(Vector2Int cell, int supportHeight, int carTopHeight) {
      for (int height = supportHeight; height <= carTopHeight; height++) {
        Vector3Int coordinates = new Vector3Int(cell.x, cell.y, height);
        if (_blockService.Contains(coordinates)
          && _blockService.AnyObjectAt(coordinates)
          && !_pathService.IsPath(coordinates)) {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Finds a level, dry terrain cell suitable as a spawn candidate. Callers should still
    /// check the returned position against live Unity colliders for buildings and objects.
    /// </summary>
    public bool TryGetFlatDryTerrainHeight(Vector2Int cell, out int height) {
      if (!TryGetTopTerrainHeight(cell, out height)
        || !HasTerrainAtHeight(new Vector2Int(cell.x - 1, cell.y), height)
        || !HasTerrainAtHeight(new Vector2Int(cell.x + 1, cell.y), height)
        || !HasTerrainAtHeight(new Vector2Int(cell.x, cell.y - 1), height)
        || !HasTerrainAtHeight(new Vector2Int(cell.x, cell.y + 1), height)
        || IsUnderwaterAtSurface(cell, height)) {
        height = 0;
        return false;
      }

      return true;
    }

    private bool HasTerrainAtHeight(Vector2Int cell, int expectedHeight) {
      return TryGetTopTerrainHeight(cell, out int height) && height == expectedHeight;
    }
  }
}
