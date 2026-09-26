using System;
using UnityEngine;

namespace CottonCircuit
{
    // A pre-rendered shop street sprite: a customer, the storefront, the street
    // backdrop or an emote. Text and order icons belong to the parent UI, so it
    // never captures pointer events. Speech bubbles stay ShopStreetGraphic shapes.
    public sealed class StreetSprite : UnityEngine.UI.Image
    {
        public ShopStreetArtKind Kind { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Configure(ShopStreetArtKind kind, int variant = 0, bool angry = false)
        {
            raycastTarget = false;
            Kind = kind;
            preserveAspect = kind == ShopStreetArtKind.Customer || kind == ShopStreetArtKind.HeartEmote || kind == ShopStreetArtKind.AngryEmote;
            sprite = SpriteFor(kind, variant, angry);
        }

        static Sprite SpriteFor(ShopStreetArtKind kind, int variant, bool angry)
        {
            switch (kind)
            {
                case ShopStreetArtKind.Customer: return UiArt.Customer(((variant % 3) + 3) % 3, angry);
                case ShopStreetArtKind.Storefront: return UiArt.Storefront;
                case ShopStreetArtKind.Backdrop: return UiArt.LocationStreet(0);
                case ShopStreetArtKind.HeartEmote: return UiArt.HeartEmote;
                case ShopStreetArtKind.AngryEmote: return UiArt.AngryEmote;
                default: throw new ArgumentException(kind + " is drawn by ShopStreetGraphic, not StreetSprite");
            }
        }
    }
}
