using System;
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
using Object = UnityEngine.Object;

namespace LouisPaulet.RcCar {
  public class RcCarController : IInputProcessor, ILoadableSingleton, IUnloadableSingleton {
    private const float ReverseSpeed = 3.5f;
    private const float Acceleration = 8f;
    private const float Braking = 12f;
    private const float Coasting = 3f;
    private const float SteeringResponse = 5f;
    private const float TurnRate = 110f;
    private const float CarBlockHeight = 0.70f;
    private const int FlatGroundSearchStartRadius = 8;
    private const float GroundClearance = 0.03f;
    private const float CollisionSkinWidth = 0.03f;
    // One block of rise per tile is the maximum supported grade for ramps and stairs.
    private const float MaxClimbableStepHeight = 1f;
    private const float MovementSubstepLength = 0.1f;
    private const float BridgeSupportRayStartOffset = 1.25f;
    private const float BridgeSupportRayDistance = 1.5f;
    private const float BridgeSupportHeightTolerance = 0.3f;
    private const float WalkableNormalDot = 0.65f;
    private const int MaxCollisionSlideIterations = 3;
    private static readonly Vector3 CarHalfExtents = new Vector3(0.45f, 0.33f, 0.73f);
    private static readonly Vector3 CarVisualOffset = new Vector3(0f, -0.0275f, 0f);

    private readonly InputService _inputService;
    private readonly CameraService _cameraService;
    private readonly MapSize _mapSize;
    private readonly RcCarWorldQuery _worldQuery;
    private GameObject? _car;
    private Mesh? _carMesh;
    private Material[]? _carMaterials;
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

      _car = new GameObject("RC Car");
      BoxCollider carCollider = _car.AddComponent<BoxCollider>();
      carCollider.size = CarHalfExtents * 2f;

      GameObject visual = new GameObject("Iron Teeth RC Car model");
      visual.transform.SetParent(_car.transform, false);
      visual.transform.localPosition = CarVisualOffset;
      visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
      MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
      MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();
      try {
        _carMesh = RcCarModel.CreateMesh(out Material[] carMaterials);
        _carMaterials = carMaterials;
        meshFilter.sharedMesh = _carMesh;
        meshRenderer.sharedMaterials = _carMaterials;
      } catch (Exception exception) {
        Debug.LogError("[LouisPaulet.RcCar] Could not load the Iron Teeth RC car model: " + exception);
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
        Object.Destroy(_car);
        _car = null;
        return;
      }

      _car.transform.SetPositionAndRotation(spawnPosition, rotation);
      _cameraService.MoveTargetTo(spawnPosition);
      Debug.Log("[LouisPaulet.RcCar] Spawned the Iron Teeth RC car at " + spawnPosition + ".");
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
      foreach (Collider collider in overlaps) {
        if (collider != null && !_worldQuery.IsDrivableSurfaceCollider(collider)) {
          return false;
        }
      }

      return true;
    }

    private static bool IsCellInsideMap(Vector2Int cell, Vector2Int mapSize) {
      return cell.x >= 0 && cell.x < mapSize.x && cell.y >= 0 && cell.y < mapSize.y;
    }

    private void Drive(Keyboard keyboard) {
      if (_car == null) {
        return;
      }

      float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
      Vector3 currentPosition = _car.transform.position;
      float supportHeight = currentPosition.y - CarHalfExtents.y - GroundClearance;
      Vector2Int supportCell = new Vector2Int(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.z));
      float forwardSpeed = RcCarSurfaceSpeed.GetForwardSpeed(
        _worldQuery.IsPathAt(supportCell, Mathf.RoundToInt(supportHeight)));
      float throttle = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
      float targetSpeed = throttle > 0f ? forwardSpeed : throttle < 0f ? -ReverseSpeed : 0f;
      float acceleration = throttle == 0f
        ? Coasting
        : _speed != 0f && Mathf.Sign(_speed) != Mathf.Sign(targetSpeed)
          ? Braking
          : Acceleration;
      if (_speed != 0f
        && Mathf.Sign(_speed) == Mathf.Sign(targetSpeed)
        && Mathf.Abs(_speed) > Mathf.Abs(targetSpeed)) {
        acceleration = Braking;
      }
      _speed = Mathf.MoveTowards(_speed, targetSpeed, acceleration * deltaTime);

      float steeringInput = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
      _steering = Mathf.MoveTowards(_steering, steeringInput, SteeringResponse * deltaTime);
      float speedRatio = Mathf.Clamp01(Mathf.Abs(_speed) / forwardSpeed);
      float reverseDirection = _speed < 0f ? -1f : 1f;
      float turnAngle = _steering * TurnRate * speedRatio * reverseDirection * deltaTime;
      Quaternion rotation = _car.transform.rotation * Quaternion.AngleAxis(turnAngle, Vector3.up);
      Vector2Int mapSize = _mapSize.TerrainSize2D;

      if (!TryGetSupportedPosition(currentPosition, rotation, supportHeight, mapSize, out Vector3 groundedStart)
        || groundedStart.y > currentPosition.y + GroundClearance
          && !TryStepUp(currentPosition, currentPosition, rotation, supportHeight, mapSize, out groundedStart)) {
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
        Vector3 desiredPosition = currentPosition + stepDisplacement;
        Vector3 collisionResolvedPosition = RcCarPhysics.MoveWithCollision(
          currentPosition,
          rotation,
          CarHalfExtents,
          stepDisplacement,
          _car.transform,
          _worldQuery.IsDrivableSurfaceCollider,
          Physics.DefaultRaycastLayers,
          CollisionSkinWidth,
          MaxCollisionSlideIterations,
          out bool collided);

        if (!TryGetSupportedPosition(collisionResolvedPosition, rotation, supportHeight, mapSize, out Vector3 groundedPosition)) {
          if (!TryStepUp(
            currentPosition,
            desiredPosition,
            rotation,
            supportHeight,
            mapSize,
            out groundedPosition)) {
            break;
          }
          currentPosition = groundedPosition;
          supportHeight = currentPosition.y - CarHalfExtents.y - GroundClearance;
          continue;
        }

        if (groundedPosition.y > currentPosition.y + GroundClearance) {
          if (!TryStepUp(
            currentPosition,
            desiredPosition,
            rotation,
            supportHeight,
            mapSize,
            out groundedPosition)) {
            break;
          }
          currentPosition = groundedPosition;
          supportHeight = currentPosition.y - CarHalfExtents.y - GroundClearance;
          continue;
        }

        if ((groundedPosition - currentPosition).sqrMagnitude < 0.000001f) {
          if (collided && TryStepUp(
            currentPosition,
            desiredPosition,
            rotation,
            supportHeight,
            mapSize,
            out groundedPosition)) {
            currentPosition = groundedPosition;
            supportHeight = currentPosition.y - CarHalfExtents.y - GroundClearance;
            continue;
          }
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

    private bool TryStepUp(
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
        out Vector3 targetGroundedPosition)) {
        return false;
      }

      float stepHeight = targetGroundedPosition.y - currentPosition.y;
      if (stepHeight <= GroundClearance || stepHeight > MaxClimbableStepHeight) {
        return false;
      }

      if (!HasVerticalStepClearance(currentPosition, rotation, stepHeight)) {
        return false;
      }

      Vector3 horizontalDisplacement = new Vector3(
        desiredPosition.x - currentPosition.x,
        0f,
        desiredPosition.z - currentPosition.z);
      Vector3 elevatedStart = currentPosition + Vector3.up * stepHeight;
      Vector3 elevatedTarget = RcCarPhysics.MoveWithCollision(
        elevatedStart,
        rotation,
        CarHalfExtents,
        horizontalDisplacement,
        _car!.transform,
        _worldQuery.IsDrivableSurfaceCollider,
        Physics.DefaultRaycastLayers,
        CollisionSkinWidth,
        MaxCollisionSlideIterations,
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
        out Vector3 finalPosition)
        || !HasClearanceAt(finalPosition, rotation)) {
        return false;
      }

      steppedPosition = finalPosition;
      return true;
    }

    private bool HasVerticalStepClearance(Vector3 position, Quaternion rotation, float stepHeight) {
      RaycastHit[] hits = Physics.BoxCastAll(
        position,
        CarHalfExtents,
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

        if (Vector3.Dot(hit.normal, Vector3.up) > WalkableNormalDot) {
          continue;
        }

        return false;
      }

      return true;
    }

    private bool HasClearanceAt(Vector3 position, Quaternion rotation) {
      Collider[] overlaps = Physics.OverlapBox(
        position + Vector3.up * CollisionSkinWidth,
        CarHalfExtents,
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

    private bool TryGetSupportedPosition(
      Vector3 position,
      Quaternion rotation,
      float referenceSupportHeight,
      Vector2Int mapSize,
      out Vector3 groundedPosition) {
      float lowestSupportHeight = float.PositiveInfinity;
      float highestSupportHeight = float.NegativeInfinity;
      bool hasSupportHeight = false;

      for (int probeIndex = 0; probeIndex < 5; probeIndex++) {
        Vector3 probe = GetFootprintProbe(position, rotation, probeIndex);
        Vector2Int cell = new Vector2Int(Mathf.FloorToInt(probe.x), Mathf.FloorToInt(probe.z));
        if (!IsCellInsideMap(cell, mapSize)) {
          groundedPosition = default;
          return false;
        }

        float cellSupportHeight = 0f;
        bool hasTerrainSupport = _worldQuery.TryGetTopTerrainHeight(cell, out int terrainHeight);
        if (hasTerrainSupport) {
          cellSupportHeight = terrainHeight;
          if (_worldQuery.TryGetFinishedPlatformTopHeight(cell, terrainHeight, out int platformTopHeight)) {
            cellSupportHeight = Mathf.Max(cellSupportHeight, platformTopHeight);
          }
          hasTerrainSupport = !_worldQuery.IsUnderwaterAtSurface(cell, cellSupportHeight);
        }

        if (!hasTerrainSupport) {
          int bridgeHeight = Mathf.RoundToInt(referenceSupportHeight);
          if (!_worldQuery.IsPathAt(cell, bridgeHeight)
            || _worldQuery.IsUnderwaterAtSurface(cell, bridgeHeight)
            || !HasBridgeDeckSupport(probe, bridgeHeight)) {
            groundedPosition = default;
            return false;
          }

          cellSupportHeight = bridgeHeight;
        }

        if (Mathf.Abs(cellSupportHeight - referenceSupportHeight) > MaxClimbableStepHeight
          || _worldQuery.IsBlockedByNonPathObject(cell, Mathf.FloorToInt(cellSupportHeight), Mathf.CeilToInt(cellSupportHeight + CarBlockHeight))) {
          groundedPosition = default;
          return false;
        }

        lowestSupportHeight = Mathf.Min(lowestSupportHeight, cellSupportHeight);
        highestSupportHeight = Mathf.Max(highestSupportHeight, cellSupportHeight);
        hasSupportHeight = true;
      }

      if (!hasSupportHeight || highestSupportHeight - lowestSupportHeight > MaxClimbableStepHeight) {
        groundedPosition = default;
        return false;
      }

      groundedPosition = new Vector3(position.x, highestSupportHeight + CarHalfExtents.y + GroundClearance, position.z);
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
