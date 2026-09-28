using System.Collections.Generic;
using UnityEngine;
namespace CottonCircuit
{
    public class WorldView : MonoBehaviour
    {
        public GameAssets Assets;
        public Camera GameCamera;
        public KartController Kart;
        public CandyView CentralCandy;
        public Transform Stick;
        public Transform Customer;
        public Transform DisplayRoot;
        public Transform[] DisplayRacks;
        public Transform[] CourseRoots;
        public Transform ShopRoot;
        public const float CandyScale = 8;
        Vector3 ShopOffset => ShopRoot ? ShopRoot.position : Vector3.zero;
        Vector3 ShopFocus => new Vector3(-52, 1.2f, 10) + ShopOffset;
        public LineRenderer SugarThread;
        public LineRenderer[] SugarWisps;
        public Light Sun;
        public bool AnimationPaused;
        public Camera CandyCamera;
        public RenderTexture CandyPreview;
        public Camera ShopCamera { get; private set; }
        public RenderTexture MinimapTexture { get; private set; }
        Camera minimapCamera;
        bool continuousMode;
        bool ChaseActive => continuousMode || mode == GameMode.Racing;
        bool ShopVisible => continuousMode || mode == GameMode.Shop;
        GameMode mode;
        Vector3 focus;
        float size = 22;
        readonly List<GameObject> displayed = new List<GameObject>();
        Transform[] queue;
        readonly string[] queueIds = new string[2];
        readonly List<Transform> departing = new List<Transform>();
        public const float FlossTaperFraction = .005f;
        const int FlossPoints = 48, WispPoints = 128;
        readonly Vector3[] flossPath = new Vector3[FlossPoints], wispPath = new Vector3[WispPoints];
        Vector3 flossBow;
        bool flossShown;
        int flossFlavor = -2;
        public void SetContinuousMode(bool enabled)
        {
            continuousMode = enabled;
            if (enabled && !ShopCamera)
            {
                var cameraObject = new GameObject("Shop preview camera", typeof(Camera));
                cameraObject.transform.SetParent(transform, false);
                ShopCamera = cameraObject.GetComponent<Camera>();
                ShopCamera.CopyFrom(GameCamera);
                ShopCamera.targetTexture = null;
                ShopCamera.orthographic = true;
                ShopCamera.orthographicSize = 5.2f;
                ShopCamera.depth = GameCamera.depth + 1;
            }
            if (ShopCamera) ShopCamera.enabled = enabled;
            SetMode(mode);
        }
        public void SetMode(GameMode value)
        {
            mode = value; GameCamera.orthographic = !ChaseActive;
            if (CandyCamera) CandyCamera.enabled = ChaseActive;
            if (ChaseActive)
            {
                GameCamera.transform.position = Kart.transform.position - Kart.transform.forward * 7.4f + Vector3.up * (Kart.DriveModel.Style == DrivingStyle.Downhill ? 3.2f : 4.1f);
                GameCamera.transform.LookAt(Kart.transform.position + Kart.transform.forward * 12 + Vector3.up);
                GameCamera.fieldOfView = 62;
            }
            else
            {
                focus = mode == GameMode.Shop ? ShopFocus : new Vector3(0, 28, 0);
                size = mode == GameMode.Shop ? 5.2f : 32;
                GameCamera.transform.position = focus + new Vector3(56, 68, 80);
                GameCamera.transform.LookAt(focus); GameCamera.orthographicSize = size;
            }
            UpdateViewport();
        }
        public void Initialize()
        {
            if (!minimapCamera)
            {
                MinimapTexture = new RenderTexture(384, 384, 16) { name = "Course overview", antiAliasing = 4 };
                var overview = new GameObject("Course overview camera", typeof(Camera));
                overview.transform.SetParent(transform, false);
                minimapCamera = overview.GetComponent<Camera>();
                minimapCamera.orthographic = true;
                minimapCamera.targetTexture = MinimapTexture;
                minimapCamera.clearFlags = CameraClearFlags.SolidColor;
                minimapCamera.backgroundColor = Palette.Cream;
                minimapCamera.nearClipPlane = .3f;
                minimapCamera.farClipPlane = 500;
            }
            focus = ShopFocus;
            size = 5.2f;
            UpdateViewport();
            if (queue == null) queue = new[] { Customer, Instantiate(Assets.Customer, transform).transform };
            foreach (var person in departing) if (person) Destroy(person.gameObject);
            departing.Clear(); queueIds[0] = queueIds[1] = null;
            AnimationPaused = false;
        }
        public void SelectCourse(int map)
        {
            if (CourseRoots != null) for (int i = 0; i < CourseRoots.Length; i++) CourseRoots[i].gameObject.SetActive(i == map);
            Kart.SetCourse(RaceCourse.ForMap(map)); SetCandyQuality(0);
            if (minimapCamera)
            {
                var course = RaceCourse.ForMap(map);
                var min = course.BoundsMin; var max = course.BoundsMax;
                minimapCamera.transform.SetPositionAndRotation(new Vector3((float)(min.X + max.X) * .5f, 220,
                    (float)(min.Z + max.Z) * .5f), Quaternion.Euler(90, 0, 0));
                minimapCamera.orthographicSize = (float)System.Math.Max(max.X - min.X, max.Z - min.Z) * .5f + 12;
            }
        }

        void OnDestroy()
        {
            if (MinimapTexture) { MinimapTexture.Release(); Destroy(MinimapTexture); }
        }
        public void SetCandyQuality(int quality)
        {
            CentralCandy.transform.localScale = Vector3.one * CandyScale * (1 + Mathf.Clamp(quality, 0, 100) * .001f);
        }
        void LateUpdate()
        {
            UpdateViewport();
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (ChaseActive)
            {
                if (AnimationPaused) return;
                Vector3 ahead = Kart.transform.forward;
                bool downhill = Kart.DriveModel.Style == DrivingStyle.Downhill;
                Vector3 target = Kart.transform.position - ahead * (Kart.Boosting ? 8.3f : 7.4f) + Vector3.up * (downhill ? 3.2f : 4.1f);
                GameCamera.transform.position = Vector3.Lerp(GameCamera.transform.position, target, 1 - Mathf.Exp(-dt * 12));
                var look = Quaternion.LookRotation(Kart.transform.position + ahead * 12 + Vector3.up * 1.05f - GameCamera.transform.position);
                GameCamera.transform.rotation = Quaternion.Slerp(GameCamera.transform.rotation, look, 1 - Mathf.Exp(-dt * (downhill ? 9 : 13)));
                float speedFov = downhill ? Mathf.Clamp01(Kart.Speed / 55) * 19 : Mathf.Clamp01(Kart.Speed / 26) * 10;
                GameCamera.fieldOfView = Mathf.Lerp(GameCamera.fieldOfView, 62 + speedFov + (Kart.Boosting ? 9 : 0), 1 - Mathf.Exp(-dt * 7));
            }
            else
            {
                Vector3 desired = mode == GameMode.Shop ? ShopFocus : new Vector3(0, 28, 0);
                float desiredSize = mode == GameMode.Shop ? 5.2f : 32;
                focus = Vector3.Lerp(focus, desired, 1 - Mathf.Exp(-dt * 5));
                size = Mathf.Lerp(size, desiredSize, 1 - Mathf.Exp(-dt * 5));
                GameCamera.transform.position = focus + new Vector3(56, 68, 80);
                GameCamera.transform.LookAt(focus); GameCamera.orthographicSize = size;
            }
            if (Stick && ChaseActive && !AnimationPaused && Kart.Speed > 0)
            {
                Stick.Rotate(Vector3.up, Time.deltaTime * Kart.Speed / Kart.Radius * Mathf.Rad2Deg);
                CentralCandy.transform.rotation = Stick.rotation;
            }
        }
        void UpdateViewport()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            float width = 1600 * scale, height = 900 * scale;
            if (continuousMode)
            {
                // Match the centered 1600 x 900 composition in Game.uxml and PanelSettings.
                GameCamera.rect = DesignViewport(new Rect(0, 92, 960, 808), scale, width, height);
                if (ShopCamera)
                {
                    ShopCamera.rect = DesignViewport(new Rect(980, 82, 596, 126), scale, width, height);
                    ShopCamera.transform.position = ShopFocus + new Vector3(56, 68, 80);
                    ShopCamera.transform.LookAt(ShopFocus);
                }
                return;
            }
            float bottom = mode == GameMode.Shop ? height * 410 / 900 : 0;
            float crop = mode == GameMode.Shop ? 350f / 900 : 1;
            GameCamera.rect = new Rect((Screen.width - width) / (2 * Screen.width), ((Screen.height - height) / 2 + bottom) / Screen.height, width * (mode == GameMode.Racing ? 1 : .755f) / Screen.width, height * crop / Screen.height);
        }
        static Rect DesignViewport(Rect rect, float scale, float width, float height)
        {
            return new Rect(((Screen.width - width) * .5f + rect.x * scale) / Screen.width,
                ((Screen.height - height) * .5f + (900 - rect.yMax) * scale) / Screen.height,
                rect.width * scale / Screen.width, rect.height * scale / Screen.height);
        }
        public void UpdateThread(bool active)
        {
            SugarThread.enabled = active;
            foreach (var wisp in SugarWisps) wisp.enabled = active;
            if (!active) { flossShown = false; return; }
            if (flossFlavor != Kart.Flavor) TintFloss(Kart.Flavor);
            var start = Kart.transform.position + Vector3.up * 1.25f;
            var end = new Vector3(0, 25.6f, 0);
            // At speed the floss bows back behind the kart and eases into each new bow, so turns swing it softly.
            var bow = Vector3.up * 1.2f - Kart.transform.forward * Mathf.Clamp01(Kart.Speed / 26) * 2.5f;
            flossBow = flossShown ? Vector3.Lerp(flossBow, bow, 1 - Mathf.Exp(-Time.deltaTime * 2.5f)) : bow;
            flossShown = true;
            Vector3 span = end - start, side = Vector3.Cross(span, Vector3.up).normalized, lift = Vector3.Cross(side, span).normalized;
            float length = span.magnitude;
            for (int i = 0; i < FlossPoints; i++) flossPath[i] = FlossPoint(start, span / length, side, lift, FlossDistance(i, FlossPoints, length), length);
            SugarThread.positionCount = FlossPoints; SugarThread.SetPositions(flossPath);
            for (int w = 0; w < SugarWisps.Length; w++)
            {
                for (int i = 0; i < WispPoints; i++)
                {
                    // Each wisp coils around the floss once every 2.5 m and gathers into it at both ends.
                    float d = FlossDistance(i, WispPoints, length), angle = d * 2.5f - Time.time * 7 + w * Mathf.PI;
                    float radius = .18f * (1 - Mathf.Exp(-d / 2)) * (1 - d / length);
                    wispPath[i] = FlossPoint(start, span / length, side, lift, d, length) + (side * Mathf.Cos(angle) + lift * Mathf.Sin(angle)) * radius;
                }
                SugarWisps[w].positionCount = WispPoints; SugarWisps[w].SetPositions(wispPath);
            }
        }
        // Vertex 0 sits in the nozzle and vertex 1 1.2 m out. The courses keep the thread shorter than
        // 1.2 m / FlossTaperFraction, so width and alpha keys at that fraction always taper the floss
        // across this first segment. Later vertices crowd toward the kart, where the camera sees the most.
        static float FlossDistance(int index, int count, float length)
        {
            if (index == 0) return 0;
            float s = (index - 1f) / (count - 2);
            return 1.2f + (length - 1.2f) * s * (.15f + .85f * s);
        }
        // The point d meters along the floss from the kart nozzle toward the candy. The sway is 0 at the
        // nozzle, peaks 10 m out where the chase camera sees the floss and returns to 0 at the candy, so
        // both ends stay put while the bow and a ripple travelling toward the candy move the floss between.
        Vector3 FlossPoint(Vector3 start, Vector3 direction, Vector3 side, Vector3 lift, float d, float length)
        {
            float near = d / 10, sway = near * Mathf.Exp(1 - near) * (1 - d / length), ripple = d * .8f - Time.time * 3;
            return start + direction * d + (flossBow + side * (Mathf.Sin(ripple) * .3f) + lift * (Mathf.Cos(ripple) * .18f)) * sway;
        }
        void TintFloss(int flavor)
        {
            flossFlavor = flavor;
            Color color = Palette.Flavor(flavor);
            var floss = new Gradient();
            floss.SetKeys(new[] { new GradientColorKey(Color.Lerp(color, Color.white, .2f), 0), new GradientColorKey(color, .03f) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, FlossTaperFraction) });
            SugarThread.colorGradient = floss;
            var wisp = new Gradient();
            wisp.SetKeys(new[] { new GradientColorKey(Color.Lerp(color, Color.white, .6f), 0), new GradientColorKey(Color.Lerp(color, Color.white, .4f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.85f, FlossTaperFraction) });
            foreach (var line in SugarWisps) line.colorGradient = wisp;
        }
        public void UpdateOrders(Economy economy, float dt)
        {
            if (queue == null) return;
            for (int i = 0; i < 2; i++)
            {
                bool business = economy.Business != null;
                bool visible = ShopVisible && (business ? i == 0 && economy.Business.Customer != null : i < economy.Orders.Count);
                queue[i].gameObject.SetActive(visible); if (!visible) continue;
                string orderId = business ? economy.Business.Customer.Id : economy.Orders[i].Id;
                if (queueIds[i] != orderId) { queueIds[i] = orderId; queue[i].position = ShopOffset + new Vector3(-50, .3f, 16.5f); }
                Vector3 target = ShopOffset + new Vector3(-52, .3f, 11.4f + i * 2);
                queue[i].position = Vector3.MoveTowards(queue[i].position, target, dt * 3);
                queue[i].rotation = Quaternion.Euler(0, 180, 0);
            }
            for (int i = departing.Count - 1; i >= 0; i--)
            {
                var person = departing[i]; person.gameObject.SetActive(ShopVisible);
                if (!ShopVisible) continue;
                Vector3 target = ShopOffset + new Vector3(-47, .3f, 17);
                person.rotation = Quaternion.LookRotation(target - person.position);
                person.position = Vector3.MoveTowards(person.position, target, dt * 3.5f);
                if ((person.position - target).sqrMagnitude < .1f) { Destroy(person.gameObject); departing.RemoveAt(i); }
            }
        }
        public void HandOver(Product product, int slot)
        {
            if (queue == null || slot < 0 || slot > 1) return;
            var person = Instantiate(Assets.Customer, queue[slot].position, queue[slot].rotation, transform).transform;
            MakeCandy(product, person, new Vector3(.48f, .65f, .4f), .15f);
            departing.Add(person); queueIds[slot] = null;
        }
        public void ShowInventory(Economy economy)
        {
            foreach (var item in displayed) Destroy(item);
            displayed.Clear();
            // The first rack holds 6 and each expansion adds a rack of 3, in both the legacy and growth modes.
            int racks = 1 + Mathf.Max(0, economy.StockCapacity - 6) / 3;
            if (DisplayRacks != null) for (int i = 0; i < DisplayRacks.Length; i++) DisplayRacks[i].gameObject.SetActive(i < racks);
            for (int i = 0; i < economy.Inventory.Count; i++)
            {
                int rack = i < 6 ? 0 : 1 + (i - 6) / 3, slot = i < 6 ? i : (i - 6) % 3;
                Transform parent = DisplayRacks != null && rack < DisplayRacks.Length ? DisplayRacks[rack] : DisplayRoot;
                var item = MakeCandy(economy.Inventory[i], parent, new Vector3((slot % 3 - 1) * .7f, slot < 3 ? .32f : 1.02f, 0), .16f);
                displayed.Add(item);
            }
        }
        GameObject MakeCandy(Product product, Transform parent, Vector3 position, float scale)
        {
                var item = new GameObject("Cotton " + product.Id);
                item.transform.SetParent(parent, false); item.transform.localPosition = position;
                item.transform.localScale = Vector3.one * scale;
                var candy = item.AddComponent<CandyView>(); candy.Assets = Assets;
                candy.Show(product.Samples);
                var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stick.name = "Product stick"; stick.transform.SetParent(item.transform, false);
                stick.transform.localPosition = Vector3.up * 1.8f;
                stick.transform.localScale = new Vector3(.13f, 1.8f, .13f);
                Destroy(stick.GetComponent<Collider>());
                stick.GetComponent<Renderer>().sharedMaterial = Assets.Flavors[2];
                return item;
        }
    }
}
