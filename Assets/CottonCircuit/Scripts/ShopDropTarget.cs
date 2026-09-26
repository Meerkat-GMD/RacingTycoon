using UnityEngine;
using UnityEngine.EventSystems;

namespace CottonCircuit
{
    public enum ShopDropKind { Customer, Trash, Race }

    public sealed class ShopDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public ShopDropKind Kind;
        public GameUI Owner;
        public int CustomerSlot = -1;
        public string CustomerId => Owner ? Owner.ShiftCustomerIdAt(CustomerSlot) : null;
        public bool IsHovered { get; private set; }

        public void OnDrop(PointerEventData data)
        {
            if (!Owner || !Owner.ShiftInteractionsAllowed || !data.pointerDrag) return;
            var item = data.pointerDrag.GetComponent<ShopDragItem>();
            if (item) item.DropOn(this, data);
            IsHovered = false;
        }

        public void OnPointerEnter(PointerEventData data)
        {
            var item = data.pointerDrag ? data.pointerDrag.GetComponent<ShopDragItem>() : null;
            IsHovered = item && item.IsDragging && item.Kind == ShopDragKind.Product;
        }

        public void OnPointerExit(PointerEventData data) { IsHovered = false; }
        void OnDisable() { IsHovered = false; }
    }
}
