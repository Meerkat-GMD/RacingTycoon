using UnityEngine;

namespace CottonCircuit
{
    // Vector illustrations remain crisp at both reference and smaller canvas sizes.
    public sealed class ProgressionArtGraphic : UnityEngine.UI.MaskableGraphic
    {
        public string Kind = "shop";
        public int Variant;
        public Color Accent = new Color(.91f, .58f, .66f);
        static readonly Color Ink = Palette.Hex("263B42"), Paper = Palette.Hex("FFF9EC");
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper v)
        {
            v.Clear(); string kind = (Kind ?? "").ToLowerInvariant();
            if (kind == "scene") { Scene(v); return; }
            if (kind == "disc") { Circle(v, .5f, .5f, .49f, color); return; }
            if (kind == "portrait") { Portrait(v); return; }
            if (kind.Contains("location") || kind.Contains("map") || kind.Contains("flag"))
            { Line(v, .34f, .2f, .34f, .82f, .065f, Ink); Quad(v, .36f, .53f, .43f, .27f, Accent); Line(v, .2f, .17f, .69f, .17f, .05f, Ink); }
            else if (kind.Contains("group"))
            {
                Circle(v, .3f, .6f, .1f, Ink); Circle(v, .7f, .6f, .1f, Ink); Quad(v, .16f, .2f, .28f, .27f, Ink); Quad(v, .56f, .2f, .28f, .27f, Ink);
                Circle(v, .5f, .68f, .13f, Ink); Circle(v, .5f, .7f, .085f, Paper); Quad(v, .32f, .18f, .36f, .33f, Accent);
            }
            else if (kind.Contains("worker") || kind.Contains("staff") || kind.Contains("person"))
            { Circle(v, .5f, .65f, .16f, Ink); Circle(v, .5f, .67f, .11f, Paper); Quad(v, .26f, .19f, .48f, .29f, Accent); Line(v, .26f, .18f, .74f, .18f, .06f, Ink); }
            else if (kind.Contains("hour") || kind.Contains("time") || kind.Contains("clock") || kind.Contains("patience"))
            { Circle(v, .5f, .5f, .33f, Ink); Circle(v, .5f, .5f, .27f, Paper); Line(v, .5f, .5f, .5f, .7f, .05f, Ink); Line(v, .5f, .5f, .65f, .43f, .05f, Accent); }
            else if (kind.Contains("engine") || kind.Contains("cart") || kind.Contains("coupe") || kind.Contains("steer") || kind.Contains("handling"))
            { Quad(v, .18f, .35f, .65f, .25f, Accent); Quad(v, .32f, .57f, .35f, .17f, Ink); Quad(v, .37f, .6f, .25f, .09f, Paper); Circle(v, .31f, .31f, .105f, Ink); Circle(v, .7f, .31f, .105f, Ink); }
            else if (kind.Contains("machine"))
            { Quad(v, .24f, .18f, .52f, .2f, Ink); Quad(v, .3f, .38f, .4f, .23f, Accent); Circle(v, .5f, .62f, .23f, Ink); Circle(v, .5f, .65f, .17f, Paper); Circle(v, .5f, .65f, .075f, Accent); }
            else if (kind.Contains("shelf") || kind.Contains("shop") || kind.Contains("ads") || kind.Contains("sales"))
            { Quad(v, .22f, .22f, .56f, .44f, Ink); Quad(v, .28f, .27f, .44f, .3f, Paper); Quad(v, .16f, .65f, .68f, .16f, Accent); Line(v, .5f, .27f, .5f, .55f, .035f, Ink); }
            else
            { Line(v, .5f, .15f, .53f, .57f, .055f, Ink); Circle(v, .37f, .59f, .19f, Accent); Circle(v, .62f, .61f, .2f, Accent); Circle(v, .49f, .75f, .19f, Accent); Circle(v, .5f, .54f, .18f, Accent); Circle(v, .42f, .76f, .045f, Paper); }
        }
        void Scene(UnityEngine.UI.VertexHelper v)
        {
            Color sky = Variant == 3 ? Palette.Hex("D6D8EB") : Variant == 2 ? Palette.Hex("D3E9E4") : Palette.Hex("EDE8DB");
            Quad(v, 0, 0, 1, 1, sky); Circle(v, .79f, .78f, .115f, Palette.Hex("F5D698"));
            Quad(v, 0, 0, 1, .27f, Palette.Hex("A9C6B8"));
            if (Variant == 0 || Variant == 1)
            {
                for (int i = 0; i < 4; i++) { float x = .06f + i * .245f; Quad(v, x, .29f, .18f, .34f + (i % 2) * .11f, i % 2 == 0 ? Palette.Hex("D6BBA4") : Palette.Hex("E6CDBB")); for (int j = 0; j < 2; j++) Quad(v, x + .035f + j * .075f, .47f, .04f, .07f, Paper); }
            }
            else if (Variant == 2)
            { Quad(v, 0, .16f, 1, .17f, Palette.Hex("86BDBF")); Line(v, .15f, .37f, .15f, .7f, .018f, Ink); Triangle(v, new Vector2(.17f,.68f), new Vector2(.17f,.4f),new Vector2(.48f,.4f), Paper); }
            else
            { Circle(v, .27f, .56f, .22f, Ink); Circle(v, .27f, .56f, .205f, sky); Line(v, .27f, .34f, .27f, .78f, .012f, Ink); Line(v, .05f, .56f, .49f, .56f, .012f, Ink); Line(v, .12f, .4f, .42f, .72f, .012f, Ink); Line(v, .12f, .72f, .42f, .4f, .012f, Ink); Line(v, .27f, .55f, .14f, .25f, .025f, Ink); Line(v, .27f, .55f, .4f, .25f, .025f, Ink); }
            Quad(v, .57f, .18f, .29f, .26f, Paper); Quad(v, .54f, .43f, .35f, .08f, Accent);
            for (int i = 0; i < 4; i++) Quad(v, .55f + i * .085f, .37f, .042f, .075f, Accent);
            Quad(v, .6f, .25f, .23f, .1f, Ink); Circle(v, .62f, .17f, .035f, Ink); Circle(v, .81f, .17f, .035f, Ink);
            Line(v, 0, .12f, 1, .12f, .01f, Paper);
        }
        void Portrait(UnityEngine.UI.VertexHelper v)
        {
            Quad(v, 0, 0, 1, 1, Palette.Hex("C5D8CC")); Circle(v,.75f,.8f,.26f,Paper);
            Quad(v,.1f,.13f,.8f,.2f,Palette.Hex("BA9381")); Circle(v,.5f,.58f,.2f,Ink);
            Circle(v,.5f,.58f,.165f,Palette.Hex("EDC2A8")); Quad(v,.27f,.14f,.46f,.28f,Accent);
            Quad(v,.39f,.12f,.23f,.29f,Paper); Circle(v,.44f,.59f,.013f,Ink); Circle(v,.57f,.59f,.013f,Ink);
            Line(v,.46f,.51f,.55f,.51f,.013f,Ink); Quad(v,.27f,.73f,.46f,.045f,Paper); Circle(v,.49f,.755f,.13f,Paper);
        }
        Vector2 P(float x, float y) => new Vector2(rectTransform.rect.xMin + x * rectTransform.rect.width, rectTransform.rect.yMin + y * rectTransform.rect.height);
        void Quad(UnityEngine.UI.VertexHelper v,float x,float y,float w,float h,Color c) { int n=v.currentVertCount; Add(v,P(x,y),c);Add(v,P(x,y+h),c);Add(v,P(x+w,y+h),c);Add(v,P(x+w,y),c);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3); }
        void Circle(UnityEngine.UI.VertexHelper v,float x,float y,float r,Color c) { int n=v.currentVertCount;Add(v,P(x,y),c);for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;Add(v,P(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r),c);if(i>0)v.AddTriangle(n,n+i,n+i+1);} }
        void Line(UnityEngine.UI.VertexHelper v,float x,float y,float x2,float y2,float width,Color c) { Vector2 a=P(x,y),b=P(x2,y2),d=(b-a).normalized,s=new Vector2(-d.y,d.x)*width*Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;int n=v.currentVertCount;Add(v,a-s,c);Add(v,a+s,c);Add(v,b+s,c);Add(v,b-s,c);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3); }
        void Triangle(UnityEngine.UI.VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Color tint) { int n=v.currentVertCount;Add(v,P(a.x,a.y),tint);Add(v,P(b.x,b.y),tint);Add(v,P(c.x,c.y),tint);v.AddTriangle(n,n+1,n+2); }
        static void Add(UnityEngine.UI.VertexHelper v,Vector2 p,Color c) {var vertex=UIVertex.simpleVert;vertex.position=p;vertex.color=c;v.AddVert(vertex);}
    }
}
