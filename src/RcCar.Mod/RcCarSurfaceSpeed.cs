namespace LouisPaulet.RcCar {
  /// <summary>
  /// Defines the RC car's forward speed rule by the verified Timberborn surface type.
  /// Paths allow 50% more forward speed than dirt; reverse speed is unchanged.
  /// </summary>
  internal static class RcCarSurfaceSpeed {
    public const float DirtForwardSpeed = 7f;
    public const float PathSpeedMultiplier = 1.5f;

    public static float GetForwardSpeed(bool isPath) {
      return DirtForwardSpeed * (isPath ? PathSpeedMultiplier : 1f);
    }
  }
}
