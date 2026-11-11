using System;
using LouisPaulet.RcCar.Core;
using UnityEngine;

namespace LouisPaulet.RcCar {
  /// <summary>
  /// Sweeps a box-shaped car body against Unity colliders and slides along blocking surfaces,
  /// skipping path and completed stair colliders identified by the game-world query.
  /// Terrain support and water rejection are handled by the game-world query before/after this
  /// horizontal movement; this class only resolves colliders such as buildings and obstacles.
  /// </summary>
  public static partial class RcCarPhysics {
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
      Func<Collider, bool> isDrivableSurfaceCollider,
      int collisionMask,
      float skinWidth,
      int maxSlideIterations,
      out bool collided,
      Vector3 preferredSlideDirection = default,
      float minimumWallSlipRatio = 0f) {
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
          isDrivableSurfaceCollider,
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
        if (iteration == 0 && minimumWallSlipRatio > 0f) {
          var lateral = RcCarWallSlide.Resolve(
            new System.Numerics.Vector2(untravelled.x, untravelled.z),
            new System.Numerics.Vector2(closestHit.normal.x, closestHit.normal.z),
            new System.Numerics.Vector2(preferredSlideDirection.x, preferredSlideDirection.z),
            minimumWallSlipRatio);
          slide = new Vector3(lateral.X, 0f, lateral.Y);
        }
        if (slide.sqrMagnitude >= untravelled.sqrMagnitude - MinProgressSqrMagnitude) {
          break;
        }

        remaining = slide;
      }

      return position;
    }
  }
}
