using System.Collections.Generic;

namespace LouisPaulet.AiHarness {
  public interface IInteractionReplayStore {
    void Save(Dictionary<string, object> payload);
  }

  public sealed class NullInteractionReplayStore : IInteractionReplayStore {
    public void Save(Dictionary<string, object> payload) {
    }
  }
}
