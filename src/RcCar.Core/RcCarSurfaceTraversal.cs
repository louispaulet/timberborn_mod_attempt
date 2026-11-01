namespace LouisPaulet.RcCar.Core;

public static class RcCarSurfaceTraversal {
  public static bool CanContinueAtSameHeight(
    RcCarSurface fromSurface,
    RcCarSurface toSurface,
    bool targetHasUniformSupport,
    bool alignedStairTraversal) {
    return fromSurface.Height == toSurface.Height
      && (targetHasUniformSupport || alignedStairTraversal);
  }

  public static bool CanTraverse(
    RcCarSurface fromSurface,
    RcCarSurface toSurface,
    RcCarCell fromCell,
    RcCarCell toCell,
    RcCarStairConnector? stair,
    float dx,
    float dy) {
    if (fromSurface.Height == toSurface.Height) return true;
    return stair != null && stair.Connects(
      fromCell, toCell, fromSurface.Height, toSurface.Height, dx, dy);
  }
}
