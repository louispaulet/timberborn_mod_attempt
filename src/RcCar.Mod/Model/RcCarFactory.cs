using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LouisPaulet.RcCar {
  internal static class RcCarFactory {
    public static bool TryCreate(out GameObject? car, List<Mesh> meshes, List<Material> materials,
      out Transform[] wheels) {
      car = new GameObject("RC Car");
      wheels = new Transform[4];
      car.AddComponent<BoxCollider>().size = RcCarMovementSettings.CarHalfExtents * 2f;
      GameObject visual = new GameObject("Iron Teeth RC Car model");
      visual.transform.SetParent(car.transform, false);
      visual.transform.localPosition = RcCarMovementSettings.CarVisualOffset;
      visual.transform.localScale = Vector3.one * RcCarMovementSettings.CarScale;
      visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

      try {
        RcCarObjData[] parts = RcCarModel.Load();
        AddPart(visual, parts[0], Vector3.zero, meshes, materials);
        for (int i = 0; i < wheels.Length; i++) {
          RcCarObjData wheel = parts[i + 1];
          string axle = i < 2 ? "Front" : "Rear";
          string side = i % 2 == 0 ? "right" : "left";
          GameObject pivot = new GameObject($"{axle} {side} wheel");
          pivot.transform.SetParent(visual.transform, false);
          pivot.transform.localPosition = WheelCenter(wheel);
          AddPart(pivot, wheel, pivot.transform.localPosition, meshes, materials);
          wheels[i] = pivot.transform;
        }
        return true;
      } catch (Exception exception) {
        Debug.LogError("[LouisPaulet.RcCar] Could not load the Iron Teeth RC car model: " + exception);
        DestroyAssets(meshes, materials);
        Object.Destroy(car);
        car = null;
        return false;
      }
    }

    private static Vector3 WheelCenter(RcCarObjData wheel) {
      Vector3 min = wheel.Vertices[0], max = min;
      foreach (Vector3 vertex in wheel.Vertices) {
        min = Vector3.Min(min, vertex);
        max = Vector3.Max(max, vertex);
      }
      Vector3 center = (min + max) * 0.5f;
      // The hardware extends past the outer sidewall. Steer around the tyre center.
      center.x = Mathf.Sign(center.x) * 0.325f;
      return center;
    }

    private static void AddPart(GameObject obj, RcCarObjData data, Vector3 pivot,
      List<Mesh> meshes, List<Material> materials) {
      Mesh mesh = RcCarMeshBuilder.Create(data, pivot);
      meshes.Add(mesh);
      Material[] partMaterials = RcCarMaterialFactory.Create(data.Groups);
      materials.AddRange(partMaterials);
      obj.AddComponent<MeshFilter>().sharedMesh = mesh;
      obj.AddComponent<MeshRenderer>().sharedMaterials = partMaterials;
    }

    public static void DestroyAssets(List<Mesh> meshes, List<Material> materials) {
      foreach (Mesh mesh in meshes) Object.Destroy(mesh);
      foreach (Material material in materials) Object.Destroy(material);
      meshes.Clear();
      materials.Clear();
    }
  }
}
