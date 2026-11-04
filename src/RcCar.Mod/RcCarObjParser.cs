using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LouisPaulet.RcCar {
  internal static partial class RcCarObjParser {
    public static RcCarObjData[] Parse(Stream stream) {
      using var reader = new StreamReader(stream);
      var sourceVertices = new List<Vector3>(60000);
      var sourceNormals = new List<Vector3>(60000);
      var parts = new[] { new RcCarObjData(), new RcCarObjData(), new RcCarObjData() };
      var caches = new[] { new Dictionary<long, int>(), new Dictionary<long, int>(), new Dictionary<long, int>() };
      string material = "IronTeeth_Charcoal";
      bool frontWheel = false;
      string? line;
      while ((line = reader.ReadLine()) != null) {
        if (line.Length == 0 || line[0] == '#') continue;
        if (line.StartsWith("v ", StringComparison.Ordinal)) AddVector(line, sourceVertices, false);
        else if (line.StartsWith("vn ", StringComparison.Ordinal)) AddVector(line, sourceNormals, true);
        else if (line.StartsWith("g ", StringComparison.Ordinal))
          frontWheel = IsFrontWheelGroup(line.Substring(2));
        else if (line.StartsWith("usemtl ", StringComparison.Ordinal))
          material = line.Substring(7).Trim();
        else if (line.StartsWith("f ", StringComparison.Ordinal)) {
          // The front wheel groups are entirely on one side of the model.
          int part = frontWheel
            ? (sourceVertices[ParseFaceVertex(SplitFields(line)[1]).PositionIndex].x < 0f ? 1 : 2)
            : 0;
          RcCarObjData result = parts[part];
          if (!result.GroupsByName.TryGetValue(material, out RcCarMaterialGroup? group)) {
            group = new RcCarMaterialGroup(material);
            result.GroupsByName.Add(material, group);
            result.Groups.Add(group);
          }
          AddFace(line, group, sourceVertices, sourceNormals, result, caches[part]);
        }
      }
      foreach (RcCarObjData part in parts)
        if (part.Vertices.Count == 0) throw new InvalidDataException("RC car body or front wheel mesh is empty.");
      return parts;
    }

    private static bool IsFrontWheelGroup(string name) =>
      name.StartsWith("Front_tyre", StringComparison.Ordinal) ||
      name.StartsWith("Front_tread_lug", StringComparison.Ordinal) ||
      name.StartsWith("Front_wheel_rim", StringComparison.Ordinal) ||
      name.StartsWith("Front_hub_plate", StringComparison.Ordinal) ||
      name.StartsWith("Front_brass_hub", StringComparison.Ordinal) ||
      name.StartsWith("Front_axle_cap", StringComparison.Ordinal);

    private static void AddVector(string line, List<Vector3> target, bool normalize) {
      string[] fields = SplitFields(line);
      Vector3 vector = new(ParseFloat(fields[1]), ParseFloat(fields[3]), ParseFloat(fields[2]));
      target.Add(normalize ? vector.normalized : vector);
    }

  }
}
