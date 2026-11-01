using System.Collections.Generic;
using UnityEngine;

namespace LouisPaulet.RcCar {
  internal sealed class RcCarMaterialGroup {
    public readonly string Name;
    public readonly List<int> TriangleIndices = new();
    public RcCarMaterialGroup(string name) { Name = name; }
  }

  internal sealed class RcCarObjData {
    public readonly List<Vector3> Vertices = new();
    public readonly List<Vector3> Normals = new();
    public readonly List<RcCarMaterialGroup> Groups = new();
  }

  internal readonly struct RcCarFaceVertex {
    public readonly int PositionIndex;
    public readonly int NormalIndex;
    public RcCarFaceVertex(int positionIndex, int normalIndex) {
      PositionIndex = positionIndex; NormalIndex = normalIndex;
    }
  }
}
