using LouisPaulet.RcCar.Core;
using Timberborn.BlockSystem;
using UnityEngine;
namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarWorldQuery {
    public bool IsAlignedStairTraversal(Vector3 from, Vector3 to, float supportHeight, Quaternion rotation) {
      int supportLevel = Mathf.RoundToInt(supportHeight);
      // The center can already be on the landing while the rear still needs stair support.
      // Follow the longitudinal centerline so a stair in a neighboring lane cannot help.
      Vector3 reach = rotation * Vector3.forward * RcCarMovementSettings.CarHalfExtents.z;
      for (int lowHeight = supportLevel - 1; lowHeight <= supportLevel; lowHeight++) {
        for (int sample = -2; sample <= 2; sample++) {
          Vector3 offset = reach * (sample / 2f);
          if (TryFindAlignedStair(from + offset, to + offset, lowHeight, out _)) return true;
        }
      }
      return false;
    }

    public bool CanTraverseByStairs(Vector3 from, Vector3 to, float fromHeight, float toHeight) {
      int fromLevel = Mathf.RoundToInt(fromHeight);
      int toLevel = Mathf.RoundToInt(toHeight);
      if (Mathf.Abs(fromLevel - toLevel) != 1) return false;
      Vector3 displacement = to - from;
      int lowHeight = Mathf.Min(fromLevel, toLevel);
      RcCarCell fromCell = new RcCarCell(Mathf.FloorToInt(from.x), Mathf.FloorToInt(from.z));
      RcCarCell toCell = new RcCarCell(Mathf.FloorToInt(to.x), Mathf.FloorToInt(to.z));
      if (!TryFindAlignedStair(from, to, lowHeight, out RcCarStairConnector? stair)) return false;
      var fromSurface = new RcCarTerrainSurface(fromLevel);
      var toSurface = new RcCarTerrainSurface(toLevel);
      return RcCarSurfaceTraversal.CanTraverse(fromSurface, toSurface,
        fromCell, toCell, stair, displacement.x, displacement.z);
    }
  }
}
