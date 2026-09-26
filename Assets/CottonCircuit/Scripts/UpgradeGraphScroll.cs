using System;
using UnityEngine.EventSystems;
namespace CottonCircuit
{
    public sealed class UpgradeGraphScroll : UnityEngine.UI.ScrollRect
    {
        public Action<float> Zoom;
        public override void OnScroll(PointerEventData data)
        {
            if (!IsActive() || Zoom == null || data.scrollDelta.y == 0) return;
            Zoom(data.scrollDelta.y); data.Use();
        }
    }
}
