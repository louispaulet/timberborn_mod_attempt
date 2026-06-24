using UnityEngine;

namespace LouisPaulet.AiHarness {
  public sealed class UnityAiHarnessLog : IAiHarnessLog {

    public void Info(string message) {
      Debug.Log("[LouisPaulet.AiHarness] " + message);
    }

    public void Error(string message) {
      Debug.LogError("[LouisPaulet.AiHarness] " + message);
    }

  }
}
