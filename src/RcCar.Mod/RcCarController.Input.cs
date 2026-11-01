using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LouisPaulet.RcCar {
  public partial class RcCarController {
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

        _movement.Drive(keyboard);
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
  }
}
