using System;
using System.IO;
using System.Reflection;

namespace LouisPaulet.RcCar {
  internal static class RcCarModel {
    private const string MeshResourceName = "LouisPaulet.RcCar.Assets.IronTeethRcCar.obj";

    public static RcCarObjData[] Load() {
      Assembly assembly = typeof(RcCarModel).Assembly;
      using Stream stream = assembly.GetManifestResourceStream(MeshResourceName)
        ?? throw new InvalidOperationException("Embedded RC car mesh was not found: " + MeshResourceName);
      return RcCarObjParser.Parse(stream);
    }
  }
}
