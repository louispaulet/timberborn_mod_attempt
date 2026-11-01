using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LouisPaulet.RcCar {
  internal static partial class RcCarObjParser {
    public static RcCarObjData Parse(Stream stream) {
      using var reader = new StreamReader(stream);
      var sourceVertices = new List<Vector3>(60000);
      var sourceNormals = new List<Vector3>(60000);
      var result = new RcCarObjData();
      var groupsByName = new Dictionary<string, RcCarMaterialGroup>(StringComparer.Ordinal);
      var meshVertexByCorner = new Dictionary<long, int>();
      RcCarMaterialGroup? currentGroup = null;
      string? line;
      while ((line = reader.ReadLine()) != null) {
        if (line.Length == 0 || line[0] == '#') continue;
        if (line.StartsWith("v ", StringComparison.Ordinal)) AddVector(line, sourceVertices, false);
        else if (line.StartsWith("vn ", StringComparison.Ordinal)) AddVector(line, sourceNormals, true);
        else if (line.StartsWith("usemtl ", StringComparison.Ordinal))
          currentGroup = GetOrCreateGroup(line.Substring(7).Trim(), groupsByName, result.Groups);
        else if (line.StartsWith("f ", StringComparison.Ordinal)) {
          currentGroup ??= GetOrCreateGroup("IronTeeth_Charcoal", groupsByName, result.Groups);
          AddFace(line, currentGroup, sourceVertices, sourceNormals, result, meshVertexByCorner);
        }
      }
      if (result.Vertices.Count == 0 || result.Groups.Count == 0)
        throw new InvalidDataException("RC car mesh is empty.");
      return result;
    }

    private static void AddVector(string line, List<Vector3> target, bool normalize) {
      string[] fields = SplitFields(line);
      Vector3 vector = new(ParseFloat(fields[1]), ParseFloat(fields[3]), ParseFloat(fields[2]));
      target.Add(normalize ? vector.normalized : vector);
    }

    private static RcCarMaterialGroup GetOrCreateGroup(
      string name, Dictionary<string, RcCarMaterialGroup> groupsByName, List<RcCarMaterialGroup> groups) {
      if (groupsByName.TryGetValue(name, out RcCarMaterialGroup? group)) return group;
      group = new RcCarMaterialGroup(name);
      groupsByName.Add(name, group); groups.Add(group);
      return group;
    }
  }
}
