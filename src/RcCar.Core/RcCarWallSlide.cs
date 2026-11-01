using System;
using System.Numerics;

namespace LouisPaulet.RcCar.Core;

public static class RcCarWallSlide {
  public static Vector2 Resolve(Vector2 displacement, Vector2 normal, Vector2 preferredSide,
    float minimumSlipRatio) {
    if (normal.LengthSquared() < 0.000001f) return displacement;
    normal = Vector2.Normalize(normal);
    float intoWall = Math.Max(0f, -Vector2.Dot(displacement, normal));
    Vector2 tangent = new Vector2(-normal.Y, normal.X);
    float alongWall = Vector2.Dot(displacement, tangent);
    float minimumSlip = intoWall * Math.Clamp(minimumSlipRatio, 0f, 1f);
    if (Math.Abs(alongWall) < minimumSlip) {
      // Preserve natural glancing motion. For a head-on contact, steering picks the side.
      float direction = Math.Abs(alongWall) > 0.00001f
        ? Math.Sign(alongWall)
        : Vector2.Dot(preferredSide, tangent) < 0f ? -1f : 1f;
      alongWall = direction * minimumSlip;
    }
    return tangent * alongWall;
  }
}
