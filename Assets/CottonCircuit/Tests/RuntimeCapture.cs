#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CottonCircuit.Tests
{
    public partial class RuntimeSmoke
    {
        // Hidden Windows players do not reliably present a readable back buffer.
        // Render the existing cameras and canvas explicitly without showing a window.
        void CaptureOffscreen(string name)
        {
            var previousTarget = RenderTexture.active;
            var frame = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = null;
            try
            {
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = frame;
                GL.Clear(true, true, Color.black);

                if (game.World.CandyCamera && game.World.CandyCamera.targetTexture)
                    game.World.CandyCamera.Render();
                var cameras = Camera.allCameras;
                Array.Sort(cameras, (left, right) => left.depth.CompareTo(right.depth));
                foreach (var camera in cameras)
                    if (camera.enabled && camera.gameObject.activeInHierarchy && !camera.targetTexture)
                        RenderCameraViewport(camera, frame);
                RenderOverlayCanvas(frame);

                RenderTexture.active = frame;
                image = new Texture2D(frame.width, frame.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, frame.width, frame.height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(Path.Combine(output, name), image.EncodeToPNG());

                var colors = new HashSet<int>();
                int lit = 0, samples = 0;
                for (int y = 8; y < image.height; y += Math.Max(1, image.height / 40))
                    for (int x = 8; x < image.width; x += Math.Max(1, image.width / 40))
                    {
                        Color32 pixel = image.GetPixel(x, y);
                        colors.Add((pixel.r / 16 << 8) | (pixel.g / 16 << 4) | pixel.b / 16);
                        if (pixel.r + pixel.g + pixel.b > 45) lit++;
                        samples++;
                    }
                Check(colors.Count > 16 && lit > samples / 10,
                    "screenshot " + name + " contains rendered pixels (" + colors.Count + " sampled colors)");
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (image) Destroy(image);
                RenderTexture.ReleaseTemporary(frame);
            }
        }

        static void RenderCameraViewport(Camera camera, RenderTexture frame)
        {
            var originalRect = camera.rect;
            var originalTarget = camera.targetTexture;
            float originalAspect = camera.aspect;
            var bounds = camera.pixelRect;
            int x = Mathf.Clamp(Mathf.RoundToInt(bounds.x), 0, frame.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(bounds.y), 0, frame.height - 1);
            int width = Mathf.Clamp(Mathf.RoundToInt(bounds.width), 1, frame.width - x);
            int height = Mathf.Clamp(Mathf.RoundToInt(bounds.height), 1, frame.height - y);
            var viewport = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            try
            {
                // targetTexture rendering fills its texture regardless of the screen viewport.
                camera.targetTexture = viewport;
                camera.rect = new Rect(0, 0, 1, 1);
                camera.aspect = width / (float)height;
                camera.Render();
                Graphics.CopyTexture(viewport, 0, 0, 0, 0, width, height, frame, 0, 0, x, y);
            }
            finally
            {
                camera.targetTexture = originalTarget;
                camera.rect = originalRect;
                camera.aspect = originalAspect;
                RenderTexture.ReleaseTemporary(viewport);
            }
        }

        static void RenderOverlayCanvas(RenderTexture frame)
        {
            const int captureLayer = 31;
            var cameraObject = new GameObject("Runtime screenshot UI camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.cullingMask = 1 << captureLayer;
            camera.orthographic = true;
            camera.orthographicSize = frame.height * .5f;
            camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.targetTexture = frame;
            var states = new List<CanvasCaptureState>();
            var layers = new Dictionary<GameObject, int>();
            try
            {
                foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || !canvas.enabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    states.Add(new CanvasCaptureState(canvas));
                    foreach (var child in canvas.GetComponentsInChildren<Transform>(true))
                    {
                        layers[child.gameObject] = child.gameObject.layer;
                        child.gameObject.layer = captureLayer;
                    }
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1;
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
            }
            finally
            {
                foreach (var state in states) state.Restore();
                foreach (var layer in layers) if (layer.Key) layer.Key.layer = layer.Value;
                Canvas.ForceUpdateCanvases();
                camera.targetTexture = null;
                Destroy(cameraObject);
            }
        }

        sealed class CanvasCaptureState
        {
            readonly Canvas canvas;
            readonly RenderMode mode;
            readonly Camera camera;
            readonly float planeDistance;
            public CanvasCaptureState(Canvas value)
            {
                canvas = value; mode = value.renderMode;
                camera = value.worldCamera; planeDistance = value.planeDistance;
            }
            public void Restore()
            {
                canvas.renderMode = mode;
                canvas.worldCamera = camera;
                canvas.planeDistance = planeDistance;
            }
        }
    }
}
#endif
