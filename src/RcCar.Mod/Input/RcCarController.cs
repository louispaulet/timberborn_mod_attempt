using System.Collections.Generic;
using Timberborn.CameraSystem;
using Timberborn.BlockSystem;
using Timberborn.InputSystem;
using Timberborn.MapStateSystem;
using Timberborn.PathSystem;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using Timberborn.WaterSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
using UnityEngine.InputSystem.LowLevel;

namespace LouisPaulet.RcCar {
  public partial class RcCarController : IInputProcessor, ILoadableSingleton, IUnloadableSingleton {
    private readonly InputService _inputService;
    private readonly CameraService _cameraService;
    private readonly RcCarSpawnFinder _spawnFinder;
    private readonly RcCarMovement _movement;
    private GameObject? _car;
    private readonly List<Mesh> _carMeshes = new();
    private readonly List<Material> _carMaterials = new();
    private bool _isDriving = true;
    private bool _escapeRequested;
    private bool _spawnAttempted;

    public RcCarController(InputService inputService, CameraService cameraService, MapSize mapSize,
      ITerrainService terrainService, IThreadSafeWaterMap waterMap,
      IBlockService blockService, IPathService pathService) {
      _inputService = inputService;
      _cameraService = cameraService;
      var worldQuery = new RcCarWorldQuery(terrainService, waterMap, blockService, pathService);
      _spawnFinder = new RcCarSpawnFinder(mapSize, worldQuery);
      _movement = new RcCarMovement(mapSize, worldQuery);
    }

    public bool IsDriving => _car != null && _isDriving;
    public bool CarAvailable => _car != null;

    public void Load() {
      _spawnAttempted = false;
      _inputService.AddInputProcessor(this);
      InputSystem.onEvent += OnInputSystemEvent;
      Debug.Log("[LouisPaulet.RcCar] Controls active: WASD to drive, Escape to exit, V to re-enter.");
    }

    public void Unload() {
      _inputService.RemoveInputProcessor(this);
      InputSystem.onEvent -= OnInputSystemEvent;
      RcCarFactory.DestroyAssets(_carMeshes, _carMaterials);
      if (_car != null) {
        Object.Destroy(_car);
        _car = null;
      }
      _spawnAttempted = false;
      _movement.Reset();
    }
    public bool ProcessInput() {
      if (!_spawnAttempted) {
        _spawnAttempted = true;
        SpawnCar();
      }
#if RC_CAR_GAME_TESTS
      ProcessGameTestCommand();
#endif

      Keyboard? keyboard = Keyboard.current;
      if (keyboard == null || _car == null) {
        return false;
      }

      if (_isDriving) {
        if (_escapeRequested || keyboard.escapeKey.wasPressedThisFrame) {
          _escapeRequested = false;
          _isDriving = false;
          _movement.Stop();
          _inputService.FlushUIInput();
          Debug.Log("[LouisPaulet.RcCar] Driving exited. Press V to drive again.");
          return true;
        }

        if (keyboard.vKey.wasPressedThisFrame) {
          StopDriving();
          return true;
        }

        _movement.Drive(
          (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f),
          (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f));
        _cameraService.MoveTargetTo(_car.transform.position);
        return true;
      }

      if (keyboard.vKey.wasPressedThisFrame) {
        _isDriving = true;
        Debug.Log("[LouisPaulet.RcCar] Driving resumed.");
        return true;
      }

      return false;
    }

    private void OnInputSystemEvent(InputEventPtr inputEvent, InputDevice device) {
      if (!_isDriving || device != Keyboard.current || inputEvent.handled) {
        return;
      }
      if (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>()) {
        return;
      }

      Keyboard? keyboard = Keyboard.current;
      if (keyboard == null || keyboard.escapeKey.ReadValueFromEvent(inputEvent) < 0.5f) {
        return;
      }

      _escapeRequested = true;
      inputEvent.handled = true;
      _inputService.FlushUIInput();
    }

    private void StopDriving() {
      _isDriving = false;
      _movement.Stop();
      Debug.Log("[LouisPaulet.RcCar] Driving paused. Press V to drive again.");
    }
    private void SpawnCar() {
      Quaternion rotation = Quaternion.Euler(0f, _cameraService.FacingCamera.eulerAngles.y, 0f);
      if (!_spawnFinder.TryFindSpawnPosition(rotation, out Vector3 spawnPosition)) {
        Debug.LogWarning("[LouisPaulet.RcCar] No safe dry, flat spawn cell was found. The RC car was not spawned.");
        return;
      }
      if (!RcCarFactory.TryCreate(out _car, _carMeshes, _carMaterials, out Transform[] wheels)) return;
      _movement.SetCar(_car!, wheels);
      _car!.transform.SetPositionAndRotation(spawnPosition, rotation);
      _cameraService.MoveTargetTo(spawnPosition);
      Debug.Log("[LouisPaulet.RcCar] Spawned the Iron Teeth RC car at " + spawnPosition + ".");
    }
  }
}
