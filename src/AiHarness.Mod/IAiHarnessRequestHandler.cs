namespace LouisPaulet.AiHarness {
  public interface IAiHarnessRequestHandler {

    AiHarnessResponse HandleRequest(string method, string path, QueryReader query);

  }
}
