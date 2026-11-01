using System.Numerics;
using LouisPaulet.RcCar.Core;
using Xunit;

namespace RcCar.Core.Tests;

public sealed class WallSlideTests {
  [Theory]
  [InlineData(1f)]
  [InlineData(-1f)]
  public void HeadOnPressureProducesGentleSlipTowardPreferredSide(float side) {
    Vector2 slide = RcCarWallSlide.Resolve(new(0, 1), new(0, -1), new(side, 0), .18f);
    Assert.Equal(side * .18f, slide.X, 5);
    Assert.Equal(0, slide.Y);
  }

  [Fact]
  public void GlancingMotionKeepsItsNaturalDirectionAndSpeed() {
    Assert.Equal(new Vector2(-.6f, 0),
      RcCarWallSlide.Resolve(new(-.6f, 1), new(0, -1), new(1, 0), .18f));
  }

  [Fact]
  public void SlightGlancingMotionIsBoostedWithoutReversingDirection() {
    Assert.Equal(new Vector2(-.18f, 0),
      RcCarWallSlide.Resolve(new(-.01f, 1), new(0, -1), new(1, 0), .18f));
  }

  [Fact]
  public void NoPressureProducesNoArtificialSlip() {
    Assert.Equal(Vector2.Zero, RcCarWallSlide.Resolve(Vector2.Zero, new(0, -1), new(1, 0), .18f));
    Assert.Equal(Vector2.Zero, RcCarWallSlide.Resolve(new(0, -1), new(0, -1), new(1, 0), .18f));
  }

  [Fact]
  public void SlipScalesWithPressureAndRemainsTangentToObliqueWall() {
    Vector2 normal = Vector2.Normalize(new Vector2(-1, -1));
    Vector2 full = RcCarWallSlide.Resolve(-normal, normal, new(1, 0), .18f);
    Vector2 half = RcCarWallSlide.Resolve(-normal * .5f, normal, new(1, 0), .18f);
    Assert.True(Vector2.Distance(full * .5f, half) < .00001f);
    Assert.True(System.Math.Abs(Vector2.Dot(full, normal)) < .00001f);
    Assert.Equal(.18f, full.Length(), 5);
  }
}
