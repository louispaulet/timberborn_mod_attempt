namespace LouisPaulet.RcCar.Core;

public abstract class RcCarSurface {
  public int Height { get; }
  public virtual float ForwardSpeed => RcCarSurfaceSpeed.DirtForwardSpeed;

  protected RcCarSurface(int height) { Height = height; }
}

public sealed class RcCarTerrainSurface : RcCarSurface {
  public RcCarTerrainSurface(int height) : base(height) { }
}

public sealed class RcCarPlatformSurface : RcCarSurface {
  public RcCarPlatformSurface(int height) : base(height) { }
}

public sealed class RcCarPathSurface : RcCarSurface {
  public RcCarSurface Underlying { get; }
  public override float ForwardSpeed => RcCarSurfaceSpeed.GetForwardSpeed(true);

  public RcCarPathSurface(RcCarSurface underlying) : base(underlying.Height) {
    Underlying = underlying;
  }
}
