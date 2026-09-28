using UnityEngine;
namespace CottonCircuit.Editor
{
    // Draws the tileable floss strip for the sugar thread. U runs along the thread and wraps;
    // V crosses it. Alpha is a soft rope whose two edges swell in their own cloud-like lumps.
    // The grey level is the highlight SugarFloss.shader lifts toward white: the middle of the rope
    // and a few fine fibres, so the floss reads as round and spun. Every wave completes whole
    // cycles in U so the strip tiles seamlessly.
    static class SugarFlossTexture
    {
        const int Width = 256, Height = 64;
        static readonly float[] FiberOffset = { -.55f, -.3f, -.08f, .12f, .32f, .55f };
        static readonly float[] FiberWave = { .10f, .07f, .12f, .06f, .09f, .08f };
        static readonly int[] FiberCycles = { 2, 3, 1, 4, 2, 3 };
        static readonly float[] FiberPhase = { .3f, 2.1f, 4f, 1.2f, 5.1f, 3.3f };

        public static byte[] EncodePng()
        {
            var pixels = new Color32[Width * Height];
            for (int x = 0; x < Width; x++)
            {
                float turn = x * Mathf.PI * 2 / Width;
                float center = .5f + .03f * Mathf.Sin(turn * 3 + .7f);
                float upper = .36f + .05f * Mathf.Sin(turn * 2 + 1.9f) + .035f * Mathf.Sin(turn * 5 + .3f) + .025f * Mathf.Sin(turn * 11 + 2.7f) + .015f * Mathf.Sin(turn * 19 + 1.1f);
                float lower = .36f + .05f * Mathf.Sin(turn * 2 + 4.2f) + .035f * Mathf.Sin(turn * 6 + 2.4f) + .025f * Mathf.Sin(turn * 13 + .6f) + .015f * Mathf.Sin(turn * 17 + 3.9f);
                float density = .88f + .12f * Mathf.Sin(turn * 3 + 2.2f);
                for (int y = 0; y < Height; y++)
                {
                    float v = (y + .5f) / Height, reach = (v - center) / (v > center ? upper : lower);
                    float body = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.6f, 1.05f, Mathf.Abs(reach)));
                    float fibers = 0;
                    for (int k = 0; k < FiberOffset.Length; k++)
                    {
                        float path = center + .3f * (FiberOffset[k] + FiberWave[k] * Mathf.Sin(turn * FiberCycles[k] + FiberPhase[k]));
                        float gap = (v - path) / .03f;
                        fibers += Mathf.Exp(-gap * gap);
                    }
                    fibers = Mathf.Clamp01(fibers);
                    float alpha = body * density * (.85f + .15f * fibers);
                    float highlight = Mathf.Clamp01(.15f * Mathf.Max(0, 1 - reach * reach) + .12f * fibers);
                    byte grey = (byte)Mathf.RoundToInt(255 * highlight);
                    pixels[y * Width + x] = new Color32(grey, grey, grey, (byte)Mathf.RoundToInt(255 * alpha));
                }
            }
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels); texture.Apply(false);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }
    }
}
