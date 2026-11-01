using System;
using UnityEngine;

namespace LouisPaulet.RcCar {
  /// <summary>
  /// Sweeps a box-shaped car body against Unity colliders and slides along blocking surfaces,
  /// skipping the path colliders identified by the game-world query.
  /// Terrain support and water rejection are handled by the game-world query before/after this
  /// horizontal movement; this class only resolves colliders such as buildings and obstacles.
  /// </summary>
  public static class RcCarPhysics {
    private const int CastBufferSize = 32;
    private const float MinMovementSqrMagnitude = 0.000001f;
    private const float MinProgressSqrMagnitude = 0.000001f;
    private const float WalkableNormalDot = 0.65f;
    private static readonly RaycastHit[] CastResults = new RaycastHit[CastBufferSize];

    /// <summary>
    /// Applies a horizontal displacement using a swept oriented box. The box center is the car
    /// transform position, and halfExtents are world-space half sizes before orientation.
    /// </summary>
    public static Vector3 MoveWithCollision(
      Vector3 startPosition,
      Quaternion orientation,
      Vector3 halfExtents,
      Vector3 desiredDisplacement,
      Transform selfRoot,
      Func<Collider, bool> isPathCollider,
      int collisionMask,
      float skinWidth,
      int maxSlideIterations,
      out bool collided) {
      Vector3 position = startPosition;
      Vector3 remaining = Vector3.ProjectOnPlane(desiredDisplacement, Vector3.up);
      collided = false;

      if (remaining.sqrMagnitude <= MinMovementSqrMagnitude) {
        return position;
      }

      float safeSkinWidth = Mathf.Max(0f, skinWidth);
      int iterations = Mathf.Clamp(maxSlideIterations, 1, 6);

      for (int iteration = 0; iteration < iterations; iteration++) {
        float distance = remaining.magnitude;
        if (distance * distance <= MinMovementSqrMagnitude) {
          break;
        }

        Vector3 direction = remaining / distance;
        RaycastHit closestHit;
        if (!TryFindBlockingHit(
          position,
          halfExtents,
          direction,
          orientation,
          distance + safeSkinWidth,
          selfRoot,
          isPathCollider,
          collisionMask,
          out closestHit)) {
          position += remaining;
          break;
        }

        collided = true;
        float travelDistance = Mathf.Clamp(closestHit.distance - safeSkinWidth, 0f, distance);
        Vector3 travelled = direction * travelDistance;
        position += travelled;

        Vector3 untravelled = remaining - travelled;
        Vector3 slide = Vector3.ProjectOnPlane(untravelled, closestHit.normal);
        if (slide.sqrMagnitude >= untravelled.sqrMagnitude - MinProgressSqrMagnitude) {
          break;
        }

        remaining = slide;
      }

      return position;
    }

    private static bool TryFindBlockingHit(
      Vector3 center,
      Vector3 halfExtents,
      Vector3 direction,
      Quaternion orientation,
      float distance,
      Transform selfRoot,
      Func<Collider, bool> isPathCollider,
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
          || isPathCollider(collider)) {
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
