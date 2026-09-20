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
        public LineRenderer SugarThread;
        public Light Sun;
        public bool AnimationPaused;
        public Camera CandyCamera;
        public RenderTexture CandyPreview;
        GameMode mode;
        Vector3 focus;
        float size = 22;
        readonly List<GameObject> displayed = new List<GameObject>();
        Transform[] queue;
        readonly string[] queueIds = new string[2];
        readonly List<Transform> departing = new List<Transform>();
        public void SetMode(GameMode value)
        {
            mode = value; GameCamera.orthographic = mode != GameMode.Racing;
            if (CandyCamera) CandyCamera.enabled = mode == GameMode.Racing;
            if (mode == GameMode.Racing)
            {
                GameCamera.transform.position = Kart.transform.position - Kart.transform.forward * 9 + Vector3.up * 5.6f;
                GameCamera.transform.LookAt(Kart.transform.position + Kart.transform.forward * 7 + Vector3.up);
            }
            else
            {
                focus = mode == GameMode.Shop ? new Vector3(-52, 1.2f, 10) : new Vector3(0, 7, 0);
                size = mode == GameMode.Shop ? 5.2f : 9;
                GameCamera.transform.position = focus + new Vector3(28, 34, 40);
                GameCamera.transform.LookAt(focus); GameCamera.orthographicSize = size;
            }
            UpdateViewport();
        }
        public void Initialize()
        {
            focus = new Vector3(-52, 1.2f, 10);
            size = 5.2f;
            UpdateViewport();
            if (queue == null) queue = new[] { Customer, Instantiate(Assets.Customer, transform).transform };
            foreach (var person in departing) if (person) Destroy(person.gameObject);
            departing.Clear(); queueIds[0] = queueIds[1] = null;
            AnimationPaused = false;
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
                Vector3 desired = mode == GameMode.Shop ? new Vector3(-52, 1.2f, 10) : new Vector3(0, 7, 0);
                float desiredSize = mode == GameMode.Shop ? 5.2f : 9;
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
            float bottom = mode == GameMode.Shop ? height * 410 / 900 : 0;
            float crop = mode == GameMode.Shop ? 350f / 900 : 1;
            GameCamera.rect = new Rect((Screen.width - width) / (2 * Screen.width), ((Screen.height - height) / 2 + bottom) / Screen.height, width * (mode == GameMode.Racing ? 1 : .755f) / Screen.width, height * crop / Screen.height);
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
        public void UpdateOrders(Economy economy, float dt)
        {
            if (queue == null) return;
            for (int i = 0; i < 2; i++)
            {
                bool visible = mode == GameMode.Shop && i < economy.Orders.Count;
                queue[i].gameObject.SetActive(visible); if (!visible) continue;
                var order = economy.Orders[i];
                if (queueIds[i] != order.Id) { queueIds[i] = order.Id; queue[i].position = new Vector3(-50, .3f, 16.5f); }
                Vector3 target = new Vector3(-52, .3f, 11.4f + i * 2);
                queue[i].position = Vector3.MoveTowards(queue[i].position, target, dt * 3);
                queue[i].rotation = Quaternion.Euler(0, 180, 0);
            }
            for (int i = departing.Count - 1; i >= 0; i--)
            {
                var person = departing[i]; person.gameObject.SetActive(mode == GameMode.Shop);
                if (mode != GameMode.Shop) continue;
                Vector3 target = new Vector3(-47, .3f, 17);
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
            if (DisplayRacks != null) for (int i = 0; i < DisplayRacks.Length; i++) DisplayRacks[i].gameObject.SetActive(i <= economy.ShelfLevel);
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
