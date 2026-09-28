using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    // A reading screen only: the title creates the session after the final page or Skip.
    public sealed class IntroStoryUI : MonoBehaviour
    {
        static readonly int[] Scenes = { 0, 1, 2, 3, 3, 4 };
        static readonly string[] Speakers = { "사장", "사장", "사장", "미나", "사장", "사장" };
        static readonly string[] Dialogue = {
            "내 꿈은 카레이서였다.\n누구보다 빠르게, 끝까지 달리고 싶었다.",
            "하지만 한 번의 사고로,\n서킷을 떠나야 했다.",
            "새로 시작해보려고 솜사탕 기계를 주문했는데…",
            "초슈퍼 첨단기술이 적용된 레이싱 솜사탕 머신이에요!\n설탕을 넣고 달리기만 하면 돼요!",
            "…솜사탕 만드는 데 자동차가 필요하다고?",
            "결승선은 조금 달라졌지만…\n나는 다시 달리기 시작했다."
        };

        Sprite[] scenes;
        VisualElement background;
        Label dialogue, speaker, counter;
        Button next, skip;
        Action completed;
        int page, lastAdvanceFrame;
        bool finished;

        public void Show(Sprite[] artwork, Action onCompleted)
        {
            scenes = artwork;
            completed = onCompleted;
            lastAdvanceFrame = Time.frameCount;
            var document = ToolkitUI.Open(gameObject, "Intro", 110);
            var root = document.rootVisualElement;
            background = ToolkitUI.Q<VisualElement>(root, "IntroBackground");
            dialogue = ToolkitUI.Q<Label>(root, "IntroDialogue");
            speaker = ToolkitUI.Q<Label>(root, "IntroSpeaker");
            counter = ToolkitUI.Q<Label>(root, "IntroPageCounter");
            next = ToolkitUI.Q<Button>(root, "IntroNextButton");
            skip = ToolkitUI.Q<Button>(root, "IntroSkipButton");
            next.clicked += Advance;
            skip.clicked += Finish;
            root.RegisterCallback<PointerUpEvent>(evt => {
                var target = evt.target as VisualElement;
                if (evt.button != 0 || target is Button || target?.GetFirstAncestorOfType<Button>() != null) return;
                Advance();
                next.Focus();
            });
            root.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode != KeyCode.Space) return;
                if (root.focusController.focusedElement == skip) Finish();
                else Advance();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(evt => {
                if (finished) return;
                (root.focusController.focusedElement == next ? skip : next).Focus();
                evt.PreventDefault();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            RefreshPage();
            root.schedule.Execute(() => next.Focus());
        }

        void Advance()
        {
            // Opening Submit and duplicate events in the same frame cannot skip a line.
            if (finished || lastAdvanceFrame == Time.frameCount) return;
            lastAdvanceFrame = Time.frameCount;
            if (page == Dialogue.Length - 1) { Finish(); return; }
            page++;
            RefreshPage();
        }

        void RefreshPage()
        {
            ToolkitUI.SetArt(background, scenes[Scenes[page]]);
            dialogue.text = Dialogue[page];
            speaker.text = Speakers[page];
            speaker.EnableInClassList("intro-speaker-mina", Speakers[page] == "미나");
            counter.text = (Scenes[page] + 1).ToString("00") + " / 05";
            next.text = page == Dialogue.Length - 1 ? "가게 시작" : "다음";
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            gameObject.SetActive(false);
            var callback = completed;
            completed = null;
            callback?.Invoke();
        }

        public void Close()
        {
            finished = true;
            completed = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
