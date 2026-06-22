using Timberborn.ModManagerScene;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessStarter : IModStarter {

    public void StartMod(IModEnvironment modEnvironment) {
      AiHarnessPaths.Initialize(modEnvironment.ModPath);
      Debug.Log("[LouisPaulet.AiHarness] AI Harness mod started from: " + modEnvironment.ModPath);
    }

  }
}
