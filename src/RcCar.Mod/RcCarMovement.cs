using UnityEngine;
using UnityEngine.InputSystem;
using Timberborn.MapStateSystem;
using LouisPaulet.RcCar.Core;

namespace LouisPaulet.RcCar {
  internal sealed partial class RcCarMovement {
    private readonly MapSize _mapSize;
    private readonly RcCarWorldQuery _worldQuery;
    private GameObject? _car;
    private Transform[]? _frontWheels;
    private float _speed;
    private float _steering;
    public RcCarMovement(MapSize mapSize, RcCarWorldQuery worldQuery) {
      _mapSize = mapSize; _worldQuery = worldQuery;
    }
    public void SetCar(GameObject car, Transform[] frontWheels) {
      _car = car;
      _frontWheels = frontWheels;
    }
    public void Stop() {
      _speed = 0f;
      _steering = 0f;
      UpdateFrontWheels();
    }
    public void Reset() {
      _car = null;
      _frontWheels = null;
      Stop();
    }
    private void UpdateFrontWheels() {
      if (_frontWheels == null) return;
      Quaternion rotation = Quaternion.AngleAxis(
        _steering * RcCarMovementSettings.FrontWheelSteeringAngle, Vector3.up);
      foreach (Transform wheel in _frontWheels) wheel.localRotation = rotation;
    }
    public void Drive(Keyboard keyboard) {
      if (_car == null) {
        return;
      }

      float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
      Vector3 currentPosition = _car.transform.position;
      float supportHeight = currentPosition.y - RcCarMovementSettings.CarHalfExtents.y - RcCarMovementSettings.GroundClearance;
      Vector2Int supportCell = new Vector2Int(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.z));
      RcCarSurface surface = _worldQuery.GetSurfaceAt(supportCell, Mathf.RoundToInt(supportHeight));
      float forwardSpeed = surface.ForwardSpeed;
      float throttle = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
      float targetSpeed = throttle > 0f ? forwardSpeed : throttle < 0f ? -RcCarMovementSettings.ReverseSpeed : 0f;
      float acceleration = throttle == 0f
        ? RcCarMovementSettings.Coasting
        : _speed != 0f && Mathf.Sign(_speed) != Mathf.Sign(targetSpeed)
          ? RcCarMovementSettings.Braking
          : RcCarMovementSettings.Acceleration;
      if (_speed != 0f
        && Mathf.Sign(_speed) == Mathf.Sign(targetSpeed)
        && Mathf.Abs(_speed) > Mathf.Abs(targetSpeed)) {
        acceleration = RcCarMovementSettings.Braking;
      }
      _speed = Mathf.MoveTowards(_speed, targetSpeed, acceleration * deltaTime);

      float steeringInput = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
      _steering = Mathf.MoveTowards(_steering, steeringInput, RcCarMovementSettings.SteeringResponse * deltaTime);
      UpdateFrontWheels();
      float speedRatio = Mathf.Clamp01(Mathf.Abs(_speed) / forwardSpeed);
      float reverseDirection = _speed < 0f ? -1f : 1f;
      float turnAngle = _steering * RcCarMovementSettings.TurnRate * speedRatio * reverseDirection * deltaTime;
      Quaternion rotation = _car.transform.rotation * Quaternion.AngleAxis(turnAngle, Vector3.up);
      Vector2Int mapSize = _mapSize.TerrainSize2D;

      if (!TryGetSupportedPosition(currentPosition, rotation, supportHeight, mapSize, false, out Vector3 groundedStart)) {
        rotation = _car.transform.rotation;
        groundedStart = currentPosition;
      } else {
        currentPosition = groundedStart;
        supportHeight = currentPosition.y - RcCarMovementSettings.CarHalfExtents.y - RcCarMovementSettings.GroundClearance;
      }

      Vector3 desiredDisplacement = rotation * Vector3.forward * (_speed * deltaTime);
      currentPosition = Move(currentPosition, rotation, desiredDisplacement, supportHeight, mapSize);
      _car.transform.SetPositionAndRotation(currentPosition, rotation);
    }
  }
}
