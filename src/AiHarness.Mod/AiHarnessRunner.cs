using Timberborn.SingletonSystem;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessRunner : ILoadableSingleton, IUpdatableSingleton, IUnloadableSingleton {

    private const int MaxCommandsPerUpdate = 16;

    private readonly AiHarnessCommandQueue _commandQueue;

    public AiHarnessRunner(AiHarnessCommandQueue commandQueue) {
      _commandQueue = commandQueue;
    }

    public void Load() {
      Debug.Log("[LouisPaulet.AiHarness] AI Harness runner loaded.");
    }

    public void UpdateSingleton() {
      _commandQueue.Drain(MaxCommandsPerUpdate);
    }

    public void Unload() {
      Debug.Log("[LouisPaulet.AiHarness] AI Harness runner unloaded.");
    }

  }
}
