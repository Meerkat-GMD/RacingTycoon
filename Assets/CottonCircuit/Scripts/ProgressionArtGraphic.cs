using UnityEngine;

namespace CottonCircuit
{
    // The flat disc behind each trait icon and its selection highlight. Pictures
    // on the preparation screen are pre-rendered sprites (UiArt), not this graphic.
    public sealed class ProgressionArtGraphic : UnityEngine.UI.MaskableGraphic
    {
        public string Kind = "disc";
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper v)
        {
            v.Clear();
            if (Kind == "disc") Circle(v, .5f, .5f, .49f, color);
        }
        Vector2 P(float x, float y) => new Vector2(rectTransform.rect.xMin + x * rectTransform.rect.width, rectTransform.rect.yMin + y * rectTransform.rect.height);
        void Circle(UnityEngine.UI.VertexHelper v,float x,float y,float r,Color c) { int n=v.currentVertCount;Add(v,P(x,y),c);for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;Add(v,P(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r),c);if(i>0)v.AddTriangle(n,n+i,n+i+1);} }
        static void Add(UnityEngine.UI.VertexHelper v,Vector2 p,Color c) {var vertex=UIVertex.simpleVert;vertex.position=p;vertex.color=c;v.AddVert(vertex);}
    }
}
