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
      var parts = new RcCarObjData[5];
      var caches = new Dictionary<long, int>[parts.Length];
      for (int i = 0; i < parts.Length; i++) {
        parts[i] = new RcCarObjData();
        caches[i] = new Dictionary<long, int>();
      }
      string material = "IronTeeth_Charcoal";
      bool isWheel = false;
      string? line;
      while ((line = reader.ReadLine()) != null) {
        if (line.Length == 0 || line[0] == '#') continue;
        if (line.StartsWith("v ", StringComparison.Ordinal)) AddVector(line, sourceVertices, false);
        else if (line.StartsWith("vn ", StringComparison.Ordinal)) AddVector(line, sourceNormals, true);
        else if (line.StartsWith("g ", StringComparison.Ordinal))
          isWheel = IsWheelGroup(line.Substring(2));
        else if (line.StartsWith("usemtl ", StringComparison.Ordinal))
          material = line.Substring(7).Trim();
        else if (line.StartsWith("f ", StringComparison.Ordinal)) {
          int part = 0;
          if (isWheel) {
            Vector3 corner = sourceVertices[ParseFaceVertex(SplitFields(line)[1]).PositionIndex];
            // The OBJ places the front axle at negative Z in model coordinates.
            part = (corner.z < 0f ? 1 : 3) + (corner.x < 0f ? 0 : 1);
          }
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
        if (part.Vertices.Count == 0) throw new InvalidDataException("RC car body or wheel mesh is empty.");
      return parts;
    }

    private static bool IsWheelGroup(string name) {
      if (name.StartsWith("Brass_wheel_lug", StringComparison.Ordinal)) return true;
      if (!name.StartsWith("Front_", StringComparison.Ordinal) &&
          !name.StartsWith("Rear_", StringComparison.Ordinal)) return false;
      string part = name.Substring(name.IndexOf('_') + 1);
      return part.StartsWith("tyre", StringComparison.Ordinal) ||
        part.StartsWith("tread_lug", StringComparison.Ordinal) ||
        part.StartsWith("wheel_rim", StringComparison.Ordinal) ||
        part.StartsWith("hub_plate", StringComparison.Ordinal) ||
        part.StartsWith("brass_hub", StringComparison.Ordinal) ||
        part.StartsWith("axle_cap", StringComparison.Ordinal);
    }

    private static void AddVector(string line, List<Vector3> target, bool normalize) {
      string[] fields = SplitFields(line);
      Vector3 vector = new(ParseFloat(fields[1]), ParseFloat(fields[3]), ParseFloat(fields[2]));
      target.Add(normalize ? vector.normalized : vector);
    }

  }
}
