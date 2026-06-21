using Timberborn.ModManagerScene;
using UnityEngine;

namespace LouisPaulet.HelloWorld {
  public class HelloWorldStarter : IModStarter {

    public void StartMod(IModEnvironment modEnvironment) {
      Debug.Log("[LouisPaulet.HelloWorld] Hello World mod started from: " + modEnvironment.ModPath);
    }

  }
}
