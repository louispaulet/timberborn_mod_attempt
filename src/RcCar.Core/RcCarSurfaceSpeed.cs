namespace LouisPaulet.RcCar.Core;

public static class RcCarSurfaceSpeed {
  public const float DirtForwardSpeed = 7f;
  public const float PathSpeedMultiplier = 1.5f;

  public static float GetForwardSpeed(bool isPath) {
    return DirtForwardSpeed * (isPath ? PathSpeedMultiplier : 1f);
  }
}
