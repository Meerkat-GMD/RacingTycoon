using UnityEngine;
using UnityEngine.EventSystems;
namespace CottonCircuit
{
    public sealed class UpgradeGraphFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public UpgradeGraphGraphic Graph;
        public string NodeId;
        public void OnPointerEnter(PointerEventData data) { if (Graph) Graph.Focus(NodeId); }
        public void OnPointerExit(PointerEventData data) { if (Graph) Graph.Focus(null); }
        void OnDisable() { if (Graph) Graph.Focus(null); }
    }
}
