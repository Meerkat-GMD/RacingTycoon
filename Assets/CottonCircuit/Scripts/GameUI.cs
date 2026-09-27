using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CottonCircuit
{
    public partial class GameUI : MonoBehaviour
    {
        GameController game;
        Font font;
        Sprite rounded;
        RectTransform root;
        GameObject shop, race, result, pause, toast, common;
        Text driftLabel, raceEvent, bestLap, mapTitle, raceQuality, resultHeading, raceControls, pauseControls, commonControls; Image driftFill; RaceMapGraphic map;
        Text coins, day, timer, grams, speed, laps, flavor, resultTitle, resultWeight, resultPrice, resultDetail, notice, status, muteLabel;
        Image productionFill;
        SpeedLinesGraphic speedLines;
        Text[] upgradeTexts = new Text[3];
        Button[] upgradeButtons = new Button[3];
        Button startButton;
        float resetConfirmUntil;
        bool shiftLayout;
        public void Initialize(GameController controller)
        {
            game = controller;
            UiArt.Use(game.World.Assets);
            if (root)
            {
                if (splitLayout == game.ContinuousMode && shiftLayout == (game.Shift != null)) { Refresh(); return; }
                root.parent.gameObject.SetActive(false); Destroy(root.parent.gameObject); root = null;
            }
            splitLayout = game.ContinuousMode;
            shiftLayout = game.Shift != null;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
            rounded = RoundSprite();
            var canvasObject = new GameObject("Cotton Circuit UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = new GameObject("Centered game composition", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasObject.transform, false); root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
            root.sizeDelta = new Vector2(1600, 900); root.anchoredPosition = Vector2.zero;
            if (!FindAnyObjectByType<EventSystem>()) new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            FindAnyObjectByType<EventSystem>().sendNavigationEvents = !splitLayout;
            common = Group(root, "Shop and result chrome"); var c = common.GetComponent<RectTransform>();
            if (splitLayout) BuildSplitChrome(c);
            else
            {
            Box(c, "Shop backdrop", 1208, 0, 392, 900, Palette.Cream, false);
            Box(c, "Divider", 1208, 0, 2, 900, Palette.Hex("E5DCCC"), false);
            Label(c, "C O T T O N   C I R C U I T", 44, 31, 660, 30, 15, Palette.Ink, FontStyle.Bold);
            Label(c, "솜사탕 서킷", 42, 62, 650, 58, 40, Palette.Ink, FontStyle.Bold);
            Label(c, "한 바퀴씩, 달콤한 꿈을 감아요.", 46, 124, 650, 35, 17, Palette.Muted);
            var badge = Box(c, "Day badge", 46, 177, 155, 38, Color.white);
            day = Label(badge.rectTransform, "DAY 01", 0, 0, 155, 38, 15, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            ButtonAt(c, "도움말  Esc", 1040, 42, 132, 43, Color.white, Palette.Ink, () => game.TogglePause(), 15);
            var wallet = Box(c, "Wallet", 1236, 30, 335, 78, Palette.Ink);
            Label(wallet.rectTransform, "우리 가게의 수익", 20, 11, 190, 24, 13, Palette.Cream);
            coins = Label(wallet.rectTransform, "80", 20, 31, 225, 40, 28, Color.white, FontStyle.Bold);
            Label(wallet.rectTransform, "COINS", 240, 36, 75, 25, 13, Palette.Yellow, FontStyle.Bold, TextAnchor.MiddleRight);
            var bottom = Box(c, "Controls strip", 38, 840, 1132, 40, new Color(1, .98f, .93f, .95f));
            commonControls = Label(bottom.rectTransform, "", 14, 0, 1104, 40, 15, Palette.Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            }
            shop = Group(root, "Shop panel"); race = Group(root, "Race panel"); result = Group(root, "Results panel");
            BuildShop(); BuildRace(); BuildResult();
            toast = Box(root, "Toast", 278, 772, 660, 48, Palette.Ink).gameObject;
            notice = Label(toast.GetComponent<RectTransform>(), "", 18, 0, 624, 48, 16, Color.white, FontStyle.Normal, TextAnchor.MiddleCenter);
            BuildOutgame();
            BuildPause();
            Refresh();
        }
        void BuildRace()
        {
            if (splitLayout) { BuildSplitRace(); return; }
            var p = race.GetComponent<RectTransform>();
            speedLines = Rect(p, "Speed streaks", 0, 0, 1600, 900).gameObject.AddComponent<SpeedLinesGraphic>();
            speedLines.raycastTarget = false;
            var mapCard = Box(p, "Course map", 32, 32, 240, 232, new Color(1, .97f, .90f, .93f));
            mapTitle = Label(mapCard.rectTransform, "SUGARWAY  /  01", 17, 10, 206, 28, 14, Palette.Ink, FontStyle.Bold);
            var mapRect = Rect(mapCard.rectTransform, "Live course", 12, 47, 216, 173); mapRect.pivot = Vector2.zero; mapRect.anchoredPosition = new Vector2(12, -220);
            map = mapRect.gameObject.AddComponent<RaceMapGraphic>(); map.raycastTarget = false; map.Kart = game.World.Kart;
            var lapCard = Box(p, "Lap times", 290, 32, 240, 130, Palette.Ink);
            laps = Label(lapCard.rectTransform, "LAP 01", 18, 11, 202, 41, 28, Color.white, FontStyle.Bold);
            bestLap = Label(lapCard.rectTransform, "첫 랩 기록에 도전!", 18, 61, 207, 56, 15, Palette.Cream);
            var clock = Box(p, "Race clock", 686, 28, 226, 108, Palette.Yellow);
            Label(clock.rectTransform, "주행 시간", 0, 7, 226, 25, 13, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            timer = Label(clock.rectTransform, "60", 0, 31, 226, 68, 48, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            var product = Box(p, "Live cotton", 1250, 32, 318, 240, new Color(1, .97f, .90f, .94f));
            Label(product.rectTransform, "설정한 맛으로 감고 있어요", 17, 10, 284, 29, 17, Palette.Ink, FontStyle.Bold);
            var preview = Rect(product.rectTransform, "Cotton preview", 13, 48, 137, 137).gameObject.AddComponent<RawImage>(); preview.texture = game.World.CandyPreview; preview.raycastTarget = false;
            grams = Label(product.rectTransform, "0 g", 163, 57, 142, 48, 27, Palette.Ink, FontStyle.Bold);
            flavor = Label(product.rectTransform, "딸기 구간", 164, 108, 142, 45, 14, Palette.Muted);
            raceQuality = Label(product.rectTransform, "완주하면 제품 완성", 163, 163, 142, 40, 12, Palette.Muted);
            productionFill = Progress(product.rectTransform, 17, 213, 284, 8, Palette.Pink);
            var speedCard = Box(p, "Speedometer", 32, 699, 253, 129, Palette.Ink);
            Label(speedCard.rectTransform, "SUGAR POWER", 18, 11, 214, 25, 13, Palette.Soda, FontStyle.Bold);
            speed = Label(speedCard.rectTransform, "0 km/h", 16, 44, 220, 68, 41, Color.white, FontStyle.Bold);
            var driftCard = Box(p, "Drift meter", 508, 726, 584, 103, new Color(.16f, .20f, .30f, .95f));
            driftLabel = Label(driftCard.rectTransform, "Space + 조향으로 드리프트", 20, 8, 544, 40, 22, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            driftFill = Progress(driftCard.rectTransform, 22, 62, 540, 14, Palette.Pink);
            raceEvent = Label(p, "", 448, 200, 704, 58, 29, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            ButtonAt(p, "주행 포기  Enter", 1323, 770, 245, 56, Palette.Cream, Palette.Ink, () => game.FinishRun(), 18);
            ButtonAt(p, "도움말  Esc", 1415, 289, 153, 42, Palette.Cream, Palette.Ink, () => game.TogglePause(), 14);
            raceOrder = Label(p, "", 558, 142, 484, 53, 16, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            var controls = Box(p, "Driving controls", 310, 848, 980, 35, new Color(1, .97f, .9f, .95f));
            raceControls = Label(controls.rectTransform, "", 10, 0, 960, 35, 15, Palette.Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        void BuildResult()
        {
            if (game.Shift != null) { BuildShiftResults(); return; }
            var p = result.GetComponent<RectTransform>();
            Label(p, "03  /  MADE BY YOUR JOURNEY", 1240, 137, 320, 25, 12, Palette.Muted, FontStyle.Bold);
            resultHeading = Label(p, "달콤한 완성!", 1240, 170, 320, 48, 32, Palette.Ink, FontStyle.Bold);
            resultTitle = Label(p, "소다 구름 솜사탕", 1240, 249, 320, 70, 24, Palette.Ink, FontStyle.Bold);
            var card = Box(p, "Finished candy stats", 1240, 350, 320, 186, Color.white);
            Label(card.rectTransform, "오늘 만든 솜사탕", 19, 17, 282, 28, 15, Palette.Muted);
            resultWeight = Label(card.rectTransform, "220 g", 18, 53, 280, 55, 38, Palette.Ink, FontStyle.Bold);
            resultDetail = Label(card.rectTransform, "3가지 맛 · 5.5바퀴", 19, 124, 282, 49, 15, Palette.Muted);
            Label(p, "예상 판매 금액", 1240, 573, 320, 30, 16, Palette.Muted);
            resultPrice = Label(p, "+ 220 코인", 1240, 615, 320, 66, 35, Palette.Ink, FontStyle.Bold);
            ButtonAt(p, "가게로 돌아가기   →", 1240, 745, 320, 63, Palette.Pink, Palette.Ink, () => game.ReturnToShop(), 20);
            Label(p, "돌아가서 재고를 골라 건네주세요", 1240, 829, 320, 30, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        void BuildPause()
        {
            pause = Box(root, "Pause dimmer", 0, 0, 1600, 900, new Color(.1f, .12f, .19f, .66f), false).gameObject;
            pause.GetComponent<Image>().raycastTarget = true;
            var card = Box(pause.GetComponent<RectTransform>(), "Pause card", 474, 166, 652, 568, Palette.Cream);
            Label(card.rectTransform, "잠깐, 달콤한 휴식", 38, 31, 577, 53, 32, Palette.Ink, FontStyle.Bold);
            pauseControls = Label(card.rectTransform, "", 40, 113, 576, 234, 17, Palette.Ink);
            pauseControls.name = "PauseControls";
            ButtonAt(card.rectTransform, "계속하기", 40, 360, 574, 56, Palette.Ink, Color.white, () => game.TogglePause(), 20);
            var mute = ButtonAt(card.rectTransform, "소리 켜짐", 40, 440, 273, 46, Color.white, Palette.Ink, () => game.ToggleMute(), 16);
            muteLabel = mute.GetComponentInChildren<Text>();
            ButtonAt(card.rectTransform, "새 가게 시작", 329, 440, 285, 46, Color.white, Palette.Ink, () =>
            {
                if (!splitLayout && game.Session.Mode != GameMode.Shop) { game.Notify("가게 화면에서 새로 시작할 수 있어요."); return; }
                if (Time.unscaledTime < resetConfirmUntil) { game.ResetSave(); resetConfirmUntil = 0; }
                else { resetConfirmUntil = Time.unscaledTime + 5; game.Notify("기존 저장을 보관하고 새로 시작하려면 한 번 더 눌러주세요."); }
            }, 16);
            Label(card.rectTransform, game.Shift != null ? "흔들어 넣기 → 달리기 → F로 꺼내기 → 드래그 판매 · 자동 저장" : "주문 선택 → 제작/재고 선택 → 건네기 · 자동 저장", 40, 510, 574, 28, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            toast.transform.SetAsLastSibling();
        }
        void RefreshDrivingHelp()
        {
            bool kart = game.RunStyle == DrivingStyle.Kart;
            if (commonControls) commonControls.text = "W / ↑ 가속    A D 조향    Space 드리프트" + (kart ? "    Shift 카트 부스터" : "") + "    S 제동    Enter 진행 / 포기    R 복귀";
            if (!pauseControls) return;
            pauseControls.text = game.Shift != null
                ? "설탕 봉지 ↕ 투입\n솜사탕 → 손님에게 드래그\n\nF  꺼내기     우클릭  설탕 비우기\nW  가속     A / D  조향     S  제동\nSpace  드리프트" + (kart ? "     Shift  부스터" : " 감속") + "\nR  코스 복귀     Esc  돌아가기"
                : splitLayout
                ? "왼쪽은 주행과 생산, 오른쪽은 주문과 판매\n자동 주행 ON  판매 중에도 계속 달려요\n수동 운전       상단 버튼으로 전환\nW / A D / S   가속 / 조향 / 제동\n" + (kart ? "Space / Shift  드리프트 / 카트 부스터" : "Space            드리프트 감속") + "\nR / Esc          위치 복귀 / 전체 일시정지\n다음 제작       선택한 맛과 맵을 다음 생산에 적용\n재고가 가득 차면 생산 대기 · 판매하면 다시 생산\n재고 선택 → 맞는 손님에게 건네기 · 자동 저장"
                : "W / ↑         가속 · 다운힐은 계속 밟아 속도 올리기\nA D / ← →  좌우 조향\n" + (kart ? "Space          드리프트 · 카트 부스터 충전\nShift            카트 부스터 사용 (최대 2개 보관)" : "Space          드리프트 감속") + "\nS / ↓            브레이크 · 고속 코너 진입 전 감속\nEnter          출발 · 포기 확인 · 가게 복귀\nR / Esc       위치 복귀 / 일시정지\n맛 설정       출발 전에 고른 맛으로 끝까지 제작\n가게            재고 선택 → 맞는 주문에 건네기";
        }
        public void Refresh()
        {
            if (!root || game.Session == null) return;
            RefreshDrivingHelp();
            if (RefreshOutgame()) return;
            if (splitLayout) { RefreshSplit(); return; }
            var s = game.Session; var e = s.Economy;
            if (speedLines) speedLines.SetDriving(game.World.Kart, s.Mode == GameMode.Racing && !s.Paused);
            coins.text = e.Coins.ToString("N0"); day.text = "DAY " + e.Day.ToString("00");
            common.SetActive(s.Mode != GameMode.Racing); shop.SetActive(s.Mode == GameMode.Shop); race.SetActive(s.Mode == GameMode.Racing); result.SetActive(s.Mode == GameMode.Results);
            pause.SetActive(s.Paused); toast.SetActive(!string.IsNullOrEmpty(game.Notice)); notice.text = game.Notice ?? "";
            toast.GetComponent<RectTransform>().anchoredPosition = new Vector2(s.Paused || s.Mode == GameMode.Racing ? 470 : 278, s.Paused ? -780 : s.Mode == GameMode.Racing ? -286 : -435);
            muteLabel.text = game.Audio.Muted ? "소리 꺼짐" : "소리 켜짐";
            if (s.Mode == GameMode.Shop) RefreshShop();
            if (s.Mode == GameMode.Racing)
            {
                timer.text = ((int)s.Elapsed / 60) + ":" + ((int)s.Elapsed % 60).ToString("00");
                mapTitle.text = RaceRecipe.Name(game.RunMap);
                grams.text = s.Production.Grams + " g";
                productionFill.rectTransform.sizeDelta = new Vector2(284f * s.Production.Grams / game.RunTargetGrams, 8);
                var kart = game.World.Kart; var drive = kart.DriveModel;
                raceControls.text = game.RunStyle == DrivingStyle.Kart ? "W 가속    A / D 조향    Space 드리프트    Shift 부스터    S 제동    R 복귀" : "W 유지 → 계속 가속    A / D 조향    S 코너 전 감속    Space 드리프트    R 복귀";
                speed.text = Mathf.RoundToInt(kart.Speed * 3.6f) + " km/h";
                laps.text = "완주 " + Mathf.Clamp01((float)(drive.TotalProgress / drive.Course.Length)).ToString("P0");
                bestLap.text = Mathf.Max(0, (float)(drive.Course.Length - drive.TotalProgress)).ToString("0") + "m 남음\n중량이 차도 완주까지 달려요";
                flavor.text = Palette.FlavorName(game.RunFlavor) + "맛 설정\n" + (s.Production.IsFull ? "완주 대기" : "주행 경로를 감는 중");
                raceQuality.text = "예상 품질 " + RaceRecipe.Quality(drive.SkillCount, drive.WallHits, e.Levels[1]) + " / 100";
                driftFill.rectTransform.sizeDelta = new Vector2(540 * (kart.Boosting ? Mathf.Clamp01((float)(drive.BoostRemaining / Math.Max(.01, drive.BoostDuration))) : kart.Charge), 14);
                driftFill.color = kart.Boosting ? (drive.BoostTier == 2 ? Palette.Yellow : Palette.Soda) : kart.Charge >= .75f ? Palette.Yellow : Palette.Pink;
                driftLabel.text = kart.Boosting ? (drive.ManualBoostActive ? "BOOSTER!  강한 가속" : drive.BoostTier == 2 ? "SUPER BOOST!" : "MINI BOOST!")
                    : kart.Charge >= .75f ? "슈퍼 부스트 준비!  Space 놓기" : kart.Charge >= .32f ? "Space 놓으면 부스터 충전!"
                    : kart.Drifting ? "코너를 유지해 충전하세요" : "Shift 부스터 · Space 드리프트";
                driftLabel.text += "   [ " + drive.StoredBoosts + " / 2 ]";
                driftLabel.transform.parent.gameObject.SetActive(game.RunStyle == DrivingStyle.Kart);
                if (game.RunStyle == DrivingStyle.Downhill)
                {
                    driftFill.color = Palette.Soda;
                    driftLabel.text = kart.Drifting ? "드리프트 감속 중 · Space를 놓고 W로 가속" : "W 유지 → 계속 가속 · 코너 전 S 감속";
                }
                bool wrongWay = kart.Speed > 3 && (Math.Sin(drive.Heading) * drive.Sample.Tangent.X + Math.Cos(drive.Heading) * drive.Sample.Tangent.Z) < -.35;
                raceEvent.text = kart.ImpactFlash > 0 ? "벽 충돌!  속도가 줄었어요" : wrongWay ? "역방향!  R로 코스 방향에 맞춰 복귀" : kart.Boosting ? "달콤한 가속!" : drive.Sample.IsShortcut ? "SUGAR CUT · 빠른 완주에 도전" : "";
                UpdateRaceOrder();
                map.SetVerticesDirty();
            }
            if (s.Mode == GameMode.Results)
            {
                var product = s.Result;
                int count = 0; var seen = new bool[3];
                if (product != null) foreach (var sample in product.Samples) seen[sample.Flavor] = true;
                foreach (bool value in seen) if (value) count++;
                resultTitle.text = product == null ? "주행을 마치지 못했어요" : ProductName(product);
                resultWeight.text = (product == null ? 0 : product.Grams) + " g";
                resultHeading.text = product == null ? "다시 도전해요" : "완주! 달콤한 완성";
                resultDetail.text = product == null ? "코스를 완주해야 제품이 완성돼요" : "품질 " + product.Quality + "/100 · 기록 " + s.Elapsed.ToString("0.0") + "초\n완주 보상 +" + s.ResultBonus + " 코인 지급";
                resultPrice.text = product == null ? "완주 후 판매 가능" : e.Price(product) + " 코인 + 팁";
            }
        }
        GameObject Group(RectTransform parent, string name) { return Rect(parent, name, 0, 0, 1600, 900).gameObject; }
        static RectTransform Rect(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        Image Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, bool round = true)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            if (round) { image.sprite = rounded; image.type = Image.Type.Sliced; }
            return image;
        }
        Text Label(RectTransform parent, string text, float x, float y, float w, float h, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.UpperLeft)
        {
            var label = Rect(parent, "Text " + text, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = color; label.fontStyle = style;
            label.alignment = align; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate; return label;
        }
        Button ButtonAt(RectTransform parent, string title, float x, float y, float w, float h, Color bg, Color fg, Action action, int size)
        {
            var image = Box(parent, title, x, y, w, h, bg); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(.93f, .93f, .93f); colors.pressedColor = new Color(.8f, .8f, .8f); colors.disabledColor = new Color(.8f, .8f, .8f, .42f); button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => action());
            Label(image.rectTransform, title, 8, 0, w - 16, h, size, fg, FontStyle.Bold, TextAnchor.MiddleCenter);
            return button;
        }
        Image Progress(RectTransform parent, float x, float y, float width, float height, Color color)
        {
            var track = Box(parent, "Track", x, y, width, height, Palette.Hex("E9E1D7"));
            return Box(track.rectTransform, "Fill", 0, 0, 0, height, color);
        }
        static Sprite RoundSprite()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Max(10 - x, x - 21), dy = Mathf.Max(10 - y, y - 21);
                float distance = new Vector2(Mathf.Max(0, dx), Mathf.Max(0, dy)).magnitude;
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(10.5f - distance)));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(11, 11, 11, 11));
        }
        void OnDestroy() { if (rounded) { Destroy(rounded.texture); Destroy(rounded); } if (font) Destroy(font); }
    }
}
