using Bindito.Core;
using Timberborn.BottomBarSystem;

namespace LouisPaulet.RcCar {
  [Context("Game")]
  public class RcCarConfigurator : Configurator {
    private class BottomBarProvider : IProvider<BottomBarModule> {
      private readonly RcCarStatusHud _hud;
      public BottomBarProvider(RcCarStatusHud hud) { _hud = hud; }
      public BottomBarModule Get() {
        var builder = new BottomBarModule.Builder();
        builder.AddRightSectionElement(_hud);
        return builder.Build();
      }
    }

    protected override void Configure() {
      Bind<RcCarController>().AsSingleton();
      Bind<RcCarStatusHud>().AsSingleton();
      MultiBind<BottomBarModule>().ToProvider<BottomBarProvider>().AsSingleton();
    }
  }
}
