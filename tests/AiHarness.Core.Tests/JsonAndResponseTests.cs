using System.Collections.Generic;
using System.Text.Json;
using LouisPaulet.AiHarness;
using Xunit;

namespace AiHarness.Core.Tests;

public sealed class JsonAndResponseTests {

  [Fact]
  public void Serialize_EscapesStringsAndKeepsResponseShape() {
    var response = AiHarnessResponse.Success(
        "log",
        "log-1",
        new Dictionary<string, object> {
          { "message", "quote \" slash \\ newline\n tab\t" },
          { "items", new object[] { "a", 2, true } }
        });

    string json = AiHarnessJson.Serialize(response);
    using JsonDocument document = JsonDocument.Parse(json);

    Assert.True(document.RootElement.GetProperty("ok").GetBoolean());
    Assert.Equal("log", document.RootElement.GetProperty("command").GetString());
    Assert.Equal("log-1", document.RootElement.GetProperty("commandId").GetString());
    Assert.Equal("quote \" slash \\ newline\n tab\t", document.RootElement.GetProperty("data").GetProperty("message").GetString());
    Assert.Null(document.RootElement.GetProperty("error").GetString());
  }

  [Fact]
  public void StatusCodeFor_MapsUnknownCommandTo404AndOtherFailuresTo400() {
    Assert.Equal(200, AiHarnessResponse.StatusCodeFor(AiHarnessResponse.Success("status", "status-1", new object())));
    Assert.Equal(404, AiHarnessResponse.StatusCodeFor(AiHarnessResponse.Failure("nope", "nope-1", "Unknown AI Harness command.")));
    Assert.Equal(400, AiHarnessResponse.StatusCodeFor(AiHarnessResponse.Failure("speed", "speed-1", "Speed value must be one of 0, 1, 2, or 3.")));
  }

}
