using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace LouisPaulet.AiHarness {
  public sealed class FileInteractionReplayStore : IInteractionReplayStore {

    public void Save(Dictionary<string, object> payload) {
      string replayKey = payload.TryGetValue("replayKey", out object value) ? value.ToString() ?? "unnamed" : "unnamed";
      string path = AiHarnessPaths.GetGeneratedFilePath("interactions", replayKey, ".json");
      Directory.CreateDirectory(Path.GetDirectoryName(path));

      if (!payload.ContainsKey("createdAtUtc")) {
        payload["createdAtUtc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
      }

      File.WriteAllText(path, AiHarnessJson.SerializeObject(payload));
    }

  }
}
