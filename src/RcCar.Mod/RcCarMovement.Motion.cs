using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarMovement {
    private Vector3 Move(Vector3 currentPosition, Quaternion rotation, Vector3 desiredDisplacement,
      float supportHeight, Vector2Int mapSize) {
      int moveSteps = Mathf.Max(1, Mathf.CeilToInt(desiredDisplacement.magnitude / RcCarMovementSettings.MovementSubstepLength));
      Vector3 stepDisplacement = desiredDisplacement / moveSteps;
      for (int step = 0; step < moveSteps && stepDisplacement.sqrMagnitude > 0.000001f; step++) {
        Vector3 desiredPosition = currentPosition + stepDisplacement;
        Vector3 resolved = RcCarPhysics.MoveWithCollision(currentPosition, rotation,
          RcCarMovementSettings.CarHalfExtents, stepDisplacement, _car!.transform,
          _worldQuery.IsDrivableSurfaceCollider, Physics.DefaultRaycastLayers,
          RcCarMovementSettings.CollisionSkinWidth, RcCarMovementSettings.MaxCollisionSlideIterations,
          out bool collided);
        if (collided) {
          // A stair's landing initially looks like a wall to the horizontal sweep.
          // Resolve a valid stair transition before introducing any sideways slip.
          if (TryTraverseStairs(currentPosition, desiredPosition, rotation, supportHeight, mapSize, out Vector3 stairPosition)
            && Vector3.Dot(stairPosition - currentPosition, stepDisplacement) > stepDisplacement.sqrMagnitude * 0.9f) {
            currentPosition = stairPosition;
            supportHeight = FloorHeight(currentPosition);
            continue;
          }
          resolved = RcCarPhysics.MoveWithCollision(currentPosition, rotation,
            RcCarMovementSettings.CarHalfExtents, stepDisplacement, _car!.transform,
            _worldQuery.IsDrivableSurfaceCollider, Physics.DefaultRaycastLayers,
            RcCarMovementSettings.CollisionSkinWidth, RcCarMovementSettings.MaxCollisionSlideIterations,
            out collided,
            rotation * Vector3.right * (_steering < -0.05f ? -1f : 1f),
            RcCarMovementSettings.MinimumWallSlipRatio);
        }
        if (!TryGetSupportedPosition(resolved, rotation, supportHeight, mapSize, false, out Vector3 grounded)) {
          if (!TryTraverseStairs(currentPosition, desiredPosition, rotation, supportHeight, mapSize, out grounded)) break;
          currentPosition = grounded; supportHeight = FloorHeight(currentPosition); continue;
        }
        if (Mathf.Abs(grounded.y - currentPosition.y) > RcCarMovementSettings.GroundClearance) {
          if (!TryTraverseStairs(currentPosition, desiredPosition, rotation, supportHeight, mapSize, out grounded)) break;
          currentPosition = grounded; supportHeight = FloorHeight(currentPosition); continue;
        }
        if ((grounded - currentPosition).sqrMagnitude < 0.000001f) {
          if (collided && TryTraverseStairs(currentPosition, desiredPosition, rotation, supportHeight, mapSize, out grounded)) {
            currentPosition = grounded; supportHeight = FloorHeight(currentPosition); continue;
          }
          break;
        }
        currentPosition = grounded;
        supportHeight = FloorHeight(currentPosition);
        if (collided && (resolved - grounded).sqrMagnitude < 0.000001f) break;
      }
      return currentPosition;
    }
    private static float FloorHeight(Vector3 position) =>
      position.y - RcCarMovementSettings.CarHalfExtents.y - RcCarMovementSettings.GroundClearance;
  }
}
