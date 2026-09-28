using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    public partial class GameUI
    {
        VisualElement toolkitPreparation;
        readonly List<PreparationTraitBinding> preparationTraits = new List<PreparationTraitBinding>();
        int toolkitPreparationPage, toolkitTraitTab, toolkitLocationPreview, toolkitPreparationFingerprint = int.MinValue;
        bool toolkitPreparationVisible;
        string toolkitHighlightedTrait, toolkitPreparationSpeech;
        float toolkitPreparationSpeechUntil;

        sealed class PreparationTraitBinding
        {
            public UpgradeNode Node;
            public int Tab;
            public VisualElement Card;
            public Button Purchase;
            public Label Rank, Description, Effect, Availability;
        }

        void BindPreparation(VisualElement root)
        {
            var screen = root.Q<VisualElement>("PreparationScreen");
            if (screen == toolkitPreparation) return;
            toolkitPreparation = screen ?? throw new InvalidOperationException("Preparation.uxml must contain PreparationScreen");
            preparationTraits.Clear();
            toolkitPreparationPage = toolkitTraitTab = toolkitLocationPreview = 0;
            toolkitPreparationVisible = false;
            toolkitPreparationFingerprint = int.MinValue;
            var navigation = new[] { "OpenUpgradeGraph", "OpenEquipment", "OpenLocations" };
            for (int i = 0; i < navigation.Length; i++)
            {
                int page = i;
                PreparationElement<Button>(navigation[i]).clicked += () => SelectPreparationPage(page);
            }
            for (int i = 0; i < UpgradeTreeLayout.Tabs.Length; i++)
            {
                int tab = i;
                PreparationElement<Button>("TraitTab_" + UpgradeTreeLayout.Tabs[i].Id).clicked += () => SelectPreparationTraitTab(tab);
            }
            foreach (var node in Progression.Nodes)
            {
                var item = node;
                var binding = new PreparationTraitBinding {
                    Node = item, Tab = UpgradeTreeLayout.TabOf(item.Id),
                    Card = PreparationElement<VisualElement>("TraitCard_" + item.Id),
                    Purchase = PreparationElement<Button>("UpgradeNode_" + item.Id),
                    Rank = PreparationElement<Label>("TraitRank_" + item.Id),
                    Description = PreparationElement<Label>("TraitDescription_" + item.Id),
                    Effect = PreparationElement<Label>("TraitEffect_" + item.Id),
                    Availability = PreparationElement<Label>("TraitAvailability_" + item.Id)
                };
                binding.Purchase.clicked += () => {
                    int before = Progression.Level(game.Session.Economy, item.Id);
                    game.PurchaseNode(item.Id);
                    if (Progression.Level(game.Session.Economy, item.Id) > before)
                    {
                        toolkitPreparationSpeech = item.Name + ", 준비됐어요!";
                        toolkitPreparationSpeechUntil = Time.unscaledTime + 5;
                    }
                    toolkitHighlightedTrait = item.Id;
                    InvalidatePreparation();
                };
                preparationTraits.Add(binding);
                foreach (string prerequisite in item.Parents)
                {
                    string parent = prerequisite;
                    PreparationElement<Button>("TraitRequirement_" + item.Id + "_" + parent).clicked +=
                        () => SelectPreparationTraitTab(UpgradeTreeLayout.TabOf(parent), parent);
                }
                SetArt(PreparationElement<VisualElement>("TraitIcon_" + item.Id), game.World.Assets.TraitIcon(TraitIcons.For(item.Id)));
            }
            for (int i = 0; i < 3; i++)
            {
                int machine = i;
                PreparationElement<Button>("SelectMachine_" + i).clicked += () => { game.ChooseMachine(machine); InvalidatePreparation(); };
                PreparationElement<Button>("AssignWorker_" + i).clicked += () => { game.ToggleWorker(machine); InvalidatePreparation(); };
                PreparationElement<Button>("RecipeFlavor_" + i).clicked += () => { game.CycleRecipeFlavor(machine); InvalidatePreparation(); };
                PreparationElement<Button>("RecipeSize_" + i).clicked += () => { game.CycleRecipeSize(machine); InvalidatePreparation(); };
                SetArt(PreparationElement<VisualElement>("PreparationMachineArt_" + i), game.World.Assets.Machines[i]);
            }
            for (int i = 0; i < 2; i++)
            {
                int cart = i;
                PreparationElement<Button>("SelectCart_" + i).clicked += () => { game.ChooseCart(cart); InvalidatePreparation(); };
            }
            for (int i = 0; i < 4; i++)
            {
                int location = i;
                PreparationElement<Button>("PreviewLocation_" + i).clicked += () => PreviewPreparationLocation(location);
            }
            PreparationElement<Button>("PreviousLocation").clicked += () => PreviewPreparationLocation((toolkitLocationPreview + 3) % 4);
            PreparationElement<Button>("NextLocation").clicked += () => PreviewPreparationLocation((toolkitLocationPreview + 1) % 4);
            PreparationElement<Button>("SelectLocation").clicked += () => { game.ChooseLocation(toolkitLocationPreview); InvalidatePreparation(); };
            PreparationElement<Button>("BeginBusiness").clicked += () => game.BeginBusiness();
            SetArt(PreparationElement<VisualElement>("CompanionPortrait"), game.World.Assets.MinaPortrait);
        }

        bool RefreshPreparation()
        {
            if (toolkitPreparation == null) return false;
            bool visible = game && game.HasProgression && game.InPreparation;
            Show(toolkitPreparation, visible);
            if (!visible) { toolkitPreparationVisible = false; return false; }
            var economy = game.Session.Economy;
            if (!toolkitPreparationVisible)
            {
                toolkitLocationPreview = economy.Progression.SelectedLocation;
                toolkitPreparationFingerprint = int.MinValue;
            }
            toolkitPreparationVisible = true;
            toolkitPreparation.SetEnabled(!game.Session.Paused && !game.GrowthHintVisible);
            PreparationElement<Label>("PreparationWallet").text = economy.Coins.ToString("N0") + " C";
            PreparationElement<Label>("PreparationDay").text = "DAY " + economy.Day.ToString("00") + "  /  영업 준비";
            string[] pageNames = { "UpgradeGraphPage", "EquipmentPage", "LocationsPage" };
            string[] navigation = { "OpenUpgradeGraph", "OpenEquipment", "OpenLocations" };
            for (int i = 0; i < pageNames.Length; i++)
            {
                Show(PreparationElement<VisualElement>(pageNames[i]), toolkitPreparationPage == i);
                PreparationElement<Button>(navigation[i]).EnableInClassList("prep-selected", toolkitPreparationPage == i);
            }
            int fingerprint = PreparationDataFingerprint(economy);
            if (fingerprint != toolkitPreparationFingerprint)
            {
                toolkitPreparationFingerprint = fingerprint;
                RefreshPreparationTraits(economy);
                RefreshPreparationEquipment(economy);
                RefreshPreparationLocations(economy);
            }
            PreparationElement<Label>("PreparationSpeech").text = Time.unscaledTime < toolkitPreparationSpeechUntil
                ? toolkitPreparationSpeech : toolkitPreparationPage == 1 ? "함께 만들면, 더 달콤해져요."
                : toolkitPreparationPage == 2 ? Progression.HasLocation(economy, toolkitLocationPreview)
                    ? "오늘은 이곳에서 만나 봐요." : "언젠가 이 거리도 우리 무대로!"
                : economy.Progression.Purchases.Count == 0 ? "작은 한 걸음부터 시작해 볼까요?" : "우리 가게가 조금씩 자라고 있어요.";
            return true;
        }

        void RefreshPreparationTraits(Economy economy)
        {
            int bought = 0, total = 0;
            foreach (var view in preparationTraits)
            {
                var node = view.Node;
                int rank = Progression.Level(economy, node.Id), cost = Progression.Cost(economy, node.Id);
                bool complete = rank >= node.MaxLevel, requirementsMet = true;
                foreach (string parent in node.Parents)
                {
                    bool met = Progression.Level(economy, parent) > 0;
                    requirementsMet &= met;
                    var requirement = PreparationElement<Button>("TraitRequirement_" + node.Id + "_" + parent);
                    requirement.text = (met ? "✓ " : "○ ") + Progression.Find(parent).Name + "  →";
                    requirement.EnableInClassList("prep-requirement-met", met);
                }
                Show(view.Card, view.Tab == toolkitTraitTab);
                if (view.Tab == toolkitTraitTab) { total++; if (rank > 0) bought++; }
                view.Card.EnableInClassList("prep-trait-complete", complete);
                view.Card.EnableInClassList("prep-trait-highlight", toolkitHighlightedTrait == node.Id);
                view.Card.EnableInClassList("prep-trait-locked", !requirementsMet);
                view.Rank.text = rank + " / " + node.MaxLevel;
                view.Description.text = node.Description;
                view.Effect.text = Progression.EffectSummary(economy, node.Id);
                Show(view.Effect, view.Effect.text != node.Description);
                view.Availability.text = complete ? "모든 단계 완료" : !requirementsMet ? "필요 특성을 먼저 해금하세요."
                    : economy.Coins < cost ? (cost - economy.Coins).ToString("N0") + " C 더 모으면 해금할 수 있어요." : "다음 단계로 성장할 준비가 됐어요.";
                view.Purchase.text = complete ? "완료" : cost.ToString("N0") + " C  ·  " + (rank == 0 ? "해금" : "성장");
                view.Purchase.SetEnabled(Progression.CanBuy(economy, node.Id));
                view.Purchase.tooltip = node.Name + "\n" + node.Description + "\n" + view.Effect.text;
            }
            for (int i = 0; i < UpgradeTreeLayout.Tabs.Length; i++)
                PreparationElement<Button>("TraitTab_" + UpgradeTreeLayout.Tabs[i].Id).EnableInClassList("prep-selected", toolkitTraitTab == i);
            PreparationElement<Label>("PreparationTraitCount").text = bought + " / " + total + " 해금";
            PreparationElement<Label>("PreparationCategoryTitle").text = UpgradeTreeLayout.Tabs[toolkitTraitTab].Name;
        }

        void RefreshPreparationEquipment(Economy economy)
        {
            int owned = Progression.OwnedMachines(economy), workers = Progression.WorkerCount(economy), assigned = 0;
            for (int i = 0; i < 3; i++) if (game.Machine(i)?.WorkerAssigned == true) assigned++;
            PreparationElement<Label>("EquipmentSummary").text = "기계 " + owned + " / 3  ·  알바 " + assigned + " / " + workers;
            for (int i = 0; i < 3; i++)
            {
                bool unlocked = i < owned, selected = economy.Progression.SelectedMachine == i;
                var machine = game.Machine(i);
                bool worker = unlocked && machine != null && machine.WorkerAssigned;
                var card = PreparationElement<VisualElement>("MachineCard_" + i);
                card.EnableInClassList("prep-machine-locked", !unlocked);
                card.EnableInClassList("prep-machine-selected", selected);
                PreparationElement<Label>("PreparationMachineName_" + i).text = Progression.MachineName(i);
                PreparationElement<Label>("PreparationMachineState_" + i).text = !unlocked ? "성장 지도에서 해금" : "TIER " + Progression.MachineTier(i) +
                    (worker ? "  ·  알바 배치" : selected ? "  ·  직접 운전" : "  ·  보유");
                var select = PreparationElement<Button>("SelectMachine_" + i);
                select.text = worker ? "기계 보기" : selected ? "선택한 기계" : "직접 운전";
                select.SetEnabled(unlocked);
                var assignment = PreparationElement<Button>("AssignWorker_" + i);
                assignment.text = worker ? "알바 배치 해제" : "알바 배치";
                assignment.EnableInClassList("prep-worker-assigned", worker);
                assignment.SetEnabled(unlocked && (worker || assigned < workers && Progression.WorkerGrade(economy) >= Progression.MachineTier(i)));
                assignment.tooltip = worker ? "알바가 운전과 제작을 맡아요. 배치를 해제하면 직접 운전할 수 있어요."
                    : workers == 0 ? "성장 지도에서 먼저 알바를 고용하세요."
                    : Progression.WorkerGrade(economy) < Progression.MachineTier(i) ? "이 기계에는 " + Progression.MachineTier(i) + "등급 알바가 필요해요."
                    : assigned >= workers ? "다른 기계의 배치를 해제하거나 알바를 추가로 고용하세요." : "이 기계의 운전과 반복 생산을 알바에게 맡겨요.";
                Show(PreparationElement<VisualElement>("WorkerRecipe_" + i), worker);
                Show(PreparationElement<Label>("PreparationRecipeHint_" + i), !worker);
                PreparationElement<Label>("PreparationRecipeHint_" + i).text = !unlocked ? "기계를 해금하면 더 많은 맛과 크기를 만들 수 있어요."
                    : "알바를 배치하면 반복해서 만들 맛과 크기를 정할 수 있어요.";
                var flavor = PreparationElement<Button>("RecipeFlavor_" + i);
                flavor.text = machine == null ? "딸기" : Palette.FlavorName(machine.RecipeFlavor);
                int flavors = 0;
                for (int f = 0; f < 3; f++) if (unlocked && game.Shift.CanMakeFlavor(i, f)) flavors++;
                flavor.SetEnabled(worker && flavors > 1);
                var size = PreparationElement<Button>("RecipeSize_" + i);
                size.text = Progression.MaxSugarGrade(economy) == 1 ? "기본" : PreparationSizeName(machine == null ? 0 : machine.RecipeSize);
                size.SetEnabled(worker && game.Shift.MaxSize(i) > 0);
                PreparationElement<Label>("RecipeGrade_" + i).text = (unlocked ? game.Shift.SugarGrade(i) : 1) + "등급 · 자동";
                PreparationElement<Label>("PreparationSizeLimit_" + i).text = unlocked ? game.Shift.SizeLimitNote(i) : "";
            }
            for (int i = 0; i < 2; i++)
            {
                var choice = PreparationElement<Button>("SelectCart_" + i);
                choice.SetEnabled(Progression.HasCartStyle(economy, i));
                choice.EnableInClassList("prep-selected", economy.Progression.CartStyle == i);
            }
            PreparationElement<Label>("PreparationCartHint").text = Progression.HasCartStyle(economy, 0)
                ? "다운힐 쿠페는 가속 유지와 감속, 클래식 카트는 드리프트와 부스터로 달려요."
                : "다운힐 쿠페는 처음부터 사용할 수 있어요. 성장 지도의 차량 탭에서 클래식 카트를 해금해 보세요.";
        }

        void RefreshPreparationLocations(Economy economy)
        {
            bool available = Progression.HasLocation(economy, toolkitLocationPreview), chosen = economy.Progression.SelectedLocation == toolkitLocationPreview;
            PreparationElement<Label>("PreparationLocationIndex").text = (toolkitLocationPreview + 1).ToString("00") + " / 04";
            PreparationElement<Label>("PreparationLocationName").text = Progression.LocationName(toolkitLocationPreview);
            PreparationElement<Label>("PreparationLocationDetail").text = Progression.LocationDescription(toolkitLocationPreview);
            SetArt(PreparationElement<VisualElement>("LocationScenery"), game.World.Assets.LocationPrep[toolkitLocationPreview]);
            var select = PreparationElement<Button>("SelectLocation");
            select.text = !available ? "아직 잠겨 있어요" : chosen ? "선택한 장소" : "이곳에서 장사";
            select.SetEnabled(available && !chosen);
            PreparationElement<Button>("BeginBusiness").SetEnabled(available && chosen);
            for (int i = 0; i < 4; i++)
            {
                PreparationElement<Button>("PreviewLocation_" + i).EnableInClassList("prep-selected", toolkitLocationPreview == i);
                PreparationElement<Label>("PreparationLocationState_" + i).text = !Progression.HasLocation(economy, i) ? "잠김"
                    : economy.Progression.SelectedLocation == i ? "선택됨" : "영업 가능";
            }
        }

        void SelectPreparationPage(int page) { toolkitPreparationPage = Mathf.Clamp(page, 0, 2); InvalidatePreparation(); }
        void PreviewPreparationLocation(int location) { toolkitLocationPreview = Mathf.Clamp(location, 0, 3); InvalidatePreparation(); }
        void SelectPreparationTraitTab(int tab, string target = null)
        {
            toolkitTraitTab = Mathf.Clamp(tab, 0, UpgradeTreeLayout.Tabs.Length - 1);
            toolkitPreparationPage = 0;
            toolkitHighlightedTrait = target;
            InvalidatePreparation();
            var scroll = PreparationElement<ScrollView>("UpgradeGraphScroll");
            if (target == null) scroll.scrollOffset = Vector2.zero;
            else scroll.schedule.Execute(() => scroll.ScrollTo(PreparationElement<VisualElement>("TraitCard_" + target)));
        }
        void InvalidatePreparation() { toolkitPreparationFingerprint = int.MinValue; RefreshPreparation(); }
        T PreparationElement<T>(string name) where T : VisualElement
            => toolkitPreparation.Q<T>(name) ?? throw new InvalidOperationException("Preparation.uxml is missing " + name);
        static string PreparationSizeName(int size) => size == 0 ? "소" : size == 1 ? "중" : "대";
        int PreparationDataFingerprint(Economy economy)
        {
            unchecked
            {
                int hash = economy.Coins * 397 + economy.Progression.SelectedMachine * 31 + economy.Progression.CartStyle * 7 +
                    economy.Progression.SelectedLocation * 3 + toolkitLocationPreview * 17 + toolkitTraitTab * 53;
                foreach (var purchase in economy.Progression.Purchases) hash = hash * 31 + purchase.Id.GetHashCode() + purchase.Level;
                for (int i = 0; i < 3; i++)
                {
                    var machine = game.Machine(i);
                    if (machine != null) hash = hash * 31 + machine.RecipeFlavor * 97 + machine.RecipeSize * 13 + machine.SugarGrade * 7 + (machine.WorkerAssigned ? 1 : 0);
                }
                return hash;
            }
        }
    }
}
