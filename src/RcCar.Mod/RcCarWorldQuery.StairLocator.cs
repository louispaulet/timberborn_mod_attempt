using LouisPaulet.RcCar.Core;
using Timberborn.BlockSystem;
using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarWorldQuery {
    private bool TryFindAlignedStair(Vector3 from, Vector3 to, Quaternion rotation,
      int lowHeight, out RcCarStairConnector? connector) {
      connector = null;
      Vector3 right = rotation * Vector3.right;
      Vector3 forward = rotation * Vector3.forward;
      float extentX = Mathf.Abs(right.x) * RcCarMovementSettings.CarHalfExtents.x
        + Mathf.Abs(forward.x) * RcCarMovementSettings.CarHalfExtents.z;
      float extentY = Mathf.Abs(right.z) * RcCarMovementSettings.CarHalfExtents.x
        + Mathf.Abs(forward.z) * RcCarMovementSettings.CarHalfExtents.z;
      int minX = Mathf.CeilToInt(Mathf.Min(from.x, to.x) - extentX - 1f);
      int maxX = Mathf.FloorToInt(Mathf.Max(from.x, to.x) + extentX);
      int minY = Mathf.CeilToInt(Mathf.Min(from.z, to.z) - extentY - 1f);
      int maxY = Mathf.FloorToInt(Mathf.Max(from.z, to.z) + extentY);

      for (int x = minX; x <= maxX; x++) {
        for (int y = minY; y <= maxY; y++) {
          if (TryGetStairConnector(new Vector2Int(x, y), lowHeight, out connector)
            && connector!.AlignsWithDirection(to.x - from.x, to.z - from.z)) return true;
        }
      }
      connector = null;
      return false;
    }

    private bool TryGetStairConnector(Vector2Int cell, int lowHeight, out RcCarStairConnector? connector) {
      connector = null;
      var coordinates = new Vector3Int(cell.x, cell.y, lowHeight);
      if (!_blockService.Contains(coordinates)) return false;
      foreach (BlockObject block in _blockService.GetObjectsAt(coordinates)) {
        if (!block.IsFinished || block.GameObject == null || !HasBlockName(block, "Stairs")) continue;
        Vector3 axis = block.GameObject.transform.forward;
        RcCarStairAxis direction = Mathf.Abs(axis.x) > Mathf.Abs(axis.z)
          ? RcCarStairAxis.EastWest : RcCarStairAxis.NorthSouth;
        connector = new RcCarStairConnector(new RcCarCell(cell.x, cell.y), lowHeight, direction, true);
        return true;
      }
      return false;
    }
  }
}
