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

namespace LouisPaulet.RcCar {
  public partial class RcCarController : IInputProcessor, ILoadableSingleton, IUnloadableSingleton {
    private readonly InputService _inputService;
    private readonly CameraService _cameraService;
    private readonly RcCarSpawnFinder _spawnFinder;
    private readonly RcCarMovement _movement;
    private GameObject? _car;
    private Mesh? _carMesh;
    private Material[]? _carMaterials;
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
      if (_carMaterials != null) {
        foreach (Material material in _carMaterials) {
          if (material != null) {
            Object.Destroy(material);
          }
        }
        _carMaterials = null;
      }
      if (_carMesh != null) {
        Object.Destroy(_carMesh);
        _carMesh = null;
      }
      if (_car != null) {
        Object.Destroy(_car);
        _car = null;
      }
      _spawnAttempted = false;
      _movement.Reset();
    }
  }
}
