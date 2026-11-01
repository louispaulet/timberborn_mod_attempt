using UnityEngine;

namespace LouisPaulet.RcCar {
  internal static class RcCarMaterialStyles {
    public static RcCarMaterialStyle Get(string name) => name switch {
      "IronTeeth_Charcoal" => new(41, 44, 46, .58f, .58f),
      "IronTeeth_Gunmetal" => new(74, 79, 85, .78f, .42f),
      "IronTeeth_WeatheredSteel" => new(122, 129, 132, .72f, .40f),
      "IronTeeth_RustOrange" => new(178, 74, 35, .32f, .54f),
      "IronTeeth_Brass" => new(176, 138, 70, .70f, .36f),
      "IronTeeth_DarkWood" => new(75, 57, 45, 0f, .82f),
      "IronTeeth_WoodLight" => new(128, 101, 69, .05f, .76f),
      "IronTeeth_TealGlass" => new(85, 200, 194, .28f, .22f, true),
      "IronTeeth_Rubber" => new(32, 35, 34, .05f, .92f),
      "IronTeeth_Exhaust" => new(52, 56, 58, .82f, .32f),
      "V2_ChemicalGlass" => new(56, 184, 184, .12f, .10f, false, 66, true),
      "V2_ChemicalFluidTeal" => new(6, 122, 61, .08f, .18f, true, 255, false, .16f),
      "V2_ChemicalFluidAmber" => new(158, 51, 9, .12f, .20f, true, 255, false, .10f),
      "V2_ChemicalGlowTeal" => new(56, 224, 143, .05f, .12f, true, 255, false, .32f),
      "V2_ChemicalGlowAmber" => new(245, 112, 26, .04f, .14f, true, 255, false, .24f),
      _ => new(74, 79, 85, .50f, .55f),
    };
  }

  internal readonly struct RcCarMaterialStyle {
    public readonly Color Color;
    public readonly float Metallic, Roughness, EmissionStrength;
    public readonly bool Emissive, Transparent;
    public RcCarMaterialStyle(byte r, byte g, byte b, float metallic, float roughness,
      bool emissive = false, byte alpha = 255, bool transparent = false, float emissionStrength = .35f) {
      Color = new Color32(r, g, b, alpha); Metallic = metallic; Roughness = roughness;
      Emissive = emissive; Transparent = transparent; EmissionStrength = emissionStrength;
    }
  }
}
