#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CottonCircuit.Tests
{
    public partial class RuntimeSmoke
    {
        IEnumerator ClickProgression(string name)
        {
            var target = GameObject.Find(name); Check(target != null, name + " exists");
            var button = target.GetComponent<Button>(); Check(button != null && button.interactable, name + " enabled");
            var data = PointerAtSource(target); var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            bool receivesPointer=hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == target;
            if (!receivesPointer)
            {
                var graphic=button.targetGraphic; var rect=target.GetComponent<RectTransform>();
                var corners=new Vector3[4]; rect.GetWorldCorners(corners);
                string detail="PROGRESSION_POINTER_DIAGNOSTIC target="+name+" frame="+Time.frameCount+" pointer="+data.position+
                    " active="+target.activeInHierarchy+" worldRect="+corners[0]+".."+corners[2]+
                    " graphic="+(graphic ? graphic.name : "none")+" depth="+(graphic ? graphic.depth.ToString() : "none")+
                    " cull="+(graphic && graphic.canvasRenderer.cull)+" raycast="+(graphic && graphic.raycastTarget)+
                    " canvas="+(graphic && graphic.canvas ? graphic.canvas.name+"/"+graphic.canvas.renderMode : "none");
                for(int i=0;i<Math.Min(5,hits.Count);i++) detail+=" hit"+i+"="+hits[i].gameObject.name+"/depth"+hits[i].depth;
                Debug.Log(detail);
            }
            Check(receivesPointer,
                name + " receives visible pointer (" + (hits.Count == 0 ? "none" : hits[0].gameObject.name) + ")");
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler); game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            // A physical follow-up click happens after the newly enabled tab has rendered.
            // Canvas.ForceUpdateCanvases alone does not assign every reactivated Graphic's render depth.
            yield return null;
        }
        IEnumerator ProgressionScenario()
        {
            Application.targetFrameRate = 60; Screen.SetResolution(1600,900,false);
            yield return new WaitForSecondsRealtime(.5f);
            var e = game.Session.Economy;
            Check(game.InPreparation && !game.Shift.IsOpen && e.Business.RemainingSeconds == 180, "fresh preparation, 180 seconds");
            Check(SaveStore.Valid(e) && game.Store.CanSave, "fresh preparation saves");
            Check(e.Progression.CartStyle == 1 && Progression.Level(e, "coupe") == 0,
                "fresh progression owns the default downhill style without a vehicle unlock");
            CheckDefaultShiftDriving("fresh progression preparation");
            var portraitObject = GameObject.Find("CompanionPortrait");
            var portrait = portraitObject ? portraitObject.GetComponent<Image>() : null;
            Check(portrait && portrait.sprite && portrait.sprite.name == "Mina_Portrait", "companion portrait shows the Mina_Portrait sprite");
            CaptureShift("01-preparation.png");
            var graphMap = GameObject.Find("TraitMap").GetComponent<RectTransform>();
            float overviewScale = graphMap.localScale.x;
            yield return ClickProgression("ZoomGraphIn");
            Check(graphMap.localScale.x > overviewScale, "graph zoom enlarges the dependency neighborhood");
            var graphScroll = GameObject.Find("UpgradeGraphScroll").GetComponent<UpgradeGraphScroll>();
            var graphPointer = PointerAtSource(graphScroll.gameObject); graphPointer.scrollDelta = new Vector2(0,1);
            float buttonZoom = graphMap.localScale.x;
            ExecuteEvents.Execute(graphScroll.gameObject, graphPointer, ExecuteEvents.scrollHandler);
            Check(graphMap.localScale.x > buttonZoom, "mouse wheel zooms the graph");
            Vector2 beforePan = graphScroll.content.anchoredPosition;
            ExecuteEvents.Execute(graphScroll.gameObject, graphPointer, ExecuteEvents.beginDragHandler);
            graphPointer.position += new Vector2(-60,60);
            ExecuteEvents.Execute(graphScroll.gameObject, graphPointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(graphScroll.gameObject, graphPointer, ExecuteEvents.endDragHandler);
            Check(Vector2.Distance(beforePan,graphScroll.content.anchoredPosition)>1,"drag pans the enlarged map");
            yield return ClickProgression("ResetGraphView");
            Check(Math.Abs(graphMap.localScale.x-overviewScale)<.001, "graph overview restores fit without hiding nodes");
            yield return ClickProgression("TraitTab_production");
            Check(GameObject.Find("UpgradeNode_sugar_2") != null && GameObject.Find("UpgradeNode_hours") == null,
                "production tab shows its traits without unrelated business nodes");
            CaptureShift("11-tree-production.png");
            yield return ClickProgression("TraitTab_machines");
            Check(GameObject.Find("UpgradeNode_machine_2") != null && GameObject.Find("UpgradeNode_sugar_2") == null,
                "machine tab separates its upgrade chain from production");
            CaptureShift("12-tree-machines.png");
            int navigationWallet=e.Coins; string navigationPurchases=JsonUtility.ToJson(e.Progression);
            yield return ClickProgression("TraitRequirement_machine_2_sugar_2");
            Check(GameObject.Find("UpgradeNode_sugar_2") != null && GameObject.Find("UpgradeNode_machine_2") == null,
                "external prerequisite opens the tab containing the required trait");
            Check(e.Coins==navigationWallet && JsonUtility.ToJson(e.Progression)==navigationPurchases,
                "following a prerequisite does not spend money or buy traits");
            Check(GameObject.Find("UpgradeNode_sugar_2").transform.Find("NavigationHighlight").gameObject.activeSelf,
                "prerequisite navigation highlights the exact required trait");
            CaptureShift("17-prerequisite-navigation.png");
            yield return ClickProgression("TraitTab_staff");
            Check(GameObject.Find("UpgradeNode_worker_1") != null && GameObject.Find("UpgradeNode_sugar_2") == null,
                "staff tab isolates worker upgrades");
            CaptureShift("13-tree-staff.png");
            yield return ClickProgression("TraitTab_sales");
            Check(GameObject.Find("UpgradeNode_location_1") != null && GameObject.Find("UpgradeNode_worker_1") == null,
                "sales tab groups destinations and sale bonuses");
            CaptureShift("14-tree-sales.png");
            yield return ClickProgression("TraitTab_kart");
            Check(GameObject.Find("UpgradeNode_engine") != null && GameObject.Find("UpgradeNode_location_1") == null,
                "kart tab isolates the independent driving chain");
            CaptureShift("15-tree-kart.png");
            yield return ClickProgression("TraitTab_business");
            Check(GameObject.Find("UpgradeNode_hours") != null && GameObject.Find("UpgradeNode_engine") == null,
                "business tab returns to the initial trait group");
            yield return ClickProgression("TraitTab_machines");
            var hover = GameObject.Find("UpgradeNode_machine_2");
            ExecuteEvents.Execute(hover, PointerAtSource(hover), ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.3f); CaptureShift("02-hover.png");
            Check(GameObject.Find("HoverDetails") != null, "hover details appear");
            HoverHint.HideAll();
            yield return ClickProgression("OpenEquipment");
            Check(!GameObject.Find("SelectCart_0").GetComponent<Button>().interactable,
                "optional classic kart is locked before its trait is purchased");
            int defaultStyleWallet = e.Coins;
            game.ChooseCart(0);
            Check(e.Progression.CartStyle == 1 && e.Coins == defaultStyleWallet,
                "locked classic kart cannot replace the free downhill default");
            yield return ClickProgression("SelectCart_1");
            Check(e.Progression.CartStyle == 1 && e.Coins == defaultStyleWallet,
                "downhill selection is available for free before the optional kart unlock");
            CheckDefaultShiftDriving("fresh progression vehicle selection");
            yield return ClickProgression("OpenLocations"); CaptureShift("03-locations.png");
            yield return ClickProgression("BeginBusiness"); Check(game.InBusiness && game.Shift.IsOpen, "begin button opens shift");
            yield return null;
            CheckDefaultShiftDriving("first progression business");
            CaptureShift("19-default-downhill.png");
            yield return CheckDownhillBoosterUI();
            CheckShakeStrength("1600x900", true);
            Screen.SetResolution(1280,720,false); yield return new WaitForSecondsRealtime(.3f);
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            CheckShakeStrength("1280x720", false);
            Screen.SetResolution(1600,900,false); yield return new WaitForSecondsRealtime(.3f);
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            int initialCoins = e.Coins; PourGesture(0,5);
            Check(e.Coins == initialCoins && e.Business.SugarGrams == 50, "real basic sugar gesture is free");
            int guard=2400;
            while (game.Shift.State.BatchMeters < ShopShift.MetersForSize(0) && guard-- > 0) game.Tick(0,0,false,.05f);
            Check(guard > 0, "auto downhill driving actually produces standard candy");
            Check(game.World.Kart.DriveModel.StoredBoosts == 0 && !game.World.Kart.Boosting,
                "downhill production never inherits kart boosters");
            var p=game.ExtractCandy(); Check(p!=null && ShopShift.SizeOf(p)==0, "first extraction is sellable");
            var c=game.Shift.State.Customers.Find(customer=>!customer.Angry&&!customer.Happy);
            Check(c!=null, "customer waits for first product");
            int starBonus=e.StarBonus(p);
            DragStock(0,"CustomerDropTarget"+c.Slot);
            Check(e.Coins > initialCoins && e.Business.DaySold == 1, "real shelf drag sells once");
            Check(ShopShift.Stars(p.Quality)==3 && starBonus>0 && game.Notice!=null && game.Notice.Contains("★★★ 보너스 +"+starBonus),
                "progression sale notice shows the clean candy's star bonus: "+game.Notice);
            CaptureShift("04-business.png");
            game.Shift.Advance(1000,0); game.UI.Refresh();
            Check(e.Progression.Phase==BusinessPhase.Results, "clock closes shift"); CaptureShift("05-results.png");
            int wallet=e.Coins; game.ReturnToPreparation(); Check(e.Coins==wallet && game.InPreparation,"results return preserves wallet");
            yield return null;
            // Isolated test wallet exercises the entire tree without modifying user saves.
            e.Coins=200000; game.UI.Refresh(); yield return ClickProgression("OpenUpgradeGraph"); yield return null;
            yield return ClickProgression("TraitTab_machines");
            var lockedMachine=GameObject.Find("UpgradeNode_machine_2");
            Check(!lockedMachine.GetComponent<Button>().interactable,
                "missing prerequisite keeps a trait locked even with sufficient money");
            int lockedWallet=e.Coins;
            ExecuteEvents.Execute(lockedMachine,PointerAtSource(lockedMachine),ExecuteEvents.pointerClickHandler);
            Check(e.Coins==lockedWallet && Progression.Level(e,"machine_2")==0,
                "clicking a locked trait cannot spend money or bypass its prerequisite");
            yield return ClickProgression("TraitRequirement_machine_2_sugar_2");
            Check(e.Coins==lockedWallet && Progression.Level(e,"sugar_2")==0,
                "prerequisite navigation stays free when the prerequisite is affordable");
            yield return ClickProgression("UpgradeNode_sugar_2");
            Check(Progression.Level(e,"sugar_2")==1 && e.Coins==lockedWallet-Progression.Find("sugar_2").BaseCost,
                "required trait is bought explicitly after navigating to it");
            yield return ClickProgression("TraitTab_machines");
            Check(GameObject.Find("UpgradeNode_machine_2").GetComponent<Button>().interactable,
                "buying the prerequisite unlocks the dependent trait on its own tab");
            yield return ClickProgression("TraitTab_business");
            int before=e.Coins; yield return ClickProgression("UpgradeNode_hours");
            Check(Progression.Level(e,"hours")==1 && e.Coins<before,"graph click purchases a rank");
            for(int pass=0;pass<10;pass++) foreach(var node in Progression.Nodes) while(Progression.CanBuy(e,node.Id)) game.PurchaseNode(node.Id);
            Check(Progression.OwnedMachines(e)==3 && Progression.WorkerCount(e)==2,"graph grants fleet and workers");
            yield return ClickProgression("OpenEquipment"); yield return null;
            Check(Progression.Level(e,"coupe") == 1, "vehicle trait unlocks the optional classic kart");
            yield return ClickProgression("SelectCart_0");
            Check(e.Progression.CartStyle == 0 && game.PreparedStyle == DrivingStyle.Kart &&
                game.RunStyle == DrivingStyle.Kart && game.World.Kart.DriveModel.Style == DrivingStyle.Kart,
                "purchased classic kart remains selectable as an optional driving style");
            Check(game.World.Kart.transform.GetChild(0).gameObject.activeInHierarchy &&
                !game.World.Kart.transform.Find("Downhill coupe visual").gameObject.activeInHierarchy,
                "optional classic kart selection swaps the vehicle visual");
            yield return ClickProgression("SelectCart_1");
            Check(e.Progression.CartStyle == 1, "downhill can be reselected after unlocking the classic kart");
            CheckDefaultShiftDriving("reselected progression downhill", true);
            Check(GameObject.Find("RecipeFlavor_1")==null && GameObject.Find("RecipeSize_1")==null,
                "worker recipe controls stay hidden until a worker is assigned");
            yield return ClickProgression("AssignWorker_1"); yield return null;
            yield return ClickProgression("RecipeFlavor_1"); yield return ClickProgression("RecipeSize_1");
            yield return ClickProgression("SelectMachine_2");
            Check(game.Machine(1).WorkerAssigned && game.Machine(1).RecipeFlavor==1 && game.Machine(1).RecipeSize==1,"equipment controls configure worker recipe");
            Check(GameObject.Find("RecipeFlavor_0")==null && GameObject.Find("RecipeFlavor_2")==null,
                "machines without a worker hide the worker recipe");
            for (int i = 0; i < 3; i++)
                Check(!GameObject.Find("RecipeGrade_" + i).GetComponent<Button>().interactable &&
                    game.Machine(i).SugarGrade == i + 1 &&
                    GameObject.Find("RecipeGrade_" + i).GetComponentInChildren<Text>().text == (i + 1) + "등급 · 자동",
                    "machine " + i + " shows its automatic sugar grade instead of a selector");
            CaptureShift("06-equipment.png");
            yield return ClickProgression("OpenLocations"); yield return null;
            yield return ClickProgression("PreviewLocation_3"); yield return ClickProgression("SelectLocation");
            CaptureShift("07-starlight.png"); yield return ClickProgression("BeginBusiness"); yield return null;
            Check(e.Day==2 && e.Business.RemainingSeconds==Progression.DaySeconds(e),"next day applies duration and clears shelf");
            Check(game.World.Kart.DriveModel.Course==RaceCourse.ForMap(2),"third machine opens third physical map");
            CheckDefaultShiftDriving("third machine business", true);
            PourGesture(2,2); game.Tick(0,0,false,2); double batch=e.Business.BatchMeters;
            var drive=game.World.Kart.DriveModel;
            game.ChooseMachine(0); game.Tick(0,0,false,1); game.ChooseMachine(2);
            Check(object.ReferenceEquals(drive,game.World.Kart.DriveModel) && Math.Abs(batch-e.Business.BatchMeters)<1e-7,"switch restores track and candy");
            CheckDefaultShiftDriving("return to third machine", true);
            game.TogglePause(); double clock=e.Business.RemainingSeconds; game.Tick(0,0,false,10);
            Check(e.Business.RemainingSeconds==clock,"pause freezes all machines"); game.TogglePause();
            game.Tick(0,0,false,100); Check(e.Inventory.Count>0,"assigned worker creates shelf products while player drives");
            Check(SaveStore.Valid(e),"operating fleet state validates");
            var loaded=new SaveStore(saveDirectory).Load();
            Check(loaded.Progression!=null && loaded.Progression.Phase==BusinessPhase.Operating && loaded.Business.Machines[1].WorkerAssigned,"operating save retains fleet and phase");
            Check(loaded.Progression.CartStyle == 1, "operating save retains the selected downhill style");
            game.TogglePause(); game.TogglePause(); double savedSeconds=e.Business.RemainingSeconds, savedBatch=e.Business.BatchMeters;
            game.Initialize(saveDirectory); e=game.Session.Economy;
            Check(game.InBusiness && e.Progression.SelectedMachine==2 && Math.Abs(e.Business.RemainingSeconds-savedSeconds)<1e-7 &&
                Math.Abs(e.Business.BatchMeters-savedBatch)<1e-7 && game.Machine(1).WorkerAssigned,
                "controller reload resumes selected machine, ingredients, workers and clock; phase=" + e.Progression.Phase +
                " machine=" + e.Progression.SelectedMachine + " clock=" + e.Business.RemainingSeconds.ToString("R") + "/" + savedSeconds.ToString("R") +
                " batch=" + e.Business.BatchMeters.ToString("R") + "/" + savedBatch.ToString("R") + " worker=" + game.Machine(1).WorkerAssigned);
            CheckDefaultShiftDriving("reloaded operating progression");
            CaptureShift("08-third-map.png");
            game.Shift.Advance(1000,0); game.ReturnToPreparation();
            game.Initialize(saveDirectory); e=game.Session.Economy;
            Check(game.InPreparation && e.Inventory.Count==0 && e.Business.Machines.TrueForAll(m=>m.BatchMeters==0&&m.SugarGrams==0),"reload preparation has no stale stock");
            Check(e.Progression.CartStyle == 1, "returning to preparation preserves the downhill selection");
            CheckDefaultShiftDriving("reloaded progression preparation");
            Screen.SetResolution(1280,720,false); yield return new WaitForSecondsRealtime(.3f);
            yield return ClickProgression("OpenUpgradeGraph"); yield return null; CaptureShift("09-graph-1280.png");
            yield return ClickProgression("TraitTab_sales"); yield return null; CaptureShift("16-tree-sales-1280.png");
            var completedTrait=GameObject.Find("UpgradeNode_location_3");
            ExecuteEvents.Execute(completedTrait,PointerAtSource(completedTrait),ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.3f);
            var completedDetails=GameObject.Find("HoverDetails"); Check(completedDetails!=null,"completed trait still exposes hover details");
            string completedText=completedDetails.GetComponentInChildren<Text>().text;
            foreach(string parent in Progression.Find("location_3").Parents)
                Check(completedText.Contains("✓ "+Progression.Find(parent).Name),"completed trait retains prerequisite: "+parent);
            CaptureShift("18-completed-trait-hover-1280.png"); HoverHint.HideAll();
            yield return ClickProgression("OpenLocations"); yield return null; CaptureShift("26-locations-1280.png");
            yield return ClickProgression("OpenEquipment"); yield return null; CaptureShift("10-equipment-1280.png");
            Check(SaveStore.Valid(e),"final save remains valid");
            // Exercise the optional kart in a separate day after the original
            // progression scenario so its day counts and production stay intact.
            yield return ClickProgression("SelectCart_0");
            yield return ClickProgression("OpenLocations");
            yield return ClickProgression("BeginBusiness");
            var kartMeter=FindProgressionUI("Drift meter");
            Check(!game.AutoDrive && kartMeter.activeInHierarchy,
                "manual optional kart shows its booster meter");
            yield return ClickProgression("Auto drive toggle");
            Check(game.AutoDrive && !kartMeter.activeInHierarchy,
                "automatic optional kart hides its booster meter");
            yield return ClickProgression("Auto drive toggle");
            Check(!game.AutoDrive && kartMeter.activeInHierarchy,
                "manual optional kart restores its visible booster meter");
            game.TogglePause();
            Check(HasBoosterWords(FindProgressionUI("PauseControls").GetComponent<Text>().text),
                "optional kart help retains its booster control");
            game.TogglePause(); CaptureShift("23-optional-kart-booster.png");
            game.Shift.Advance(1000,0); game.UI.Refresh(); game.ReturnToPreparation();
            yield return null; yield return ClickProgression("OpenEquipment");
            yield return ClickProgression("SelectCart_1");
            CheckDefaultShiftDriving("after optional kart UI regression");
            Check(SaveStore.Valid(e),"booster regression leaves a valid downhill preparation save");
            yield return CheckInspectorSugarShake();
        }

        GameObject FindProgressionUI(string name)
        {
            foreach (var child in game.UI.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
            throw new Exception("RUNTIME_CHECK_FAILED missing UI object: " + name);
        }

        static bool HasBoosterWords(string value)
        {
            return value.IndexOf("boost",StringComparison.OrdinalIgnoreCase)>=0 ||
                value.Contains("부스터") || value.Contains("부스트");
        }

        void CheckVisibleDownhillText(string context)
        {
            foreach (var label in game.UI.GetComponentsInChildren<Text>())
                if (label.gameObject.activeInHierarchy && label.enabled)
                    Check(!HasBoosterWords(label.text),context+" has no visible booster text: "+label.name);
        }

        IEnumerator CheckDownhillBoosterUI()
        {
            var meter=FindProgressionUI("Drift meter");
            Check(!game.AutoDrive && !meter.activeSelf && !meter.activeInHierarchy,
                "manual downhill hides the booster meter");
            CheckVisibleDownhillText("manual downhill");
            CaptureShift("20-manual-downhill-no-booster.png");
            var toggle=GameObject.Find("Auto drive toggle");
            ExecuteEvents.Execute(toggle,PointerAtSource(toggle),ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.3f);
            var tooltip=GameObject.Find("HoverDetails");
            Check(tooltip!=null && !HasBoosterWords(tooltip.GetComponentInChildren<Text>().text),
                "downhill driving hover omits booster controls");
            HoverHint.HideAll();
            game.TogglePause();
            var controls=FindProgressionUI("PauseControls");
            Check(controls.activeInHierarchy && !HasBoosterWords(controls.GetComponent<Text>().text),
                "downhill pause help omits booster controls");
            game.TogglePause();
            // The production checks after this regression use the automatic driver.
            yield return ClickProgression("Auto drive toggle");
            Check(game.AutoDrive && !meter.activeSelf && !meter.activeInHierarchy,
                "automatic downhill hides the booster meter");
            CheckVisibleDownhillText("automatic downhill");
            CheckDefaultShiftDriving("after manual downhill UI regression", true);
        }

        void CheckShakeStrength(string resolution, bool capture)
        {
            var state=game.Shift.State; var economy=game.Session.Economy;
            Check(state.SugarGrams==0 && state.BatchMeters==0,resolution+" strength check starts empty");
            int wallet=economy.Coins;
            var bag=GameObject.Find("SugarBag0").GetComponent<ShopDragItem>();
            float[] amplitudes={8,24,60,132};
            double[] expected={0,1,4,10};
            for(int i=0;i<amplitudes.Length;i++)
            {
                double before=state.SugarGrams;
                var pointer=PointerAtSource(bag.gameObject); CheckRaycast(pointer,bag.gameObject,false);
                ExecuteEvents.Execute(bag.gameObject,pointer,ExecuteEvents.beginDragHandler);
                MoveDrag(bag,pointer,430,340);
                MoveDrag(bag,pointer,430,340+amplitudes[i]);
                MoveDrag(bag,pointer,430,340);
                ExecuteEvents.Execute(bag.gameObject,pointer,ExecuteEvents.endDragHandler);
                double poured=state.SugarGrams-before;
                Check(Math.Abs(poured-expected[i])<.00001,
                    resolution+" "+amplitudes[i]+"px shake pours "+expected[i]+"g, actual="+poured.ToString("R"));
                Check(economy.Coins==wallet,resolution+" basic sugar stays free at every shake strength");
                if(capture && i==1) CaptureShift("21-weak-shake-one-gram.png");
                if(capture && i==3) CaptureShift("22-strong-shake-ten-grams.png");
            }
            Check(state.BatchMeters==0,resolution+" shake input alone cannot grow the candy");
            Check(game.EmptySugar() && state.SugarGrams==0 && state.SugarFlavor==-1,
                resolution+" strength fixture empties through the production action");
        }
    }
}
#endif
