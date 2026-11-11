using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarMovement {
    private Vector3 Move(Vector3 position, Quaternion rotation, Vector3 displacement,
      float supportHeight, Vector2Int mapSize) {
      Vector3 resolved = MoveWithSupport(position, rotation, displacement, supportHeight, mapSize);
      if (displacement.sqrMagnitude <= 0.000001f
        || (resolved - position).sqrMagnitude >= displacement.sqrMagnitude * 0.0025f) {
        return resolved;
      }

      // Terrain/platform support can block driving without a Unity collider hit.
      // Give that contact the same gentle slip as a wall, without authorizing a step.
      Vector3 side = rotation * Vector3.right * (_steering < -0.05f ? -1f : 1f);
      float distance = Mathf.Min(RcCarMovementSettings.MovementSubstepLength,
        displacement.magnitude * RcCarMovementSettings.MinimumWallSlipRatio);
      Vector3 slipped = RcCarPhysics.MoveWithCollision(resolved, rotation,
        RcCarMovementSettings.CarHalfExtents, side * distance, _car!.transform,
        _worldQuery.IsDrivableSurfaceCollider, Physics.DefaultRaycastLayers,
        RcCarMovementSettings.CollisionSkinWidth, RcCarMovementSettings.MaxCollisionSlideIterations,
        out _);
      return TryGetSupportedPosition(slipped, rotation, FloorHeight(resolved), mapSize, false, out Vector3 grounded)
        && HasClearanceAt(grounded, rotation) ? grounded : resolved;
    }
  }
}
