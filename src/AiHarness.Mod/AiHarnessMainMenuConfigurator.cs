using Bindito.Core;

namespace LouisPaulet.AiHarness {
  [Context("MainMenu")]
  public class AiHarnessMainMenuConfigurator : Configurator {

    protected override void Configure() {
      Bind<AiHarnessCommandQueue>().AsSingleton();
      Bind<IAiHarnessRequestHandler>().To<AiHarnessMainMenuEndpoint>().AsSingleton();
      Bind<AiHarnessRunner>().AsSingleton();
      Bind<AiHarnessServer>().AsSingleton();
    }

  }
}
