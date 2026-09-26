using UnityEngine;

namespace CottonCircuit
{
    public partial class GameUI
    {
        readonly UnityEngine.UI.Image[] streetTargets = new UnityEngine.UI.Image[ShopShift.CustomerCapacity];
        readonly ShopDropTarget[] streetDrops = new ShopDropTarget[ShopShift.CustomerCapacity];
        readonly ShopStreetGraphic[] streetPeople = new ShopStreetGraphic[ShopShift.CustomerCapacity];
        readonly ShopStreetGraphic[] streetBubbles = new ShopStreetGraphic[ShopShift.CustomerCapacity];
        readonly ShopArtGraphic[] streetOrders = new ShopArtGraphic[ShopShift.CustomerCapacity];
        readonly ShopStreetGraphic[] streetEmotes = new ShopStreetGraphic[ShopShift.CustomerCapacity];
        readonly UnityEngine.UI.Image[] streetPatience = new UnityEngine.UI.Image[ShopShift.CustomerCapacity];
        readonly UnityEngine.UI.Text[] streetFlavors = new UnityEngine.UI.Text[ShopShift.CustomerCapacity];
        readonly UnityEngine.UI.Text[] streetSizes = new UnityEngine.UI.Text[ShopShift.CustomerCapacity];
        readonly UnityEngine.UI.Text[] streetReactions = new UnityEngine.UI.Text[ShopShift.CustomerCapacity];
        UnityEngine.UI.Text streetStatus;
        ProgressionArtGraphic progressionStreetScene;
        int streetLocation = -1;

        public string ShiftCustomerIdAt(int slot) => game && game.Shift != null ? game.Shift.CustomerAt(slot)?.Id : null;

        void BuildShopStreet(RectTransform parent)
        {
            var scene = Rect(parent, "Shop street", 980, 90, 596, 314);
            if (game.HasProgression) progressionStreetScene = PrepArt(scene, "LocationStreetBackdrop", 0, 0, 596, 314, "scene", PrepPink);
            else StreetArt(scene, "Street backdrop", 0, 0, 596, 314, ShopStreetArtKind.Backdrop);
            StreetArt(scene, "Side view storefront", 6, 48, 169, 230, ShopStreetArtKind.Storefront);
            Label(scene, "솜사탕", 34, 69, 113, 30, 20, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(scene, "COTTON SHOP", 24, 211, 132, 18, 11, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            streetStatus = Label(scene, "", 13, 288, 570, 22, 12, Palette.Ink, FontStyle.Normal, TextAnchor.MiddleRight);

            for (int slot = 0; slot < streetTargets.Length; slot++)
            {
                var target = Box(scene, "CustomerDropTarget" + slot, 178 + slot * 136, 8, 132, 276, Color.clear, false);
                streetTargets[slot] = target;
                var drop = target.gameObject.AddComponent<ShopDropTarget>();
                drop.Owner = this; drop.Kind = ShopDropKind.Customer; drop.CustomerSlot = slot; streetDrops[slot] = drop;
                int customerSlot = slot;
                HoverHint.Attach(target.gameObject, () =>
                {
                    var customer = game.Shift.CustomerAt(customerSlot);
                    return customer == null ? "" : Palette.FlavorName(customer.Flavor) + (game.HasProgression && Progression.MaxSugarGrade(game.Session.Economy) == 1 ? "" : " · " + ShiftTierName(customer.Size)) + "\n" + Mathf.CeilToInt((float)customer.PatienceRemaining) + "초 남음\n주문에 맞는 제품을 끌어다 건네주세요.";
                });
                streetPeople[slot] = StreetArt(target.rectTransform, "Street customer " + slot, 25, 132, 82, 140, ShopStreetArtKind.Customer, slot);
                var bubble = StreetArt(target.rectTransform, "Order bubble " + slot, 0, 0, 132, 130, ShopStreetArtKind.SpeechBubble);
                streetBubbles[slot] = bubble;
                streetOrders[slot] = ShiftArt(bubble.rectTransform, "Requested cotton " + slot, 23, 4, 80, 77, ShopArtKind.CottonCandy);
                var size = Box(bubble.rectTransform, "Order size badge " + slot, 94, 17, 25, 27, Palette.Ink, false);
                streetSizes[slot] = Label(size.rectTransform, "소", 0, 0, 25, 27, 18, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
                streetFlavors[slot] = Label(bubble.rectTransform, "", 9, 79, 114, 21, 14, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
                streetPatience[slot] = Progress(bubble.rectTransform, 18, 104, 96, 4, Palette.Soda);
                streetPatience[slot].name = "Customer patience " + slot;
                streetEmotes[slot] = StreetArt(bubble.rectTransform, "Customer emote " + slot, 30, 16, 72, 66, ShopStreetArtKind.HeartEmote);
                streetReactions[slot] = Label(bubble.rectTransform, "", 9, 84, 114, 23, 12, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
        }

        void RefreshShopStreet(bool allowed)
        {
            int count = 0;
            int location = game.HasProgression ? game.Session.Economy.Progression.SelectedLocation : 0;
            bool showSizes = !game.HasProgression || Progression.MaxSugarGrade(game.Session.Economy) > 1;
            if (progressionStreetScene && streetLocation != location)
            {
                streetLocation = location; progressionStreetScene.Variant = location; progressionStreetScene.SetVerticesDirty();
            }
            for (int slot = 0; slot < streetTargets.Length; slot++)
            {
                var customer = game.Shift.CustomerAt(slot);
                bool exists = customer != null, angry = exists && customer.Angry, happy = exists && customer.Happy;
                bool reacting = angry || happy;
                if (exists) count++;
                streetPeople[slot].gameObject.SetActive(exists);
                streetBubbles[slot].gameObject.SetActive(exists);
                streetTargets[slot].raycastTarget = allowed && exists && !reacting;
                streetTargets[slot].color = allowed && exists && !reacting && shiftDrag &&
                    shiftDrag.Kind == ShopDragKind.Product && streetDrops[slot].IsHovered
                    ? new Color(.48f, .80f, .80f, .19f) : Color.clear;
                if (!exists) continue;
                streetPeople[slot].Configure(ShopStreetArtKind.Customer, game.HasProgression ? slot + location + customer.Flavor : slot, angry);
                streetBubbles[slot].Configure(ShopStreetArtKind.SpeechBubble, slot, angry);
                streetOrders[slot].gameObject.SetActive(!reacting);
                streetFlavors[slot].gameObject.SetActive(!reacting);
                streetSizes[slot].transform.parent.gameObject.SetActive(!reacting && showSizes);
                streetPatience[slot].transform.parent.gameObject.SetActive(!reacting);
                streetReactions[slot].gameObject.SetActive(reacting);
                streetEmotes[slot].gameObject.SetActive(reacting);
                if (reacting)
                {
                    streetEmotes[slot].Configure(happy ? ShopStreetArtKind.HeartEmote : ShopStreetArtKind.AngryEmote);
                    streetReactions[slot].text = happy ? "고마워요!" : customer.TimedOut ? "너무 오래 걸려요!" : "주문이 달라요!";
                    continue;
                }
                float patience = Mathf.Clamp01((float)(customer.PatienceRemaining / game.Shift.PatienceLimit));
                streetPatience[slot].rectTransform.sizeDelta = new Vector2(96 * patience, 4);
                streetPatience[slot].color = patience < .25f ? Palette.Pink : Palette.Soda;
                streetOrders[slot].Configure(ShopArtKind.CottonCandy, customer.Flavor, customer.Size);
                streetOrders[slot].SetDistance(ShopShift.MetersForSize(customer.Size));
                streetFlavors[slot].text = Palette.FlavorName(customer.Flavor);
                streetSizes[slot].text = ShiftTierName(customer.Size);
            }
            streetStatus.text = !game.Shift.IsOpen ? "영업을 쉬고 있어요"
                : count < ShopShift.CustomerCapacity ? "손님 " + count + "명  ·  다음 손님 " + Mathf.CeilToInt((float)game.Shift.State.NextCustomerIn) + "초 후"
                : game.HasProgression ? "손님 3명" : "손님 3명  ·  말풍선이나 손님에게 솜사탕을 건네주세요";
        }

        ShopStreetGraphic StreetArt(RectTransform parent, string name, float x, float y, float width, float height,
            ShopStreetArtKind kind, int variant = 0)
        {
            var graphic = Rect(parent, name, x, y, width, height).gameObject.AddComponent<ShopStreetGraphic>();
            graphic.raycastTarget = false; graphic.Configure(kind, variant); return graphic;
        }
    }
}
