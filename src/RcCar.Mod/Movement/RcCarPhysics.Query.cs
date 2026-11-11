using System;
using UnityEngine;

namespace LouisPaulet.RcCar {
  public static partial class RcCarPhysics {
    private static bool TryFindBlockingHit(
      Vector3 center,
      Vector3 halfExtents,
      Vector3 direction,
      Quaternion orientation,
      float distance,
      Transform selfRoot,
      Func<Collider, bool> isDrivableSurfaceCollider,
      int collisionMask,
      out RaycastHit closestHit) {
      int hitCount = Physics.BoxCastNonAlloc(
        center,
        halfExtents,
        direction,
        CastResults,
        orientation,
        distance,
        collisionMask,
        QueryTriggerInteraction.Ignore);

      // NonAlloc queries do not guarantee that the closest result is present when the result
      // buffer fills. Fall back to the allocating query only for this unusually dense case.
      RaycastHit[]? overflowResults = null;
      if (hitCount >= CastResults.Length) {
        overflowResults = Physics.BoxCastAll(
          center,
          halfExtents,
          direction,
          orientation,
          distance,
          collisionMask,
          QueryTriggerInteraction.Ignore);
      }

      bool found = false;
      closestHit = default;
      float closestDistance = float.PositiveInfinity;
      int resultCount = overflowResults != null ? overflowResults.Length : hitCount;

      for (int index = 0; index < resultCount; index++) {
        RaycastHit hit = overflowResults != null ? overflowResults[index] : CastResults[index];
        Collider? collider = hit.collider;
        if (collider == null
          || IsPartOfSelf(collider.transform, selfRoot)
          || isDrivableSurfaceCollider(collider)) {
          continue;
        }

        // The car's horizontal sweep can touch a flat floor or roof from above. Such a surface
        // supports the vehicle but does not obstruct horizontal progress.
        if (Vector3.Dot(hit.normal, Vector3.up) > WalkableNormalDot) {
          continue;
        }

        if (hit.distance < closestDistance) {
          closestDistance = hit.distance;
          closestHit = hit;
          found = true;
        }
      }

      return found;
    }

    private static bool IsPartOfSelf(Transform candidate, Transform selfRoot) {
      return candidate == selfRoot || candidate.IsChildOf(selfRoot);
    }
  }
}
