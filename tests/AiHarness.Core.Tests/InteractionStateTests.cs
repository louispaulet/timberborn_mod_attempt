using System;
using System.Collections.Generic;
using LouisPaulet.AiHarness;
using Xunit;

namespace AiHarness.Core.Tests;

public sealed class InteractionStateTests {

  [Fact]
  public void ValidateMenu_RequiresFourOptionsAndRequiredMenuMix() {
    Assert.Equal("", AiHarnessInteractionState.ValidateMenu("What next?", ValidMenu()));

    string missingTool = AiHarnessInteractionState.ValidateMenu(
        "What next?",
        new[] {
          new AiHarnessInteractionOption(1, "Build tips", "menu", "building.pathing"),
          new AiHarnessInteractionOption(2, "Pathing", "menu", "path"),
          new AiHarnessInteractionOption(3, "Context", "menu", "context"),
          new AiHarnessInteractionOption(4, "No", "no", "")
        });
    Assert.Equal("Non-confirmation menus must include at least one tool option, one menu/navigation option, and one back/cancel/no option.", missingTool);

    string confirmationWithoutNo = AiHarnessInteractionState.ValidateMenu(
        "Place it?",
        new[] {
          new AiHarnessInteractionOption(1, "Yes", "yes", ""),
          new AiHarnessInteractionOption(2, "Confirm", "confirm", ""),
          new AiHarnessInteractionOption(3, "Details", "menu", ""),
          new AiHarnessInteractionOption(4, "Context", "tool", "")
        });
    Assert.Equal("Confirmation menus must include yes and no choices.", confirmationWithoutNo);
  }

  [Fact]
  public void Lifecycle_RecordsRequestMenuAnswerToolResultAndClear() {
    var replayStore = new RecordingReplayStore();
    var state = new AiHarnessInteractionState(new NoOpAiHarnessLog(), replayStore);

    Dictionary<string, object> requested = Snapshot(state.RequestInteraction("current situation", "test"));
    Assert.Equal("requested", requested["status"]);
    Assert.Equal(1, requested["revision"]);

    Dictionary<string, object> menu = Snapshot(state.ShowMenu(
        (string) requested["interactionId"],
        "root",
        "What should Pi inspect?",
        ValidMenu(),
        "water.storage-readiness",
        "root",
        "live"));
    Assert.Equal("menuShown", menu["status"]);
    Assert.Equal("0-1-0-water-storage-readiness-root-live", menu["replayKey"]);
    Assert.Single(replayStore.Payloads);

    Dictionary<string, object> answer = Snapshot(state.SubmitAnswer(1));
    Assert.Equal("toolRequested", answer["status"]);
    Assert.Equal(1, answer["lastButton"]);
    Dictionary<string, object> lastAnswer = Snapshot(answer["lastAnswer"]);
    Assert.Equal("Water readiness", lastAnswer["label"]);

    Dictionary<string, object> toolResult = Snapshot(state.RecordToolResult("timberborn_water_readiness", true, "Water looks fine."));
    Assert.Equal("toolCompleted", toolResult["status"]);
    Assert.Equal("Water looks fine.", toolResult["question"]);

    Dictionary<string, object> cleared = Snapshot(state.Clear());
    Assert.Equal("idle", cleared["status"]);
    Assert.Equal("", cleared["interactionId"]);
  }

  [Fact]
  public void ShowMenu_ThrowsAndStoresLastErrorForInvalidMenu() {
    var state = new AiHarnessInteractionState(new NoOpAiHarnessLog(), new NullInteractionReplayStore());

    ArgumentException exception = Assert.Throws<ArgumentException>(() => state.ShowMenu(
        "",
        "",
        "",
        ValidMenu(),
        "general",
        "root",
        "test"));
    Assert.Equal("Interaction menu requires a question.", exception.Message);

    Dictionary<string, object> snapshot = Snapshot(state.SnapshotData());
    Assert.Equal("Interaction menu requires a question.", snapshot["lastError"]);
  }

  private static AiHarnessInteractionOption[] ValidMenu() {
    return new[] {
      new AiHarnessInteractionOption(1, "Water readiness", "tool", "timberborn_water_readiness"),
      new AiHarnessInteractionOption(2, "Build tips", "menu", "building.pathing"),
      new AiHarnessInteractionOption(3, "Game context", "tool", "timberborn_game_context"),
      new AiHarnessInteractionOption(4, "No", "no", "")
    };
  }

  private static Dictionary<string, object> Snapshot(object value) {
    return Assert.IsType<Dictionary<string, object>>(value);
  }

  private sealed class RecordingReplayStore : IInteractionReplayStore {
    public List<Dictionary<string, object>> Payloads { get; } = new();

    public void Save(Dictionary<string, object> payload) {
      Payloads.Add(payload);
    }
  }

}
