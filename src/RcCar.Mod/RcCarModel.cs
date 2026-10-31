using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace LouisPaulet.RcCar {
  /// <summary>
  /// Loads the Blender-authored Iron Teeth RC car mesh embedded in the mod assembly.
  /// The OBJ uses Blender's X-right, Y-forward, Z-up coordinates; this converts it to
  /// Unity's X-right, Y-up, Z-forward coordinates while reversing triangle winding.
  /// </summary>
  internal static class RcCarModel {
    private const string MeshResourceName = "LouisPaulet.RcCar.Assets.IronTeethRcCar.obj";

    private sealed class MaterialGroup {
      public readonly string Name;
      public readonly List<int> TriangleIndices = new List<int>();

      public MaterialGroup(string name) {
        Name = name;
      }
    }

    private readonly struct FaceVertex {
      public readonly int PositionIndex;
      public readonly int NormalIndex;

      public FaceVertex(int positionIndex, int normalIndex) {
        PositionIndex = positionIndex;
        NormalIndex = normalIndex;
      }
    }

    public static Mesh CreateMesh(out Material[] materials) {
      Assembly assembly = typeof(RcCarModel).Assembly;
      using Stream stream = assembly.GetManifestResourceStream(MeshResourceName)
        ?? throw new InvalidOperationException("Embedded RC car mesh was not found: " + MeshResourceName);
      using StreamReader reader = new StreamReader(stream);

      List<Vector3> sourceVertices = new List<Vector3>(60000);
      List<Vector3> sourceNormals = new List<Vector3>(60000);
      List<Vector3> vertices = new List<Vector3>(60000);
      List<Vector3> normals = new List<Vector3>(60000);
      List<MaterialGroup> groups = new List<MaterialGroup>();
      Dictionary<string, MaterialGroup> groupsByName = new Dictionary<string, MaterialGroup>(StringComparer.Ordinal);
      Dictionary<long, int> meshVertexByCorner = new Dictionary<long, int>();
      MaterialGroup? currentGroup = null;

      string? line;
      while ((line = reader.ReadLine()) != null) {
        if (line.Length == 0 || line[0] == '#') {
          continue;
        }

        if (line.StartsWith("v ", StringComparison.Ordinal)) {
          string[] fields = SplitFields(line);
          float x = ParseFloat(fields[1]);
          float y = ParseFloat(fields[2]);
          float z = ParseFloat(fields[3]);
          sourceVertices.Add(new Vector3(x, z, y));
        } else if (line.StartsWith("vn ", StringComparison.Ordinal)) {
          string[] fields = SplitFields(line);
          float x = ParseFloat(fields[1]);
          float y = ParseFloat(fields[2]);
          float z = ParseFloat(fields[3]);
          sourceNormals.Add(new Vector3(x, z, y).normalized);
        } else if (line.StartsWith("usemtl ", StringComparison.Ordinal)) {
          string name = line.Substring(7).Trim();
          if (!groupsByName.TryGetValue(name, out currentGroup)) {
            currentGroup = new MaterialGroup(name);
            groupsByName.Add(name, currentGroup);
            groups.Add(currentGroup);
          }
        } else if (line.StartsWith("f ", StringComparison.Ordinal)) {
          if (currentGroup == null) {
            currentGroup = GetOrCreateGroup("IronTeeth_Charcoal", groupsByName, groups);
          }

          string[] fields = SplitFields(line);
          if (fields.Length < 4) {
            throw new InvalidDataException("RC car mesh contains a face with fewer than three vertices.");
          }

          FaceVertex first = ParseFaceVertex(fields[1]);
          int firstIndex = GetMeshVertex(first, sourceVertices, sourceNormals, vertices, normals, meshVertexByCorner);
          for (int fieldIndex = 2; fieldIndex < fields.Length - 1; fieldIndex++) {
            FaceVertex second = ParseFaceVertex(fields[fieldIndex]);
            FaceVertex third = ParseFaceVertex(fields[fieldIndex + 1]);
            int secondIndex = GetMeshVertex(second, sourceVertices, sourceNormals, vertices, normals, meshVertexByCorner);
            int thirdIndex = GetMeshVertex(third, sourceVertices, sourceNormals, vertices, normals, meshVertexByCorner);

            // Swapping Y and Z is a reflection, so reverse each triangle to preserve its outward face.
            currentGroup.TriangleIndices.Add(firstIndex);
            currentGroup.TriangleIndices.Add(thirdIndex);
            currentGroup.TriangleIndices.Add(secondIndex);
          }
        }
      }

      if (vertices.Count == 0 || groups.Count == 0) {
        throw new InvalidDataException("RC car mesh is empty.");
      }

      Mesh mesh = new Mesh {
        name = "Iron Teeth RC Car",
        indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
        subMeshCount = groups.Count,
      };
      mesh.SetVertices(vertices);
      if (normals.Count == vertices.Count) {
        mesh.SetNormals(normals);
      } else {
        mesh.RecalculateNormals();
      }

      for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++) {
        mesh.SetTriangles(groups[groupIndex].TriangleIndices, groupIndex, false);
      }
      mesh.RecalculateBounds();

      materials = CreateMaterials(groups);
      return mesh;
    }

    private static MaterialGroup GetOrCreateGroup(
      string name,
      Dictionary<string, MaterialGroup> groupsByName,
      List<MaterialGroup> groups) {
      if (groupsByName.TryGetValue(name, out MaterialGroup? group)) {
        return group;
      }

      group = new MaterialGroup(name);
      groupsByName.Add(name, group);
      groups.Add(group);
      return group;
    }

    private static string[] SplitFields(string line) {
      return line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
    }

    private static float ParseFloat(string value) {
      return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static FaceVertex ParseFaceVertex(string token) {
      string[] parts = token.Split('/');
      int positionIndex = ParseIndex(parts[0]);
      int normalIndex = parts.Length >= 3 && parts[2].Length > 0
        ? ParseIndex(parts[2])
        : positionIndex;
      return new FaceVertex(positionIndex, normalIndex);
    }

    private static int ParseIndex(string value) {
      int index = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
      if (index <= 0) {
        throw new InvalidDataException("RC car mesh uses an unsupported relative OBJ index.");
      }
      return index - 1;
    }

    private static int GetMeshVertex(
      FaceVertex corner,
      List<Vector3> sourceVertices,
      List<Vector3> sourceNormals,
      List<Vector3> vertices,
      List<Vector3> normals,
      Dictionary<long, int> meshVertexByCorner) {
      long key = ((long)corner.PositionIndex << 32) | (uint)corner.NormalIndex;
      if (meshVertexByCorner.TryGetValue(key, out int existingIndex)) {
        return existingIndex;
      }

      if (corner.PositionIndex < 0 || corner.PositionIndex >= sourceVertices.Count) {
        throw new InvalidDataException("RC car mesh references a missing vertex.");
      }
      if (corner.NormalIndex < 0 || corner.NormalIndex >= sourceNormals.Count) {
        throw new InvalidDataException("RC car mesh references a missing normal.");
      }

      int newIndex = vertices.Count;
      vertices.Add(sourceVertices[corner.PositionIndex]);
      normals.Add(sourceNormals[corner.NormalIndex]);
      meshVertexByCorner.Add(key, newIndex);
      return newIndex;
    }

    private static Material[] CreateMaterials(List<MaterialGroup> groups) {
      Shader shader = Shader.Find("Universal Render Pipeline/Lit")
        ?? Shader.Find("Standard")
        ?? Shader.Find("Universal Render Pipeline/Unlit")
        ?? throw new InvalidOperationException("No supported Unity shader was found for the RC car.");

      Material[] materials = new Material[groups.Count];
      for (int index = 0; index < groups.Count; index++) {
        MaterialStyle style = GetStyle(groups[index].Name);
        Material material = new Material(shader) {
          name = groups[index].Name,
          color = style.Color,
        };
        if (material.HasProperty("_BaseColor")) {
          material.SetColor("_BaseColor", style.Color);
        }
        if (material.HasProperty("_Metallic")) {
          material.SetFloat("_Metallic", style.Metallic);
        }
        if (material.HasProperty("_Smoothness")) {
          material.SetFloat("_Smoothness", 1f - style.Roughness);
        } else if (material.HasProperty("_Glossiness")) {
          material.SetFloat("_Glossiness", 1f - style.Roughness);
        }
        if (style.Emissive) {
          material.EnableKeyword("_EMISSION");
          if (material.HasProperty("_EmissionColor")) {
            material.SetColor("_EmissionColor", style.Color * 0.35f);
          }
        }
        materials[index] = material;
      }
      return materials;
    }

    private static MaterialStyle GetStyle(string name) {
      switch (name) {
        case "IronTeeth_Charcoal": return new MaterialStyle(41, 44, 46, 0.58f, 0.58f);
        case "IronTeeth_Gunmetal": return new MaterialStyle(74, 79, 85, 0.78f, 0.42f);
        case "IronTeeth_WeatheredSteel": return new MaterialStyle(122, 129, 132, 0.72f, 0.40f);
        case "IronTeeth_RustOrange": return new MaterialStyle(178, 74, 35, 0.32f, 0.54f);
        case "IronTeeth_Brass": return new MaterialStyle(176, 138, 70, 0.70f, 0.36f);
        case "IronTeeth_DarkWood": return new MaterialStyle(75, 57, 45, 0.00f, 0.82f);
        case "IronTeeth_WoodLight": return new MaterialStyle(128, 101, 69, 0.05f, 0.76f);
        case "IronTeeth_TealGlass": return new MaterialStyle(85, 200, 194, 0.28f, 0.22f, true);
        case "IronTeeth_Rubber": return new MaterialStyle(32, 35, 34, 0.05f, 0.92f);
        case "IronTeeth_Exhaust": return new MaterialStyle(52, 56, 58, 0.82f, 0.32f);
        default: return new MaterialStyle(74, 79, 85, 0.50f, 0.55f);
      }
    }

    private readonly struct MaterialStyle {
      public readonly Color Color;
      public readonly float Metallic;
      public readonly float Roughness;
      public readonly bool Emissive;

      public MaterialStyle(byte red, byte green, byte blue, float metallic, float roughness, bool emissive = false) {
        Color = new Color32(red, green, blue, 255);
        Metallic = metallic;
        Roughness = roughness;
        Emissive = emissive;
      }
    }
  }
}
