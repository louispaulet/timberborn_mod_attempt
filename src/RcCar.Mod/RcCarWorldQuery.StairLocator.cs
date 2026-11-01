using LouisPaulet.RcCar.Core;
using Timberborn.BlockSystem;
using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarWorldQuery {
    private bool TryFindAlignedStair(Vector3 from, Vector3 to, int lowHeight,
      out RcCarStairConnector? connector) {
      connector = null;
      RcCarCell fromCell = ToCell(from);
      RcCarCell toCell = ToCell(to);
      float dx = to.x - from.x;
      float dy = to.z - from.z;

      if (TryGetStairConnector(new Vector2Int(fromCell.X, fromCell.Y), lowHeight, out connector)
        && connector!.AlignsWith(fromCell, toCell, dx, dy)) {
        return true;
      }

      if (toCell != fromCell
        && TryGetStairConnector(new Vector2Int(toCell.X, toCell.Y), lowHeight, out connector)
        && connector!.AlignsWith(fromCell, toCell, dx, dy)) {
        return true;
      }

      connector = null;
      return false;
    }

    private static RcCarCell ToCell(Vector3 position) =>
      new RcCarCell(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z));

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
