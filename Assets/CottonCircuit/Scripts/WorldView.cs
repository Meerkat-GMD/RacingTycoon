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
        public LineRenderer SugarThread;
        public Light Sun;
        public bool AnimationPaused;
        public Camera CandyCamera;
        public RenderTexture CandyPreview;
        GameMode mode;
        Vector3 focus;
        float size = 22;
        readonly List<GameObject> displayed = new List<GameObject>();
        public void SetMode(GameMode value)
        {
            mode = value; GameCamera.orthographic = mode != GameMode.Racing;
            if (CandyCamera) CandyCamera.enabled = mode == GameMode.Racing;
            if (mode == GameMode.Racing)
            {
                GameCamera.transform.position = Kart.transform.position - Kart.transform.forward * 9 + Vector3.up * 5.6f;
                GameCamera.transform.LookAt(Kart.transform.position + Kart.transform.forward * 7 + Vector3.up);
            }
        }
        public void Initialize()
        {
            focus = new Vector3(-52, 1, 8);
            UpdateViewport();
            Customer.gameObject.SetActive(true);
        }
        void LateUpdate()
        {
            UpdateViewport();
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (mode == GameMode.Racing)
            {
                Vector3 ahead = Kart.transform.forward;
                Vector3 target = Kart.transform.position - ahead * (Kart.Boosting ? 10.2f : 9) + Vector3.up * 5.6f;
                GameCamera.transform.position = Vector3.Lerp(GameCamera.transform.position, target, 1 - Mathf.Exp(-dt * 8));
                var look = Quaternion.LookRotation(Kart.transform.position + ahead * 7 + Vector3.up * 1.1f - GameCamera.transform.position);
                GameCamera.transform.rotation = Quaternion.Slerp(GameCamera.transform.rotation, look, 1 - Mathf.Exp(-dt * 11));
                GameCamera.fieldOfView = Mathf.Lerp(GameCamera.fieldOfView, 58 + Mathf.Clamp01(Kart.Speed / 26) * 9 + (Kart.Boosting ? 6 : 0), 1 - Mathf.Exp(-dt * 5));
            }
            else
            {
                Vector3 desired = mode == GameMode.Shop ? new Vector3(-52, 1, 8) : new Vector3(0, 7, 0);
                float desiredSize = mode == GameMode.Shop ? 13.5f : 9;
                focus = Vector3.Lerp(focus, desired, 1 - Mathf.Exp(-dt * 5));
                size = Mathf.Lerp(size, desiredSize, 1 - Mathf.Exp(-dt * 5));
                GameCamera.transform.position = focus + new Vector3(28, 34, 40);
                GameCamera.transform.LookAt(focus); GameCamera.orthographicSize = size;
            }
            if (Stick && mode == GameMode.Racing && !AnimationPaused && Kart.Speed > 0)
            {
                Stick.Rotate(Vector3.up, Time.deltaTime * Kart.Speed / Kart.Radius * Mathf.Rad2Deg);
                CentralCandy.transform.rotation = Stick.rotation;
            }
        }
        void UpdateViewport()
        {
            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            float width = 1600 * scale, height = 900 * scale;
            GameCamera.rect = new Rect((Screen.width - width) / (2 * Screen.width), (Screen.height - height) / (2 * Screen.height), width * (mode == GameMode.Racing ? 1 : .755f) / Screen.width, height / Screen.height);
        }
        public void UpdateThread(bool active)
        {
            SugarThread.enabled = active;
            if (!active) return;
            var start = Kart.transform.position + Vector3.up * 1.25f;
            var end = new Vector3(0, 6.4f, 0);
            SugarThread.startColor = Palette.Flavor(Kart.Flavor);
            SugarThread.endColor = Palette.Flavor(Kart.Flavor);
            SugarThread.positionCount = 24;
            for (int i = 0; i < 24; i++)
            {
                float t = i / 23f;
                var p = Vector3.Lerp(start, end, t);
                p.y += Mathf.Sin(t * Mathf.PI) * .8f;
                SugarThread.SetPosition(i, p);
            }
        }
        public void UpdateCustomer(float progress, bool shopping)
        {
            Customer.gameObject.SetActive(mode == GameMode.Shop && shopping);
            if (!shopping) return;
            var entry = new Vector3(-56, .3f, 17);
            var counter = new Vector3(-52, .3f, 11.3f);
            float t = progress < .5f ? Mathf.SmoothStep(0, 1, progress * 2) : progress < .7f ? 1 : 1 - Mathf.SmoothStep(0, 1, (progress - .7f) / .3f);
            Customer.position = Vector3.Lerp(entry, counter, t) + Vector3.up * (Mathf.Sin(progress * 60) * .035f);
            Customer.rotation = Quaternion.LookRotation(progress < .7f ? counter - entry : entry - counter);
        }
        public void ShowInventory(Economy economy)
        {
            foreach (var item in displayed) Destroy(item);
            displayed.Clear();
            for (int i = 0; i < Mathf.Min(3, economy.Inventory.Count); i++)
            {
                var item = new GameObject("Product on display " + i);
                item.transform.SetParent(DisplayRoot, false);
                item.transform.localPosition = new Vector3((i - 1) * .75f, 0, 0);
                item.transform.localScale = Vector3.one * .24f;
                var candy = item.AddComponent<CandyView>(); candy.Assets = Assets;
                candy.Show(economy.Inventory[i].Samples);
                var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stick.name = "Product stick"; stick.transform.SetParent(item.transform, false);
                stick.transform.localPosition = Vector3.up * 1.8f;
                stick.transform.localScale = new Vector3(.13f, 1.8f, .13f);
                Destroy(stick.GetComponent<Collider>());
                stick.GetComponent<Renderer>().sharedMaterial = Assets.Flavors[2];
                displayed.Add(item);
            }
        }
    }
}
