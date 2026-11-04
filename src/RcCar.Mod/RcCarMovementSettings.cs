using UnityEngine;

namespace LouisPaulet.RcCar {
  internal static class RcCarMovementSettings {
    public const float ReverseSpeed = 3.5f;
    public const float Acceleration = 8f;
    public const float Braking = 12f;
    public const float Coasting = 3f;
    public const float SteeringResponse = 5f;
    public const float WheelRadius = 0.195f;
    public const float FrontWheelSteeringAngle = 28f;
    public const float TurnRate = 110f;
    public const float CarBlockHeight = 0.70f;
    public const int FlatGroundSearchStartRadius = 8;
    public const float GroundClearance = 0.03f;
    public const float CollisionSkinWidth = 0.03f;
    public const float MinimumWallSlipRatio = 0.18f;
    public const float MaxStairHeightChange = 1f;
    public const float SupportHeightTolerance = 0.03f;
    public const float MovementSubstepLength = 0.1f;
    public const float BridgeSupportRayStartOffset = 1.25f;
    public const float BridgeSupportRayDistance = 1.5f;
    public const float BridgeSupportHeightTolerance = 0.3f;
    public const float WalkableNormalDot = 0.65f;
    public const int MaxCollisionSlideIterations = 3;
    public static readonly Vector3 CarHalfExtents = new Vector3(0.45f, 0.33f, 0.73f);
    public static readonly Vector3 CarVisualOffset = new Vector3(0f, -0.0275f, 0f);
  }
}
