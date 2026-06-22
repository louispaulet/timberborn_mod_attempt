using System.Collections.Generic;
using System.Linq;
using Timberborn.BottomBarSystem;
using Timberborn.SingletonSystem;
using UnityEngine.UIElements;

namespace LouisPaulet.AiHarness {
  public class AiHarnessInteractionHud : IBottomBarElementsProvider, IUpdatableSingleton {

    private readonly AiHarnessInteractionState _interactionState;

    private VisualElement? _root;
    private Label? _question;
    private Button? _askButton;
    private Button? _refreshButton;
    private Button[] _answerButtons = new Button[0];
    private int _seenRevision = -1;
    private bool _collapsed;

    public AiHarnessInteractionHud(AiHarnessInteractionState interactionState) {
      _interactionState = interactionState;
    }

    public IEnumerable<BottomBarElement> GetElements() {
      _root = BuildRoot();
      Refresh();
      yield return BottomBarElement.CreateSingleLevel(_root);
    }

    public void UpdateSingleton() {
      if (_root == null || _seenRevision == _interactionState.Revision) {
        return;
      }

      Refresh();
    }

    private VisualElement BuildRoot() {
      var root = new VisualElement();
      root.name = "AiHarnessInteractionHud";
      root.style.flexDirection = FlexDirection.Column;
      root.style.width = 360;
      root.style.paddingLeft = 8;
      root.style.paddingRight = 8;
      root.style.paddingTop = 6;
      root.style.paddingBottom = 6;
      root.style.marginLeft = 6;
      root.style.marginRight = 6;
      root.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.07f, 0.08f, 0.1f, 0.88f));

      var topRow = new VisualElement();
      topRow.style.flexDirection = FlexDirection.Row;
      topRow.style.marginBottom = 4;
      root.Add(topRow);

      _askButton = new Button(() => {
        if (_collapsed) {
          _collapsed = false;
          Refresh();
          return;
        }

        _interactionState.RequestInteraction("current situation", "hud");
      });
      _askButton.text = "Ask AI";
      _askButton.style.width = 88;
      StyleButton(_askButton);
      topRow.Add(_askButton);

      _refreshButton = new Button(() => {
        _collapsed = false;
        _interactionState.RequestChoiceRefresh();
      });
      _refreshButton.text = "♻";
      _refreshButton.tooltip = "Refresh choices";
      _refreshButton.style.width = 30;
      _refreshButton.style.marginLeft = 4;
      StyleButton(_refreshButton);
      topRow.Add(_refreshButton);

      var collapseButton = new Button(() => {
        _collapsed = !_collapsed;
        Refresh();
      });
      collapseButton.text = "-";
      collapseButton.style.width = 30;
      collapseButton.style.marginLeft = 4;
      StyleButton(collapseButton);
      topRow.Add(collapseButton);

      _question = new Label();
      _question.style.whiteSpace = WhiteSpace.Normal;
      _question.style.flexGrow = 1;
      _question.style.marginLeft = 6;
      _question.style.color = new StyleColor(new UnityEngine.Color(0.94f, 0.92f, 0.82f, 1f));
      topRow.Add(_question);

      var answerGrid = new VisualElement();
      answerGrid.name = "AiHarnessAnswerGrid";
      answerGrid.style.flexDirection = FlexDirection.Row;
      answerGrid.style.flexWrap = Wrap.Wrap;
      root.Add(answerGrid);

      _answerButtons = new Button[4];
      for (int i = 0; i < 4; i++) {
        int button = i + 1;
        var answerButton = new Button(() => _interactionState.SubmitAnswer(button));
        answerButton.style.width = 166;
        answerButton.style.marginTop = 3;
        answerButton.style.marginRight = 3;
        StyleButton(answerButton);
        answerGrid.Add(answerButton);
        _answerButtons[i] = answerButton;
      }

      return root;
    }

    private void Refresh() {
      if (_root == null || _question == null || _askButton == null || _refreshButton == null || _answerButtons.Length != 4) {
        return;
      }

      var snapshot = (Dictionary<string, object>) _interactionState.SnapshotData();
      _seenRevision = (int) snapshot["revision"];
      string status = (string) snapshot["status"];
      _askButton.text = _collapsed ? "AI" : "Ask AI";
      _question.text = _collapsed ? status : status + ": " + (string) snapshot["question"];
      VisualElement answerGrid = _root.Q<VisualElement>("AiHarnessAnswerGrid");
      answerGrid.style.display = _collapsed ? DisplayStyle.None : DisplayStyle.Flex;

      object[] options = ((IEnumerable<object>) snapshot["options"]).Cast<object>().ToArray();
      for (int i = 0; i < 4; i++) {
        var option = (Dictionary<string, object>) options[i];
        _answerButtons[i].text = (i + 1).ToString() + ". " + (string) option["label"];
      }
    }

    private static void StyleButton(Button button) {
      button.style.color = new StyleColor(new UnityEngine.Color(0.98f, 0.96f, 0.86f, 1f));
      button.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.14f, 0.22f, 0.24f, 0.96f));
      button.style.borderTopColor = new StyleColor(new UnityEngine.Color(0.75f, 0.62f, 0.33f, 1f));
      button.style.borderRightColor = new StyleColor(new UnityEngine.Color(0.75f, 0.62f, 0.33f, 1f));
      button.style.borderBottomColor = new StyleColor(new UnityEngine.Color(0.75f, 0.62f, 0.33f, 1f));
      button.style.borderLeftColor = new StyleColor(new UnityEngine.Color(0.75f, 0.62f, 0.33f, 1f));
      button.style.borderTopWidth = 1;
      button.style.borderRightWidth = 1;
      button.style.borderBottomWidth = 1;
      button.style.borderLeftWidth = 1;
    }

  }
}
