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
using UnityEngine.InputSystem.LowLevel;

namespace LouisPaulet.RcCar {
  public class RcCarController : IInputProcessor, ILoadableSingleton, IUnloadableSingleton {
    private const float ForwardSpeed = 7f;
    private const float ReverseSpeed = 3.5f;
    private const float Acceleration = 8f;
    private const float Braking = 12f;
    private const float Coasting = 3f;
    private const float SteeringResponse = 5f;
    private const float TurnRate = 110f;
    private const float CarSize = 0.8f;
    private const int FlatGroundSearchStartRadius = 8;
    private const float GroundClearance = 0.03f;
    private const float CollisionSkinWidth = 0.03f;
    private const float MaxGroundHeightChange = 0.25f;
    private const float MovementSubstepLength = 0.1f;
    private const float BridgeSupportRayStartOffset = 1.25f;
    private const float BridgeSupportRayDistance = 1.5f;
    private const float BridgeSupportHeightTolerance = 0.3f;
    private const int MaxCollisionSlideIterations = 3;
    private static readonly Vector3 CarHalfExtents = Vector3.one * (CarSize * 0.5f);

    private readonly InputService _inputService;
    private readonly CameraService _cameraService;
    private readonly MapSize _mapSize;
    private readonly RcCarWorldQuery _worldQuery;
    private GameObject? _car;
    private Material? _carMaterial;
    private float _speed;
    private float _steering;
    private bool _isDriving = true;
    private bool _escapeRequested;
    private bool _spawnAttempted;

    public RcCarController(
      InputService inputService,
      CameraService cameraService,
      MapSize mapSize,
      ITerrainService terrainService,
      IThreadSafeWaterMap waterMap,
      IBlockService blockService,
      IPathService pathService) {
      _inputService = inputService;
      _cameraService = cameraService;
      _mapSize = mapSize;
      _worldQuery = new RcCarWorldQuery(terrainService, waterMap, blockService, pathService);
    }

    public void Load() {
      _spawnAttempted = false;
      _inputService.AddInputProcessor(this);
      InputSystem.onEvent += OnInputSystemEvent;
      Debug.Log("[LouisPaulet.RcCar] Controls active: WASD to drive, Escape to exit, V to re-enter.");
    }

    public void Unload() {
      _inputService.RemoveInputProcessor(this);
      InputSystem.onEvent -= OnInputSystemEvent;
      if (_carMaterial != null) {
        Object.Destroy(_carMaterial);
        _carMaterial = null;
      }
      if (_car != null) {
        Object.Destroy(_car);
        _car = null;
      }
      _spawnAttempted = false;
      _speed = 0f;
      _steering = 0f;
    }

    public bool ProcessInput() {
      if (!_spawnAttempted) {
        _spawnAttempted = true;
        SpawnCar();
      }

      Keyboard? keyboard = Keyboard.current;
      if (keyboard == null || _car == null) {
        return false;
      }

      if (_isDriving) {
        if (_escapeRequested || keyboard.escapeKey.wasPressedThisFrame) {
          _escapeRequested = false;
          _isDriving = false;
          _speed = 0f;
          _steering = 0f;
          _inputService.FlushUIInput();
          Debug.Log("[LouisPaulet.RcCar] Driving exited. Press V to drive again.");
          return true;
        }

        if (keyboard.vKey.wasPressedThisFrame) {
          StopDriving();
          return true;
        }

        Drive(keyboard);
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

    private void SpawnCar() {
      Quaternion rotation = Quaternion.Euler(0f, _cameraService.FacingCamera.eulerAngles.y, 0f);
      if (!TryFindSpawnPosition(rotation, out Vector3 spawnPosition)) {
        Debug.LogWarning("[LouisPaulet.RcCar] No safe dry, flat spawn cell was found. The RC car was not spawned.");
        return;
      }

      _car = GameObject.CreatePrimitive(PrimitiveType.Cube);
      _car.name = "RC Car";
      _car.transform.localScale = Vector3.one * CarSize;
      _car.transform.rotation = rotation;

      Renderer? renderer = _car.GetComponent<Renderer>();
      if (renderer != null) {
        Shader? shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) {
          shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader != null) {
          _carMaterial = new Material(shader);
          renderer.material = _carMaterial;
        } else {
          _carMaterial = renderer.material;
          Debug.LogWarning("[LouisPaulet.RcCar] Could not find a Timberborn-compatible cube shader.");
        }
        _carMaterial.color = new Color(1f, 0.28f, 0.04f);
      }

      _car.transform.position = spawnPosition;
      _cameraService.MoveTargetTo(spawnPosition);
      Debug.Log("[LouisPaulet.RcCar] Spawned grounded RC car at " + spawnPosition + ".");
    }

    private bool TryFindSpawnPosition(Quaternion rotation, out Vector3 spawnPosition) {
      Vector2Int mapSize = _mapSize.TerrainSize2D;
      Vector2Int center = new Vector2Int(Mathf.RoundToInt(mapSize.x * 0.5f), Mathf.RoundToInt(mapSize.y * 0.5f));
      int maxRadius = Mathf.Max(mapSize.x, mapSize.y);

      for (int radius = FlatGroundSearchStartRadius; radius <= maxRadius; radius++) {
        for (int xOffset = -radius; xOffset <= radius; xOffset++) {
          for (int zOffset = -radius; zOffset <= radius; zOffset++) {
            if (radius > 0 && Mathf.Abs(xOffset) != radius && Mathf.Abs(zOffset) != radius) {
              continue;
            }

            Vector2Int cell = new Vector2Int(center.x + xOffset, center.y + zOffset);
            if (cell.x < 1 || cell.x >= mapSize.x - 1 || cell.y < 1 || cell.y >= mapSize.y - 1) {
              continue;
            }

            if (!_worldQuery.TryGetFlatDryTerrainHeight(cell, out int height)) {
              continue;
            }

            Vector3 candidate = new Vector3(cell.x + 0.5f, height + CarHalfExtents.y + GroundClearance, cell.y + 0.5f);
            if (!IsSafeSpawnPosition(candidate, rotation, height, mapSize)) {
              continue;
            }

            spawnPosition = candidate;
            return true;
          }
        }
      }

      spawnPosition = default;
      return false;
    }

    private bool IsSafeSpawnPosition(Vector3 position, Quaternion rotation, int supportHeight, Vector2Int mapSize) {
      for (int probeIndex = 0; probeIndex < 5; probeIndex++) {
        Vector3 probe = GetFootprintProbe(position, rotation, probeIndex);
        Vector2Int cell = new Vector2Int(Mathf.FloorToInt(probe.x), Mathf.FloorToInt(probe.z));
        if (!IsCellInsideMap(cell, mapSize)
          || !_worldQuery.TryGetTopTerrainHeight(cell, out int cellHeight)
          || cellHeight != supportHeight
          || _worldQuery.IsUnderwaterAtSurface(cell, cellHeight)
          || _worldQuery.IsBlockedByNonPathObject(cell, supportHeight, supportHeight + 1)) {
          return false;
        }
      }

      Collider[] overlaps = Physics.OverlapBox(
        position,
        CarHalfExtents,
        rotation,
        Physics.DefaultRaycastLayers,
        QueryTriggerInteraction.Ignore);
      return overlaps.Length == 0;
    }

    private static bool IsCellInsideMap(Vector2Int cell, Vector2Int mapSize) {
      return cell.x >= 0 && cell.x < mapSize.x && cell.y >= 0 && cell.y < mapSize.y;
    }

    private void Drive(Keyboard keyboard) {
      if (_car == null) {
        return;
      }

      float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
      float throttle = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
      float targetSpeed = throttle > 0f ? ForwardSpeed : throttle < 0f ? -ReverseSpeed : 0f;
      float acceleration = throttle == 0f
        ? Coasting
        : _speed != 0f && Mathf.Sign(_speed) != Mathf.Sign(targetSpeed)
          ? Braking
          : Acceleration;
      _speed = Mathf.MoveTowards(_speed, targetSpeed, acceleration * deltaTime);

      float steeringInput = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
      _steering = Mathf.MoveTowards(_steering, steeringInput, SteeringResponse * deltaTime);
      float speedRatio = Mathf.Clamp01(Mathf.Abs(_speed) / ForwardSpeed);
      float reverseDirection = _speed < 0f ? -1f : 1f;
      float turnAngle = _steering * TurnRate * speedRatio * reverseDirection * deltaTime;
      Quaternion rotation = _car.transform.rotation * Quaternion.AngleAxis(turnAngle, Vector3.up);
      Vector3 currentPosition = _car.transform.position;
      float supportHeight = currentPosition.y - CarHalfExtents.y - GroundClearance;
      Vector2Int mapSize = _mapSize.TerrainSize2D;

      if (!TryGetSupportedPosition(currentPosition, rotation, supportHeight, mapSize, out Vector3 groundedStart)) {
        rotation = _car.transform.rotation;
        groundedStart = currentPosition;
      } else {
        currentPosition = groundedStart;
        supportHeight = currentPosition.y - CarHalfExtents.y - GroundClearance;
      }

      Vector3 desiredDisplacement = rotation * Vector3.forward * (_speed * deltaTime);
      int moveSteps = Mathf.Max(1, Mathf.CeilToInt(desiredDisplacement.magnitude / MovementSubstepLength));
      Vector3 stepDisplacement = desiredDisplacement / moveSteps;

      for (int step = 0; step < moveSteps && stepDisplacement.sqrMagnitude > 0.000001f; step++) {
        Vector3 collisionResolvedPosition = RcCarPhysics.MoveWithCollision(
          currentPosition,
          rotation,
          CarHalfExtents,
          stepDisplacement,
          _car.transform,
          Physics.DefaultRaycastLayers,
          CollisionSkinWidth,
          MaxCollisionSlideIterations,
          out bool collided);

        if ((collisionResolvedPosition - currentPosition).sqrMagnitude < 0.000001f) {
          break;
        }

        if (!TryGetSupportedPosition(collisionResolvedPosition, rotation, supportHeight, mapSize, out Vector3 groundedPosition)) {
          break;
        }

        currentPosition = groundedPosition;
        supportHeight = currentPosition.y - CarHalfExtents.y - GroundClearance;
        if (collided && (collisionResolvedPosition - groundedPosition).sqrMagnitude < 0.000001f) {
          break;
        }
      }

      _car.transform.SetPositionAndRotation(currentPosition, rotation);
    }

    private bool TryGetSupportedPosition(
      Vector3 position,
      Quaternion rotation,
      float referenceSupportHeight,
      Vector2Int mapSize,
      out Vector3 groundedPosition) {
      float supportHeight = 0f;
      bool hasSupportHeight = false;

      for (int probeIndex = 0; probeIndex < 5; probeIndex++) {
        Vector3 probe = GetFootprintProbe(position, rotation, probeIndex);
        Vector2Int cell = new Vector2Int(Mathf.FloorToInt(probe.x), Mathf.FloorToInt(probe.z));
        if (!IsCellInsideMap(cell, mapSize)) {
          groundedPosition = default;
          return false;
        }

        float cellSupportHeight;
        if (_worldQuery.TryGetTopTerrainHeight(cell, out int terrainHeight)
          && !_worldQuery.IsUnderwaterAtSurface(cell, terrainHeight)) {
          cellSupportHeight = terrainHeight;
        } else {
          int bridgeHeight = Mathf.RoundToInt(referenceSupportHeight);
          if (!_worldQuery.IsPathAt(cell, bridgeHeight)
            || _worldQuery.IsUnderwaterAtSurface(cell, bridgeHeight)
            || !HasBridgeDeckSupport(probe, bridgeHeight)) {
            groundedPosition = default;
            return false;
          }

          cellSupportHeight = bridgeHeight;
        }

        if (Mathf.Abs(cellSupportHeight - referenceSupportHeight) > MaxGroundHeightChange
          || _worldQuery.IsBlockedByNonPathObject(cell, Mathf.FloorToInt(cellSupportHeight), Mathf.CeilToInt(cellSupportHeight + CarSize))) {
          groundedPosition = default;
          return false;
        }

        if (hasSupportHeight && Mathf.Abs(cellSupportHeight - supportHeight) > MaxGroundHeightChange) {
          groundedPosition = default;
          return false;
        }

        supportHeight = cellSupportHeight;
        hasSupportHeight = true;
      }

      groundedPosition = new Vector3(position.x, supportHeight + CarHalfExtents.y + GroundClearance, position.z);
      return hasSupportHeight;
    }

    private bool HasBridgeDeckSupport(Vector3 probe, int bridgeHeight) {
      Vector3 rayOrigin = new Vector3(probe.x, bridgeHeight + BridgeSupportRayStartOffset, probe.z);
      if (!Physics.Raycast(
        rayOrigin,
        Vector3.down,
        out RaycastHit hit,
        BridgeSupportRayDistance,
        Physics.DefaultRaycastLayers,
        QueryTriggerInteraction.Ignore)) {
        return false;
      }

      if (hit.collider == null || _car != null && IsPartOfCar(hit.collider.transform)) {
        return false;
      }

      return Vector3.Dot(hit.normal, Vector3.up) > 0.65f
        && Mathf.Abs(hit.point.y - bridgeHeight) <= BridgeSupportHeightTolerance;
    }

    private bool IsPartOfCar(Transform candidate) {
      return _car != null && (candidate == _car.transform || candidate.IsChildOf(_car.transform));
    }

    private static Vector3 GetFootprintProbe(Vector3 position, Quaternion rotation, int probeIndex) {
      Vector3 localOffset = probeIndex switch {
        1 => new Vector3(-CarHalfExtents.x, 0f, -CarHalfExtents.z),
        2 => new Vector3(-CarHalfExtents.x, 0f, CarHalfExtents.z),
        3 => new Vector3(CarHalfExtents.x, 0f, -CarHalfExtents.z),
        4 => new Vector3(CarHalfExtents.x, 0f, CarHalfExtents.z),
        _ => Vector3.zero,
      };
      return position + rotation * localOffset;
    }

    private void StopDriving() {
      _isDriving = false;
      _speed = 0f;
      _steering = 0f;
      Debug.Log("[LouisPaulet.RcCar] Driving paused. Press V to drive again.");
    }
  }
}
