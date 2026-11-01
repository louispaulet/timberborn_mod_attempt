using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarMovement {
private bool HasVerticalStepClearance(Vector3 position, Quaternion rotation, float stepHeight) {
      RaycastHit[] hits = Physics.BoxCastAll(
        position,
        RcCarMovementSettings.CarHalfExtents,
        Vector3.up,
        rotation,
        stepHeight,
        Physics.DefaultRaycastLayers,
        QueryTriggerInteraction.Ignore);
      foreach (RaycastHit hit in hits) {
        if (hit.collider == null
          || IsPartOfCar(hit.collider.transform)
          || _worldQuery.IsDrivableSurfaceCollider(hit.collider)) {
          continue;
        }

        if (Vector3.Dot(hit.normal, Vector3.up) > RcCarMovementSettings.WalkableNormalDot) {
          continue;
        }

        return false;
      }

      return true;
    }

private bool HasClearanceAt(Vector3 position, Quaternion rotation) {
      Collider[] overlaps = Physics.OverlapBox(
        position + Vector3.up * RcCarMovementSettings.CollisionSkinWidth,
        RcCarMovementSettings.CarHalfExtents,
        rotation,
        Physics.DefaultRaycastLayers,
        QueryTriggerInteraction.Ignore);
      foreach (Collider collider in overlaps) {
        if (collider != null
          && !IsPartOfCar(collider.transform)
          && !_worldQuery.IsDrivableSurfaceCollider(collider)) {
          return false;
        }
      }

      return true;
    }
  }
}
