using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LouisPaulet.RcCar {
  internal static class RcCarMeshBuilder {
    public static Mesh Create(RcCarObjData data, Vector3 pivot) {
      var mesh = new Mesh {
        name = "Iron Teeth RC Car",
        indexFormat = data.Vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
        subMeshCount = data.Groups.Count,
      };
      for (int i = 0; i < data.Vertices.Count; i++) data.Vertices[i] -= pivot;
      mesh.SetVertices(data.Vertices);
      if (data.Normals.Count == data.Vertices.Count) mesh.SetNormals(data.Normals);
      else mesh.RecalculateNormals();
      for (int i = 0; i < data.Groups.Count; i++) mesh.SetTriangles(data.Groups[i].TriangleIndices, i, false);
      mesh.RecalculateBounds();
      return mesh;
    }
  }
}
