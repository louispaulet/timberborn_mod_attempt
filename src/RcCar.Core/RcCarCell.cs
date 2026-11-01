namespace LouisPaulet.RcCar.Core;

public readonly struct RcCarCell {
  public readonly int X;
  public readonly int Y;

  public RcCarCell(int x, int y) { X = x; Y = y; }

  public override bool Equals(object? value) => value is RcCarCell cell && X == cell.X && Y == cell.Y;
  public override int GetHashCode() => (X * 397) ^ Y;
  public static bool operator ==(RcCarCell left, RcCarCell right) => left.Equals(right);
  public static bool operator !=(RcCarCell left, RcCarCell right) => !left.Equals(right);
}
