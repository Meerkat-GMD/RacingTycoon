using UnityEngine;
using UnityEngine.UI;
namespace CottonCircuit
{
    public partial class GameUI
    {
        readonly Text[] orderTitles = new Text[2], orderHints = new Text[2];
        readonly Image[] orderFills = new Image[2], orderCards = new Image[2];
        readonly Button[] orderSelect = new Button[2], orderMake = new Button[2], orderServe = new Button[2], stockButtons = new Button[12];
        Text stockCount, business, shelfText, raceOrder;
        Button shelfButton, discardButton, smallButton, largeButton;
        readonly Button[] flavorButtons = new Button[3];
        readonly Button[] styleButtons = new Button[2];
        Text styleHint;
        static string SizeName(int size) => size < 0 ? "미완성" : size == 0 ? "작은" : "큰";
        static string ProductName(Product product) => Palette.FlavorName(CandyRecipe.FlavorOf(product)) + " · " + SizeName(CandyRecipe.SizeOf(product));
        CustomerOrder OrderAt(int i) => i < game.Orders.Orders.Count ? game.Orders.Orders[i] : null;
        void BuildShop()
        {
            var p = shop.GetComponent<RectTransform>();
            Label(p, "01  /  MADE TO ORDER", 1240, 123, 320, 22, 12, Palette.Muted, FontStyle.Bold);
            Label(p, "주문을 받았어요", 1240, 152, 320, 42, 28, Palette.Ink, FontStyle.Bold);
            Label(p, "재고가 있으면 골라서 건네고,\n없으면 직접 만들어 돌아오세요.", 1240, 202, 320, 44, 16, Palette.Muted);
            var stats = Box(p, "Shop reputation", 1240, 252, 320, 58, Color.white);
            business = Label(stats.rectTransform, "", 14, 7, 292, 49, 15, Palette.Ink);
            string[] names = { "카트 모터", "설탕통", "가게 꾸미기" };
            string[] hints = { "최고 속도 증가", "감기 속도 +15% · 품질 +5", "판매가 증가" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var row = Box(p, "Upgrade " + i, 1240, 322 + 52 * i, 320, 48, Color.white);
                upgradeTexts[i] = Label(row.rectTransform, names[i], 13, 2, 192, 29, 16, Palette.Ink, FontStyle.Bold);
                Label(row.rectTransform, hints[i], 13, 27, 188, 20, 12, Palette.Muted);
                upgradeButtons[i] = ButtonAt(row.rectTransform, "120", 207, 4, 98, 40, Palette.Yellow, Palette.Ink, () => game.BuyUpgrade(index), 16);
            }
            var shelf = Box(p, "Shelf expansion", 1240, 485, 320, 55, Color.white);
            shelfText = Label(shelf.rectTransform, "", 13, 4, 189, 47, 15, Palette.Ink, FontStyle.Bold);
            shelfButton = ButtonAt(shelf.rectTransform, "160", 207, 7, 98, 41, Palette.Soda, Palette.Ink, () => game.BuyShelf(), 16);
            Label(p, "맵 선택 · 제품 크기", 1240, 550, 320, 22, 14, Palette.Muted);
            smallButton = ButtonAt(p, "1번 맵 · 작은", 1240, 576, 155, 36, Palette.Pink, Palette.Ink, () => game.SetSize(0), 16);
            largeButton = ButtonAt(p, "2번 맵 · 큰", 1404, 576, 156, 36, Color.white, Palette.Ink, () => game.SetSize(1), 16);
            Label(p, "기계 세팅 · 솜사탕 맛", 1240, 622, 320, 22, 14, Palette.Muted);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                flavorButtons[i] = ButtonAt(p, Palette.FlavorName(i), 1240 + i * 110, 647, 100, 34, Color.white, Palette.Ink, () => game.SetFlavor(index), 16);
            }
            Label(p, "주행 스타일 · 같은 맵에서 비교", 1240, 690, 320, 22, 14, Palette.Muted);
            styleButtons[0] = ButtonAt(p, "카트 스타일", 1240, 714, 155, 38, Palette.Pink, Palette.Ink, () => game.SetStyle(0), 16);
            styleButtons[1] = ButtonAt(p, "다운힐 스타일", 1404, 714, 156, 38, Color.white, Palette.Ink, () => game.SetStyle(1), 16);
            styleHint = Label(p, "", 1240, 760, 320, 33, 12, Palette.Muted);
            startButton = ButtonAt(p, "재고 미리 만들기  →", 1240, 804, 320, 48, Palette.Ink, Color.white, () => game.PrepareStock(), 19);
            status = Label(p, "", 1240, 858, 320, 40, 12, Palette.Muted, FontStyle.Normal, TextAnchor.UpperCenter);
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var card = Box(p, "Customer order " + i, 38 + i * 574, 495, 558, 167, Palette.Cream);
                orderCards[i] = card;
                orderTitles[i] = Label(card.rectTransform, "", 18, 12, 514, 31, 22, Palette.Ink, FontStyle.Bold);
                orderHints[i] = Label(card.rectTransform, "", 18, 50, 514, 27, 15, Palette.Muted);
                orderFills[i] = Progress(card.rectTransform, 18, 89, 522, 5, Palette.Soda);
                orderSelect[i] = ButtonAt(card.rectTransform, "주문 선택", 18, 108, 140, 42, Color.white, Palette.Ink, () => game.SelectOrder(OrderAt(index)?.Id), 16);
                orderMake[i] = ButtonAt(card.rectTransform, "만들러 가기", 168, 108, 180, 42, Palette.Ink, Color.white, () => game.MakeOrder(OrderAt(index)?.Id), 16);
                orderServe[i] = ButtonAt(card.rectTransform, "건네기", 358, 108, 182, 42, Palette.Soda, Palette.Ink, () => game.Serve(OrderAt(index)?.Id), 17);
            }
            var stocks = Box(p, "Stock selector", 38, 680, 1132, 146, Palette.Cream);
            stockCount = Label(stocks.rectTransform, "", 16, 8, 850, 29, 16, Palette.Ink, FontStyle.Bold);
            discardButton = ButtonAt(stocks.rectTransform, "선택 재고 정리", 934, 5, 182, 30, Color.white, Palette.Muted, () => game.DiscardSelected(), 13);
            for (int i = 0; i < 12; i++)
            {
                int index = i;
                stockButtons[i] = ButtonAt(stocks.rectTransform, "빈 칸", 16 + i % 6 * 185, 42 + i / 6 * 49, 175, 42, Color.white, Palette.Ink,
                    () => { if (index < game.Session.Economy.Inventory.Count) game.SelectProduct(game.Session.Economy.Inventory[index].Id); }, 14);
            }
        }
        void RefreshShop()
        {
            var e = game.Session.Economy;
            business.text = "판매 " + e.TotalSold + "개   ·   받은 팁 " + e.TotalTips + " 코인\n" +
                (e.OrdersServed > 0 ? "만족도 " + (e.SatisfactionTotal / e.OrdersServed * 100).ToString("0") + "%" : "첫 손님을 기다리는 가게") + "   ·   놓친 주문 " + e.MissedOrders;
            bool canMake = e.Inventory.Count < e.StockCapacity && game.Store.CanSave;
            for (int i = 0; i < 2; i++)
            {
                var order = OrderAt(i); bool exists = order != null;
                orderTitles[i].text = exists ? "손님 " + (i + 1) + "   " + Palette.FlavorName(order.Flavor) + " · " + SizeName(order.Size) + " 솜사탕" : "다음 손님을 기다려요";
                int matches = exists ? e.Inventory.FindAll(product => CandyRecipe.Matches(product, order)).Count : 0;
                orderHints[i].text = exists ? "남은 시간 " + Mathf.CeilToInt((float)order.Remaining) + "초   ·   " + (matches > 0 ? "맞는 재고 " + matches + "개 · 골라서 건네주세요" : "맞는 재고 없음 · 만들어 주세요") : "여유가 있을 때 인기 메뉴를 미리 만들어두세요.";
                orderFills[i].rectTransform.sizeDelta = new Vector2(exists ? 522 * (float)(order.Remaining / CustomerOrder.Patience) : 0, 5);
                orderFills[i].color = exists && order.Remaining < 30 ? Palette.Pink : Palette.Soda;
                orderCards[i].color = exists && game.SelectedOrderId == order.Id ? Palette.Hex("FCE7B8") : Palette.Cream;
                orderSelect[i].interactable = exists; orderMake[i].interactable = exists && canMake;
                orderServe[i].interactable = exists && game.Store.CanSave && CandyRecipe.Matches(game.SelectedProduct, order);
            }
            stockCount.text = "내 진열대   " + e.Inventory.Count + " / " + e.StockCapacity + "    ·    솜사탕 선택 → 손님의 ‘건네기’";
            for (int i = 0; i < 12; i++)
            {
                bool stocked = i < e.Inventory.Count;
                var b = stockButtons[i]; b.interactable = stocked;
                b.GetComponentInChildren<Text>().text = stocked ? ProductName(e.Inventory[i]) + "  " + e.Inventory[i].Grams + "g" : i < e.StockCapacity ? "빈 칸" : "확장하면 사용 가능";
                b.image.color = stocked && e.Inventory[i].Id == game.SelectedProductId ? Palette.Soda : Color.white;
            }
            discardButton.interactable = game.SelectedProduct != null && game.Store.CanSave;
            string[] names = { "카트 모터", "설탕통", "가게 꾸미기" };
            for (int i = 0; i < 3; i++)
            {
                upgradeTexts[i].text = names[i] + "  " + e.Levels[i] + "/3";
                int cost = e.UpgradeCost(i); upgradeButtons[i].GetComponentInChildren<Text>().text = cost == 0 ? "MAX" : cost.ToString();
                upgradeButtons[i].interactable = cost > 0 && e.Coins >= cost && game.Store.CanSave;
            }
            shelfText.text = "진열대  " + e.ShelfLevel + "/2\n" + e.StockCapacity + "칸";
            shelfButton.GetComponentInChildren<Text>().text = e.ShelfCost == 0 ? "MAX" : e.ShelfCost.ToString();
            shelfButton.interactable = e.ShelfCost > 0 && e.Coins >= e.ShelfCost && game.Store.CanSave;
            startButton.interactable = canMake;
            smallButton.image.color = game.PreparedSize == 0 ? Palette.Pink : Color.white;
            largeButton.image.color = game.PreparedSize == 1 ? Palette.Pink : Color.white;
            for (int i = 0; i < 3; i++) flavorButtons[i].image.color = game.PreparedFlavor == i ? Palette.Flavor(i) : Color.white;
            for (int i = 0; i < 2; i++) styleButtons[i].image.color = (int)game.PreparedStyle == i ? Palette.Soda : Color.white;
            styleHint.text = game.PreparedStyle == DrivingStyle.Kart ? "Shift 부스터 · 드리프트로 충전 (시작 1개)" : "W 유지하면 계속 가속 · 코너 전 S 감속";
            status.text = game.Store.Error != null ? "저장 오류 · 도움말에서 새 가게 시작" : canMake ? "선택한 맛으로 제작 · 한 바퀴 완주 시 완성\n1번: 작은 60g / 2번: 큰 120g" : "진열대가 찼어요 · 판매하거나 확장하세요";
        }
        void UpdateRaceOrder()
        {
            var order = game.SelectedOrder;
            raceOrder.text = (game.RunStyle == DrivingStyle.Kart ? "카트" : "다운힐") + " · " + RaceRecipe.Name(game.RunMap) + " · " + Palette.FlavorName(game.RunFlavor) + " " + game.RunTargetGrams + "g\n" +
                (order == null ? "코스 완주로 제작 완료" : "주문 손님 " + Mathf.CeilToInt((float)order.Remaining) + "초") + " · 대기 " + game.Orders.Orders.Count + "/2";
        }
    }
}
