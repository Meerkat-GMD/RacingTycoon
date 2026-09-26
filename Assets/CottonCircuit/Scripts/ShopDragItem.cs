using UnityEngine;
using UnityEngine.EventSystems;

namespace CottonCircuit
{
    public enum ShopDragKind { Sugar, Product }

    // The dragged product is frozen at pickup: refreshing the shelf must never
    // redirect an in-flight drag to another product. The customer is resolved at
    // drop time, so candy held in advance can be handed to a newly arrived customer.
    public sealed class ShopDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ICancelHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public ShopDragKind Kind;
        public int FlavorIndex;
        public string ProductId;
        public GameUI Owner;
        public bool IsDragging { get; private set; }
        public string CapturedProductId { get; private set; }
        public DeliveryResult LastDeliveryResult { get; private set; }
        public bool LastDropSucceeded { get; private set; }
        public int PointerId { get; private set; }
        CanvasGroup sourceGroup;

        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || IsDragging || !Owner || !Owner.ShiftInteractionsAllowed) return;
            CapturedProductId = Kind == ShopDragKind.Product ? ProductId : null;
            LastDeliveryResult = DeliveryResult.Rejected; LastDropSucceeded = false;
            if (Kind == ShopDragKind.Product && string.IsNullOrEmpty(CapturedProductId)) return;
            PointerId = data.pointerId;
            if (!Owner.BeginShiftDrag(this, data)) return;
            IsDragging = true;
            if (!sourceGroup) sourceGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            sourceGroup.alpha = Kind == ShopDragKind.Product ? 0 : .38f;
            // The source must not intercept the raycast when its ghost is over a target.
            sourceGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData data)
        {
            if (!IsDragging || data.pointerId != PointerId) return;
            if (!Owner || !Owner.ShiftInteractionsAllowed) { CancelDrag(); return; }
            Owner.MoveShiftDrag(this, data);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (IsDragging && data.pointerId == PointerId) CancelDrag();
        }

        public void OnCancel(BaseEventData data) { CancelDrag(); }

        public void OnPointerEnter(PointerEventData data) { if (Owner) Owner.HoverShelfProduct(this, true); }
        public void OnPointerExit(PointerEventData data) { if (Owner) Owner.HoverShelfProduct(this, false); }
        public void OnPointerClick(PointerEventData data)
        {
            if (Owner && !IsDragging && data.button == PointerEventData.InputButton.Left) Owner.SelectShelfProduct(this);
        }

        public void DropOn(ShopDropTarget target, PointerEventData data)
        {
            if (!IsDragging || data.pointerId != PointerId || Kind != ShopDragKind.Product || !Owner || target.Owner != Owner) return;
            // End first, so synchronous controller refreshes and repeated OnDrop
            // events cannot consume the same product a second time.
            IsDragging = false; RestoreSource();
            Owner.EndShiftDrag(this);
            if (!Owner.ShiftInteractionsAllowed) return;
            LastDeliveryResult = Owner.CompleteShiftDrop(this, target, out bool consumed);
            LastDropSucceeded = consumed;
        }

        public void CancelDrag()
        {
            IsDragging = false; RestoreSource();
            if (Owner) Owner.EndShiftDrag(this);
        }

        void RestoreSource()
        {
            if (!sourceGroup) return;
            sourceGroup.alpha = 1; sourceGroup.blocksRaycasts = true;
        }

        void OnDisable() { CancelDrag(); }
        void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
    }
}
