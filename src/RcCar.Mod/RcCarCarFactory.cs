using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LouisPaulet.RcCar {
  internal static class RcCarCarFactory {
    public static bool TryCreate(out GameObject? car, out Mesh? mesh, out Material[]? materials) {
      car = new GameObject("RC Car");
      mesh = null;
      materials = null;
      car.AddComponent<BoxCollider>().size = RcCarMovementSettings.CarHalfExtents * 2f;
      GameObject visual = new GameObject("Iron Teeth RC Car model");
      visual.transform.SetParent(car.transform, false);
      visual.transform.localPosition = RcCarMovementSettings.CarVisualOffset;
      visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

      try {
        mesh = RcCarModel.CreateMesh(out Material[] createdMaterials);
        materials = createdMaterials;
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.AddComponent<MeshRenderer>().sharedMaterials = materials;
        return true;
      } catch (Exception exception) {
        Debug.LogError("[LouisPaulet.RcCar] Could not load the Iron Teeth RC car model: " + exception);
        Destroy(car, mesh, materials);
        car = null;
        mesh = null;
        materials = null;
        return false;
      }
    }

    private static void Destroy(GameObject car, Mesh? mesh, Material[]? materials) {
      if (materials != null) foreach (Material material in materials) if (material != null) Object.Destroy(material);
      if (mesh != null) Object.Destroy(mesh);
      if (car != null) Object.Destroy(car);
    }
  }
}
