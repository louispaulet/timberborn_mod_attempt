using UnityEngine;
namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarWorldQuery {
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
