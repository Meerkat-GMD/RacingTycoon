#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CottonCircuit.Tests
{
    public partial class RuntimeSmoke
    {
        // This scenario uses real UI events and isolated test stock. Run it alone
        // with --shop-shift-smoke --candy-rack-smoke, or as part of the full shift.
        IEnumerator CandyRackScenario()
        {
            Application.targetFrameRate = 60;
            Screen.SetResolution(1600, 900, false);
            yield return new WaitForSecondsRealtime(.3f);
            game.Initialize(System.IO.Path.Combine(output, "rack-save-" + Guid.NewGuid().ToString("N")), true, true, false);
            if (game.AutoDrive) game.ToggleAutoDrive();
            game.World.Kart.Stop();
            if (!game.Shift.IsOpen) game.StartNextDay();
            game.Tick(0, 0, false, 5);
            var economy = game.Session.Economy;
            economy.Inventory.Clear();
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            Check(GameObject.Find("Cotton candy stand") != null,
                "stock is displayed on the cotton candy stand");

            // Every maximum-size product must remain individually reachable,
            // including upgraded saves with twelve products and narrow screens.
            for (int level = 0; level <= 2; level++)
            {
                economy.Inventory.Clear(); economy.ShelfLevel = level;
                for (int i = 0; i < economy.StockCapacity; i++) AddRackStock(i % 3, 2, true);
                game.UI.Refresh(); Canvas.ForceUpdateCanvases();
                Check(economy.StockCapacity == 6 + level * 3, "rack preserves capacity " + economy.StockCapacity);
                CheckRackCount(economy.StockCapacity, economy.StockCapacity);
                CaptureShift("10-rack-full-" + economy.StockCapacity + ".png");
                CheckRackProducts();
                CheckRackDragAlignment();
                if (level == 1) CaptureShift("11-rack-nine-full.png");
                if (level == 2)
                {
                    foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 960), new Vector2Int(1920, 820) })
                    {
                        Screen.SetResolution(resolution.x, resolution.y, false);
                        yield return new WaitForSecondsRealtime(.25f);
                        game.UI.Refresh(); Canvas.ForceUpdateCanvases();
                        CheckRackProducts(); CheckShiftBounds();
                        CaptureShift("12-rack-twelve-" + resolution.x + "x" + resolution.y + ".png");
                        CheckRackDragAlignment();
                    }
                }
                // The longer incomplete tag must also fit at every capacity.
                economy.Inventory.RemoveAt(economy.Inventory.Count - 1);
                AddRackStock(1, -1);
                game.UI.Refresh(); Canvas.ForceUpdateCanvases();
                CaptureShift("12b-rack-mixed-" + economy.StockCapacity + ".png");
                CheckRackProducts();
            }

            Screen.SetResolution(1600, 900, false);
            yield return new WaitForSecondsRealtime(.25f);
            economy.Inventory.Clear(); economy.ShelfLevel = 1;
            var customer = game.Shift.CustomerAt(0);
            Check(customer != null && !customer.Angry, "rack sale fixture has an available customer");
            var sellable = AddRackStock(customer.Flavor, customer.Size);
            var incomplete = AddRackStock(1, -1);
            var retained = AddRackStock(2, 2);
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            var retainedItem = RackItemFor(retained.Id);
            Vector3 retainedPosition = retainedItem.transform.position;

            // Cancel returns the exact same bag to its existing clip.
            var source = RackItemFor(sellable.Id);
            var data = PointerAtSource(source.gameObject);
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.pointerEnterHandler);
            Check(GameObject.Find("Shelf product details").GetComponent<Text>().text.Contains(Palette.FlavorName(sellable.FlavorIndex)),
                "hover reveals the bag's flavor in the fixed detail line");
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.pointerClickHandler);
            Check(game.SelectedProductId == sellable.Id, "click selects the bag's exact product");
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.pointerExitHandler);
            CheckRaycast(data, source.gameObject, false);
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.beginDragHandler);
            CheckRackHeldSource(source);
            MoveDrag(source, data, 1250, 350);
            Check(RackHasBagArt(game.UI.ShiftDragGhost.gameObject), "carried product remains a bagged cotton candy");
            CaptureShift("13-rack-carry.png");
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.endDragHandler);
            Check(!source.IsDragging && !game.UI.ShiftDragGhost.gameObject.activeSelf && economy.Inventory.Contains(sellable),
                "canceled bag drag preserves its exact product and removes the ghost");
            CheckRackSourceRestored(source);

            // Accepted sale consumes only the selected bag and pays once.
            int beforeCoins = economy.Coins, price = economy.Price(sellable);
            DragStock(economy.Inventory.IndexOf(sellable), "CustomerDropTarget0");
            Check(!economy.Inventory.Contains(sellable) && economy.Inventory.Contains(incomplete) && economy.Inventory.Contains(retained) &&
                economy.Coins == beforeCoins + price && source.LastDeliveryResult == DeliveryResult.Sold && source.LastDropSucceeded,
                "bag sale consumes its exact identity and pays the expected price");
            Check(!game.UI.ShiftDragGhost.gameObject.activeSelf, "accepted bag sale removes the drag ghost");
            Check(RackItemFor(retained.Id) == retainedItem && Vector3.Distance(retainedItem.transform.position, retainedPosition) < .01f,
                "remaining bag stays on its original clip after a sale");

            // Incomplete candy remains visibly identified and is removable by its ID.
            var incompleteItem = RackItemFor(incomplete.Id);
            bool incompleteTag = false;
            foreach (var label in incompleteItem.GetComponentsInChildren<Text>(true))
                if (label.text.Contains("미완성")) incompleteTag = true;
            Check(incompleteTag, "undersize bag carries a readable incomplete tag");
            int beforeTrash = game.Shift.State.DayTrashed;
            DragStock(economy.Inventory.IndexOf(incomplete), "TrashDropTarget");
            Check(!economy.Inventory.Contains(incomplete) && economy.Inventory.Contains(retained) && economy.Inventory.Count == 1 &&
                game.Shift.State.DayTrashed == beforeTrash + 1 && economy.Coins == beforeCoins + price,
                "trash removes only the selected incomplete bag without paying coins");
            Check(RackItemFor(retained.Id) == retainedItem && Vector3.Distance(retainedItem.transform.position, retainedPosition) < .01f,
                "remaining bag stays on its original clip after trashing a neighbor");
            CheckRackCount(1, 9); CheckRackProducts();
            CaptureShift("14-rack-sparse.png");

            // Refresh during a drag must not rebind or hide the held item's source
            // when another inventory entry disappears.
            var neighbor = AddRackStock(0, 0);
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            source = RackItemFor(neighbor.Id); data = PointerAtSource(source.gameObject);
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.beginDragHandler);
            Check(game.TrashCandy(retained.Id), "earlier inventory entry can be removed during the held bag fixture");
            Check(source.IsDragging && source.CapturedProductId == neighbor.Id && game.UI.ActiveShiftDrag == source,
                "refresh preserves an in-flight bag when another product is removed");
            CheckRackHeldSource(source);
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.endDragHandler);
            CheckRackSourceRestored(source);

            DragStock(0, "TrashDropTarget");
            CheckRackCount(0, 9);
            int activeBags = 0;
            foreach (var item in game.UI.GetComponentsInChildren<ShopDragItem>())
                if (item.Kind == ShopDragKind.Product && !string.IsNullOrEmpty(item.ProductId)) activeBags++;
            Check(activeBags == 0, "empty stand has no draggable placeholder products");
            foreach (var label in GameObject.Find("Cotton candy stand").GetComponentsInChildren<Text>(true))
                Check(!label.gameObject.activeInHierarchy || !label.text.Contains("빈 자리"), "empty stand contains no card-slot labels");
            CaptureShift("15-rack-empty.png");
        }

        void CheckRackDragAlignment()
        {
            var source = RackItemFor(game.Session.Economy.Inventory[0].Id);
            var data = PointerAtSource(source.gameObject);
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.beginDragHandler);
            CheckHeldBagAtPointer(data, "pickup");
            foreach (var position in new[]
            {
                new Vector2(Screen.width * .5f, Screen.height * .5f),
                new Vector2(2, 2), new Vector2(Screen.width - 2, 2),
                new Vector2(2, Screen.height - 2), new Vector2(Screen.width - 2, Screen.height - 2)
            })
            {
                data.position = position;
                ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.dragHandler);
                CheckHeldBagAtPointer(data, "move");
            }
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.endDragHandler);
            CheckRackSourceRestored(source);
        }

        void CheckHeldBagAtPointer(PointerEventData data, string phase)
        {
            var ghost = game.UI.ShiftDragGhost;
            var illustration = ghost.GetComponentInChildren<ShopArtGraphic>().rectTransform;
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, illustration.TransformPoint(illustration.rect.center));
            Check(ghost.gameObject.activeInHierarchy && Vector2.Distance(center, data.position) < 1,
                "held candy stays at pointer on " + phase + " at " + Screen.width + "x" + Screen.height +
                "; pointer=" + data.position + ", candy=" + center);
            Check(!ghost.GetComponent<CanvasGroup>().blocksRaycasts, "held candy lets pointer reach drop targets");
        }

        Product AddRackStock(int flavor, int tier, bool maximumGrowth = false)
        {
            double meters = maximumGrowth ? ShopShift.LapMeters * 9 : tier < 0 ? ShopShift.LapMeters * .2 : ShopShift.MetersForSize(tier);
            var product = ShopShift.Preview(meters, flavor);
            product.Id = Guid.NewGuid().ToString("N");
            game.Session.Economy.Inventory.Add(product);
            game.Session.Economy.CompletedIds.Add(product.Id);
            return product;
        }

        ShopDragItem RackItemFor(string productId)
        {
            foreach (var item in game.UI.GetComponentsInChildren<ShopDragItem>(true))
                if (item.Kind == ShopDragKind.Product && item.ProductId == productId && item.gameObject.activeInHierarchy) return item;
            throw new Exception("RUNTIME_CHECK_FAILED no active shelf bag for product " + productId);
        }

        void CheckRackProducts()
        {
            var found = new HashSet<ShopDragItem>();
            var occupiedRects = new List<Rect>();
            var stand = GameObject.Find("Cotton candy stand").GetComponent<RectTransform>();
            foreach (var product in game.Session.Economy.Inventory)
            {
                var item = RackItemFor(product.Id);
                Check(found.Add(item), "each rack product has its own drag source");
                Check(RackHasBagArt(item.gameObject), "rack product is visibly bagged: " + item.name);
                int tier = ShopShift.SizeOf(product);
                string tierLabel = tier < 0 ? "미완성" : tier == 0 ? "소" : tier == 1 ? "중" : "대";
                bool tagged = false;
                foreach (var label in item.GetComponentsInChildren<Text>())
                    if (label.text.Contains(tierLabel)) tagged = true;
                Check(tagged, "rack product carries its size tag: " + tierLabel);
                CheckRackRaycast(PointerAtSource(item.gameObject), item);
                var itemRect = item.GetComponent<RectTransform>();
                // Test the body of the bag, not just one central pickup pixel.
                foreach (var sample in new[] { new Vector2(.25f, .4f), new Vector2(.75f, .4f), new Vector2(.25f, .7f), new Vector2(.75f, .7f) })
                {
                    var pointer = PointerAtSource(item.gameObject);
                    pointer.position = RectTransformUtility.WorldToScreenPoint(null, itemRect.TransformPoint(new Vector3(
                        itemRect.rect.xMin + itemRect.rect.width * sample.x,
                        itemRect.rect.yMin + itemRect.rect.height * sample.y, 0)));
                    CheckRackRaycast(pointer, item);
                }
                var corners = new Vector3[4]; itemRect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corner);
                    Check(point.x >= -.5f && point.x <= Screen.width + .5f && point.y >= -.5f && point.y <= Screen.height + .5f,
                        item.name + " remains inside the viewport");
                    var local = stand.InverseTransformPoint(corner);
                    Check(local.x >= stand.rect.xMin - .01f && local.x <= stand.rect.xMax + .01f &&
                        local.y >= stand.rect.yMin - .01f && local.y <= stand.rect.yMax + .01f,
                        item.name + " stays inside the stand footprint, clear of the header and trash");
                }
                foreach (var tag in item.GetComponentsInChildren<Text>())
                {
                    tag.rectTransform.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector3 local = itemRect.InverseTransformPoint(corner);
                        Check(local.x >= itemRect.rect.xMin - .01f && local.x <= itemRect.rect.xMax + .01f &&
                            local.y >= itemRect.rect.yMin - .01f && local.y <= itemRect.rect.yMax + .01f,
                            "rotated size tag stays inside its bag pickup region");
                    }
                }
                itemRect.GetWorldCorners(corners);
                var min = stand.InverseTransformPoint(corners[0]);
                var max = stand.InverseTransformPoint(corners[2]);
                var bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                foreach (var occupied in occupiedRects) Check(!bounds.Overlaps(occupied), "bag pickup regions do not overlap");
                occupiedRects.Add(bounds);
            }
        }

        void CheckRackCount(int count, int capacity)
        {
            bool found = false;
            string expected = count + " / " + capacity;
            foreach (var label in game.UI.GetComponentsInChildren<Text>())
                if (label.text.Contains("내 진열대") && label.text.Contains(expected)) found = true;
            Check(found, "rack title reports " + expected);
        }

        void CheckRackRaycast(PointerEventData pointer, ShopDragItem item)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            var target = hits.Count > 0 ? ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject) : null;
            var graphic = item.GetComponent<Graphic>();
            Check(target == item.gameObject, item.name + " receives bag pointer at " + pointer.position +
                " (first=" + (hits.Count > 0 ? hits[0].gameObject.name : "none") +
                ", depth=" + graphic.depth + ", cull=" + graphic.canvasRenderer.cull +
                ", enabled=" + graphic.raycastTarget + ")");
        }

        void CheckRackHeldSource(ShopDragItem item)
        {
            var group = item.GetComponent<CanvasGroup>();
            Check(item.IsDragging && group && group.alpha <= .001f && !group.blocksRaycasts,
                "held bag leaves its clip without a second translucent source bag");
            Check(game.UI.ShiftDragGhost && game.UI.ShiftDragGhost.gameObject.activeSelf && RackHasBagArt(game.UI.ShiftDragGhost.gameObject),
                "drag ghost displays the bagged product");
        }

        void CheckRackSourceRestored(ShopDragItem item)
        {
            var group = item.GetComponent<CanvasGroup>();
            Check(group && group.alpha >= .999f && group.blocksRaycasts && item.gameObject.activeInHierarchy,
                "cancel restores the bag's visibility and pointer target");
        }

        static bool RackHasBagArt(GameObject rootObject)
        {
            foreach (var graphic in rootObject.GetComponentsInChildren<Graphic>())
            {
                var type = graphic.GetType();
                var field = type.GetField("Kind");
                object kind = field == null ? type.GetProperty("Kind")?.GetValue(graphic, null) : field.GetValue(graphic);
                if (kind != null && kind.ToString() == "BaggedCottonCandy") return true;
            }
            return false;
        }
    }
}
#endif
