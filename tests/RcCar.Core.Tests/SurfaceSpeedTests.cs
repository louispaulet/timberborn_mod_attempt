using LouisPaulet.RcCar.Core;
using Xunit;

namespace RcCar.Core.Tests;

public sealed class SurfaceSpeedTests {
  [Fact]
  public void DirtAndPathKeepCurrentForwardSpeeds() {
    var dirt = new RcCarTerrainSurface(2);
    var path = new RcCarPathSurface(dirt);
    Assert.Equal(7f, dirt.ForwardSpeed);
    Assert.Equal(10.5f, path.ForwardSpeed);
    Assert.Equal(10.5f, RcCarSurfaceSpeed.GetForwardSpeed(true));
  }
}
