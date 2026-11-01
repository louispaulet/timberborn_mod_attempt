using System;

namespace LouisPaulet.RcCar.Core;

public enum RcCarStairAxis { EastWest, NorthSouth }

public sealed class RcCarStairConnector {
  public RcCarCell Cell { get; }
  public int LowHeight { get; }
  public int HighHeight { get; }
  public RcCarStairAxis Axis { get; }
  public bool IsFinished { get; }

  public RcCarStairConnector(
    RcCarCell cell, int lowHeight, RcCarStairAxis axis, bool isFinished) {
    Cell = cell;
    LowHeight = lowHeight;
    HighHeight = lowHeight + 1;
    Axis = axis;
    IsFinished = isFinished;
  }

  public bool Connects(RcCarCell from, RcCarCell to, int fromHeight, int toHeight, float dx, float dy) {
    return Math.Abs(fromHeight - toHeight) == 1
      && Math.Min(fromHeight, toHeight) == LowHeight
      && AlignsWith(from, to, dx, dy);
  }

  public bool AlignsWith(RcCarCell from, RcCarCell to, float dx, float dy) {
    if (!IsFinished || from != Cell && to != Cell) return false;
    return AlignsWithDirection(dx, dy);
  }

  public bool AlignsWithDirection(float dx, float dy) {
    if (!IsFinished) return false;
    if (dx * dx + dy * dy < 0.000001f) return false;
    return Axis == (Math.Abs(dx) > Math.Abs(dy) ? RcCarStairAxis.EastWest : RcCarStairAxis.NorthSouth);
  }
}
