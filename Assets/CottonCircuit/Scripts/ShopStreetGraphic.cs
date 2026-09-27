using UnityEngine;

namespace CottonCircuit
{
    public enum ShopStreetArtKind { Backdrop, Storefront, Customer, SpeechBubble, AngryEmote, HeartEmote }

    // The order speech bubble, a flat UI frame drawn in the shared palette.
    // Customers, the storefront, the backdrop and emotes are StreetSprite images.
    // Text and order icons belong to the parent UI, so it never captures pointer events.
    public sealed class ShopStreetGraphic : UnityEngine.UI.MaskableGraphic
    {
        public ShopStreetArtKind Kind { get; private set; }
        public int Variant { get; private set; }
        public bool Angry { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Configure(ShopStreetArtKind kind, int variant = 0, bool angry = false)
        {
            raycastTarget = false;
            if (Kind == kind && Variant == variant && Angry == angry) return;
            Kind = kind;
            Variant = variant;
            Angry = angry;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if (Kind == ShopStreetArtKind.SpeechBubble) DrawSpeechBubble(vh);
        }

        void DrawSpeechBubble(UnityEngine.UI.VertexHelper vh)
        {
            // Palette White fill; the outline is Navy, or Strawberry while the customer is angry.
            Color outline = Angry ? Palette.Pink : Palette.Ink;
            Color fill = Palette.Hex("FFF9ED");
            // The tail aims down-left at the person below, leaving room for a
            // parent pictogram in the broad central field.
            Polygon(vh, outline, P(.22f,.17f), P(.35f,.17f), P(.20f,.015f), P(.20f,.17f));
            Polygon(vh, fill, P(.24f,.18f), P(.33f,.18f), P(.215f,.055f), P(.215f,.18f));
            RoundedRect(vh,.035f,.14f,.965f,.975f,.13f,outline);
            RoundedRect(vh,.055f,.16f,.945f,.955f,.115f,fill);
            if (Angry)
            {
                Line(vh,.075f,.825f,.14f,.89f,.012f,Palette.Pink);
                Line(vh,.89f,.84f,.94f,.89f,.012f,Palette.Pink);
            }
        }

        static Vector2 P(float x, float y) { return new Vector2(x,y); }

        Vector2 Point(float x, float y)
        {
            UnityEngine.Rect r = rectTransform.rect;
            return new Vector2(r.xMin + x * r.width, r.yMin + y * r.height);
        }

        void Polygon(UnityEngine.UI.VertexHelper vh, Color tint, params Vector2[] points)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < points.Length; i++)
                vh.AddVert(Point(points[i].x, points[i].y), tint * color, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++)
                vh.AddTriangle(start, start + i, start + i + 1);
        }

        void Rect(UnityEngine.UI.VertexHelper vh, float left, float bottom, float right, float top, Color tint)
        {
            Polygon(vh, tint, P(left,bottom), P(right,bottom), P(right,top), P(left,top));
        }

        void RoundedRect(UnityEngine.UI.VertexHelper vh, float left, float bottom, float right, float top, float radius, Color tint)
        {
            radius = Mathf.Min(radius, (right-left)*.5f, (top-bottom)*.5f);
            Rect(vh, left+radius,bottom,right-radius,top,tint);
            Rect(vh, left,bottom+radius,right,top-radius,tint);
            CircleCorner(vh,left+radius,bottom+radius,radius,180,270,tint);
            CircleCorner(vh,right-radius,bottom+radius,radius,270,360,tint);
            CircleCorner(vh,right-radius,top-radius,radius,0,90,tint);
            CircleCorner(vh,left+radius,top-radius,radius,90,180,tint);
        }

        void CircleCorner(UnityEngine.UI.VertexHelper vh, float cx, float cy, float radius, int startDegrees, int endDegrees, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(Point(cx,cy), tint * color, Vector2.zero);
            const int segments = 5;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Deg2Rad * Mathf.Lerp(startDegrees,endDegrees,i/(float)segments);
                vh.AddVert(Point(cx+Mathf.Cos(angle)*radius,cy+Mathf.Sin(angle)*radius),tint*color,Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
                vh.AddTriangle(start,start+i+1,start+i+2);
        }

        void Line(UnityEngine.UI.VertexHelper vh, float ax, float ay, float bx, float by, float thickness, Color tint)
        {
            Vector2 a = Point(ax,ay), b = Point(bx,by);
            Vector2 side = new Vector2(-(b-a).y,(b-a).x).normalized * Mathf.Min(rectTransform.rect.width,rectTransform.rect.height) * thickness * .5f;
            int start = vh.currentVertCount;
            vh.AddVert(a-side,tint*color,Vector2.zero);
            vh.AddVert(a+side,tint*color,Vector2.zero);
            vh.AddVert(b+side,tint*color,Vector2.zero);
            vh.AddVert(b-side,tint*color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);
            vh.AddTriangle(start,start+2,start+3);
        }
    }
}
