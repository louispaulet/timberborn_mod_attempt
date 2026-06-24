namespace LouisPaulet.AiHarness {
  public interface IAiHarnessLog {
    void Info(string message);
    void Error(string message);
  }

  public sealed class NoOpAiHarnessLog : IAiHarnessLog {
    public void Info(string message) {
    }

    public void Error(string message) {
    }
  }
}
