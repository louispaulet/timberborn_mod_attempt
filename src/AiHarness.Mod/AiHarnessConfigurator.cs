using Bindito.Core;
using Timberborn.BottomBarSystem;
using Timberborn.HttpApiSystem;

namespace LouisPaulet.AiHarness {
  [Context("Game")]
  public class AiHarnessConfigurator : Configurator {

    private class BottomBarModuleProvider : IProvider<BottomBarModule> {

      private readonly AiHarnessInteractionHud _interactionHud;

      public BottomBarModuleProvider(AiHarnessInteractionHud interactionHud) {
        _interactionHud = interactionHud;
      }

      public BottomBarModule Get() {
        BottomBarModule.Builder builder = new BottomBarModule.Builder();
        builder.AddRightSectionElement(_interactionHud);
        return builder.Build();
      }

    }

    protected override void Configure() {
      Bind<AiHarnessBuildingPlacement>().AsSingleton();
      Bind<AiHarnessCommandQueue>().AsSingleton();
      Bind<AiHarnessGameContext>().AsSingleton();
      Bind<AiHarnessInteractionHud>().AsSingleton();
      Bind<AiHarnessInteractionState>().AsSingleton();
      Bind<IAiHarnessLog>().To<UnityAiHarnessLog>().AsSingleton();
      Bind<IInteractionReplayStore>().To<FileInteractionReplayStore>().AsSingleton();
      Bind<IAiHarnessRequestHandler>().To<AiHarnessEndpoint>().AsSingleton();
      Bind<AiHarnessRunner>().AsSingleton();
      Bind<AiHarnessServer>().AsSingleton();
      MultiBind<IHttpApiEndpoint>().To<AiHarnessEndpoint>().AsSingleton();
      MultiBind<BottomBarModule>().ToProvider<BottomBarModuleProvider>().AsSingleton();
    }

  }
}
