using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LouisPaulet.RcCar {
  internal static class RcCarMaterialFactory {
    public static Material[] Create(List<RcCarMaterialGroup> groups) {
      Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")
        ?? Shader.Find("Universal Render Pipeline/Unlit")
        ?? throw new System.InvalidOperationException("No supported Unity shader was found for the RC car.");
      var materials = new Material[groups.Count];
      for (int i = 0; i < groups.Count; i++) materials[i] = Create(shader, groups[i].Name);
      return materials;
    }

    private static Material Create(Shader shader, string name) {
      RcCarMaterialStyle style = RcCarMaterialStyles.Get(name);
      // Use an explicitly alpha-blended shader for glass. A runtime switch on
      // URP/Lit can fall back to an opaque variant in the shipped game player.
      if (style.Transparent) {
        shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default")
          ?? throw new System.InvalidOperationException("No alpha-blended shader was found for RC car glass.");
        Debug.Log("[LouisPaulet.RcCar] Glass material " + name + " uses " + shader.name + ".");
      }
      var material = new Material(shader) { name = name, color = style.Color };
      if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", style.Color);
      if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", style.Metallic);
      if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f - style.Roughness);
      else if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1f - style.Roughness);
      if (style.Transparent) ConfigureTransparent(material);
      if (style.Emissive) ConfigureEmission(material, style);
      return material;
    }

    private static void ConfigureEmission(Material material, RcCarMaterialStyle style) {
      material.EnableKeyword("_EMISSION");
      if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", style.Color * style.EmissionStrength);
    }

    private static void ConfigureTransparent(Material material) {
      material.SetOverrideTag("RenderType", "Transparent");
      if (material.HasProperty("_Surface")) {
        material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
      }
      if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
      if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
      if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
      if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
      if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
      material.DisableKeyword("_ALPHATEST_ON"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
      material.EnableKeyword("_ALPHABLEND_ON"); material.renderQueue = (int)RenderQueue.Transparent;
      material.SetShaderPassEnabled("ShadowCaster", false);
      material.SetShaderPassEnabled("DepthOnly", false);
    }
  }
}
