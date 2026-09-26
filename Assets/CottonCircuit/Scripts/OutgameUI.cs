using System;
using System.Collections.Generic;
using UnityEngine;

namespace CottonCircuit
{
    public partial class GameUI
    {
        static readonly Color PrepPaper = Palette.Hex("F8F4EA"), PrepWhite = Palette.Hex("FFFCF5"), PrepInk = Palette.Hex("263B42"), PrepMuted = Palette.Hex("778780"), PrepPink = Palette.Hex("EBA5B3"), PrepMint = Palette.Hex("B8D9CC");
        GameObject outgame;
        readonly GameObject[] preparationPages = new GameObject[3];
        readonly UnityEngine.UI.Button[] preparationNav = new UnityEngine.UI.Button[3];
        UnityEngine.UI.Text preparationWallet, preparationDay, preparationSpeech, preparationCount, equipmentSummary, locationName, locationDetail, locationIndex;
        UnityEngine.UI.Button locationSelect, businessStart;
        UnityEngine.UI.Text locationSelectText;
        ProgressionArtGraphic locationArt;
        readonly UnityEngine.UI.Button[] locationCards = new UnityEngine.UI.Button[4];
        readonly UnityEngine.UI.Text[] locationCardStates = new UnityEngine.UI.Text[4];
        readonly UnityEngine.UI.Button[] cartChoices = new UnityEngine.UI.Button[2];
        readonly MachineCardView[] machineCards = new MachineCardView[3];
        int preparationPage, locationPreview, outgameFingerprint = int.MinValue;
        bool preparationWasVisible;
        float speechUntil;
        string speechOverride;

        sealed class MachineCardView
        {
            public UnityEngine.UI.Image Card;
            public UnityEngine.UI.Text State, WorkerText, FlavorText, SizeText, GradeText, RecipeHint, FlavorLabel, SizeLabel;
            public UnityEngine.UI.Button Select, Worker, Flavor, Size, Grade;
            public ProgressionArtGraphic Art;
        }

        void BuildOutgame()
        {
            if (outgame) return;
            upgradeViews.Clear(); preparationWasVisible = false;
            outgame = Group(root, "PreparationScreen"); var p = (RectTransform)outgame.transform;
            Box(p, "PreparationPaper", 0, 0, 1600, 900, PrepPaper, false).raycastTarget = true;
            Box(p, "CompanionBackground", 1130, 0, 470, 900, Palette.Hex("EFE9DD"), false);
            Box(p, "PreparationDivider", 1130, 30, 1, 840, Palette.Hex("DEDCD1"), false);
            Label(p, "C O T T O N   C I R C U I T", 44, 28, 460, 23, 12, PrepMuted, FontStyle.Bold);
            Label(p, "내일의 달콤함", 41, 54, 520, 55, 34, PrepInk, FontStyle.Bold);
            preparationDay = Label(p, "DAY 01  /  영업 준비", 715, 37, 363, 24, 13, PrepMuted, FontStyle.Bold, TextAnchor.MiddleRight);
            preparationWallet = Label(p, "80  C", 710, 64, 368, 43, 28, PrepInk, FontStyle.Bold, TextAnchor.MiddleRight);

            for (int i = 0; i < 3; i++) preparationPages[i] = Rect(p, new[] { "UpgradeGraphPage", "EquipmentPage", "LocationsPage" }[i], 30, 126, 1080, 742).gameObject;
            BuildUpgradeGraph((RectTransform)preparationPages[0].transform);
            BuildEquipmentPage((RectTransform)preparationPages[1].transform);
            BuildLocationsPage((RectTransform)preparationPages[2].transform);

            Label(p, "SUGAR & COMPANY", 1160, 34, 330, 23, 13, PrepInk, FontStyle.Bold);
            Label(p, "작은 가게, 커다란 꿈", 1160, 67, 386, 34, 22, PrepInk, FontStyle.Bold);
            var portraitFrame = Box(p, "CompanionPortraitFrame", 1154, 122, 418, 405, PrepWhite);
            var texture = Resources.Load<Texture2D>("Progression/NpcPortrait");
            if (texture)
            {
                var portrait = Rect(portraitFrame.rectTransform, "CompanionPortrait", 7, 7, 404, 391).gameObject.AddComponent<UnityEngine.UI.RawImage>();
                portrait.texture = texture; portrait.raycastTarget = false;
                float ratio = 404f / 391f / ((float)texture.width / texture.height);
                portrait.uvRect = ratio <= 1 ? new Rect((1 - ratio) * .5f, 0, ratio, 1) : new Rect(0, (1 - 1 / ratio) * .5f, 1, 1 / ratio);
            }
            else PrepArt(portraitFrame.rectTransform, "CompanionPortraitFallback", 7, 7, 404, 391, "portrait", PrepPink);
            Label(p, "M I N A   /   가게 친구", 1168, 545, 380, 22, 12, PrepMuted, FontStyle.Bold);
            preparationSpeech = Label(p, "작은 한 걸음부터 시작해 볼까요?", 1168, 578, 372, 67, 21, PrepInk, FontStyle.Bold);
            string[] nav = { "01     성장 지도", "02     기계와 레시피", "03     오늘의 장사" };
            for (int i = 0; i < 3; i++)
            {
                int page = i;
                preparationNav[i] = ButtonAt(p, nav[i], 1158, 664 + i * 61, 410, 51, PrepWhite, PrepInk, () => ShowPreparationPage(page), 18);
                preparationNav[i].name = new[] { "OpenUpgradeGraph", "OpenEquipment", "OpenLocations" }[i];
                var label = preparationNav[i].GetComponentInChildren<UnityEngine.UI.Text>(); label.alignment = TextAnchor.MiddleLeft; label.rectTransform.anchoredPosition = new Vector2(23, 0); label.rectTransform.sizeDelta = new Vector2(368, 51);
            }
            Label(p, "한 바퀴씩, 우리만의 가게로.", 1161, 858, 403, 21, 12, PrepMuted, FontStyle.Normal, TextAnchor.MiddleCenter);
            HoverHint.Configure(root, font, rounded);
            locationPreview = game.HasProgression ? game.Session.Economy.Progression.SelectedLocation : 0;
            ShowPreparationPage(0); outgame.SetActive(false);
        }

        void BuildEquipmentPage(RectTransform p)
        {
            Label(p, "기계와 레시피", 15, 14, 520, 45, 27, PrepInk, FontStyle.Bold);
            equipmentSummary = Label(p, "", 559, 25, 506, 30, 14, PrepMuted, FontStyle.Normal, TextAnchor.MiddleRight);
            for (int i = 0; i < 3; i++)
            {
                int machine = i; float x = 12 + i * 356;
                var view = new MachineCardView(); machineCards[i] = view;
                view.Card = Box(p, "MachineCard_" + i, x, 83, 340, 487, PrepWhite); var card = view.Card.rectTransform;
                Label(card, "MACHINE  /  " + (i + 1).ToString("00"), 22, 21, 296, 22, 11, PrepMuted, FontStyle.Bold);
                view.Art = PrepArt(card, "MachineIllustration", 107, 60, 126, 126, "machine", i == 0 ? PrepPink : i == 1 ? PrepMint : Palette.Hex("D0C5E4"));
                Label(card, Progression.MachineName(i), 19, 196, 302, 36, 21, PrepInk, FontStyle.Bold, TextAnchor.MiddleCenter);
                view.State = Label(card, "", 20, 233, 300, 23, 12, PrepMuted, FontStyle.Normal, TextAnchor.MiddleCenter);
                view.Select = ButtonAt(card, "직접 운전", 24, 274, 140, 39, PrepMint, PrepInk, () => game.ChooseMachine(machine), 14); view.Select.name = "SelectMachine_" + i;
                view.Worker = ButtonAt(card, "알바 배치", 176, 274, 140, 39, PrepPaper, PrepInk, () => game.ToggleWorker(machine), 14); view.Worker.name = "AssignWorker_" + i; view.WorkerText = view.Worker.GetComponentInChildren<UnityEngine.UI.Text>();
                view.Flavor = ButtonAt(card, "딸기", 100, 331, 216, 34, PrepPaper, PrepInk, () => game.CycleRecipeFlavor(machine), 13); view.Flavor.name = "RecipeFlavor_" + i; view.FlavorText = view.Flavor.GetComponentInChildren<UnityEngine.UI.Text>();
                view.Size = ButtonAt(card, "기본", 100, 378, 216, 34, PrepPaper, PrepInk, () => game.CycleRecipeSize(machine), 13); view.Size.name = "RecipeSize_" + i; view.SizeText = view.Size.GetComponentInChildren<UnityEngine.UI.Text>();
                // Sugar grade is automatic: the button only displays it.
                view.Grade = ButtonAt(card, "1등급", 100, 425, 216, 34, PrepPaper, PrepInk, () => { }, 13); view.Grade.name = "RecipeGrade_" + i; view.GradeText = view.Grade.GetComponentInChildren<UnityEngine.UI.Text>();
                view.Grade.interactable = false;
                // Flavor and size are the worker's standing orders, shown only while a worker is assigned here.
                view.FlavorLabel = Label(card, "알바 맛", 19, 332, 80, 30, 13, PrepMuted); view.SizeLabel = Label(card, "알바 크기", 19, 379, 80, 30, 13, PrepMuted); Label(card, "설탕", 25, 426, 68, 30, 13, PrepMuted);
                view.RecipeHint = Label(card, "", 20, 331, 300, 81, 12, PrepMuted, FontStyle.Normal, TextAnchor.MiddleCenter);
                HoverHint.Attach(view.Select.gameObject, () => MachineAvailability(machine), true);
                HoverHint.Attach(view.Worker.gameObject, () => WorkerDetails(machine), true);
                HoverHint.Attach(view.Flavor.gameObject, () => RecipeDetails(machine, 0), true);
                HoverHint.Attach(view.Size.gameObject, () => RecipeDetails(machine, 1), true);
                HoverHint.Attach(view.Grade.gameObject, () => RecipeDetails(machine, 2), true);
            }
            var carts = Box(p, "CartChoice", 12, 593, 1052, 129, PrepWhite);
            Label(carts.rectTransform, "나의 차량", 25, 20, 226, 30, 19, PrepInk, FontStyle.Bold);
            Label(carts.rectTransform, "DRIVING STYLE", 26, 62, 208, 23, 11, PrepMuted, FontStyle.Bold);
            string[] names = { "클래식 카트", "다운힐 쿠페" };
            for (int i = 0; i < 2; i++)
            {
                int choice = i; cartChoices[i] = ButtonAt(carts.rectTransform, names[i], 319 + (1 - i) * 346, 33, 316, 60, PrepPaper, PrepInk, () => game.ChooseCart(choice), 17); cartChoices[i].name = "SelectCart_" + i;
                HoverHint.Attach(cartChoices[i].gameObject, () => choice == 1 ? "기본 주행 · 다운힐 쿠페. 가속을 유지하고 코너 전에 감속해요." : Progression.HasCartStyle(game.Session.Economy, 0) ? "드리프트와 부스터를 사용하는 클래식 카트." : "성장 지도의 차량 탭에서 클래식 카트를 해금하세요.", true);
            }
        }

        void BuildLocationsPage(RectTransform p)
        {
            Label(p, "오늘의 장사", 15, 14, 510, 45, 27, PrepInk, FontStyle.Bold);
            locationIndex = Label(p, "01 / 04", 861, 23, 200, 27, 14, PrepMuted, FontStyle.Bold, TextAnchor.MiddleRight);
            var hero = Box(p, "LocationPreview", 12, 82, 1052, 378, PrepWhite);
            locationArt = PrepArt(hero.rectTransform, "LocationScenery", 12, 12, 588, 354, "scene", PrepPink);
            Label(hero.rectTransform, "TODAY'S DESTINATION", 634, 43, 383, 23, 11, PrepMuted, FontStyle.Bold);
            locationName = Label(hero.rectTransform, "", 633, 89, 381, 63, 29, PrepInk, FontStyle.Bold);
            locationDetail = Label(hero.rectTransform, "", 635, 160, 371, 69, 16, PrepMuted);
            locationSelect = ButtonAt(hero.rectTransform, "이곳에서 장사", 635, 270, 366, 59, PrepMint, PrepInk, () => { game.ChooseLocation(locationPreview); outgameFingerprint = int.MinValue; }, 17); locationSelect.name = "SelectLocation"; locationSelectText = locationSelect.GetComponentInChildren<UnityEngine.UI.Text>();
            HoverHint.Attach(locationSelect.gameObject, () => Progression.HasLocation(game.Session.Economy, locationPreview) ? Progression.LocationDescription(locationPreview) : "성장 지도에서 " + Progression.LocationName(locationPreview) + " 지역을 해금하세요.", true);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                locationCards[i] = ButtonAt(p, "", 12 + i * 265, 480, 257, 88, PrepWhite, PrepInk, () => PreviewLocation(index), 14); locationCards[i].name = "PreviewLocation_" + i;
                Label((RectTransform)locationCards[i].transform, Progression.LocationName(i), 17, 16, 223, 27, 16, PrepInk, FontStyle.Bold);
                locationCardStates[i] = Label((RectTransform)locationCards[i].transform, "", 18, 49, 221, 23, 12, PrepMuted);
            }
            var previous = ButtonAt(p, "<", 13, 589, 63, 44, PrepWhite, PrepInk, () => PreviewLocation((locationPreview + 3) % 4), 22); previous.name = "PreviousLocation";
            var next = ButtonAt(p, ">", 87, 589, 63, 44, PrepWhite, PrepInk, () => PreviewLocation((locationPreview + 1) % 4), 22); next.name = "NextLocation";
            businessStart = ButtonAt(p, "영업 시작", 686, 642, 378, 73, PrepInk, PrepWhite, () => { HoverHint.HideAll(); game.BeginBusiness(); }, 23); businessStart.name = "BeginBusiness";
            HoverHint.Attach(businessStart.gameObject, () => Progression.HasLocation(game.Session.Economy, locationPreview) ? "선택한 지역에서 새 영업을 시작해요.\n진열대와 기계는 비운 상태로 출발합니다." : "이 지역을 해금한 뒤 선택하세요.", true);
            Label(p, "READY WHEN YOU ARE", 17, 671, 520, 28, 12, PrepMuted, FontStyle.Bold);
        }

        void ShowPreparationPage(int page)
        {
            preparationPage = Mathf.Clamp(page, 0, 2); HoverHint.HideAll();
            for (int i = 0; i < preparationPages.Length; i++) if (preparationPages[i]) preparationPages[i].SetActive(i == preparationPage);
            for (int i = 0; i < preparationNav.Length; i++) if (preparationNav[i]) preparationNav[i].image.color = i == preparationPage ? PrepMint : PrepWhite;
            outgameFingerprint = int.MinValue;
            if (pause && outgame.activeInHierarchy) RefreshOutgame();
        }
        void PreviewLocation(int index)
        {
            locationPreview = index; HoverHint.HideAll(); outgameFingerprint = int.MinValue;
            RefreshOutgame();
        }

        bool RefreshOutgame()
        {
            if (!outgame) return false;
            bool visible = game.HasProgression && game.InPreparation;
            outgame.SetActive(visible);
            if (!visible) { if (preparationWasVisible) HoverHint.HideAll(); preparationWasVisible = false; return false; }
            if (!preparationWasVisible) { outgameFingerprint = int.MinValue; locationPreview = game.Session.Economy.Progression.SelectedLocation; }
            preparationWasVisible = true;
            common.SetActive(false); shop.SetActive(false); race.SetActive(false); result.SetActive(false);
            bool openingPause = game.Session.Paused && !pause.activeSelf;
            pause.SetActive(game.Session.Paused);
            if (openingPause) { pause.transform.SetAsLastSibling(); HoverHint.HideAll(); }
            toast.SetActive(!string.IsNullOrEmpty(game.Notice)); notice.text = game.Notice ?? "";
            toast.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, -816); toast.transform.SetAsLastSibling();
            muteLabel.text = game.Audio.Muted ? "소리 꺼짐" : "소리 켜짐";
            var economy = game.Session.Economy;
            preparationWallet.text = economy.Coins.ToString("N0") + "  C"; preparationDay.text = "DAY " + economy.Day.ToString("00") + "  /  영업 준비";
            int fingerprint = PreparationFingerprint(economy);
            if (fingerprint != outgameFingerprint)
            {
                outgameFingerprint = fingerprint; RefreshUpgradeGraph(economy); RefreshEquipmentPage(economy); RefreshLocationPage(economy);
            }
            preparationSpeech.text = Time.unscaledTime < speechUntil ? speechOverride : preparationPage == 1 ? "함께 만들면, 더 달콤해져요." : preparationPage == 2 ? Progression.HasLocation(economy, locationPreview) ? "오늘은 이곳에서 만나 봐요." : "언젠가 이 거리도 우리 무대로!" : economy.Progression.Purchases.Count == 0 ? "작은 한 걸음부터 시작해 볼까요?" : "우리 가게가 조금씩 자라고 있어요.";
            return true;
        }

        int PreparationFingerprint(Economy economy)
        {
            unchecked
            {
                int hash = economy.Coins * 397 + economy.Progression.SelectedMachine * 31 + economy.Progression.CartStyle * 7 + economy.Progression.SelectedLocation * 3 + locationPreview * 17;
                foreach (var purchase in economy.Progression.Purchases) hash = hash * 31 + purchase.Id.GetHashCode() + purchase.Level;
                for (int i = 0; i < 3; i++) { var m = game.Machine(i); if (m != null) hash = hash * 31 + m.RecipeFlavor * 97 + m.RecipeSize * 13 + m.SugarGrade * 7 + (m.WorkerAssigned ? 1 : 0); }
                return hash;
            }
        }
        void RefreshEquipmentPage(Economy economy)
        {
            int owned = Progression.OwnedMachines(economy), workers = Progression.WorkerCount(economy), assigned = 0;
            for (int i = 0; i < 3; i++) if (game.Machine(i) != null && game.Machine(i).WorkerAssigned) assigned++;
            equipmentSummary.text = "기계 " + owned + " / 3    ·    알바 " + assigned + " / " + workers;
            for (int i = 0; i < 3; i++)
            {
                var view = machineCards[i]; var machine = game.Machine(i); bool unlocked = i < owned;
                bool selected = economy.Progression.SelectedMachine == i;
                view.Card.color = unlocked ? PrepWhite : Palette.Hex("ECECE4"); view.Art.canvasRenderer.SetAlpha(unlocked ? 1 : .35f);
                bool sized = Progression.MaxSugarGrade(economy) > 1;
                view.State.text = unlocked ? "TIER " + Progression.MachineTier(i) + (selected ? "  ·  직접 운전" : "  ·  보유") +
                    (sized ? "  ·  최대 " + ShiftTierName(game.Shift.MaxSize(i)) : "") : "성장 지도에서 해금";
                view.Select.interactable = unlocked; view.Select.image.color = selected ? PrepMint : PrepPaper;
                bool workerAssigned = machine != null && machine.WorkerAssigned;
                view.Worker.interactable = unlocked && (workerAssigned || assigned < workers && Progression.WorkerGrade(economy) >= Progression.MachineTier(i));
                view.Worker.image.color = workerAssigned ? PrepPink : PrepPaper; view.WorkerText.text = workerAssigned ? "알바 배치 중" : "알바 배치";
                bool showRecipe = unlocked && workerAssigned;
                view.Flavor.gameObject.SetActive(showRecipe); view.Size.gameObject.SetActive(showRecipe);
                view.FlavorLabel.gameObject.SetActive(showRecipe); view.SizeLabel.gameObject.SetActive(showRecipe);
                view.RecipeHint.gameObject.SetActive(!showRecipe);
                view.RecipeHint.text = !unlocked ? "" : workers == 0 ? "성장 지도에서 알바를 고용하면\n이 기계에 만들 맛과 크기를 맡길 수 있어요."
                    : "알바를 배치하면 알바가 반복해서 만들\n맛과 크기를 정할 수 있어요.";
                view.Flavor.interactable = showRecipe && HasAlternateRecipeFlavor(economy, i);
                view.Size.interactable = showRecipe && game.Shift.MaxSize(i) > 0;
                view.Grade.interactable = false;
                view.FlavorText.text = machine == null ? "딸기" : Palette.FlavorName(machine.RecipeFlavor);
                view.SizeText.text = Progression.MaxSugarGrade(economy) == 1 ? "기본" : machine == null ? "소" : ShiftTierName(machine.RecipeSize);
                view.GradeText.text = (unlocked ? game.Shift.SugarGrade(i) : 1) + "등급 · 자동";
            }
            for (int i = 0; i < 2; i++) { cartChoices[i].interactable = Progression.HasCartStyle(economy, i); cartChoices[i].image.color = economy.Progression.CartStyle == i ? PrepMint : PrepPaper; }
        }
        bool HasAlternateRecipeFlavor(Economy economy, int machine)
        { for (int flavorIndex = 1; flavorIndex < 3; flavorIndex++) if (Progression.HasFlavor(economy, flavorIndex) && Progression.FlavorMachineTier(flavorIndex) <= Progression.MachineTier(machine)) return true; return false; }
        void RefreshLocationPage(Economy economy)
        {
            locationIndex.text = (locationPreview + 1).ToString("00") + " / 04"; locationName.text = Progression.LocationName(locationPreview); locationDetail.text = Progression.LocationDescription(locationPreview);
            locationArt.Variant = locationPreview; locationArt.Accent = locationPreview == 1 ? PrepMint : locationPreview == 2 ? Palette.Hex("C8B6D8") : PrepPink; locationArt.SetVerticesDirty();
            bool available = Progression.HasLocation(economy, locationPreview), chosen = economy.Progression.SelectedLocation == locationPreview;
            locationSelect.interactable = available && !chosen; locationSelectText.text = !available ? "아직 잠겨 있어요" : chosen ? "선택한 장소" : "이곳에서 장사";
            businessStart.interactable = available && chosen;
            for (int i = 0; i < 4; i++) { locationCards[i].image.color = locationPreview == i ? PrepMint : PrepWhite; locationCardStates[i].text = !Progression.HasLocation(economy, i) ? "잠김" : economy.Progression.SelectedLocation == i ? "선택됨" : "영업 가능"; }
        }
        string MachineAvailability(int machine)
        {
            if (machine >= Progression.OwnedMachines(game.Session.Economy)) return "성장 지도에서 " + Progression.MachineName(machine) + " 해금이 필요해요.";
            string note = game.Shift.SizeLimitNote(machine);
            return "영업 중 직접 운전할 기계를 선택해요." + (note.Length > 0 ? "\n크기: " + note : "");
        }
        string RecipeDetails(int machine, int setting)
        {
            var economy = game.Session.Economy;
            if (machine >= Progression.OwnedMachines(economy)) return MachineAvailability(machine);
            if (setting == 0) return HasAlternateRecipeFlavor(economy, machine) ? "알바가 반복해서 만들 맛.\n클릭하면 다음 가능한 맛을 선택해요." : "현재 이 기계에서는 딸기만 만들 수 있어요.\n특별한 맛에는 상위 기계와 맛 해금이 필요해요.";
            if (setting == 1) return "알바가 꺼낼 크기.\n" + game.Shift.SizeLimitNote(machine);
            return "해금한 최고 등급 설탕을 이 기계가 쓸 수 있는 만큼 자동으로 써요.\n" + Progression.MachineName(machine) + "는 " +
                Progression.MachineTier(machine) + "등급 설탕까지 쓸 수 있어요." +
                (Progression.MaxSugarGrade(economy) < 3 ? "\n성장 지도에서 더 좋은 설탕을 해금할 수 있어요." : "");
        }
        string WorkerDetails(int machine)
        {
            var economy = game.Session.Economy; var slot = game.Machine(machine);
            if (machine >= Progression.OwnedMachines(economy)) return "먼저 이 기계를 해금하세요.";
            if (slot != null && slot.WorkerAssigned) return "지정한 레시피를 반복 생산해요. 클릭하면 배치를 해제해요.\n재료비 부족이나 진열대 가득 참에는 기다려요." +
                (economy.Progression.SelectedMachine == machine ? "\n내가 직접 운전하는 동안에는 알바가 쉬어요." : "");
            if (Progression.WorkerCount(economy) == 0) return "성장 지도에서 알바를 고용하세요.";
            if (Progression.WorkerGrade(economy) < Progression.MachineTier(machine)) return "이 기계에는 " + Progression.MachineTier(machine) + "등급 알바가 필요해요.";
            int assigned = 0; for (int i = 0; i < 3; i++) if (game.Machine(i) != null && game.Machine(i).WorkerAssigned) assigned++;
            return assigned >= Progression.WorkerCount(economy) ? "다른 기계의 배치를 해제하거나 알바를 추가 고용하세요." : "이 기계에 알바를 배치해요.\n지정한 레시피를 반복 생산하며 일급은 없어요.";
        }
        ProgressionArtGraphic PrepArt(RectTransform parent, string name, float x, float y, float width, float height, string kind, Color tint)
        {
            var art = Rect(parent, name, x, y, width, height).gameObject.AddComponent<ProgressionArtGraphic>(); art.Kind = kind; art.Accent = tint; art.color = kind == "disc" ? tint : Color.white; art.raycastTarget = false; return art;
        }
        static Color CategoryColor(string category)
        {
            switch (category) { case "business": return Palette.Hex("EBC997"); case "production": return PrepPink; case "equipment": return PrepMint; case "staff": return Palette.Hex("C9BDE0"); case "location": return Palette.Hex("ACD2D9"); case "sales": return Palette.Hex("E2C2AD"); default: return PrepMint; }
        }
    }
}
