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
        static string SizeName(int size) => size < 0 ? "미완성" : size == 0 ? "작은" : "큰";
        static string ProductName(Product product) => Palette.FlavorName(CandyRecipe.FlavorOf(product)) + " · " + SizeName(CandyRecipe.SizeOf(product));
        CustomerOrder OrderAt(int i) => i < game.Orders.Orders.Count ? game.Orders.Orders[i] : null;
        void BuildShop()
        {
            var p = shop.GetComponent<RectTransform>();
            Label(p, "01  /  MADE TO ORDER", 1240, 136, 320, 25, 12, Palette.Muted, FontStyle.Bold);
            Label(p, "주문을 받았어요", 1240, 172, 320, 48, 28, Palette.Ink, FontStyle.Bold);
            Label(p, "재고가 있으면 골라서 건네고,\n없으면 직접 만들어 돌아오세요.", 1240, 225, 320, 53, 16, Palette.Muted);
            var stats = Box(p, "Shop reputation", 1240, 294, 320, 60, Color.white);
            business = Label(stats.rectTransform, "", 14, 7, 292, 49, 15, Palette.Ink);
            string[] names = { "카트 모터", "설탕통", "가게 꾸미기" };
            string[] hints = { "최고 속도 증가", "수집량 +15%", "판매가 증가" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var row = Box(p, "Upgrade " + i, 1240, 374 + 69 * i, 320, 61, Color.white);
                upgradeTexts[i] = Label(row.rectTransform, names[i], 13, 5, 192, 29, 16, Palette.Ink, FontStyle.Bold);
                Label(row.rectTransform, hints[i], 13, 34, 188, 22, 12, Palette.Muted);
                upgradeButtons[i] = ButtonAt(row.rectTransform, "120", 207, 10, 98, 41, Palette.Yellow, Palette.Ink, () => game.BuyUpgrade(index), 16);
            }
            var shelf = Box(p, "Shelf expansion", 1240, 581, 320, 65, Color.white);
            shelfText = Label(shelf.rectTransform, "", 13, 7, 189, 52, 16, Palette.Ink, FontStyle.Bold);
            shelfButton = ButtonAt(shelf.rectTransform, "160", 207, 12, 98, 41, Palette.Soda, Palette.Ink, () => game.BuyShelf(), 16);
            Label(p, "미리 만들기 · 크기 선택", 1240, 667, 320, 28, 14, Palette.Muted);
            smallButton = ButtonAt(p, "작은 · 60g", 1240, 703, 155, 42, Palette.Pink, Palette.Ink, () => game.SetSize(0), 16);
            largeButton = ButtonAt(p, "큰 · 120g", 1404, 703, 156, 42, Color.white, Palette.Ink, () => game.SetSize(1), 16);
            startButton = ButtonAt(p, "재고 미리 만들기  →", 1240, 763, 320, 55, Palette.Ink, Color.white, () => game.PrepareStock(), 19);
            status = Label(p, "", 1240, 831, 320, 43, 12, Palette.Muted, FontStyle.Normal, TextAnchor.UpperCenter);
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
            status.text = game.Store.Error != null ? "저장 오류 · 도움말에서 새 가게 시작" : canMake ? "작은 50g+ / 큰 100g+ · 최대 30초 제작\n가장 많이 모은 맛으로 주문을 맞춰요" : "진열대가 찼어요 · 판매하거나 확장하세요";
        }
        void UpdateRaceOrder()
        {
            var order = game.SelectedOrder;
            raceOrder.text = (game.RunFlavor < 0 ? "미리 만들기" : Palette.FlavorName(game.RunFlavor) + " 주문") + " · 목표 " + game.RunTargetGrams + "g" +
                (game.RunFlavor < 0 ? "" : order == null ? " · 주문 시간 종료" : " · 손님 " + Mathf.CeilToInt((float)order.Remaining) + "초") + "\n대기 손님 " + game.Orders.Orders.Count + " / 2";
        }
    }
}
