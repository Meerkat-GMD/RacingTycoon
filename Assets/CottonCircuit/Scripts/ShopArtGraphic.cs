using UnityEngine;

namespace CottonCircuit
{
    public enum ShopArtKind { CottonCandy, SugarBag, Customer, Trash, BaggedCottonCandy }

    // Small resolution-independent illustrations use the same ink and pastel
    // palette as the 3D shop. They never participate in pointer hit testing.
    public sealed class ShopArtGraphic : UnityEngine.UI.MaskableGraphic
    {
        public ShopArtKind Kind;
        public int FlavorIndex;
        public int SizeTier;
        public bool Angry;
        public float GrowthScale { get; private set; } = .69f;
        // Shared outlines avoid allocating a polygon array whenever stock changes.
        static readonly Vector2[] CellophaneOutline =
        {
            new Vector2(.11f,.92f), new Vector2(.83f,.98f), new Vector2(.91f,.64f),
            new Vector2(.86f,.40f), new Vector2(.73f,.26f), new Vector2(.5f,.14f),
            new Vector2(.28f,.245f), new Vector2(.135f,.385f), new Vector2(.075f,.65f)
        };
        static readonly Vector2[] CellophaneTail =
        {
            new Vector2(.49f,.15f), new Vector2(.43f,.065f),
            new Vector2(.56f,.08f), new Vector2(.52f,.15f)
        };
        public void Configure(ShopArtKind kind, int flavor = 0, int tier = 0, bool angry = false)
        {
            if (Kind == kind && FlavorIndex == flavor && SizeTier == tier && Angry == angry) return;
            Kind = kind; FlavorIndex = flavor; SizeTier = tier; Angry = angry; SetVerticesDirty();
        }

        public void SetDistance(double meters)
        {
            // Grow throughout every tier, with a bounded final silhouette. The
            // floss thickens down a long stick, and its strands turn with distance.
            float growth = .38f + .62f * Mathf.Sqrt(Mathf.Clamp01((float)(meters / (ShopShift.MetersForSize(2) * 4 / 3))));
            float phase = (float)(meters / ShopShift.LapMeters * Mathf.PI * 6 % (Mathf.PI * 2));
            if (Mathf.Abs(growth - GrowthScale) < .00001f && Mathf.Abs(phase - spinPhase) < .01f) return;
            GrowthScale = growth; spinPhase = phase; SetVerticesDirty();
        }

        float spinPhase;
        float Wound => Mathf.Clamp01((GrowthScale - .38f) / .62f);

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            switch (Kind)
            {
                case ShopArtKind.SugarBag: Bag(vh); break;
                case ShopArtKind.Customer: Person(vh); break;
                case ShopArtKind.Trash: Bin(vh); break;
                case ShopArtKind.BaggedCottonCandy: BaggedCandy(vh); break;
                default: Candy(vh); break;
            }
        }

        void Candy(UnityEngine.UI.VertexHelper vh)
        {
            Color tint = Palette.Flavor(FlavorIndex), shade = Color.Lerp(tint, Palette.Ink, .11f), light = Color.Lerp(tint, Color.white, .40f);
            // The whole stick stays visible; floss starts at its tip and fills out downward.
            PaperStick(vh, .03f, .9f, .04f);
            FluffyCotton(vh, .93f, .2f + .56f * Wound, .1f + .27f * Wound, tint, shade, light);
        }

        // A soft cloud of floss without an ink outline: overlapping lobes make an uneven
        // fluffy rim, a translucent halo blurs it, and thin fibers swirl with distance.
        // Small batches cling to the stick tip; larger ones round out downward.
        void FluffyCotton(UnityEngine.UI.VertexHelper vh, float top, float height, float halfWidth, Color tint, Color shade, Color light)
        {
            float halfHeight = height * .5f, centerY = top - halfHeight;
            const int lobes = 14;
            shade = Color.Lerp(shade, tint, .45f);
            Color halo = Color.Lerp(tint, Color.white, .25f); halo.a = .32f;
            for (int i = 0; i < lobes; i++)
                Lobe(vh, i, centerY, halfWidth, halfHeight, .84f, .4f, halo);
            Puff(vh, .5f, centerY, halfWidth * .86f, halfHeight * .86f, shade);
            for (int i = 0; i < lobes; i++)
                Lobe(vh, i, centerY, halfWidth, halfHeight, .78f, .31f, shade);
            Puff(vh, .49f, centerY + halfHeight * .04f, halfWidth * .8f, halfHeight * .8f, tint);
            for (int i = 0; i < lobes; i++)
                Lobe(vh, i + lobes, centerY, halfWidth, halfHeight, .6f, .3f, tint);
            Color glow = light; glow.a = .6f;
            for (int i = 0; i < 5; i++)
            {
                float angle = -1.2f + i * .38f;
                Puff(vh, .5f + Mathf.Sin(angle) * halfWidth * .5f, centerY + Mathf.Cos(angle) * halfHeight * .5f,
                    halfWidth * (.2f + .06f * Hash(i, 5)), halfHeight * (.17f + .05f * Hash(i, 6)), glow);
            }
            Color fiber = Color.Lerp(tint, Color.white, .7f); fiber.a = .55f;
            Color deepFiber = shade; deepFiber.a = .4f;
            for (int i = 0; i < 28; i++)
            {
                float angle = spinPhase + i * 2.4f, reach = .2f + .62f * Hash(i, 3), length = .22f + .2f * Hash(i, 4);
                float x = .5f + Mathf.Sin(angle) * halfWidth * reach, y = centerY + Mathf.Cos(angle) * halfHeight * reach;
                Line(vh, x, y, x + Mathf.Cos(angle) * halfWidth * length, y - Mathf.Sin(angle) * halfHeight * length,
                    i % 3 == 0 ? .01f : .008f, i % 3 == 0 ? deepFiber : fiber);
            }
        }

        void Lobe(UnityEngine.UI.VertexHelper vh, int index, float centerY, float halfWidth, float halfHeight, float distance, float size, Color tint)
        {
            float angle = (index % 14 + .5f * (index / 14)) * Mathf.PI * 2 / 14, jitter = .86f + .24f * Hash(index, 1);
            Puff(vh, .5f + Mathf.Sin(angle) * halfWidth * distance * jitter, centerY + Mathf.Cos(angle) * halfHeight * distance * jitter,
                halfWidth * size * jitter, halfHeight * size * (.9f + .2f * Hash(index, 2)), tint);
        }

        static float Hash(int index, int salt)
        {
            float value = Mathf.Sin(index * 12.9898f + salt * 78.233f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        // A thin white paper stick with a faint edge so it reads on cream panels.
        void PaperStick(UnityEngine.UI.VertexHelper vh, float bottom, float top, float width)
        {
            Line(vh, .5f, bottom, .5f, top, width, Palette.Hex("D8CEBF"));
            Line(vh, .497f, bottom + .004f, .497f, top - .004f, width * .62f, Color.white);
        }

        void BaggedCandy(UnityEngine.UI.VertexHelper vh)
        {
            Color tint = Palette.Flavor(FlavorIndex);
            Color edge = new Color(.64f,.76f,.80f,.78f);
            Color fold = new Color(.76f,.85f,.87f,.64f);
            Color reflection = new Color(1,1,1,.78f);
            Color shade = Color.Lerp(tint, Palette.Ink, .14f);
            Color middle = Color.Lerp(tint, Palette.Ink, .035f);
            Color light = Color.Lerp(tint, Color.white, .35f);

            // A very light film behind the candy leaves its flavor color visible.
            Polygon(vh, new Color(.88f,.96f,1,.075f), CellophaneOutline);
            Outline(vh, CellophaneOutline, .012f, edge);
            Line(vh,.11f,.92f,.29f,.79f,.011f,fold);
            Line(vh,.11f,.92f,.27f,.90f,.015f,reflection);
            Line(vh,.83f,.98f,.71f,.80f,.011f,fold);
            Line(vh,.83f,.98f,.85f,.77f,.019f,reflection);

            // The stick rises from the tied tail, and the floss fills out down it as it grows.
            PaperStick(vh, .1f, .86f, .026f);
            FluffyCotton(vh, .89f, .24f + .44f * Wound, .11f + .21f * Wound, tint, shade, light);

            // Short angular highlights describe crinkled cellophane without
            // covering the cloud in a solid white front face.
            Line(vh,.135f,.84f,.12f,.66f,.022f,reflection);
            Line(vh,.14f,.56f,.18f,.42f,.016f,new Color(1,1,1,.48f));
            Line(vh,.855f,.68f,.83f,.46f,.018f,reflection);
            Line(vh,.83f,.46f,.75f,.35f,.012f,new Color(1,1,1,.60f));
            Line(vh,.28f,.245f,.43f,.24f,.009f,fold);
            Line(vh,.73f,.26f,.57f,.23f,.009f,fold);
            Line(vh,.32f,.30f,.5f,.14f,.009f,fold);
            Line(vh,.67f,.31f,.5f,.14f,.009f,fold);
            Polygon(vh, new Color(.86f,.94f,.96f,.25f), CellophaneTail);
            Outline(vh, CellophaneTail, .011f, edge);
            Line(vh,.48f,.135f,.49f,.078f,.009f,reflection);
            Line(vh,.455f,.151f,.54f,.14f,.029f,shade);
            Line(vh,.46f,.155f,.54f,.145f,.012f,light);
        }

        void Outline(UnityEngine.UI.VertexHelper vh, Vector2[] points, float width, Color tint)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                Line(vh, a.x, a.y, b.x, b.y, width, tint);
            }
        }

        void Bag(UnityEngine.UI.VertexHelper vh)
        {
            Color tint = Palette.Flavor(FlavorIndex);
            Polygon(vh, Palette.Ink, new Vector2(.23f,.91f), new Vector2(.77f,.91f), new Vector2(.72f,.77f), new Vector2(.85f,.16f), new Vector2(.75f,.08f), new Vector2(.22f,.08f), new Vector2(.14f,.16f), new Vector2(.28f,.77f));
            Polygon(vh, Color.Lerp(tint,Color.white,.72f), new Vector2(.25f,.88f), new Vector2(.75f,.88f), new Vector2(.69f,.77f), new Vector2(.81f,.17f), new Vector2(.73f,.115f), new Vector2(.25f,.115f), new Vector2(.18f,.17f), new Vector2(.31f,.77f));
            Line(vh,.28f,.78f,.71f,.78f,.028f,Palette.Ink);
            Line(vh,.26f,.85f,.74f,.85f,.024f,tint);
            Polygon(vh,tint,new Vector2(.25f,.61f),new Vector2(.75f,.61f),new Vector2(.79f,.29f),new Vector2(.20f,.29f));
            Puff(vh,.5f,.46f,.16f,.13f,Color.white);
            if (FlavorIndex == 0)
            {
                Polygon(vh,Palette.Pink,new Vector2(.4f,.48f),new Vector2(.45f,.53f),new Vector2(.56f,.53f),new Vector2(.61f,.47f),new Vector2(.51f,.36f));
                Line(vh,.46f,.54f,.56f,.54f,.034f,Palette.Soda);
            }
            else if (FlavorIndex == 1)
            {
                Puff(vh,.47f,.47f,.065f,.052f,Palette.Soda); Puff(vh,.57f,.42f,.038f,.031f,Palette.Soda); Puff(vh,.55f,.53f,.022f,.018f,Palette.Soda);
            }
            else
            {
                for(int i=0;i<5;i++) { float angle=i*Mathf.PI*2/5; Puff(vh,.5f+Mathf.Sin(angle)*.065f,.46f+Mathf.Cos(angle)*.055f,.05f,.04f,Palette.Yellow); }
                Puff(vh,.5f,.46f,.025f,.022f,Palette.Ink);
            }
            Line(vh,.30f,.19f,.69f,.19f,.018f,Color.Lerp(tint,Palette.Ink,.14f));
        }

        void Person(UnityEngine.UI.VertexHelper vh)
        {
            Color skin = Palette.Hex("F4C2A0"), hair=Palette.Hex("65515A"), shirt=Angry?Palette.Pink:Palette.Soda;
            Puff(vh,.5f,.62f,.29f,.30f,Palette.Ink);
            Puff(vh,.5f,.63f,.275f,.285f,hair);
            Polygon(vh,Palette.Ink,new Vector2(.13f,.04f),new Vector2(.17f,.26f),new Vector2(.32f,.35f),new Vector2(.68f,.35f),new Vector2(.82f,.26f),new Vector2(.88f,.04f));
            Polygon(vh,shirt,new Vector2(.15f,.055f),new Vector2(.19f,.25f),new Vector2(.34f,.325f),new Vector2(.66f,.325f),new Vector2(.80f,.25f),new Vector2(.86f,.055f));
            Puff(vh,.5f,.36f,.085f,.10f,skin);
            Puff(vh,.26f,.57f,.05f,.062f,skin); Puff(vh,.74f,.57f,.05f,.062f,skin);
            Puff(vh,.5f,.60f,.225f,.225f,skin);
            Puff(vh,.39f,.77f,.145f,.10f,hair); Puff(vh,.61f,.79f,.12f,.09f,hair);
            Puff(vh,.41f,.60f,.019f,.023f,Palette.Ink); Puff(vh,.59f,.60f,.019f,.023f,Palette.Ink);
            Puff(vh,.345f,.54f,.042f,.025f,Color.Lerp(skin,Palette.Pink,.6f)); Puff(vh,.655f,.54f,.042f,.025f,Color.Lerp(skin,Palette.Pink,.6f));
            if(Angry)
            {
                Line(vh,.365f,.665f,.455f,.64f,.015f,Palette.Ink); Line(vh,.545f,.64f,.635f,.665f,.015f,Palette.Ink);
                Line(vh,.455f,.455f,.5f,.472f,.014f,Palette.Ink); Line(vh,.5f,.472f,.545f,.455f,.014f,Palette.Ink);
                Line(vh,.83f,.79f,.9f,.84f,.015f,Palette.Pink); Line(vh,.84f,.73f,.94f,.74f,.015f,Palette.Pink);
            }
            else
            {
                Line(vh,.45f,.49f,.49f,.469f,.014f,Palette.Ink); Line(vh,.49f,.469f,.55f,.49f,.014f,Palette.Ink);
            }
            Polygon(vh,Palette.Cream,new Vector2(.35f,.28f),new Vector2(.41f,.25f),new Vector2(.59f,.25f),new Vector2(.66f,.28f),new Vector2(.68f,.055f),new Vector2(.32f,.055f));
            Line(vh,.41f,.14f,.59f,.14f,.015f,shirt);
        }

        void Bin(UnityEngine.UI.VertexHelper vh)
        {
            Polygon(vh,Palette.Ink,new Vector2(.2f,.71f),new Vector2(.8f,.71f),new Vector2(.73f,.12f),new Vector2(.28f,.12f));
            Polygon(vh,Palette.Hex("CADBD9"),new Vector2(.24f,.68f),new Vector2(.76f,.68f),new Vector2(.69f,.16f),new Vector2(.32f,.16f));
            Line(vh,.35f,.60f,.39f,.25f,.021f,Palette.Ink); Line(vh,.5f,.60f,.5f,.25f,.021f,Palette.Ink); Line(vh,.65f,.60f,.61f,.25f,.021f,Palette.Ink);
            Line(vh,.15f,.76f,.85f,.76f,.09f,Palette.Ink); Line(vh,.18f,.77f,.82f,.77f,.032f,Palette.Cream);
            Line(vh,.40f,.86f,.6f,.86f,.065f,Palette.Ink);
        }

        Vector2 Point(float x,float y) { Rect r=rectTransform.rect; return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height); }
        void Puff(UnityEngine.UI.VertexHelper vh,float x,float y,float rx,float ry,Color tint)
        {
            int start=vh.currentVertCount; vh.AddVert(Point(x,y),tint*color,Vector2.zero);
            const int steps=24;
            for(int i=0;i<=steps;i++) { float a=i*Mathf.PI*2/steps; vh.AddVert(Point(x+Mathf.Sin(a)*rx,y+Mathf.Cos(a)*ry),tint*color,Vector2.zero); }
            for(int i=0;i<steps;i++) vh.AddTriangle(start,start+i+1,start+i+2);
        }
        void Line(UnityEngine.UI.VertexHelper vh,float ax,float ay,float bx,float by,float width,Color tint)
        {
            Vector2 a=Point(ax,ay),b=Point(bx,by),side=new Vector2(-(b-a).y,(b-a).x).normalized*rectTransform.rect.width*width*.5f;
            int start=vh.currentVertCount; vh.AddVert(a-side,tint*color,Vector2.zero); vh.AddVert(a+side,tint*color,Vector2.zero); vh.AddVert(b+side,tint*color,Vector2.zero); vh.AddVert(b-side,tint*color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
        void Polygon(UnityEngine.UI.VertexHelper vh,Color tint,params Vector2[] points)
        {
            int start=vh.currentVertCount; foreach(var p in points) vh.AddVert(Point(p.x,p.y),tint*color,Vector2.zero);
            for(int i=1;i<points.Length-1;i++) vh.AddTriangle(start,start+i,start+i+1);
        }
    }
}
