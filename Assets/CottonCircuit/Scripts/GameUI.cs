using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CottonCircuit
{
    public class GameUI : MonoBehaviour
    {
        GameController game;
        Font font;
        Sprite rounded;
        RectTransform root;
        GameObject shop, race, result, pause, toast;
        Text coins, day, inventory, customer, timer, grams, speed, laps, flavor, resultTitle, resultWeight, resultPrice, resultDetail, notice, status, muteLabel;
        Image productionFill, customerFill;
        Text[] upgradeTexts = new Text[3];
        Button[] upgradeButtons = new Button[3];
        Image[] flavorCards = new Image[3];
        Button startButton;
        float resetConfirmUntil;
        public void Initialize(GameController controller)
        {
            game = controller;
            if (root) { Refresh(); return; }
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
            Box(root, "Shop backdrop", 1208, 0, 392, 900, Palette.Cream, false);
            Box(root, "Divider", 1208, 0, 2, 900, Palette.Hex("E5DCCC"), false);
            Label(root, "C O T T O N   C I R C U I T", 44, 31, 660, 30, 15, Palette.Ink, FontStyle.Bold);
            Label(root, "솜사탕 서킷", 42, 62, 650, 58, 40, Palette.Ink, FontStyle.Bold);
            Label(root, "한 바퀴씩, 달콤한 꿈을 감아요.", 46, 124, 650, 35, 17, Palette.Muted);
            var badge = Box(root, "Day badge", 46, 177, 155, 38, Color.white);
            day = Label(badge.rectTransform, "DAY 01", 0, 0, 155, 38, 15, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            ButtonAt(root, "도움말  Esc", 1040, 42, 132, 43, Color.white, Palette.Ink, () => game.TogglePause(), 15);
            var wallet = Box(root, "Wallet", 1236, 30, 335, 78, Palette.Ink);
            Label(wallet.rectTransform, "우리 가게의 수익", 20, 11, 190, 24, 13, Palette.Cream);
            coins = Label(wallet.rectTransform, "80", 20, 31, 225, 40, 28, Color.white, FontStyle.Bold);
            Label(wallet.rectTransform, "COINS", 240, 36, 75, 25, 13, Palette.Yellow, FontStyle.Bold, TextAnchor.MiddleRight);
            var bottom = Box(root, "Controls strip", 38, 840, 1132, 40, new Color(1, .98f, .93f, .95f));
            Label(bottom.rectTransform, "W / ↑  가속     A D / ← →  레인 이동     S / Space  제동     Enter  진행     R  위치 복귀", 14, 0, 1104, 40, 15, Palette.Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            shop = Group(root, "Shop panel"); race = Group(root, "Race panel"); result = Group(root, "Results panel");
            BuildShop(); BuildRace(); BuildResult();
            toast = Box(root, "Toast", 278, 772, 660, 48, Palette.Ink).gameObject;
            notice = Label(toast.GetComponent<RectTransform>(), "", 18, 0, 624, 48, 16, Color.white, FontStyle.Normal, TextAnchor.MiddleCenter);
            BuildPause();
            Refresh();
        }
        void BuildShop()
        {
            var p = shop.GetComponent<RectTransform>();
            Label(p, "01  /  YOUR LITTLE CANDY SHOP", 1240, 137, 320, 25, 12, Palette.Muted, FontStyle.Bold);
            Label(p, "오늘도 달콤하게", 1240, 169, 320, 48, 29, Palette.Ink, FontStyle.Bold);
            Label(p, "직접 만든 솜사탕으로\n작은 가게를 키워보세요.", 1240, 223, 320, 60, 17, Palette.Muted);
            var card = Box(p, "Inventory card", 1240, 301, 320, 104, Color.white);
            Label(card.rectTransform, "진열대", 18, 12, 140, 28, 17, Palette.Ink, FontStyle.Bold);
            inventory = Label(card.rectTransform, "0 / 6", 170, 12, 132, 28, 19, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleRight);
            customer = Label(card.rectTransform, "첫 솜사탕을 만들어보세요", 18, 48, 285, 28, 14, Palette.Muted);
            customerFill = Progress(card.rectTransform, 18, 85, 284, 5, Palette.Soda);
            Label(p, "더 멀리 달리고, 더 많이 만들어요", 1240, 429, 320, 28, 15, Palette.Ink, FontStyle.Bold);
            string[] names = { "카트 모터", "설탕통", "가게 꾸미기" };
            string[] hints = { "최고 속도 증가", "솜사탕 적재량 증가", "제품 판매가 증가" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var row = Box(p, "Upgrade " + i, 1240, 472 + 85 * i, 320, 74, Color.white);
                upgradeTexts[i] = Label(row.rectTransform, names[i], 14, 8, 185, 29, 17, Palette.Ink, FontStyle.Bold);
                Label(row.rectTransform, hints[i], 14, 39, 184, 24, 12, Palette.Muted);
                upgradeButtons[i] = ButtonAt(row.rectTransform, "120", 207, 17, 98, 42, Palette.Yellow, Palette.Ink, () => game.BuyUpgrade(index), 16);
            }
            startButton = ButtonAt(p, "만들러 가기   →", 1240, 745, 320, 63, Palette.Ink, Color.white, () => game.StartRun(), 21);
            status = Label(p, "자동 저장 · 60초 제작 레이스", 1240, 824, 320, 45, 12, Palette.Muted, FontStyle.Normal, TextAnchor.UpperCenter);
        }
        void BuildRace()
        {
            var p = race.GetComponent<RectTransform>();
            Label(p, "02  /  SPIN SOMETHING SWEET", 1240, 137, 320, 25, 12, Palette.Muted, FontStyle.Bold);
            Label(p, "솜사탕을 감아요", 1240, 169, 320, 48, 29, Palette.Ink, FontStyle.Bold);
            Label(p, "중앙 막대를 돌수록 풍성해져요.", 1240, 224, 320, 30, 15, Palette.Muted);
            var clock = Box(p, "Clock", 1240, 277, 320, 112, Palette.Yellow);
            Label(clock.rectTransform, "남은 시간", 19, 13, 130, 25, 14, Palette.Ink);
            timer = Label(clock.rectTransform, "60", 17, 35, 180, 66, 49, Palette.Ink, FontStyle.Bold);
            Label(clock.rectTransform, "SECONDS", 179, 67, 122, 28, 12, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleRight);
            grams = Label(p, "0 / 220 g", 1240, 413, 320, 37, 24, Palette.Ink, FontStyle.Bold);
            productionFill = Progress(p, 1240, 463, 320, 10, Palette.Pink);
            speed = Label(p, "0 km/h", 1240, 493, 150, 30, 19, Palette.Ink, FontStyle.Bold);
            laps = Label(p, "0.0 바퀴", 1401, 493, 159, 30, 19, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleRight);
            Label(p, "설탕 레인   A ←  → D", 1240, 550, 320, 27, 14, Palette.Muted);
            for (int i = 0; i < 3; i++)
            {
                flavorCards[i] = Box(p, "Flavor " + i, 1240 + 109 * i, 589, 102, 53, Palette.Flavor(i));
                Label(flavorCards[i].rectTransform, Palette.FlavorName(i), 0, 0, 102, 53, 17, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            flavor = Label(p, "지금은 소다 맛을 감고 있어요", 1240, 663, 320, 55, 15, Palette.Muted);
            ButtonAt(p, "이만큼 완성하기", 1240, 745, 320, 63, Palette.Ink, Color.white, () => game.FinishRun(), 20);
            Label(p, "가득 차면 자동으로 완성돼요", 1240, 829, 320, 30, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        void BuildResult()
        {
            var p = result.GetComponent<RectTransform>();
            Label(p, "03  /  MADE BY YOUR JOURNEY", 1240, 137, 320, 25, 12, Palette.Muted, FontStyle.Bold);
            Label(p, "달콤한 완성!", 1240, 170, 320, 48, 32, Palette.Ink, FontStyle.Bold);
            resultTitle = Label(p, "소다 구름 솜사탕", 1240, 249, 320, 70, 24, Palette.Ink, FontStyle.Bold);
            var card = Box(p, "Finished candy stats", 1240, 350, 320, 186, Color.white);
            Label(card.rectTransform, "오늘 만든 솜사탕", 19, 17, 282, 28, 15, Palette.Muted);
            resultWeight = Label(card.rectTransform, "220 g", 18, 53, 280, 55, 38, Palette.Ink, FontStyle.Bold);
            resultDetail = Label(card.rectTransform, "3가지 맛 · 5.5바퀴", 19, 124, 282, 40, 16, Palette.Muted);
            Label(p, "예상 판매 금액", 1240, 573, 320, 30, 16, Palette.Muted);
            resultPrice = Label(p, "+ 220 코인", 1240, 615, 320, 66, 35, Palette.Ink, FontStyle.Bold);
            ButtonAt(p, "가게로 돌아가기   →", 1240, 745, 320, 63, Palette.Pink, Palette.Ink, () => game.ReturnToShop(), 20);
            Label(p, "가게에 돌아가면 손님이 구매해요", 1240, 829, 320, 30, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        void BuildPause()
        {
            pause = Box(root, "Pause dimmer", 0, 0, 1600, 900, new Color(.1f, .12f, .19f, .66f), false).gameObject;
            pause.GetComponent<Image>().raycastTarget = true;
            var card = Box(pause.GetComponent<RectTransform>(), "Pause card", 474, 166, 652, 568, Palette.Cream);
            Label(card.rectTransform, "잠깐, 달콤한 휴식", 38, 31, 577, 53, 32, Palette.Ink, FontStyle.Bold);
            Label(card.rectTransform, "W / ↑         가속해서 중앙 막대 주위를 달려요\nA D / ← →  안쪽 딸기 · 가운데 소다 · 바깥 바닐라\nS / Space   브레이크\nEnter          출발 · 완성 · 가게로 돌아가기\nR                 주행 위치 복귀\nEsc             일시정지 / 계속하기", 40, 113, 576, 218, 18, Palette.Ink);
            ButtonAt(card.rectTransform, "계속하기", 40, 360, 574, 56, Palette.Ink, Color.white, () => game.TogglePause(), 20);
            var mute = ButtonAt(card.rectTransform, "소리 켜짐", 40, 440, 273, 46, Color.white, Palette.Ink, () => game.ToggleMute(), 16);
            muteLabel = mute.GetComponentInChildren<Text>();
            ButtonAt(card.rectTransform, "새 가게 시작", 329, 440, 285, 46, Color.white, Palette.Ink, () =>
            {
                if (game.Session.Mode != GameMode.Shop) { game.Notify("가게 화면에서 새로 시작할 수 있어요."); return; }
                if (Time.unscaledTime < resetConfirmUntil) { game.ResetSave(); resetConfirmUntil = 0; }
                else { resetConfirmUntil = Time.unscaledTime + 5; game.Notify("기존 저장을 보관하고 새로 시작하려면 한 번 더 눌러주세요."); }
            }, 16);
            Label(card.rectTransform, "게임은 로컬에 자동 저장됩니다.", 40, 510, 574, 28, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            toast.transform.SetAsLastSibling();
        }
        public void Refresh()
        {
            if (!root || game.Session == null) return;
            var s = game.Session; var e = s.Economy;
            coins.text = e.Coins.ToString("N0"); day.text = "DAY " + e.Day.ToString("00");
            shop.SetActive(s.Mode == GameMode.Shop); race.SetActive(s.Mode == GameMode.Racing); result.SetActive(s.Mode == GameMode.Results);
            pause.SetActive(s.Paused); toast.SetActive(!string.IsNullOrEmpty(game.Notice)); notice.text = game.Notice ?? "";
            muteLabel.text = game.Audio.Muted ? "소리 꺼짐" : "소리 켜짐";
            if (s.Mode == GameMode.Shop)
            {
                inventory.text = e.Inventory.Count + " / 6";
                customer.text = e.Inventory.Count == 0 ? (e.TotalSold == 0 ? "첫 솜사탕을 만들어보세요" : "모두 팔렸어요! 다시 만들어볼까요?") : "손님이 가게를 찾아오고 있어요";
                customerFill.rectTransform.sizeDelta = new Vector2(284 * game.SaleProgress, 5);
                string[] names = { "카트 모터", "설탕통", "가게 꾸미기" };
                for (int i = 0; i < 3; i++)
                {
                    upgradeTexts[i].text = names[i] + "  " + e.Levels[i] + "/3";
                    int cost = e.UpgradeCost(i);
                    upgradeButtons[i].GetComponentInChildren<Text>().text = cost == 0 ? "MAX" : cost.ToString();
                    upgradeButtons[i].interactable = cost > 0 && e.Coins >= cost;
                }
                startButton.interactable = e.Inventory.Count < Economy.InventoryLimit && game.Store.CanSave;
                status.text = game.Store.Error == null ? (e.TotalSold > 0 ? e.TotalSold + "개 판매 · 자동 저장 완료" : "자동 저장 · 60초 제작 레이스") : "저장 오류 · 도움말에서 새 가게 시작";
            }
            if (s.Mode == GameMode.Racing)
            {
                timer.text = Mathf.CeilToInt((float)s.Remaining).ToString("00");
                grams.text = s.Production.Grams + " / " + e.Capacity + " g";
                productionFill.rectTransform.sizeDelta = new Vector2(320 * s.Production.Grams / e.Capacity, 10);
                speed.text = Mathf.RoundToInt(game.World.Kart.Speed * 3.6f) + " km/h";
                laps.text = s.Production.Turns.ToString("0.0") + " 바퀴";
                int current = game.World.Kart.Flavor;
                for (int i = 0; i < 3; i++) flavorCards[i].color = Color.Lerp(Palette.Flavor(i), Palette.Cream, i == current ? 0 : .65f);
                flavor.text = game.World.Kart.Speed < .2f ? "W를 길게 눌러 출발하세요!" : "지금은 " + Palette.FlavorName(current) + " 맛을 감고 있어요";
            }
            if (s.Mode == GameMode.Results)
            {
                var product = s.Result;
                int count = 0; var seen = new bool[3];
                if (product != null) foreach (var sample in product.Samples) seen[sample.Flavor] = true;
                foreach (bool value in seen) if (value) count++;
                resultTitle.text = product == null ? "이번에는 연습 주행!" : count == 3 ? "무지개 구름 솜사탕" : count == 2 ? "두 가지 맛 구름 솜사탕" : Palette.FlavorName(seen[0] ? 0 : seen[1] ? 1 : 2) + " 구름 솜사탕";
                resultWeight.text = (product == null ? 0 : product.Grams) + " g";
                resultDetail.text = product == null ? "다음에는 W로 달려보세요" : count + "가지 맛 · " + s.Production.Turns.ToString("0.0") + "바퀴";
                resultPrice.text = "+ " + e.Price(product) + " 코인";
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
