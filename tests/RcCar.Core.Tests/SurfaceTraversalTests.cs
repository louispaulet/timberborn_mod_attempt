using LouisPaulet.RcCar.Core;
using Xunit;

namespace RcCar.Core.Tests;

public sealed class SurfaceTraversalTests {
  private static readonly RcCarCell LowCell = new(3, 4);
  private static readonly RcCarCell HighCell = new(3, 5);

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  public void EqualHeightSurfacesAllowMovement(int height) {
    Assert.True(Can(new RcCarTerrainSurface(height), new RcCarPlatformSurface(height), null, 0, 1));
    Assert.True(Can(new RcCarPathSurface(new RcCarTerrainSurface(height)),
      new RcCarPathSurface(new RcCarPlatformSurface(height)), null, 0, 1));
  }

  [Theory]
  [InlineData(1, 0)]
  [InlineData(0, 1)]
  public void DifferentHeightSurfacesBlockWithoutStairs(int from, int to) {
    Assert.False(Can(new RcCarTerrainSurface(from), new RcCarPlatformSurface(to), null, 0, 1));
    Assert.False(Can(new RcCarPathSurface(new RcCarTerrainSurface(from)),
      new RcCarPathSurface(new RcCarPlatformSurface(to)), null, 0, 1));
  }

  [Fact]
  public void HigherAndLowerPlatformEdgesBlockWithoutStairs() {
    Assert.False(Can(new RcCarPlatformSurface(0), new RcCarPlatformSurface(1), null, 0, 1));
    Assert.False(Can(new RcCarPlatformSurface(1), new RcCarPlatformSurface(0), null, 0, -1));
  }

  [Fact]
  public void CompletedAlignedStairsAllowAscentAndDescent() {
    var stair = new RcCarStairConnector(LowCell, 0, RcCarStairAxis.NorthSouth, true);
    Assert.True(Can(new RcCarTerrainSurface(0), new RcCarPlatformSurface(1), stair, 0, 1));
    Assert.True(Can(new RcCarPlatformSurface(1), new RcCarTerrainSurface(0), stair, 0, -1));
  }

  [Fact]
  public void StairBesideTransitionDoesNotConnectPlatformEdge() {
    var adjacentStair = new RcCarStairConnector(new RcCarCell(4, 4), 0, RcCarStairAxis.NorthSouth, true);
    Assert.False(Can(new RcCarTerrainSurface(0), new RcCarPlatformSurface(1), adjacentStair, 0, 1));
  }

  [Fact]
  public void StairCanCarryTheCarAcrossItsTileWhileSupportHeightIsChanging() {
    var stair = new RcCarStairConnector(LowCell, 0, RcCarStairAxis.NorthSouth, true);
    Assert.True(stair.AlignsWith(LowCell, HighCell, 0, 1));
    Assert.False(stair.AlignsWith(LowCell, HighCell, 1, 0));
  }

  [Fact]
  public void UnfinishedMisplacedOrMisalignedStairsDoNotConnect() {
    var unfinished = new RcCarStairConnector(LowCell, 0, RcCarStairAxis.NorthSouth, false);
    var misplaced = new RcCarStairConnector(new RcCarCell(9, 9), 0, RcCarStairAxis.NorthSouth, true);
    var sideways = new RcCarStairConnector(LowCell, 0, RcCarStairAxis.EastWest, true);
    var wrongHeight = new RcCarStairConnector(LowCell, 1, RcCarStairAxis.NorthSouth, true);
    Assert.False(Can(new RcCarTerrainSurface(0), new RcCarPlatformSurface(1), unfinished, 0, 1));
    Assert.False(Can(new RcCarTerrainSurface(0), new RcCarPlatformSurface(1), misplaced, 0, 1));
    Assert.False(Can(new RcCarTerrainSurface(0), new RcCarPlatformSurface(1), sideways, 0, 1));
    Assert.False(Can(new RcCarTerrainSurface(0), new RcCarPlatformSurface(1), wrongHeight, 0, 1));
  }

  [Fact]
  public void TwoLevelChangeRequiresTwoStairTransitions() {
    var first = new RcCarStairConnector(LowCell, 0, RcCarStairAxis.NorthSouth, true);
    Assert.False(Can(new RcCarTerrainSurface(0), new RcCarPlatformSurface(2), first, 0, 1));
    Assert.True(Can(new RcCarTerrainSurface(0), new RcCarPlatformSurface(1), first, 0, 1));
    var second = new RcCarStairConnector(HighCell, 1, RcCarStairAxis.NorthSouth, true);
    Assert.True(Can(new RcCarPlatformSurface(1), new RcCarPlatformSurface(2), second, 0, 1));
  }

  private static bool Can(RcCarSurface from, RcCarSurface to, RcCarStairConnector? stair, float dx, float dy) {
    return RcCarSurfaceTraversal.CanTraverse(from, to, LowCell, HighCell, stair, dx, dy);
  }
}
