using UnityEngine;

namespace CottonCircuit
{
    public partial class GameUI
    {
        readonly ShopDragItem[] shiftStock = new ShopDragItem[12];
        readonly ShopArtGraphic[] shiftStockArt = new ShopArtGraphic[12];
        readonly UnityEngine.UI.Image[] shiftStockHits = new UnityEngine.UI.Image[12];
        readonly UnityEngine.UI.Image[] shiftStockTags = new UnityEngine.UI.Image[12];
        readonly UnityEngine.UI.Text[] shiftStockGrades = new UnityEngine.UI.Text[12];
        UnityEngine.UI.Text shiftShelfCount, shiftShelfDetails;
        CandyRackGraphic shiftRack;
        string shiftHoveredProductId;

        void BuildCandyRackShelf(RectTransform shelf)
        {
            shiftShelfCount = Label(shelf, "내 진열대  0 / 6", 16, 11, 300, 28, 18, Palette.Ink, FontStyle.Bold);
            if (!game.HasProgression) Label(shelf, "F로 꺼내면 여기에 보관돼요", 331, 16, 248, 20, 12, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleRight);
            var stand = Rect(shelf, "Cotton candy stand", 12, 43, CandyRackGraphic.Width, CandyRackGraphic.Height);
            shiftRack = stand.gameObject.AddComponent<CandyRackGraphic>();
            shiftRack.raycastTarget = false;
            for (int i = 0; i < shiftStock.Length; i++)
            {
                // A transparent hit area belongs to each bag; the stand and empty clips never catch a pointer.
                var hit = Box(stand, "StockItem" + i, 0, 0, 74, 92, Color.clear, false);
                shiftStockHits[i] = hit;
                var item = hit.gameObject.AddComponent<ShopDragItem>();
                item.Owner = this; item.Kind = ShopDragKind.Product;
                shiftStock[i] = item;
                int stockIndex = i;
                HoverHint.Attach(hit.gameObject, () =>
                {
                    var product = RackProduct(shiftStock[stockIndex].ProductId);
                    if (product == null) return "";
                    int tier = ShopShift.SizeOf(product);
                    string size = game.HasProgression && Progression.MaxSugarGrade(game.Session.Economy) == 1 ? "" : " · " + ShiftTierName(tier);
                    return Palette.FlavorName(product.FlavorIndex) + size + "\n" + ShiftDistanceLabel(product.DistanceMeters) + " · " + ShopShift.StarText(product.Quality) + "\n" +
                        (tier < 0 ? "판매 불가" : game.Session.Economy.Price(product).ToString("N0") + " C · 별 보너스 +" + game.Session.Economy.StarBonus(product) + " C") + "\n손님에게 건네거나 주행 화면에서 이어 만드세요.";
                });
                var art = ShiftArt(hit.rectTransform, "Bagged shelf cotton candy", 0, 0, 74, 92, ShopArtKind.BaggedCottonCandy);
                // The bag fills its pickup rect at every capacity without keeping the sprite's
                // aspect, so the tie knot (the art pivot) lands on CandyRackGraphic.ClipPoint.
                art.preserveAspect = false;
                art.rectTransform.anchorMin = Vector2.zero; art.rectTransform.anchorMax = Vector2.one;
                art.rectTransform.sizeDelta = Vector2.zero; art.rectTransform.anchoredPosition = Vector2.zero;
                shiftStockArt[i] = art;
                var tag = Box(hit.rectTransform, "Paper grade tag", 43, 68, 21, 20, Palette.Cream, false);
                tag.rectTransform.localRotation = Quaternion.Euler(0, 0, -8);
                shiftStockTags[i] = tag;
                shiftStockGrades[i] = Label(tag.rectTransform, "", 0, 0, 21, 20, 12, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            shiftShelfDetails = Label(shelf, game.HasProgression ? "" : "손님에게 건네거나 주행 화면으로 가져와 이어 만드세요", 16, 267, 439, 18, 11, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            shiftShelfDetails.name = "Shelf product details";
        }

        void RefreshShiftShelf(bool allowed)
        {
            var economy = game.Session.Economy;
            int capacity = economy.StockCapacity;
            shiftShelfCount.text = "내 진열대  " + economy.Inventory.Count + " / " + capacity;
            shiftRack.Configure(capacity);
            BindRackProducts(capacity);
            for (int i = 0; i < shiftStock.Length; i++)
            {
                var hit = shiftStockHits[i];
                hit.gameObject.SetActive(i < capacity);
                if (i >= capacity) continue;
                Rect placement = CandyRackGraphic.BagRect(i, capacity);
                hit.rectTransform.anchoredPosition = new Vector2(placement.x, -placement.y);
                hit.rectTransform.sizeDelta = placement.size;
                var product = RackProduct(shiftStock[i].ProductId);
                hit.raycastTarget = allowed && product != null;
                var art = shiftStockArt[i];
                art.gameObject.SetActive(product != null);
                shiftStockTags[i].gameObject.SetActive(product != null);
                if (product == null) continue;

                int tier = ShopShift.SizeOf(product);
                shiftStock[i].FlavorIndex = product.FlavorIndex;
                art.Configure(ShopArtKind.BaggedCottonCandy, product.FlavorIndex, tier);
                art.SetDistance(product.DistanceMeters);
                bool selected = product.Id == game.SelectedProductId || product.Id == shiftHoveredProductId;
                var tag = shiftStockTags[i];
                bool sized = !game.HasProgression || Progression.MaxSugarGrade(economy) > 1;
                float width = tier < 0 ? 42 : sized ? 46 : 34;
                float rotatedHeight = Mathf.Sin(8 * Mathf.Deg2Rad) * width + Mathf.Cos(8 * Mathf.Deg2Rad) * 19;
                float tagTop = Mathf.Min(placement.height * .76f, placement.height - rotatedHeight - 2);
                tag.rectTransform.anchoredPosition = new Vector2(placement.width - width - 4, -tagTop);
                tag.rectTransform.sizeDelta = new Vector2(width, 19);
                tag.color = tier < 0 ? Palette.Hex("F8D38C") : selected ? Palette.Yellow : Palette.Hex("FFF0D4");
                var grade = shiftStockGrades[i];
                grade.rectTransform.sizeDelta = new Vector2(width, 19);
                grade.fontSize = tier < 0 ? 9 : 10;
                // Unfinished candy cannot be sold yet, so its tag says so instead of showing stars.
                grade.text = tier < 0 ? ShiftTierName(tier) : (sized ? ShiftTierName(tier) + " " : "") + ShopShift.StarText(product.Quality);
            }
            RefreshShelfDetails();
        }

        void BindRackProducts(int capacity)
        {
            // Inventory is compacted after sales. Clip bindings are not: a neighbor
            // keeps its position and drag source when an earlier product is removed.
            for (int i = 0; i < shiftStock.Length; i++)
                if (i >= capacity || RackProduct(shiftStock[i].ProductId) == null)
                    shiftStock[i].ProductId = null;
            foreach (var product in game.Session.Economy.Inventory)
            {
                int empty = -1;
                bool bound = false;
                for (int i = 0; i < capacity; i++)
                {
                    if (shiftStock[i].ProductId == product.Id) { bound = true; break; }
                    if (empty < 0 && shiftStock[i].ProductId == null) empty = i;
                }
                if (!bound && empty >= 0) shiftStock[empty].ProductId = product.Id;
            }
        }

        Product RackProduct(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var product in game.Session.Economy.Inventory)
                if (product.Id == id) return product;
            return null;
        }

        void RefreshShelfDetails()
        {
            if (!shiftShelfDetails) return;
            if (game.HasProgression) { shiftShelfDetails.text = ""; return; }
            var product = shiftDrag && shiftDrag.Kind == ShopDragKind.Product ? RackProduct(shiftDrag.CapturedProductId) : null;
            if (product == null) product = RackProduct(shiftHoveredProductId) ?? game.SelectedProduct;
            shiftShelfDetails.color = product == null ? Palette.Muted : Palette.Ink;
            shiftShelfDetails.text = product == null ? "손님에게 건네거나 주행 화면으로 가져와 이어 만드세요" :
                Palette.FlavorName(product.FlavorIndex) + " · " + ShiftTierName(ShopShift.SizeOf(product)) + " · " +
                ShiftDistanceLabel(product.DistanceMeters) + (ShopShift.SizeOf(product) < 0 ? " · 판매 불가" : " · " + ShopShift.StarText(product.Quality));
        }

        public void HoverShelfProduct(ShopDragItem item, bool hovered)
        {
            if (item.Owner != this || item.Kind != ShopDragKind.Product) return;
            if (hovered) shiftHoveredProductId = item.ProductId;
            else if (shiftHoveredProductId == item.ProductId) shiftHoveredProductId = null;
            RefreshShelfDetails();
        }

        public void SelectShelfProduct(ShopDragItem item)
        {
            if (item.Owner == this && item.Kind == ShopDragKind.Product && ShiftInteractionsAllowed)
                game.SelectProduct(item.ProductId);
        }
    }
}
