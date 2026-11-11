using System.Collections.Generic;
using Timberborn.BottomBarSystem;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace LouisPaulet.RcCar {
  public class RcCarStatusHud : IBottomBarElementsProvider, IUpdatableSingleton {
    private readonly RcCarController _controller;
    private Label? _label;

    public RcCarStatusHud(RcCarController controller) { _controller = controller; }

    public IEnumerable<BottomBarElement> GetElements() {
      _label = new Label();
      _label.style.position = Position.Relative;
      _label.style.left = -220;
      _label.style.top = -180;
      _label.style.backgroundColor = new StyleColor(new Color(0.06f, 0.09f, 0.08f, 0.95f));
      _label.style.paddingTop = 6;
      _label.style.paddingBottom = 6;
      _label.style.paddingLeft = 8;
      _label.style.paddingRight = 8;
      Refresh();
      yield return BottomBarElement.CreateSingleLevel(_label);
    }

    public void UpdateSingleton() { Refresh(); }

    private void Refresh() {
      if (_label == null) return;
      _label.text = !_controller.CarAvailable ? "RC car unavailable" :
        _controller.IsDriving ? "RC driving: ON (Esc to exit)" : "RC driving: OFF (V to drive)";
      _label.style.color = new StyleColor(_controller.IsDriving
        ? new Color(0.65f, 1f, 0.55f) : new Color(1f, 0.8f, 0.5f));
    }
  }
}
