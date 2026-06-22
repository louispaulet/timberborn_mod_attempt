namespace LouisPaulet.AiHarness {
  public class AiHarnessResponse {

    public bool ok { get; set; }
    public string command { get; set; } = "";
    public string commandId { get; set; } = "";
    public object? data { get; set; }
    public string? error { get; set; }

    public static AiHarnessResponse Success(string command, string commandId, object data) {
      return new AiHarnessResponse {
        ok = true,
        command = command,
        commandId = commandId,
        data = data,
        error = null
      };
    }

    public static AiHarnessResponse Failure(string command, string commandId, string error) {
      return new AiHarnessResponse {
        ok = false,
        command = command,
        commandId = commandId,
        data = null,
        error = error
      };
    }

    public static int StatusCodeFor(AiHarnessResponse response) {
      if (response.ok) {
        return 200;
      }

      return response.error == "Unknown AI Harness command." ? 404 : 400;
    }

  }
}
