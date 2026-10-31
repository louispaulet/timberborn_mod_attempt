using Bindito.Core;

namespace LouisPaulet.RcCar {
  [Context("Game")]
  public class RcCarConfigurator : Configurator {
    protected override void Configure() {
      Bind<RcCarController>().AsSingleton();
    }
  }
}
