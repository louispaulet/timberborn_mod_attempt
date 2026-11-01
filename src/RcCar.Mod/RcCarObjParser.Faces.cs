using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace LouisPaulet.RcCar {
  internal static partial class RcCarObjParser {
    private static void AddFace(string line, RcCarMaterialGroup group,
      List<Vector3> sourceVertices, List<Vector3> sourceNormals,
      RcCarObjData result, Dictionary<long, int> cache) {
      string[] fields = SplitFields(line);
      if (fields.Length < 4) throw new InvalidDataException("OBJ face has fewer than three vertices.");
      int first = GetMeshVertex(ParseFaceVertex(fields[1]), sourceVertices, sourceNormals, result, cache);
      for (int i = 2; i < fields.Length - 1; i++) {
        int second = GetMeshVertex(ParseFaceVertex(fields[i]), sourceVertices, sourceNormals, result, cache);
        int third = GetMeshVertex(ParseFaceVertex(fields[i + 1]), sourceVertices, sourceNormals, result, cache);
        group.TriangleIndices.Add(first); group.TriangleIndices.Add(third); group.TriangleIndices.Add(second);
      }
    }

    private static int GetMeshVertex(RcCarFaceVertex corner, List<Vector3> sourceVertices,
      List<Vector3> sourceNormals, RcCarObjData result, Dictionary<long, int> cache) {
      long key = ((long)corner.PositionIndex << 32) | (uint)corner.NormalIndex;
      if (cache.TryGetValue(key, out int existing)) return existing;
      if (corner.PositionIndex >= sourceVertices.Count || corner.NormalIndex >= sourceNormals.Count)
        throw new InvalidDataException("RC car mesh references a missing vertex or normal.");
      int index = result.Vertices.Count;
      result.Vertices.Add(sourceVertices[corner.PositionIndex]);
      result.Normals.Add(sourceNormals[corner.NormalIndex]);
      cache.Add(key, index);
      return index;
    }

    private static RcCarFaceVertex ParseFaceVertex(string token) {
      string[] parts = token.Split('/');
      int position = ParseIndex(parts[0]);
      int normal = parts.Length >= 3 && parts[2].Length > 0 ? ParseIndex(parts[2]) : position;
      return new RcCarFaceVertex(position, normal);
    }

    private static int ParseIndex(string value) {
      int index = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
      if (index <= 0) throw new InvalidDataException("Relative OBJ indices are unsupported.");
      return index - 1;
    }

    private static float ParseFloat(string value) => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static string[] SplitFields(string line) => line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
  }
}
