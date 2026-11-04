using UnityEngine;

namespace LouisPaulet.RcCar {
  public partial class RcCarController {
    private void SpawnCar() {
      Quaternion rotation = Quaternion.Euler(0f, _cameraService.FacingCamera.eulerAngles.y, 0f);
      if (!_spawnFinder.TryFindSpawnPosition(rotation, out Vector3 spawnPosition)) {
        Debug.LogWarning("[LouisPaulet.RcCar] No safe dry, flat spawn cell was found. The RC car was not spawned.");
        return;
      }
      if (!RcCarCarFactory.TryCreate(out _car, _carMeshes, _carMaterials, out Transform[] wheels)) return;
      _movement.SetCar(_car!, wheels);
      _car!.transform.SetPositionAndRotation(spawnPosition, rotation);
      _cameraService.MoveTargetTo(spawnPosition);
      Debug.Log("[LouisPaulet.RcCar] Spawned the Iron Teeth RC car at " + spawnPosition + ".");
    }
  }
}
