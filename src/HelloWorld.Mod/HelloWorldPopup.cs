using Timberborn.CoreUI;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace LouisPaulet.HelloWorld {
  public class HelloWorldPopup : ILoadableSingleton {

    private const string Message = "Hello World";

    private readonly DialogBoxShower _dialogBoxShower;

    public HelloWorldPopup(DialogBoxShower dialogBoxShower) {
      _dialogBoxShower = dialogBoxShower;
    }

    public void Load() {
      Debug.Log("[LouisPaulet.HelloWorld] Showing Hello World popup.");
      _dialogBoxShower.Create()
          .SetMessage(Message)
          .Show();
    }

  }
}
