using Timberborn.CameraSystem;
using Timberborn.InputSystem;
using Timberborn.MapStateSystem;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
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
    private const int FlatGroundSearchRadius = 32;
    private const int FlatGroundSearchStartRadius = 8;

    private readonly InputService _inputService;
    private readonly CameraService _cameraService;
    private readonly MapSize _mapSize;
    private readonly ITerrainService _terrainService;
    private GameObject? _car;
    private Material? _carMaterial;
    private float _speed;
    private float _steering;
    private bool _isDriving = true;
    private bool _escapeRequested;

    public RcCarController(InputService inputService, CameraService cameraService, MapSize mapSize, ITerrainService terrainService) {
      _inputService = inputService;
      _cameraService = cameraService;
      _mapSize = mapSize;
      _terrainService = terrainService;
    }

    public void Load() {
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
    }

    public bool ProcessInput() {
      if (_car == null) {
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
      _car = GameObject.CreatePrimitive(PrimitiveType.Cube);
      _car.name = "RC Car Prototype";
      _car.transform.localScale = Vector3.one * CarSize;
      _car.transform.rotation = Quaternion.Euler(0f, _cameraService.FacingCamera.eulerAngles.y, 0f);

      Collider? collider = _car.GetComponent<Collider>();
      if (collider != null) {
        Object.Destroy(collider);
      }

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

      Vector3 spawnPosition = FindFlatSpawnPosition();
      _car.transform.position = spawnPosition;
      _cameraService.MoveTargetTo(spawnPosition);
      Debug.Log("[LouisPaulet.RcCar] Spawned prototype cube at " + spawnPosition + ".");
    }

    private Vector3 FindFlatSpawnPosition() {
      Vector2Int mapSize = _mapSize.TerrainSize2D;
      Vector3 center = new Vector3(mapSize.x * 0.5f, 0f, mapSize.y * 0.5f);

      for (int radius = FlatGroundSearchStartRadius; radius <= FlatGroundSearchRadius; radius++) {
        for (int xOffset = -radius; xOffset <= radius; xOffset++) {
          for (int zOffset = -radius; zOffset <= radius; zOffset++) {
            if (radius > 0 && Mathf.Abs(xOffset) != radius && Mathf.Abs(zOffset) != radius) {
              continue;
            }

            int x = Mathf.RoundToInt(center.x) + xOffset;
            int z = Mathf.RoundToInt(center.z) + zOffset;
            if (x < 1 || x >= mapSize.x - 1 || z < 1 || z >= mapSize.y - 1) {
              continue;
            }

            if (!TryGetTopTerrainHeight(x, z, out int height)
              || !IsFlatAt(x - 1, z, height)
              || !IsFlatAt(x + 1, z, height)
              || !IsFlatAt(x, z - 1, height)
              || !IsFlatAt(x, z + 1, height)) {
              continue;
            }

            return new Vector3(x + 0.5f, height + CarSize * 0.5f + 0.03f, z + 0.5f);
          }
        }
      }

      return _cameraService.Target + Vector3.up * (CarSize * 0.5f + 0.03f);
    }

    private bool TryGetTopTerrainHeight(int x, int z, out int height) {
      height = int.MinValue;
      foreach (Vector3Int surface in _terrainService.GetAllHeightsInCell(new Vector2Int(x, z))) {
        height = Mathf.Max(height, surface.z);
      }
      return height != int.MinValue;
    }

    private bool IsFlatAt(int x, int z, int height) {
      return TryGetTopTerrainHeight(x, z, out int neighborHeight) && neighborHeight == height;
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
      _car.transform.Rotate(Vector3.up, _steering * TurnRate * speedRatio * reverseDirection * deltaTime);
      _car.transform.position += _car.transform.forward * (_speed * deltaTime);
    }

    private void StopDriving() {
      _isDriving = false;
      _speed = 0f;
      _steering = 0f;
      Debug.Log("[LouisPaulet.RcCar] Driving paused. Press V to drive again.");
    }
  }
}
