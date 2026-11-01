using Timberborn.BlockSystem;
using Timberborn.PathSystem;
using Timberborn.TerrainSystem;
using Timberborn.WaterSystem;
using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarWorldQuery {
    private readonly ITerrainService _terrainService;
    private readonly IThreadSafeWaterMap _waterMap;
    private readonly IBlockService _blockService;
    private readonly IPathService _pathService;
    public RcCarWorldQuery(ITerrainService terrainService, IThreadSafeWaterMap waterMap,
      IBlockService blockService, IPathService pathService) {
      _terrainService = terrainService; _waterMap = waterMap;
      _blockService = blockService; _pathService = pathService;
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
  }
}
