using UnityEngine;
namespace CottonCircuit
{
    public class GameController : MonoBehaviour
    {
        public WorldView World;
        public GameUI UI;
        public AudioFeedback Audio;
        public GameSession Session { get; private set; }
        public SaveStore Store { get; private set; }
        public string Notice { get; private set; }
        public float SaleProgress => customerTimer / 7;
        float customerTimer;
        bool customerPurchased;
        float noticeTimer;
        int renderedSamples = -1;
        GameMode lastMode;
        public void Initialize(string saveDirectory)
        {
            Store = new SaveStore(saveDirectory);
            Session = new GameSession(Store.Load());
            customerTimer = 0; customerPurchased = false;
            lastMode = Session.Mode;
            if (World)
            {
                World.Initialize(); World.SetMode(GameMode.Shop); World.Kart.ResetPosition();
                World.ShowInventory(Session.Economy);
            }
            if (UI) UI.Initialize(this);
            if (Store.Error != null) Notify(Store.Error);
        }
        void Start() { if (Session == null) Initialize(Application.persistentDataPath); }
        void Update()
        {
            if (Session == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (Session.Paused) { if (UI) UI.Refresh(); return; }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (Session.Mode == GameMode.Shop) StartRun();
                else if (Session.Mode == GameMode.Racing) FinishRun();
                else ReturnToShop();
            }
            if (Session.Mode == GameMode.Racing && Input.GetKeyDown(KeyCode.R))
            { World.Kart.Recover(); Notify("코스에 복귀했어요. W로 다시 출발하세요."); }
            bool braking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            float throttle = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
            float steering = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            Tick(throttle, steering, braking, Time.deltaTime, Input.GetKey(KeyCode.Space));
        }
        public void Tick(float throttle, float steering, bool brake, float deltaTime, bool drift = false)
        {
            if (Session == null || Session.Paused) return;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0) return;
            // Simulate all elapsed time in small steps. Sixty seconds covers the longest
            // run and also the six-slot shop queue; no gameplay is lost on a slow frame.
            float remaining = Mathf.Min(deltaTime, 60);
            while (remaining > .000001f)
            {
                float step = Mathf.Min(.05f, remaining);
                Step(throttle, steering, brake, step, drift);
                remaining -= step;
            }
        }
        void Step(float throttle, float steering, bool brake, float dt, bool drift)
        {
            if (noticeTimer > 0) { noticeTimer -= dt; if (noticeTimer <= 0) Notice = null; }
            if (Session.Mode == GameMode.Racing)
            {
                int boosts = World.Kart.DriveModel.BoostCount;
                double delta = World.Kart.Drive(throttle, steering, brake, dt, drift);
                if (World.Kart.DriveModel.BoostCount > boosts) Audio.Play(3);
                Session.Tick(dt, delta, World.Kart.Radius, World.Kart.Flavor);
                if (Session.Production.Samples.Count != renderedSamples)
                {
                    renderedSamples = Session.Production.Samples.Count;
                    World.CentralCandy.Show(Session.Production.Samples);
                }
            }
            else if (Session.Mode == GameMode.Shop)
            {
                bool customerActive = Session.Economy.Inventory.Count > 0 || customerPurchased;
                if (customerActive)
                {
                    customerTimer += dt;
                    if (customerTimer >= 4.2f && !customerPurchased)
                    {
                        int earned = Session.Economy.SellNext();
                        customerPurchased = true;
                        if (earned > 0)
                        {
                            Notify("솜사탕 판매!  +" + earned + " 코인");
                            Audio.Play(1); Save(); World.ShowInventory(Session.Economy);
                        }
                    }
                    if (customerTimer >= 7) { customerTimer = 0; customerPurchased = false; }
                }
                World.UpdateCustomer(customerTimer / 7, customerActive);
            }
            SyncMode();
            World.UpdateThread(Session.Mode == GameMode.Racing && World.Kart.Speed > .2f);
            Audio.UpdateDriving(World.Kart, Session.Mode == GameMode.Racing && !Session.Paused);
            if (UI) UI.Refresh();
        }
        public void StartRun()
        {
            if (Session.Paused) return;
            if (!Store.CanSave) { Notify(Store.Error + "  도움말 → 새 가게 시작"); return; }
            if (!Session.StartRun()) { Notify("진열대가 가득 찼어요. 손님이 구매할 때까지 기다려주세요."); return; }
            World.Kart.MaximumSpeed = Session.Economy.MaxSpeed;
            World.Kart.ResetPosition(); renderedSamples = -1;
            World.CentralCandy.Show(null); World.Customer.gameObject.SetActive(false);
            customerTimer = 0; customerPurchased = false;
            Audio.Play(0); SyncMode();
            Notify("W 가속 · A / D 조향 · Space로 드리프트, 놓으면 부스트!");
        }
        public void FinishRun() { Session.FinishRun(); SyncMode(); }
        public void ReturnToShop()
        {
            Session.ReturnToShop(); SyncMode();
            World.ShowInventory(Session.Economy);
        }
        void SyncMode()
        {
            if (lastMode == Session.Mode) return;
            lastMode = Session.Mode;
            World.SetMode(lastMode);
            if (lastMode == GameMode.Results)
            { World.Kart.Stop(); World.UpdateThread(false); Audio.UpdateDriving(World.Kart, false); Audio.Play(2); Save(); }
            if (UI) UI.Refresh();
        }
        public void BuyUpgrade(int index)
        {
            if (Session.Mode != GameMode.Shop || Session.Paused) return;
            if (Session.Economy.BuyUpgrade(index)) { Audio.Play(1); Notify("업그레이드 완료! 다음 제작부터 더 좋아져요."); Save(); }
            else { Audio.Play(0); Notify("코인이 부족하거나 최고 레벨이에요."); }
            UI.Refresh();
        }
        public void TogglePause() { Session.Paused = !Session.Paused; World.AnimationPaused = Session.Paused; World.UpdateThread(false); World.Kart.SetEffects(!Session.Paused && Session.Mode == GameMode.Racing); Audio.UpdateDriving(World.Kart, !Session.Paused && Session.Mode == GameMode.Racing); UI.Refresh(); }
        public void ToggleMute() { Audio.Toggle(); UI.Refresh(); }
        public void ResetSave()
        {
            if (Session.Mode != GameMode.Shop || !Store.ArchiveAndReset()) return;
            Session = new GameSession(new Economy()); customerTimer = 0; customerPurchased = false;
            World.AnimationPaused = false;
            Save(); World.ShowInventory(Session.Economy); Notify("새로운 솜사탕 가게를 열었어요."); UI.Refresh();
        }
        void Save() { if (!Store.Save(Session.Economy) && Store.Error != null) Notify(Store.Error); }
        public void Notify(string message) { Notice = message; noticeTimer = 5; }
        void OnApplicationQuit() { if (Session != null && Store != null) Save(); }
        void OnApplicationFocus(bool focused)
        {
            if (enabled && !focused && Session != null && Session.Mode == GameMode.Racing && !Application.isBatchMode)
            { Session.Paused = true; World.AnimationPaused = true; World.Kart.SetEffects(false); World.UpdateThread(false); Audio.UpdateDriving(World.Kart, false); if (UI) UI.Refresh(); }
        }
    }
}
