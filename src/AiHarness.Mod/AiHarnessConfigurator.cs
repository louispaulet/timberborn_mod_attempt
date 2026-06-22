using Bindito.Core;
using Timberborn.HttpApiSystem;

namespace LouisPaulet.AiHarness {
  [Context("Game")]
  public class AiHarnessConfigurator : Configurator {

    protected override void Configure() {
      Bind<AiHarnessBuildingPlacement>().AsSingleton();
      Bind<AiHarnessCommandQueue>().AsSingleton();
      Bind<IAiHarnessRequestHandler>().To<AiHarnessEndpoint>().AsSingleton();
      Bind<AiHarnessRunner>().AsSingleton();
      Bind<AiHarnessServer>().AsSingleton();
      MultiBind<IHttpApiEndpoint>().To<AiHarnessEndpoint>().AsSingleton();
    }

  }
}
