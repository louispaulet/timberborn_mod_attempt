using System;
using System.IO;
using System.Text;

namespace LouisPaulet.AiHarness {
  public static class AiHarnessPaths {

    public static string ModPath { get; private set; } = "";

    public static void Initialize(string modPath) {
      ModPath = modPath;
    }

    public static string GetGeneratedFilePath(string kind, string name, string extension) {
      string root = string.IsNullOrWhiteSpace(ModPath)
          ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Timberborn", "Mods", "AiHarness")
          : ModPath;

      return Path.Combine(root, "generated", SanitizePathPart(kind), SanitizePathPart(name) + extension);
    }

    private static string SanitizePathPart(string value) {
      if (string.IsNullOrWhiteSpace(value)) {
        return "unnamed";
      }

      var builder = new StringBuilder(value.Length);
      foreach (char character in value.Trim()) {
        if (char.IsLetterOrDigit(character) || character == '-' || character == '_') {
          builder.Append(character);
        } else {
          builder.Append('-');
        }
      }

      string sanitized = builder.ToString().Trim('-');
      return sanitized.Length == 0 ? "unnamed" : sanitized;
    }

  }
}
