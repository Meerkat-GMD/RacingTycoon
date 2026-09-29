using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    // Layout lives in Settings.uxml / Settings.uss; this component binds values, focus and keys only.
    public sealed class SettingsUI : MonoBehaviour
    {
        const int SortingOrder = 200, VolumeStep = 10;
        UIDocument document;
        VisualElement screen, musicRow, effectsRow, muteRow, modeRow, resolutionRow, languageRow;
        Slider music, effects;
        Label musicValue, effectsValue, modeValue, resolutionValue, languageValue;
        Button mute, close;
        VisualElement[] rows;
        VisualElement lastRow;
        Action closed;
        int closedFrame = -1;

        public bool IsOpen => screen != null && !screen.ClassListContains("hidden");
        /// <summary>True while open and in the frame it closed, so the Esc that closed it does not also toggle pause.</summary>
        public bool HandlesEscape => IsOpen || closedFrame == Time.frameCount;

        public void Open(Action onClosed)
        {
            if (!document) Build();
            closed = onClosed;
            ToolkitUI.Show(screen, true);
            Refresh();
            screen.schedule.Execute(() => musicRow.Focus());
        }

        public void Close()
        {
            if (!IsOpen) return;
            SettingsStore.Save();
            ToolkitUI.Show(screen, false);
            closedFrame = Time.frameCount;
            var callback = closed;
            closed = null;
            callback?.Invoke();
        }

        void Build()
        {
            document = ToolkitUI.Open(gameObject, "Settings", SortingOrder);
            var root = document.rootVisualElement;
            screen = Q<VisualElement>("SettingsScreen");
            musicRow = Q<VisualElement>("SettingsMusicRow"); effectsRow = Q<VisualElement>("SettingsEffectsRow");
            muteRow = Q<VisualElement>("SettingsMuteRow"); modeRow = Q<VisualElement>("SettingsModeRow");
            resolutionRow = Q<VisualElement>("SettingsResolutionRow"); languageRow = Q<VisualElement>("SettingsLanguageRow");
            music = Q<Slider>("SettingsMusicSlider"); effects = Q<Slider>("SettingsEffectsSlider");
            musicValue = Q<Label>("SettingsMusicValue"); effectsValue = Q<Label>("SettingsEffectsValue");
            modeValue = Q<Label>("SettingsModeValue"); resolutionValue = Q<Label>("SettingsResolutionValue");
            languageValue = Q<Label>("SettingsLanguageValue");
            mute = Q<Button>("SettingsMuteButton"); close = Q<Button>("SettingsCloseButton");
            rows = new[] { musicRow, effectsRow, muteRow, modeRow, resolutionRow, languageRow, (VisualElement)close };
            lastRow = musicRow;

            // A drag applies every step at once so the music follows the handle; settings.json is written when the handle is let go.
            music.RegisterValueChangedCallback(evt => SettingsStore.Apply(s => s.MusicVolume = evt.newValue / 100f, persist: false));
            effects.RegisterValueChangedCallback(evt => SettingsStore.Apply(s => s.EffectsVolume = evt.newValue / 100f, persist: false));
            music.RegisterCallback<PointerCaptureOutEvent>(evt => SettingsStore.Save());
            effects.RegisterCallback<PointerCaptureOutEvent>(evt => { SettingsStore.Save(); ToolkitUI.PlayClick(); });
            mute.clicked += ToggleMute;
            Q<Button>("SettingsModePrev").clicked += ToggleMode;
            Q<Button>("SettingsModeNext").clicked += ToggleMode;
            Q<Button>("SettingsResolutionPrev").clicked += () => StepResolution(-1);
            Q<Button>("SettingsResolutionNext").clicked += () => StepResolution(1);
            Q<Button>("SettingsLanguagePrev").clicked += ToggleLanguage;
            Q<Button>("SettingsLanguageNext").clicked += ToggleLanguage;
            close.clicked += Close;
            // Arrows, the mute toggle and sliders are not focusable, so a press on them focuses their row instead.
            foreach (var row in rows)
            {
                row.RegisterCallback<PointerDownEvent>(evt => row.Focus());
                row.RegisterCallback<FocusEvent>(evt => lastRow = row);
            }
            // A click on the card or the backdrop clears focus, and the keys would no longer reach this window.
            root.RegisterCallback<PointerUpEvent>(evt => {
                if (IsOpen && root.focusController.focusedElement == null) lastRow.Focus();
            });
            root.RegisterCallback<NavigationMoveEvent>(Navigate, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(Submit, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            SettingsStore.Changed += Refresh;
            Localization.Changed += Refresh;
        }

        void OnDestroy() { SettingsStore.Changed -= Refresh; Localization.Changed -= Refresh; }

        void Refresh()
        {
            if (screen == null) return;
            var settings = SettingsStore.Current;
            music.SetValueWithoutNotify(Mathf.Round(settings.MusicVolume * 100));
            effects.SetValueWithoutNotify(Mathf.Round(settings.EffectsVolume * 100));
            musicValue.text = Mathf.RoundToInt(settings.MusicVolume * 100) + "%";
            effectsValue.text = Mathf.RoundToInt(settings.EffectsVolume * 100) + "%";
            mute.text = Strings.Get(settings.Muted ? "common.on" : "common.off");
            mute.EnableInClassList("settings-on", settings.Muted);
            modeValue.text = Strings.Get(Screen.fullScreenMode == FullScreenMode.Windowed ? "settings.mode.windowed" : "settings.mode.fullscreen");
            resolutionValue.text = new ScreenSize(Screen.width, Screen.height).ToString();
            languageValue.text = Strings.Get("language.self");
        }

        void Navigate(NavigationMoveEvent evt)
        {
            int index = Array.IndexOf(rows, document.rootVisualElement.focusController.focusedElement as VisualElement);
            // Tab and Shift+Tab move between rows like Down and Up; only Left and Right change a value.
            var direction = evt.direction == NavigationMoveEvent.Direction.Next ? NavigationMoveEvent.Direction.Down
                : evt.direction == NavigationMoveEvent.Direction.Previous ? NavigationMoveEvent.Direction.Up : evt.direction;
            if (direction == NavigationMoveEvent.Direction.Up || direction == NavigationMoveEvent.Direction.Down)
            {
                int delta = direction == NavigationMoveEvent.Direction.Up ? -1 : 1;
                rows[(Math.Max(0, index) + delta + rows.Length) % rows.Length].Focus();
            }
            else if (index >= 0 && (direction == NavigationMoveEvent.Direction.Left || direction == NavigationMoveEvent.Direction.Right))
            {
                int delta = direction == NavigationMoveEvent.Direction.Left ? -1 : 1;
                var row = rows[index];
                if (row == musicRow) SettingsStore.Apply(s => s.MusicVolume = Step(s.MusicVolume, delta));
                else if (row == effectsRow) { SettingsStore.Apply(s => s.EffectsVolume = Step(s.EffectsVolume, delta)); ToolkitUI.PlayClick(); }
                else if (row == muteRow) ToggleMute();
                else if (row == modeRow) ToggleMode();
                else if (row == resolutionRow) StepResolution(delta);
                else if (row == languageRow) ToggleLanguage();
            }
            evt.PreventDefault();
            evt.StopPropagation();
        }

        static float Step(float volume, int delta) => Mathf.Clamp01(Mathf.Round(volume * 100 + delta * VolumeStep) / 100f);

        void Submit(NavigationSubmitEvent evt)
        {
            var focused = document.rootVisualElement.focusController.focusedElement as VisualElement;
            if (focused == muteRow) ToggleMute();
            else if (focused == modeRow) ToggleMode();
            else if (focused == languageRow) ToggleLanguage();
            else return;
            evt.StopPropagation();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;
            Close();
            evt.StopPropagation();
        }

        void ToggleMute() => SettingsStore.Apply(s => s.Muted = !s.Muted);

        void ToggleLanguage() => Localization.Choose(Strings.Current == Language.Korean ? Language.English : Language.Korean);

        void ToggleMode()
        {
            Screen.fullScreenMode = Screen.fullScreenMode == FullScreenMode.Windowed ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            screen.schedule.Execute(Refresh).ExecuteLater(150);
        }

        void StepResolution(int delta)
        {
            var available = new List<ScreenSize>();
            foreach (var resolution in Screen.resolutions) available.Add(new ScreenSize(resolution.width, resolution.height));
            var monitor = Screen.mainWindowDisplayInfo;
            var current = new ScreenSize(Screen.width, Screen.height);
            var choices = ScreenSizes.Choices(available, new ScreenSize(monitor.width, monitor.height), current);
            int index = Mathf.Clamp(choices.IndexOf(current) + delta, 0, choices.Count - 1);
            var target = choices[index];
            if (target.Equals(current)) return;
            Screen.SetResolution(target.Width, target.Height, Screen.fullScreenMode);
            screen.schedule.Execute(Refresh).ExecuteLater(150);
        }

        T Q<T>(string name) where T : VisualElement => ToolkitUI.Q<T>(document.rootVisualElement, name);
    }
}
