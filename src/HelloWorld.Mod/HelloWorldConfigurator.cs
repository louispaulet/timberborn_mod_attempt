using Bindito.Core;

namespace LouisPaulet.HelloWorld {
  [Context("Game")]
  public class HelloWorldConfigurator : Configurator {

    protected override void Configure() {
      Bind<HelloWorldPopup>().AsSingleton();
    }

  }
}
