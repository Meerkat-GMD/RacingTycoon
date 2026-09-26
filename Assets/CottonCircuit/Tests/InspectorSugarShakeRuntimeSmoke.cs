#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CottonCircuit.Tests
{
    public partial class RuntimeSmoke
    {
        IEnumerator CheckInspectorSugarShake()
        {
            var field=typeof(GameController).GetField("sugarShakeFullStrokePixels",BindingFlags.Instance|BindingFlags.NonPublic);
            Check(field!=null && field.IsDefined(typeof(SerializeField),false),"shake width is a serialized Inspector field");
            float original=(float)field.GetValue(game);
            try
            {
                // The Editor checks exercise SerializedObject; this player checks the
                // same serialized backing field during actual drag input.
                field.SetValue(game,252f);
                string directory=game.Store.DirectoryPath;
                File.WriteAllText(Path.Combine(directory,"sugar-input-settings.txt"),"24");
                game.Initialize(directory,true,true,true); yield return null;
                Check(game.SugarShakeFullStrokePixels==252,"initialization and obsolete player preferences preserve Inspector value");
                yield return ClickProgression("OpenLocations");
                yield return ClickProgression("BeginBusiness");
                double before=game.Shift.State.SugarGrams;
                PourGesture(0,1);
                Check(Math.Abs(game.Shift.State.SugarGrams-before-5)<.00001,"Inspector252 makes actual132px drag pour5g");
                field.SetValue(game,132f);
                before=game.Shift.State.SugarGrams; PourGesture(0,1);
                Check(Math.Abs(game.Shift.State.SugarGrams-before-10)<.00001,"live Inspector132 restores10g without reinitializing game");
                Check(game.EmptySugar(),"Inspector fixture empties the current tank");

                var bag=GameObject.Find("SugarBag0").GetComponent<ShopDragItem>();
                var pointer=PointerAtSource(bag.gameObject); CheckRaycast(pointer,bag.gameObject,false);
                ExecuteEvents.Execute(bag.gameObject,pointer,ExecuteEvents.beginDragHandler);
                MoveDrag(bag,pointer,430,340); MoveDrag(bag,pointer,430,472);
                field.SetValue(game,252f);
                MoveDrag(bag,pointer,430,340);
                Check(game.Shift.State.SugarGrams==0,"editing width discards the previous unfinished stroke");
                MoveDrag(bag,pointer,430,472); MoveDrag(bag,pointer,430,340);
                ExecuteEvents.Execute(bag.gameObject,pointer,ExecuteEvents.endDragHandler);
                Check(Math.Abs(game.Shift.State.SugarGrams-5)<.00001,"next fresh stroke immediately uses edited Inspector width");
                CaptureShift("24-inspector-width-252-five-grams.png");
                game.TogglePause(); yield return null;
                bool playerSettingFound=false;
                foreach(var child in game.UI.GetComponentsInChildren<Transform>(true))
                    if(child.name=="SugarShakeSettings" || child.name=="SugarShakeWidthSlider" || child.name=="SugarShakeWidthInput") playerSettingFound=true;
                Check(!playerSettingFound,"player UI has no Inspector-only shake control");
                var card=FindProgressionUI("Pause card").GetComponent<RectTransform>();
                Check(Math.Abs(card.rect.height-568)<.01,"help panel restored to compact layout");
                CaptureShift("25-help-without-shake-setting.png");
                game.TogglePause();
                game.EmptySugar(); game.Shift.Advance(1000,0); game.UI.Refresh(); game.ReturnToPreparation();
                game.ResetSave();
                Check(game.SugarShakeFullStrokePixels==252,"starting a new shop leaves serialized Inspector tuning intact");
                Check(File.ReadAllText(Path.Combine(directory,"sugar-input-settings.txt"))=="24","obsolete preference file is neither read nor rewritten");
            }
            finally { field.SetValue(game,original); }
            Check(SaveStore.Valid(game.Session.Economy),"Inspector fixture leaves a valid save");
        }
    }
}
#endif
