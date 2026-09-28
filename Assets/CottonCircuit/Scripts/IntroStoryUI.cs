using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    // A reading screen only: the title creates the session after the final page or Skip.
    public sealed class IntroStoryUI : MonoBehaviour
    {
        static readonly int[] Scenes = { 0, 1, 2, 3, 3, 4 };
        static readonly bool[] MinaSpeaks = { false, false, false, true, false, false };

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
            Localization.Changed += RefreshPage;
        }

        void Advance()
        {
            // Opening Submit and duplicate events in the same frame cannot skip a line.
            if (finished || lastAdvanceFrame == Time.frameCount) return;
            lastAdvanceFrame = Time.frameCount;
            if (page == MinaSpeaks.Length - 1) { Finish(); return; }
            page++;
            RefreshPage();
        }

        void RefreshPage()
        {
            ToolkitUI.SetArt(background, scenes[Scenes[page]]);
            dialogue.text = Strings.Get("intro.line." + page);
            speaker.text = Strings.Get(MinaSpeaks[page] ? "intro.speaker.mina" : "intro.speaker.me");
            speaker.EnableInClassList("intro-speaker-mina", MinaSpeaks[page]);
            counter.text = (Scenes[page] + 1).ToString("00") + " / 05";
            next.text = Strings.Get(page == MinaSpeaks.Length - 1 ? "intro.start" : "intro.next");
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
            Localization.Changed -= RefreshPage;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void OnDestroy() { Localization.Changed -= RefreshPage; }
    }
}
