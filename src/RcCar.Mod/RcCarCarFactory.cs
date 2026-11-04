using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LouisPaulet.RcCar {
  internal static class RcCarCarFactory {
    public static bool TryCreate(out GameObject? car, List<Mesh> meshes, List<Material> materials,
      out Transform[] frontWheels) {
      car = new GameObject("RC Car");
      frontWheels = new Transform[2];
      car.AddComponent<BoxCollider>().size = RcCarMovementSettings.CarHalfExtents * 2f;
      GameObject visual = new GameObject("Iron Teeth RC Car model");
      visual.transform.SetParent(car.transform, false);
      visual.transform.localPosition = RcCarMovementSettings.CarVisualOffset;
      visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

      try {
        RcCarObjData[] parts = RcCarModel.Load();
        AddPart(visual, parts[0], Vector3.zero, meshes, materials);
        for (int i = 0; i < frontWheels.Length; i++) {
          RcCarObjData wheel = parts[i + 1];
          GameObject pivot = new GameObject(i == 0 ? "Front right wheel" : "Front left wheel");
          pivot.transform.SetParent(visual.transform, false);
          pivot.transform.localPosition = WheelCenter(wheel);
          AddPart(pivot, wheel, pivot.transform.localPosition, meshes, materials);
          frontWheels[i] = pivot.transform;
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
      return (min + max) * 0.5f;
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
