using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CottonCircuit
{
    /// <summary>Reusable delayed, screen-clamped details. Attach(target, text) or Attach(target, () => text).</summary>
    public sealed class HoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        static RectTransform host, panel;
        static UnityEngine.UI.Text caption;
        static HoverHint active;
        Func<string> content;
        bool hovering, allowDisabled;
        float showAt;
        Vector2 pointer;
        Camera eventCamera;
        UnityEngine.UI.Selectable selectable;

        public static void Configure(RectTransform root, Font font, Sprite background)
        {
            HideAll(); host = root;
            if (panel) Destroy(panel.gameObject);
            panel = new GameObject("HoverDetails", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup)).GetComponent<RectTransform>();
            panel.SetParent(root, false); panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
            panel.sizeDelta = new Vector2(330, 160);
            var image = panel.GetComponent<UnityEngine.UI.Image>(); image.sprite = background; image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Palette.Hex("263B42"); image.raycastTarget = false;
            var group = panel.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            caption = new GameObject("Details", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            caption.transform.SetParent(panel, false);
            var rect = caption.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(20, 16); rect.offsetMax = new Vector2(-20, -16);
            caption.font = font; caption.fontSize = 16; caption.lineSpacing = 1.18f; caption.color = Palette.Hex("FFF9EC"); caption.raycastTarget = false;
            caption.supportRichText = false; panel.gameObject.SetActive(false);
        }

        public static HoverHint Attach(GameObject target, string text, bool showWhenDisabled = false) => Attach(target, () => text, showWhenDisabled);
        public static HoverHint Attach(GameObject target, Func<string> text, bool showWhenDisabled = false)
        {
            var hint = target.GetComponent<HoverHint>() ?? target.AddComponent<HoverHint>();
            hint.content = text; hint.allowDisabled = showWhenDisabled; hint.selectable = target.GetComponent<UnityEngine.UI.Selectable>(); return hint;
        }
        public static void HideAll()
        {
            if (active) active.hovering = false;
            active = null; if (panel) panel.gameObject.SetActive(false);
        }
        public void OnPointerEnter(PointerEventData data)
        {
            HideAll(); hovering = true; active = this; showAt = Time.unscaledTime + .2f; pointer = data.position; eventCamera = data.enterEventCamera;
        }
        public void OnPointerExit(PointerEventData data) { if (active == this) HideAll(); }
        public void OnPointerDown(PointerEventData data) { if (active == this) HideAll(); }
        void OnDisable() { if (active == this) HideAll(); }
        void Update()
        {
            if (!hovering || active != this || !panel || !host) return;
            if (selectable && !selectable.IsInteractable() && !allowDisabled) { HideAll(); return; }
            if (Time.unscaledTime < showAt) return;
            string value = content == null ? "" : content();
            if (string.IsNullOrEmpty(value)) { HideAll(); return; }
            if (caption.text != value) caption.text = value;
            float height = Mathf.Clamp(caption.preferredHeight + 36, 72, 310);
            panel.sizeDelta = new Vector2(330, height);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(host, pointer, eventCamera, out Vector2 local);
            float x = local.x - host.rect.xMin + 20, y = host.rect.yMax - local.y + 20;
            if (x + 330 > host.rect.width - 16) x -= 370;
            if (y + height > host.rect.height - 16) y -= height + 40;
            panel.anchoredPosition = new Vector2(Mathf.Clamp(x, 16, host.rect.width - 346), -Mathf.Clamp(y, 16, host.rect.height - height - 16));
            panel.SetAsLastSibling(); panel.gameObject.SetActive(true);
        }
    }
}
