using UnityEngine;

namespace CottonCircuit
{
    public enum ShopStreetArtKind { Backdrop, Storefront, Customer, SpeechBubble, AngryEmote, HeartEmote }

    // Decorative, resolution-independent shop street art. Text and order icons
    // belong to the parent UI, so this graphic never captures pointer events.
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
            switch (Kind)
            {
                case ShopStreetArtKind.Backdrop: DrawBackdrop(vh); break;
                case ShopStreetArtKind.Storefront: DrawStorefront(vh); break;
                case ShopStreetArtKind.Customer: DrawCustomer(vh); break;
                case ShopStreetArtKind.SpeechBubble: DrawSpeechBubble(vh); break;
                case ShopStreetArtKind.AngryEmote: DrawAngryEmote(vh); break;
                case ShopStreetArtKind.HeartEmote: DrawHeartEmote(vh); break;
            }
        }

        void DrawBackdrop(UnityEngine.UI.VertexHelper vh)
        {
            Color sky = Palette.Hex("EAF4F3");
            Color far = Palette.Hex("DCE8E4");
            Color sidewalk = Palette.Hex("EEDCCD");
            Rect(vh, 0, 0, 1, 1, sky);
            Ellipse(vh, .88f, .83f, .065f, .11f, Palette.Hex("FFF3CE"));
            // Quiet block shapes give depth without competing with the orders.
            Rect(vh, .31f, .11f, .43f, .52f, far);
            Rect(vh, .73f, .11f, .87f, .61f, Palette.Hex("E2EBEA"));
            Rect(vh, .89f, .11f, 1, .47f, Palette.Hex("E4EBE8"));
            Rect(vh, .33f, .38f, .39f, .47f, Palette.Hex("F7FAF5"));
            Rect(vh, .78f, .43f, .82f, .52f, Palette.Hex("F7FAF5"));
            Rect(vh, .93f, .31f, .97f, .38f, Palette.Hex("F7FAF5"));
            Ellipse(vh, .45f, .58f, .09f, .025f, Palette.Hex("F8FCF9"));
            Ellipse(vh, .50f, .59f, .065f, .028f, Palette.Hex("F8FCF9"));
            Ellipse(vh, .66f, .79f, .055f, .020f, Palette.Hex("F9FCF9"));
            Ellipse(vh, .69f, .80f, .040f, .025f, Palette.Hex("F9FCF9"));
            Rect(vh, 0, 0, 1, .11f, sidewalk);
            Rect(vh, 0, .107f, 1, .119f, Palette.Ink);
            Rect(vh, 0, .080f, 1, .096f, Palette.Hex("FFF0D9"));
            for (int i = 0; i < 5; i++)
                Line(vh, .12f + i * .20f, .02f, .17f + i * .20f, .08f, .004f, Palette.Hex("D5BDB0"));
            // A small planted edge balances the storefront at the left.
            Rect(vh, .95f, .11f, .957f, .33f, Palette.Ink);
            Ellipse(vh, .93f, .28f, .036f, .075f, Palette.Hex("8AC5A8"));
            Ellipse(vh, .972f, .33f, .032f, .079f, Palette.Hex("A5D8B7"));
            Ellipse(vh, .946f, .39f, .037f, .067f, Palette.Hex("82BC9E"));
            Ellipse(vh, .967f, .19f, .026f, .04f, Palette.Pink);
        }

        void DrawStorefront(UnityEngine.UI.VertexHelper vh)
        {
            Color wall = Palette.Hex("FFF7E9");
            Color timber = Palette.Hex("D9B995");
            Color glass = Palette.Hex("BBDCDD");
            // Shop shadow, walls and two posts.
            Rect(vh, .035f, .012f, .98f, .035f, Palette.Hex("CBB5A6"));
            Rect(vh, .07f, .035f, .95f, .85f, Palette.Ink);
            Rect(vh, .086f, .049f, .934f, .837f, wall);
            Rect(vh, .10f, .067f, .14f, .67f, timber);
            Rect(vh, .88f, .067f, .92f, .67f, timber);
            // The glass display stays visible above the service counter.
            Rect(vh, .145f, .31f, .875f, .65f, Palette.Ink);
            Rect(vh, .16f, .325f, .86f, .636f, glass);
            Rect(vh, .18f, .34f, .83f, .62f, Palette.Hex("DDF0E8"));
            Rect(vh, .20f, .38f, .81f, .40f, Palette.Hex("98BCB8"));
            Rect(vh, .47f, .33f, .49f, .64f, Palette.Ink);
            // Shelves and candy jars behind the window.
            Rect(vh, .21f, .47f, .78f, .485f, Palette.Hex("B8877B"));
            Jar(vh, .28f, .485f, Palette.Pink);
            Jar(vh, .45f, .485f, Palette.Soda);
            Jar(vh, .64f, .485f, Palette.Yellow);
            Rect(vh, .12f, .18f, .90f, .32f, Palette.Ink);
            Rect(vh, .135f, .19f, .885f, .31f, Palette.Hex("E7BCA5"));
            Rect(vh, .10f, .30f, .92f, .345f, Palette.Ink);
            Rect(vh, .115f, .313f, .905f, .335f, Palette.Cream);
            Rect(vh, .16f, .052f, .84f, .175f, Palette.Hex("F6DFC5"));
            Line(vh, .49f, .07f, .49f, .165f, .006f, Palette.Hex("D6AD98"));
            // Open sign panel for the parent label.
            RoundedRect(vh, .15f, .715f, .85f, .925f, .035f, Palette.Ink);
            RoundedRect(vh, .165f, .731f, .835f, .909f, .027f, Palette.Cream);
            Ellipse(vh, .23f, .82f, .018f, .020f, Palette.Pink);
            Ellipse(vh, .77f, .82f, .018f, .020f, Palette.Soda);
            // A projecting striped awning, each stripe drawn as a convex panel.
            Polygon(vh, Palette.Ink, P(.015f,.705f), P(.985f,.705f), P(.93f,.605f), P(.07f,.605f));
            Polygon(vh, Palette.Cream, P(.031f,.695f), P(.969f,.695f), P(.920f,.620f), P(.08f,.620f));
            for (int i = 0; i < 7; i++)
            {
                float a = .032f + i * .134f;
                float b = a + .134f;
                float insetA = .08f + i * .12f;
                float insetB = insetA + .12f;
                Polygon(vh, i % 2 == 0 ? Palette.Pink : Palette.Soda,
                    P(a,.695f), P(b,.695f), P(insetB,.622f), P(insetA,.622f));
            }
            Rect(vh, .065f, .604f, .935f, .622f, Palette.Ink);
            Rect(vh, .08f, .610f, .92f, .621f, Palette.Cream);
            for (int i = 0; i < 7; i++)
            {
                float cx = .14f + i * .12f;
                Ellipse(vh, cx, .607f, .059f, .024f, i % 2 == 0 ? Palette.Pink : Palette.Soda);
            }
            // A little planter and hand lettered-looking display dots.
            Rect(vh, .015f, .075f, .14f, .14f, Palette.Ink);
            Rect(vh, .025f, .084f, .13f, .135f, Palette.Hex("D8958A"));
            Line(vh, .08f, .13f, .08f, .29f, .012f, Palette.Ink);
            Ellipse(vh, .055f, .25f, .045f, .085f, Palette.Hex("81B99D"));
            Ellipse(vh, .112f, .24f, .043f, .078f, Palette.Hex("9BD3A9"));
        }

        void Jar(UnityEngine.UI.VertexHelper vh, float x, float y, Color candy)
        {
            Rect(vh, x-.05f, y, x+.05f, y+.105f, Palette.Ink);
            Rect(vh, x-.041f, y+.008f, x+.041f, y+.096f, Palette.Hex("FFF7EA"));
            Ellipse(vh, x, y+.045f, .033f, .032f, candy);
            Rect(vh, x-.055f, y+.102f, x+.055f, y+.118f, Palette.Ink);
        }

        void DrawCustomer(UnityEngine.UI.VertexHelper vh)
        {
            int style = ((Variant % 3) + 3) % 3;
            Color shirt = style == 0 ? Palette.Soda : style == 1 ? Palette.Pink : Palette.Yellow;
            Color hair = style == 0 ? Palette.Hex("574F59") : style == 1 ? Palette.Hex("8D5B4A") : Palette.Hex("45475B");
            Color skin = style == 1 ? Palette.Hex("B87A65") : style == 2 ? Palette.Hex("E8AF88") : Palette.Hex("F1C6A6");
            Color pants = style == 1 ? Palette.Hex("65688B") : style == 2 ? Palette.Hex("5F968F") : Palette.Hex("7C789D");
            // Two separated legs and broad shoes keep the silhouette full-body.
            Polygon(vh, Palette.Ink, P(.34f,.095f), P(.48f,.095f), P(.47f,.335f), P(.32f,.335f));
            Polygon(vh, pants, P(.355f,.102f), P(.465f,.102f), P(.455f,.32f), P(.335f,.32f));
            Polygon(vh, Palette.Ink, P(.53f,.095f), P(.67f,.095f), P(.68f,.335f), P(.53f,.335f));
            Polygon(vh, pants, P(.545f,.102f), P(.655f,.102f), P(.665f,.32f), P(.545f,.32f));
            Ellipse(vh, .38f, .079f, .14f, .052f, Palette.Ink);
            Ellipse(vh, .65f, .079f, .14f, .052f, Palette.Ink);
            Ellipse(vh, .36f, .093f, .105f, .028f, Palette.Cream);
            Ellipse(vh, .63f, .093f, .105f, .028f, Palette.Cream);
            // Shoulders, sleeves and arms. The face turns toward the shop.
            Polygon(vh, Palette.Ink, P(.30f,.36f), P(.69f,.36f), P(.74f,.64f), P(.63f,.70f), P(.37f,.70f), P(.26f,.60f));
            Polygon(vh, shirt, P(.32f,.37f), P(.67f,.37f), P(.71f,.625f), P(.625f,.675f), P(.38f,.675f), P(.29f,.595f));
            Line(vh, .29f,.61f,.18f,.40f,.065f, Palette.Ink);
            Line(vh, .29f,.61f,.18f,.40f,.045f, shirt);
            Ellipse(vh, .18f,.385f,.040f,.047f,skin);
            Line(vh, .70f,.62f,.82f,.42f,.064f, Palette.Ink);
            Line(vh, .70f,.62f,.82f,.42f,.045f, shirt);
            Ellipse(vh, .82f,.405f,.04f,.047f,skin);
            Rect(vh, .455f,.64f,.555f,.735f,skin);
            Ellipse(vh, .495f,.80f,.235f,.19f,Palette.Ink);
            Ellipse(vh, .505f,.78f,.211f,.17f,skin);
            if (style == 0)
            {
                Ellipse(vh,.51f,.92f,.225f,.08f,hair);
                Ellipse(vh,.32f,.83f,.075f,.125f,hair);
                Ellipse(vh,.70f,.83f,.065f,.105f,hair);
            }
            else if (style == 1)
            {
                Ellipse(vh,.50f,.92f,.245f,.08f,hair);
                Polygon(vh,hair,P(.27f,.83f),P(.36f,.94f),P(.48f,.91f),P(.47f,.77f));
                Ellipse(vh,.78f,.88f,.10f,.085f,hair);
            }
            else
            {
                Ellipse(vh,.50f,.92f,.22f,.08f,hair);
                Rect(vh,.29f,.91f,.70f,.95f,Palette.Ink);
                Rect(vh,.32f,.916f,.68f,.944f,Palette.Hex("A6BBD0"));
                Rect(vh,.23f,.895f,.43f,.916f,Palette.Ink);
            }
            Ellipse(vh,.435f,.80f,.017f,.020f,Palette.Ink);
            Ellipse(vh,.58f,.80f,.017f,.020f,Palette.Ink);
            Ellipse(vh,.37f,.755f,.040f,.019f,Palette.Pink);
            Ellipse(vh,.635f,.755f,.040f,.019f,Palette.Pink);
            if (Angry)
            {
                Line(vh,.385f,.848f,.46f,.82f,.012f,Palette.Ink);
                Line(vh,.55f,.82f,.625f,.848f,.012f,Palette.Ink);
                Line(vh,.46f,.735f,.55f,.735f,.013f,Palette.Ink);
            }
            else
            {
                Line(vh,.46f,.735f,.505f,.713f,.013f,Palette.Ink);
                Line(vh,.505f,.713f,.55f,.735f,.013f,Palette.Ink);
            }
            if (style == 1)
                Rect(vh,.40f,.47f,.60f,.495f,Palette.Cream);
            else if (style == 2)
                Line(vh,.38f,.66f,.61f,.40f,.025f,Palette.Cream);
            else
                Ellipse(vh,.50f,.50f,.046f,.037f,Palette.Cream);
        }

        void DrawSpeechBubble(UnityEngine.UI.VertexHelper vh)
        {
            Color outline = Angry ? Palette.Hex("AB4F69") : Palette.Ink;
            Color fill = Angry ? Palette.Hex("FFE6E9") : Palette.Hex("FFFCF4");
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

        // The emotes are drawn in normalized space so the parent can use a
        // roughly 70 x 65 rect without needing a separate texture or sprite.
        void DrawAngryEmote(UnityEngine.UI.VertexHelper vh)
        {
            Color face = Palette.Hex("FFD4C9");
            Color anger = Palette.Hex("DD6678");
            Ellipse(vh, .47f, .47f, .35f, .36f, Palette.Ink);
            Ellipse(vh, .47f, .48f, .325f, .335f, face);
            Ellipse(vh, .27f, .55f, .055f, .035f, Palette.Pink);
            Ellipse(vh, .67f, .55f, .055f, .035f, Palette.Pink);
            Line(vh, .245f, .68f, .40f, .62f, .025f, Palette.Ink);
            Line(vh, .54f, .62f, .695f, .68f, .025f, Palette.Ink);
            Ellipse(vh, .35f, .55f, .026f, .035f, Palette.Ink);
            Ellipse(vh, .59f, .55f, .026f, .035f, Palette.Ink);
            Line(vh, .37f, .31f, .47f, .355f, .025f, Palette.Ink);
            Line(vh, .47f, .355f, .57f, .31f, .025f, Palette.Ink);
            // Four bent strokes form the familiar red anger mark above the face.
            Line(vh, .77f, .79f, .86f, .79f, .034f, anger);
            Line(vh, .86f, .79f, .88f, .89f, .034f, anger);
            Line(vh, .75f, .75f, .75f, .66f, .034f, anger);
            Line(vh, .75f, .75f, .85f, .73f, .034f, anger);
        }

        void DrawHeartEmote(UnityEngine.UI.VertexHelper vh)
        {
            Color heart = Palette.Hex("F39DB2");
            Color light = Palette.Hex("FFD2DD");
            // Two round lobes and a convex point keep the silhouette soft.
            Polygon(vh, Palette.Ink, P(.15f,.68f), P(.85f,.68f), P(.50f,.10f));
            Ellipse(vh, .34f, .65f, .205f, .205f, Palette.Ink);
            Ellipse(vh, .66f, .65f, .205f, .205f, Palette.Ink);
            Polygon(vh, heart, P(.19f,.67f), P(.81f,.67f), P(.50f,.155f));
            Ellipse(vh, .34f, .66f, .173f, .173f, heart);
            Ellipse(vh, .66f, .66f, .173f, .173f, heart);
            Ellipse(vh, .32f, .74f, .075f, .032f, light);
            Ellipse(vh, .26f, .68f, .026f, .020f, Palette.Cream);
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

        void Ellipse(UnityEngine.UI.VertexHelper vh, float cx, float cy, float rx, float ry, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(Point(cx,cy), tint * color, Vector2.zero);
            const int steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                float angle = i * Mathf.PI * 2 / steps;
                vh.AddVert(Point(cx + Mathf.Sin(angle)*rx, cy + Mathf.Cos(angle)*ry), tint * color, Vector2.zero);
            }
            for (int i = 0; i < steps; i++)
                vh.AddTriangle(start, start + i + 1, start + i + 2);
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
