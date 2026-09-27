using UnityEngine;

namespace CottonCircuit
{
    public enum ShopArtKind { CottonCandy, SugarBag, Trash, BaggedCottonCandy }

    // Pre-rendered shop item sprites. They never participate in pointer hit testing.
    // Loose and bagged cotton candy keep growing with distance between size tiers:
    // the sprite scales about its stick base or tie knot, so a bag stays on its
    // rack clip, while the layout rect stays where the parent placed it.
    public sealed class ShopArtGraphic : UnityEngine.UI.Image
    {
        public ShopArtKind Kind;
        public int FlavorIndex;
        public int SizeTier;
        public float GrowthScale { get; private set; } = .69f;
        bool configured;

        public void Configure(ShopArtKind kind, int flavor = 0, int tier = 0)
        {
            raycastTarget = false;
            if (configured && Kind == kind && FlavorIndex == flavor && SizeTier == tier) return;
            configured = true; Kind = kind; FlavorIndex = flavor; SizeTier = tier;
            // Unfinished candy has no size tier yet and uses the Small sprite.
            int size = Mathf.Max(tier, 0);
            sprite = kind == ShopArtKind.CottonCandy ? UiArt.CottonCandy(flavor, size)
                : kind == ShopArtKind.BaggedCottonCandy ? UiArt.BaggedCandy(flavor, size)
                : kind == ShopArtKind.SugarBag ? UiArt.SugarBag(flavor) : UiArt.Trash;
            ApplyGrowth();
        }

        public void SetDistance(double meters)
        {
            // Grow throughout every tier, with a bounded final silhouette.
            float growth = .38f + .62f * Mathf.Sqrt(Mathf.Clamp01((float)(meters / (ShopShift.MetersForSize(2) * 4 / 3))));
            if (Mathf.Abs(growth - GrowthScale) < .00001f) return;
            GrowthScale = growth; ApplyGrowth();
        }

        // Each size sprite is drawn at full size at its tier's threshold distance.
        // Moving the pivot also moves the anchored position by the same fraction of
        // the rect, so the rect itself does not move; only the scale is visible.
        void ApplyGrowth()
        {
            var rect = rectTransform;
            bool candy = Kind == ShopArtKind.CottonCandy || Kind == ShopArtKind.BaggedCottonCandy;
            if (candy)
            {
                Vector2 pivot = Kind == ShopArtKind.CottonCandy ? UiArt.CandyPivot : UiArt.BagPivot;
                if (rect.pivot != pivot)
                {
                    rect.anchoredPosition += Vector2.Scale(pivot - rect.pivot, rect.sizeDelta);
                    rect.pivot = pivot;
                }
            }
            float scale = candy ? GrowthScale / UiArt.ReferenceGrowth[Mathf.Clamp(SizeTier, 0, 2)] : 1;
            if (rect.localScale.x != scale) rect.localScale = Vector3.one * scale;
        }
    }
}
