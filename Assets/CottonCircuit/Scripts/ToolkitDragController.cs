using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    // Only attaches input handling to slots authored in Business.uxml.
    public sealed class ToolkitDragController : PointerManipulator
    {
        readonly GameUI owner;
        readonly bool sugar;
        readonly int slot;

        public ToolkitDragController(GameUI owner, bool sugar, int slot)
        {
            this.owner = owner; this.sugar = sugar; this.slot = slot;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(Down);
            target.RegisterCallback<PointerMoveEvent>(Move);
            target.RegisterCallback<PointerUpEvent>(Up);
            target.RegisterCallback<PointerCancelEvent>(Cancel);
            target.RegisterCallback<PointerCaptureOutEvent>(CaptureLost);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(Down);
            target.UnregisterCallback<PointerMoveEvent>(Move);
            target.UnregisterCallback<PointerUpEvent>(Up);
            target.UnregisterCallback<PointerCancelEvent>(Cancel);
            target.UnregisterCallback<PointerCaptureOutEvent>(CaptureLost);
        }

        void Down(PointerDownEvent evt)
        {
            if (evt.button != 0 || !owner.BeginBusinessDrag(target, sugar, slot, evt.pointerId, evt.position)) return;
            target.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void Move(PointerMoveEvent evt)
        {
            if (!target.HasPointerCapture(evt.pointerId)) return;
            owner.MoveBusinessDrag(evt.pointerId, evt.position);
            evt.StopPropagation();
        }

        void Up(PointerUpEvent evt)
        {
            if (evt.button != 0 || !target.HasPointerCapture(evt.pointerId)) return;
            owner.DropBusinessDrag(evt.pointerId, evt.position);
            evt.StopPropagation();
        }

        void Cancel(PointerCancelEvent evt) { owner.CancelBusinessPointer(target, evt.pointerId); }
        void CaptureLost(PointerCaptureOutEvent evt) { owner.CancelBusinessPointer(target, evt.pointerId); }
    }

    public partial class GameUI
    {
        // These methods are the real pointer-event boundary, also used by development smoke tests.
        public bool BeginBusinessDrag(VisualElement source, bool sugar, int slot, int pointerId, Vector2 position)
        {
            if (!ShiftInteractionsAllowed || source == null || businessDragSource != null) return false;
            if (sugar && (slot < 0 || slot >= businessSugarBags.Length || source != businessSugarBags[slot] || game.SelectedMachineHasWorker ||
                game.HasProgression && !game.Shift.CanMakeFlavor(game.Shift.SelectedMachine, slot))) return false;
            if (!sugar && (slot < 0 || slot >= businessStock.Length || source != businessStock[slot])) return false;
            var product = sugar ? null : BusinessProduct(businessStockIds[slot]);
            if (!sugar && product == null) return false;
            businessDragSource = source;
            businessDraggingSugar = sugar;
            businessDragProductId = product?.Id;
            businessDragFlavor = sugar ? slot : product.FlavorIndex;
            businessPointerId = pointerId;
            businessShake.Reset();
            businessPourMessageUntil = 0;
            int tier = product == null ? 0 : ShopShift.SizeOf(product);
            SetArt(Q<VisualElement>("businessDragArt"), sugar ? UiArt.SugarBag(slot) : UiArt.BaggedCandy(product.FlavorIndex, Mathf.Max(0, tier)));
            SetText("businessDragCaption", Palette.FlavorName(businessDragFlavor) + (sugar ? " 설탕" : tier < 0 ? " · 미완성" : ""));
            source.AddToClassList("drag-source");
            Show(businessGhost, true);
            Show(Q<Label>("businessDropMessage"), !game.SelectedMachineHasWorker);
            if (!sugar) { game.SelectProduct(product.Id); game.Audio.Play(Sound.CandyDrop); }
            MoveBusinessDrag(pointerId, position);
            return true;
        }

        public void MoveBusinessDrag(int pointerId, Vector2 position)
        {
            if (businessDragSource == null || pointerId != businessPointerId) return;
            if (!ShiftInteractionsAllowed) { CancelShiftDrag(); return; }
            Vector2 local = businessScreen.WorldToLocal(position);
            businessGhost.style.translate = new Translate(local.x - 59, local.y - 58, 0);
            bool inside = businessRace.worldBound.Contains(position) && !game.SelectedMachineHasWorker;
            businessRace.EnableInClassList("drop-hover", inside);
            for (int i = 0; i < businessCustomers.Length; i++)
            {
                var customer = game.Shift.CustomerAt(i);
                businessCustomers[i].EnableInClassList("drop-hover", !businessDraggingSugar && customer != null &&
                    !customer.Angry && !customer.Happy && businessCustomers[i].worldBound.Contains(position));
            }
            var trash = Q<VisualElement>("businessTrash");
            trash.EnableInClassList("drop-hover", !businessDraggingSugar && trash.worldBound.Contains(position));
            if (!businessDraggingSugar)
            {
                SetText("businessDropMessage", game.Shift.State.BatchMeters > 0 ? "여기에 놓으면 만들던 솜사탕과 교환해요" : "여기에 놓으면 이어서 만들어요");
                return;
            }
            businessShake.FullStrokePixels = game.SugarShakeFullStrokePixels;
            bool shake = businessShake.Move(local.x, local.y, inside);
            double beforeSugar = game.Shift.State.SugarFlavor == businessDragFlavor ? game.Shift.State.SugarGrams : 0;
            int beforeCoins = game.Session.Economy.Coins;
            if (shake && game.PourSugar(businessDragFlavor, businessShake.Amount))
            {
                game.Audio.Play(Sound.SugarShake);
                businessPourMessageUntil = Time.unscaledTime + .5f;
                double poured = game.Shift.State.SugarGrams - beforeSugar;
                int cost = beforeCoins - game.Session.Economy.Coins;
                SetText("businessDropMessage", "+" + poured.ToString("0.#") + " g" + (cost > 0 ? "  ·  -" + cost + " C" : ""));
            }
            else if (shake)
            {
                businessPourMessageUntil = Time.unscaledTime + .7f;
                var state = game.Shift.State;
                SetText("businessDropMessage", state.BatchMeters > 0 && state.BatchFlavor != businessDragFlavor ? "제작 중인 맛과 달라요" :
                    state.SugarGrams >= 100 ? "설탕이 가득해요" : game.TutorialActive && game.TutorialStep != TutorialStep.PourSugar ? "설탕을 넣었어요. 직접 달려보세요!" : "재료비가 부족해요");
            }
            else if (Time.unscaledTime >= businessPourMessageUntil)
                SetText("businessDropMessage", inside ? "봉지를 위아래로 흔들어 설탕을 넣어요" : "주행 화면으로 가져와 흔들어 주세요");
        }

        public DeliveryResult DropBusinessDrag(int pointerId, Vector2 position)
        {
            if (businessDragSource == null || pointerId != businessPointerId) return DeliveryResult.Rejected;
            bool sugar = businessDraggingSugar;
            string productId = businessDragProductId;
            // Clear capture before invoking a controller action, which may synchronously refresh every slot.
            CancelShiftDrag();
            if (sugar || !ShiftInteractionsAllowed || BusinessProduct(productId) == null) return DeliveryResult.Rejected;
            if (Q<VisualElement>("businessTrash").worldBound.Contains(position))
            {
                game.TrashCandy(productId);
                return DeliveryResult.Rejected;
            }
            if (businessRace.worldBound.Contains(position) && !game.SelectedMachineHasWorker)
            {
                game.ResumeCandy(productId);
                return DeliveryResult.Rejected;
            }
            for (int i = 0; i < businessCustomers.Length; i++)
            {
                var customer = game.Shift.CustomerAt(i);
                if (customer != null && !customer.Angry && !customer.Happy && businessCustomers[i].worldBound.Contains(position))
                    return game.DeliverCandy(productId, customer.Id);
            }
            game.Audio.Play(Sound.CandyDrop);
            return DeliveryResult.Rejected;
        }

        public void CancelBusinessPointer(VisualElement source, int pointerId)
        {
            if (businessDragSource == source && pointerId == businessPointerId) CancelShiftDrag();
        }

        public void CancelShiftDrag()
        {
            var source = businessDragSource;
            businessDragSource = null;
            businessDragProductId = null;
            businessShake.Reset();
            if (source != null)
            {
                source.RemoveFromClassList("drag-source");
                if (source.HasPointerCapture(businessPointerId)) source.ReleasePointer(businessPointerId);
            }
            if (businessGhost != null) Show(businessGhost, false);
            if (businessRace != null) businessRace.RemoveFromClassList("drop-hover");
            if (uiRoot == null) return;
            var message = Q<Label>("businessDropMessage");
            if (message != null) Show(message, false);
            var trash = Q<VisualElement>("businessTrash");
            trash?.RemoveFromClassList("drop-hover");
            foreach (var customer in businessCustomers) customer?.RemoveFromClassList("drop-hover");
        }
    }
}
