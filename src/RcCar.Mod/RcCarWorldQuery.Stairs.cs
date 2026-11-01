using LouisPaulet.RcCar.Core;
using Timberborn.BlockSystem;
using UnityEngine;
namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarWorldQuery {
    public bool CanTraverseByStairs(Vector3 from, Vector3 to, Quaternion rotation, float fromHeight, float toHeight) {
      int fromLevel = Mathf.RoundToInt(fromHeight);
      int toLevel = Mathf.RoundToInt(toHeight);
      if (Mathf.Abs(fromLevel - toLevel) != 1) return false;
      Vector3 displacement = to - from;
      int lowHeight = Mathf.Min(fromLevel, toLevel);
      if (!TryFindAlignedStair(from, to, rotation, lowHeight, out RcCarStairConnector? stair)) return false;
      var fromSurface = new RcCarTerrainSurface(fromLevel);
      var toSurface = new RcCarTerrainSurface(toLevel);
      return RcCarSurfaceTraversal.CanTraverse(fromSurface, toSurface,
        stair!.Cell, stair.Cell, stair, displacement.x, displacement.z);
    }

    public bool IsAlignedStairTraversal(Vector3 from, Vector3 to, Quaternion rotation, float supportHeight) {
      int supportLevel = Mathf.RoundToInt(supportHeight);
      for (int lowHeight = supportLevel - 1; lowHeight <= supportLevel; lowHeight++) {
        if (TryFindAlignedStair(from, to, rotation, lowHeight, out _)) return true;
      }
      return false;
    }
  }
}
