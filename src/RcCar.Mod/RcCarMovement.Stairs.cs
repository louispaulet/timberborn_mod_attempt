using UnityEngine;
namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarMovement {
    private bool TryTraverseStairs(
      Vector3 currentPosition,
      Vector3 desiredPosition,
      Quaternion rotation,
      float referenceSupportHeight,
      Vector2Int mapSize,
      out Vector3 steppedPosition) {
      steppedPosition = default;
      if (!TryGetSupportedPosition(
        desiredPosition,
        rotation,
        referenceSupportHeight,
        mapSize,
        true,
        out Vector3 targetGroundedPosition)) {
        return false;
      }

      float stepHeight = targetGroundedPosition.y - currentPosition.y;
      float targetSupportHeight = targetGroundedPosition.y
        - RcCarMovementSettings.CarHalfExtents.y - RcCarMovementSettings.GroundClearance;
      bool stairAligned = Mathf.Abs(stepHeight) <= RcCarMovementSettings.SupportHeightTolerance
        ? _worldQuery.IsAlignedStairTraversal(currentPosition, desiredPosition, referenceSupportHeight)
        : Mathf.Abs(Mathf.Abs(stepHeight) - RcCarMovementSettings.MaxStairHeightChange)
            <= RcCarMovementSettings.SupportHeightTolerance
          && _worldQuery.CanTraverseByStairs(
            currentPosition, desiredPosition, referenceSupportHeight, targetSupportHeight);
      if (!stairAligned) {
        return false;
      }

      if (stepHeight > 0f && !HasVerticalStepClearance(currentPosition, rotation, stepHeight)) {
        return false;
      }

      Vector3 horizontalDisplacement = new Vector3(
        desiredPosition.x - currentPosition.x,
        0f,
        desiredPosition.z - currentPosition.z);
      Vector3 elevatedStart = currentPosition + Vector3.up * Mathf.Max(0f, stepHeight);
      Vector3 elevatedTarget = RcCarPhysics.MoveWithCollision(
        elevatedStart,
        rotation,
        RcCarMovementSettings.CarHalfExtents,
        horizontalDisplacement,
        _car!.transform,
        _worldQuery.IsDrivableSurfaceCollider,
        Physics.DefaultRaycastLayers,
        RcCarMovementSettings.CollisionSkinWidth,
        RcCarMovementSettings.MaxCollisionSlideIterations,
        out _);

      if ((elevatedTarget - elevatedStart - horizontalDisplacement).sqrMagnitude > 0.0025f) {
        return false;
      }

      Vector3 groundedTarget = new Vector3(elevatedTarget.x, targetGroundedPosition.y, elevatedTarget.z);
      if (!TryGetSupportedPosition(
        groundedTarget,
        rotation,
        referenceSupportHeight + stepHeight,
        mapSize,
        true,
        out Vector3 finalPosition)
        || !HasClearanceAt(finalPosition, rotation)) {
        return false;
      }

      steppedPosition = finalPosition;
      return true;
    }
  }
}
