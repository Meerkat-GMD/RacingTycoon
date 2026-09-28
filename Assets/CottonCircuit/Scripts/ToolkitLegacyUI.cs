using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    public partial class GameUI
    {
        void BindLegacy()
        {
            Q<Button>("legacyPause").clicked += game.TogglePause;
            Q<Button>("legacyAbort").clicked += game.FinishRun;
            Q<Button>("legacyReturnShop").clicked += game.ReturnToShop;
            Q<Button>("legacyDiscard").clicked += game.DiscardSelected;
            Q<Button>("legacyMakeStock").clicked += game.PrepareStock;
            Q<Button>("legacyShelf").clicked += game.BuyShelf;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                Q<Button>("legacyFlavor" + i).clicked += () => game.SetFlavor(index);
                Q<Button>("legacyUpgrade" + i).clicked += () => game.BuyUpgrade(index);
            }
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                Q<Button>("legacySize" + i).clicked += () => game.SetSize(index);
                Q<Button>("legacyStyle" + i).clicked += () => game.SetStyle(index);
                Q<Button>("legacyChooseOrder" + i).clicked += () => { var order = LegacyOrder(index); if (order != null) game.SelectOrder(order.Id); };
                Q<Button>("legacyMakeOrder" + i).clicked += () => { var order = LegacyOrder(index); if (order != null) game.MakeOrder(order.Id); };
                Q<Button>("legacyServe" + i).clicked += () => { var order = LegacyOrder(index); if (order != null) game.Serve(order.Id); };
            }
            for (int i = 0; i < 12; i++)
            {
                int index = i;
                Q<Button>("legacyStock" + i).clicked += () =>
                {
                    if (index < game.Session.Economy.Inventory.Count) game.SelectProduct(game.Session.Economy.Inventory[index].Id);
                };
            }
            Q<VisualElement>("legacyScreen").Query<Button>().ForEach(button => button.focusable = false);
        }

        CustomerOrder LegacyOrder(int index) => game.Orders != null && index < game.Orders.Orders.Count ? game.Orders.Orders[index] : null;

        void RefreshLegacy()
        {
            var session = game.Session;
            var economy = session.Economy;
            bool allowed = !session.Paused && game.Store.CanSave && (game.ContinuousMode || session.Mode == GameMode.Shop);
            SetText("legacyDay", "DAY " + economy.Day.ToString("00"));
            SetText("legacyCoins", economy.Coins.ToString("N0") + " C");
            SetText("legacyStockCount", "진열대  " + economy.Inventory.Count + " / " + economy.StockCapacity);
            SetText("legacySpeed", Mathf.RoundToInt(game.World.Kart.Speed * 3.6f) + " km/h");
            SetText("legacyRecipe", Palette.FlavorName(game.RunFlavor) + "  " + game.RunTargetGrams + "g");
            SetText("legacyDrivingStatus", session.ProductionWaiting ? "진열대가 가득 찼어요" : "직접 운전 · 이번 바퀴 " + game.RunProgress.ToString("P0"));
            Q<ProgressBar>("legacyProgress").value = (float)(game.RunProgress * 100);
            Show(Q<VisualElement>("legacyDriveHud"), game.RaceVisible);
            Show(Q<Button>("legacyAbort"), !game.ContinuousMode && session.Mode == GameMode.Racing);
            for (int i = 0; i < 2; i++)
            {
                var order = LegacyOrder(i);
                Show(Q<VisualElement>("legacyOrder" + i), order != null);
                if (order != null)
                {
                    SetText("legacyOrderText" + i, Palette.FlavorName(order.Flavor) + " · " + CandyRecipe.TargetGrams(order.Size) + "g  /  " + Math.Ceiling(order.Remaining) + "초");
                    Q<ProgressBar>("legacyPatience" + i).value = (float)(100 * order.Remaining / CustomerOrder.Patience);
                }
                Q<Button>("legacyChooseOrder" + i).SetEnabled(allowed);
                Q<Button>("legacyMakeOrder" + i).SetEnabled(allowed && economy.Inventory.Count < economy.StockCapacity);
                Q<Button>("legacyServe" + i).SetEnabled(allowed && game.SelectedProduct != null);
                Q<Button>("legacySize" + i).SetEnabled(allowed);
                Q<Button>("legacyStyle" + i).SetEnabled(allowed);
                Q<Button>("legacySize" + i).EnableInClassList("accent", game.PreparedMap == i);
                Q<Button>("legacyStyle" + i).EnableInClassList("accent", (int)game.PreparedStyle == i);
            }
            for (int i = 0; i < 12; i++)
            {
                var slot = Q<Button>("legacyStock" + i);
                Show(slot, i < economy.StockCapacity);
                var product = i < economy.Inventory.Count ? economy.Inventory[i] : null;
                slot.SetEnabled(allowed && product != null);
                slot.EnableInClassList("selected", product != null && game.SelectedProductId == product.Id);
                SetArt(Q<VisualElement>("legacyStockArt" + i), product == null ? null : UiArt.BaggedCandy(Math.Max(0, CandyRecipe.FlavorOf(product)), Math.Max(0, CandyRecipe.SizeOf(product))));
                SetText("legacyStockText" + i, product == null ? "빈 자리" : Palette.FlavorName(CandyRecipe.FlavorOf(product)) + " " + product.Grams + "g");
            }
            string[] names = { "카트 모터", "설탕통", "가게 꾸미기" };
            for (int i = 0; i < 3; i++)
            {
                Q<Button>("legacyFlavor" + i).SetEnabled(allowed);
                Q<Button>("legacyFlavor" + i).EnableInClassList("accent", game.PreparedFlavor == i);
                var upgrade = Q<Button>("legacyUpgrade" + i);
                upgrade.text = names[i] + " Lv." + economy.Levels[i] + "  " + economy.UpgradeCost(i) + " C";
                upgrade.SetEnabled(allowed && economy.Coins >= economy.UpgradeCost(i));
            }
            Q<Button>("legacyShelf").text = "진열대 확장  " + economy.ShelfCost + " C";
            Q<Button>("legacyShelf").SetEnabled(allowed && economy.ShelfLevel < 2 && economy.Coins >= economy.ShelfCost);
            Q<Button>("legacyDiscard").SetEnabled(allowed && game.SelectedProduct != null);
            Q<Button>("legacyMakeStock").SetEnabled(allowed && economy.Inventory.Count < economy.StockCapacity);
            Q<Button>("legacyMakeStock").text = game.ContinuousMode ? "다음 생산에 설정 적용" : "재고 만들기";
            Show(Q<VisualElement>("legacyResult"), session.Mode == GameMode.Results);
            SetText("legacyResultText", session.Result == null ? "완주하지 못했어요. 다시 도전해 주세요." : "완성! " + session.Result.Grams + "g\n품질 " + session.Result.Quality + " · 진열대에 보관했어요.");
        }
    }
}
