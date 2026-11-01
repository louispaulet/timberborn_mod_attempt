using UnityEngine;

namespace LouisPaulet.RcCar {
  internal static class RcCarMaterialStyles {
    public static RcCarMaterialStyle Get(string name) => name switch {
      "V3_worn_blue_gunmetal" => new(95, 106, 111, .78f, .38f),
      "V3_charcoal_cast_iron" => new(52, 58, 61, .65f, .52f),
      "V3_aged_brass" => new(186, 147, 85, .80f, .37f),
      "V3_oxidised_iron" => new(137, 75, 44, .48f, .65f),
      "V3_sawn_warm_oak" => new(153, 106, 63, 0f, .67f),
      "V3_honey_timber" => new(177, 136, 87, 0f, .61f),
      "V3_green_extract" => new(101, 205, 41, .12f, .22f, true, 255, false, .50f),
      "V3_red_badwater" => new(200, 36, 28, .12f, .23f, true, 255, false, .40f),
      "V3_warm_lamp_glass" => new(255, 186, 89, .15f, .19f, true, 255, false, 2f),
      "V3_enamel_lettering" => new(229, 214, 177, .10f, .50f),
      "V3_clear_chemical_vessel" => new(218, 243, 237, .25f, .16f, false, 30, true),
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
