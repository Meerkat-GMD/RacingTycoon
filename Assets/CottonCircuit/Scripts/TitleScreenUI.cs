using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    // Layout and styling live in Title.uxml / Title.uss; this component binds actions only.
    public sealed class TitleScreenUI : MonoBehaviour
    {
        GameController game;
        SaveStore store;
        string saveDirectory;
        UIDocument document;
        VisualElement screen, confirmation;
        Button primary, newGame, settings, quit, confirm, cancel;
        Label error;
        IntroStoryUI intro;
        bool entering, confirming;

        public bool StoryShowing => intro != null;

        public void Show(GameController controller, string directory)
        {
            game = controller;
            saveDirectory = directory;
            store = new SaveStore(directory);
            document = ToolkitUI.Open(gameObject, "Title", 100);
            var root = document.rootVisualElement;
            screen = ToolkitUI.Q<VisualElement>(root, "TitleScreen");
            primary = ToolkitUI.Q<Button>(root, "TitlePrimaryButton");
            newGame = ToolkitUI.Q<Button>(root, "TitleNewGameButton");
            settings = ToolkitUI.Q<Button>(root, "TitleSettingsButton");
            quit = ToolkitUI.Q<Button>(root, "TitleQuitButton");
            confirm = ToolkitUI.Q<Button>(root, "TitleConfirmNewGameButton");
            cancel = ToolkitUI.Q<Button>(root, "TitleCancelNewGameButton");
            confirmation = ToolkitUI.Q<VisualElement>(root, "TitleConfirmation");
            error = ToolkitUI.Q<Label>(root, "TitleError");
            bool hasSave = store.HasSave;
            ToolkitUI.Show(newGame, hasSave);
            ToolkitUI.Show(confirmation, false);
            RefreshText();
            primary.clicked += () => Enter(false);
            newGame.clicked += OpenConfirmation;
            settings.clicked += OpenSettings;
            quit.clicked += Quit;
            confirm.clicked += () => Enter(true);
            cancel.clicked += CancelConfirmation;
            root.RegisterCallback<NavigationMoveEvent>(Navigate, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(evt => {
                if (root.focusController.focusedElement == null) (confirming ? cancel : primary).Focus();
            });
            root.schedule.Execute(() => primary.Focus());
            Localization.Changed += RefreshText;
        }

        void RefreshText()
        {
            bool hasSave = store.HasSave;
            primary.text = Strings.Get(hasSave ? "title.continue" : "title.start");
            ToolkitUI.Q<Label>(document.rootVisualElement, "TitleSavedNote").text = Strings.Get(hasSave ? "title.note.saved" : "title.note.fresh");
        }

        void Navigate(NavigationMoveEvent evt)
        {
            if (entering) return;
            int delta = evt.direction == NavigationMoveEvent.Direction.Up || evt.direction == NavigationMoveEvent.Direction.Left ? -1 : 1;
            var buttons = confirming ? new[] { cancel, confirm }
                : store.HasSave ? new[] { primary, newGame, settings, quit } : new[] { primary, settings, quit };
            int index = System.Array.IndexOf(buttons, document.rootVisualElement.focusController.focusedElement as Button);
            buttons[(index + delta + buttons.Length) % buttons.Length].Focus();
            evt.PreventDefault();
            evt.StopPropagation();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape || entering) return;
            if (confirming) CancelConfirmation();
            else quit.Focus();
            evt.StopPropagation();
        }

        void OpenConfirmation()
        {
            confirming = true;
            ToolkitUI.Show(confirmation, true);
            primary.SetEnabled(false);
            quit.SetEnabled(false);
            newGame.SetEnabled(false);
            settings.SetEnabled(false);
            cancel.Focus();
        }

        void CancelConfirmation()
        {
            confirming = false;
            ToolkitUI.Show(confirmation, false);
            primary.SetEnabled(true);
            quit.SetEnabled(true);
            newGame.SetEnabled(true);
            settings.SetEnabled(true);
            (store.HasSave ? newGame : primary).Focus();
        }

        void OpenSettings()
        {
            if (entering || confirming) return;
            game.OpenSettings(() => settings.Focus());
        }

        void Enter(bool reset)
        {
            if (entering) return;
            entering = true;
            if (reset || !store.HasSave)
            {
                var host = new GameObject("Intro Story");
                host.transform.SetParent(transform.parent, false);
                intro = host.AddComponent<IntroStoryUI>();
                intro.Show(game.World.Assets.IntroScenes, () => CompleteNewGame(reset));
                ToolkitUI.Show(screen, false);
                return;
            }
            game.Initialize(saveDirectory);
        }

        void CompleteNewGame(bool reset)
        {
            // Retain the old save until the final story page or Skip.
            if (reset && !store.ArchiveAndReset())
            {
                intro.Close();
                intro = null;
                ToolkitUI.Show(screen, true);
                entering = false;
                CancelConfirmation();
                error.text = store.Error;
                ToolkitUI.Show(error, true);
                return;
            }
            game.Initialize(saveDirectory, tutorial: true);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Close()
        {
            if (intro) { intro.Close(); intro = null; }
            Localization.Changed -= RefreshText;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void OnDestroy() { Localization.Changed -= RefreshText; }
    }
}
