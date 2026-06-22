using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LouisPaulet.AiHarness {
  public static class AiHarnessJson {

    public static string Serialize(AiHarnessResponse response) {
      var json = new StringBuilder();
      WriteResponse(json, response);
      return json.ToString();
    }

    public static string SerializeObject(object? value) {
      var json = new StringBuilder();
      WriteValue(json, value);
      return json.ToString();
    }

    private static void WriteResponse(StringBuilder json, AiHarnessResponse response) {
      json.Append('{');
      WriteProperty(json, "ok", response.ok);
      json.Append(',');
      WriteProperty(json, "command", response.command);
      json.Append(',');
      WriteProperty(json, "commandId", response.commandId);
      json.Append(',');
      WriteProperty(json, "data", response.data);
      json.Append(',');
      WriteProperty(json, "error", response.error);
      json.Append('}');
    }

    private static void WriteProperty(StringBuilder json, string name, object? value) {
      WriteString(json, name);
      json.Append(':');
      WriteValue(json, value);
    }

    private static void WriteValue(StringBuilder json, object? value) {
      if (value == null) {
        json.Append("null");
        return;
      }

      if (value is string text) {
        WriteString(json, text);
        return;
      }

      if (value is bool boolean) {
        json.Append(boolean ? "true" : "false");
        return;
      }

      if (value is int || value is long || value is short || value is byte
          || value is uint || value is ulong || value is ushort || value is sbyte
          || value is float || value is double || value is decimal) {
        json.Append(System.Convert.ToString(value, CultureInfo.InvariantCulture));
        return;
      }

      if (value is IDictionary<string, object> dictionary) {
        WriteDictionary(json, dictionary);
        return;
      }

      if (value is IEnumerable enumerable) {
        WriteEnumerable(json, enumerable);
        return;
      }

      WriteString(json, value.ToString() ?? "");
    }

    private static void WriteDictionary(StringBuilder json, IDictionary<string, object> dictionary) {
      bool first = true;
      json.Append('{');
      foreach (KeyValuePair<string, object> pair in dictionary) {
        if (!first) {
          json.Append(',');
        }

        first = false;
        WriteProperty(json, pair.Key, pair.Value);
      }

      json.Append('}');
    }

    private static void WriteEnumerable(StringBuilder json, IEnumerable enumerable) {
      bool first = true;
      json.Append('[');
      foreach (object value in enumerable) {
        if (!first) {
          json.Append(',');
        }

        first = false;
        WriteValue(json, value);
      }

      json.Append(']');
    }

    private static void WriteString(StringBuilder json, string value) {
      json.Append('"');
      foreach (char character in value) {
        switch (character) {
          case '"':
            json.Append("\\\"");
            break;
          case '\\':
            json.Append("\\\\");
            break;
          case '\b':
            json.Append("\\b");
            break;
          case '\f':
            json.Append("\\f");
            break;
          case '\n':
            json.Append("\\n");
            break;
          case '\r':
            json.Append("\\r");
            break;
          case '\t':
            json.Append("\\t");
            break;
          default:
            if (character < 32) {
              json.Append("\\u");
              json.Append(((int) character).ToString("x4", CultureInfo.InvariantCulture));
            } else {
              json.Append(character);
            }
            break;
        }
      }

      json.Append('"');
    }

  }
}
