using System.Linq;

namespace LouisPaulet.AiHarness {
  public static class AiHarnessText {

    public static string Normalize(string value) {
      return string.IsNullOrWhiteSpace(value)
          ? ""
          : new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }

  }
}
