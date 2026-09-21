using UnityEngine;
namespace CottonCircuit
{
    public class GameController : MonoBehaviour
    {
        public WorldView World;
        public GameUI UI;
        public AudioFeedback Audio;
        public GameSession Session { get; private set; }
        public OrderManager Orders { get; private set; }
        public SaveStore Store { get; private set; }
        public string Notice { get; private set; }
        public string SelectedOrderId { get; private set; }
        public string SelectedProductId { get; private set; }
        public int PreparedMap { get; private set; }
        public int PreparedFlavor { get; private set; }
        public int PreparedSize => PreparedMap;
        public int RunMap { get; private set; }
        public DrivingStyle PreparedStyle { get; private set; }
        public DrivingStyle RunStyle { get; private set; }
        public int RunTargetGrams { get; private set; } = 60;
        public int RunFlavor { get; private set; } = -1;
        public CustomerOrder SelectedOrder => Session?.Economy.Orders.Find(o => o.Id == SelectedOrderId);
        public Product SelectedProduct => Session?.Economy.Inventory.Find(p => p.Id == SelectedProductId);
        float noticeTimer, discardConfirmUntil, abortConfirmUntil;
        string discardCandidate;
        int renderedSamples = -1, orderRevision;
        GameMode lastMode;
        public void Initialize(string saveDirectory)
        {
            Store = new SaveStore(saveDirectory); Session = new GameSession(Store.Load());
            Orders = new OrderManager(Session.Economy); orderRevision = Orders.Revision;
            lastMode = Session.Mode; PreparedMap = PreparedFlavor = 0; RunFlavor = 0;
            SelectedOrderId = SelectedProductId = null;
            if (World)
            {
                World.Initialize(); World.SelectCourse(0); World.SetMode(GameMode.Shop); World.Kart.ResetPosition();
                World.ShowInventory(Session.Economy); World.UpdateOrders(Session.Economy, 0);
            }
            if (Orders.Orders.Count > 0) SelectOrder(Orders.Orders[0].Id);
            if (UI) UI.Initialize(this);
            if (Store.Error != null) Notify(Store.Error); else Save();
        }
        void Start() { if (Session == null) Initialize(Application.persistentDataPath); }
        void Update()
        {
            if (Session == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (Session.Paused) { UI.Refresh(); return; }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (Session.Mode == GameMode.Shop) StartRun();
                else if (Session.Mode == GameMode.Racing) FinishRun();
                else ReturnToShop();
            }
            if (Session.Mode == GameMode.Racing && Input.GetKeyDown(KeyCode.R))
            { World.Kart.Recover(); Notify("코스에 복귀했어요. 결승선까지 한 바퀴 완주하세요."); }
            bool brake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            float throttle = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
            float steer = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            Tick(throttle, steer, brake, Time.deltaTime, Input.GetKey(KeyCode.Space),
                Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift));
        }
        public void Tick(float throttle, float steering, bool brake, float deltaTime, bool drift = false, bool boost = false)
        {
            if (Session == null || Session.Paused || !float.IsFinite(deltaTime) || deltaTime <= 0) return;
            float remaining = Mathf.Min(deltaTime, 120);
            while (remaining > .000001f)
            {
                float dt = Mathf.Min(.05f, remaining); remaining -= dt;
                Step(throttle, steering, brake, drift, dt, boost);
                boost = false;
            }
        }
        void Step(float throttle, float steering, bool brake, bool drift, float dt, bool boost)
        {
            if (noticeTimer > 0) { noticeTimer -= dt; if (noticeTimer <= 0) Notice = null; }
            if (Session.Mode != GameMode.Results)
            {
                int missed = Session.Economy.MissedOrders;
                Orders.Tick(dt);
                if (missed != Session.Economy.MissedOrders) Notify("기다리던 손님이 떠났어요. 만든 솜사탕은 재고에 남아요.");
            }
            if (Session.Mode == GameMode.Racing)
            {
                int boosts = World.Kart.DriveModel.BoostCount;
                int drifts = World.Kart.DriveModel.DriftCount;
                double delta = World.Kart.Drive(throttle, steering, brake, dt, drift, boost);
                if (World.Kart.DriveModel.BoostCount > boosts) Audio.PlayBoost(World.Kart.DriveModel.BoostTier);
                if (World.Kart.DriveModel.DriftCount > drifts) Notify(RunStyle == DrivingStyle.Kart
                    ? "드리프트 성공! 부스터 " + World.Kart.DriveModel.StoredBoosts + "/2 · 가속이 끝나면 Shift로 사용"
                    : "드리프트 성공! 엑셀을 유지해 속도를 더 올리세요.");
                Session.TickRecipe(dt, delta * RunTargetGrams / 40.0 * 1.18 * (1 + Session.Economy.Levels[1] * .15), World.Kart.Radius, World.Kart.DriveModel.Laps, World.Kart.DriveModel.SkillCount, World.Kart.DriveModel.WallHits);
                if (Session.Production.Samples.Count != renderedSamples)
                {
                    renderedSamples = Session.Production.Samples.Count; World.CentralCandy.Show(Session.Production.Samples);
                }
            }
            SyncMode();
            World.UpdateOrders(Session.Economy, dt);
            World.UpdateThread(Session.Mode == GameMode.Racing && World.Kart.IsCollecting);
            Audio.UpdateDriving(World.Kart, Session.Mode == GameMode.Racing);
            if (Orders.Revision != orderRevision) { orderRevision = Orders.Revision; Save(); }
            UI.Refresh();
        }
        public void SelectOrder(string id)
        {
            if (Session == null || Session.Paused || Session.Mode != GameMode.Shop) return;
            var order = Session.Economy.Orders.Find(o => o.Id == id); if (order == null) return;
            SelectedOrderId = id; PreparedMap = order.Size; PreparedFlavor = order.Flavor;
            var match = Session.Economy.Inventory.Find(p => CandyRecipe.Matches(p, order));
            SelectedProductId = match?.Id; if (UI) UI.Refresh();
        }
        public void SelectProduct(string id)
        {
            if (Session.Mode != GameMode.Shop || Session.Paused) return;
            if (Session.Economy.Inventory.Exists(p => p.Id == id)) SelectedProductId = id;
            discardCandidate = null; UI.Refresh();
        }
        public void SetSize(int size) { SetMap(size); }
        public void SetMap(int map)
        {
            if (Session.Mode != GameMode.Shop || Session.Paused || map < 0 || map > 1) return;
            PreparedMap = map; SelectedOrderId = null; UI.Refresh();
        }
        public void SetFlavor(int flavor)
        {
            if (Session.Mode != GameMode.Shop || Session.Paused || flavor < 0 || flavor > 2) return;
            PreparedFlavor = flavor; SelectedOrderId = null; UI.Refresh();
        }
        public void SetStyle(int style)
        {
            if (Session.Mode != GameMode.Shop || Session.Paused || style < 0 || style > 1) return;
            PreparedStyle = (DrivingStyle)style; UI.Refresh();
        }
        public void PrepareStock() { if (Session.Mode != GameMode.Shop || Session.Paused) return; SelectedOrderId = null; StartRun(); }
        public void MakeOrder(string id) { SelectOrder(id); if (SelectedOrderId == id) StartRun(); }
        public void StartRun()
        {
            if (Session.Paused || Session.Mode != GameMode.Shop) return;
            if (!Store.CanSave) { Notify(Store.Error + "  도움말 → 새 가게 시작"); return; }
            RunMap = PreparedMap; RunFlavor = PreparedFlavor; RunStyle = PreparedStyle;
            RunTargetGrams = RaceRecipe.TargetGrams(RunMap);
            if (!Session.StartRecipe(RunMap, RunFlavor)) { Notify("진열대가 가득 찼어요. 손님에게 건네거나 재고를 정리하세요."); return; }
            World.SelectCourse(RunMap); World.Kart.ConfiguredFlavor = RunFlavor;
            World.Kart.SetStyle(RunStyle, World.Assets.DownhillCoupe);
            World.Kart.MaximumSpeed = Session.Economy.MaxSpeed; World.Kart.ResetPosition(); renderedSamples = -1; abortConfirmUntil = 0;
            World.CentralCandy.Show(null); Audio.Play(0); SyncMode();
            Notify(RaceRecipe.Name(RunMap) + " · " + Palette.FlavorName(RunFlavor) + " 설정! 한 바퀴 완주하면 완성됩니다.");
        }
        public void FinishRun()
        {
            if (Session.Mode != GameMode.Racing || Session.Paused) return;
            if (Time.unscaledTime >= abortConfirmUntil)
            { abortConfirmUntil = Time.unscaledTime + 5; Notify("솜사탕 없이 돌아가려면 '주행 포기' 또는 Enter를 한 번 더 누르세요."); return; }
            Session.FinishRun(); SyncMode();
        }
        public void ReturnToShop()
        {
            if (Session.Paused) return;
            Session.ReturnToShop(); SyncMode(); World.ShowInventory(Session.Economy);
            if (Session.Result != null) SelectedProductId = Session.Result.Id;
            if (SelectedOrder == null) SelectedOrderId = null;
            World.UpdateOrders(Session.Economy, 0); UI.Refresh();
        }
        public bool Serve(string orderId)
        {
            if (Session.Mode != GameMode.Shop || Session.Paused || !Store.CanSave) return false;
            var product = SelectedProduct; int slot = Session.Economy.Orders.FindIndex(o => o.Id == orderId);
            var receipt = Orders.Serve(orderId, SelectedProductId);
            if (receipt == null) { Notify("재고에서 주문의 맛과 크기에 맞는 솜사탕을 선택하세요."); return false; }
            World.HandOver(product, slot); SelectedProductId = null;
            Audio.Play(1); Notify("직접 전달했어요!  +" + receipt.Price + " 코인 · 팁 +" + receipt.Tip);
            orderRevision = Orders.Revision; Save(); World.ShowInventory(Session.Economy); World.UpdateOrders(Session.Economy, 0); UI.Refresh(); return true;
        }
        public void DiscardSelected()
        {
            if (Session.Mode != GameMode.Shop || Session.Paused || !Store.CanSave || SelectedProduct == null) return;
            if (discardCandidate != SelectedProductId || Time.unscaledTime > discardConfirmUntil)
            { discardCandidate = SelectedProductId; discardConfirmUntil = Time.unscaledTime + 5; Notify("선택한 솜사탕을 버리려면 '재고 정리'를 한 번 더 누르세요."); return; }
            Session.Economy.Discard(SelectedProductId); SelectedProductId = null; discardCandidate = null;
            World.ShowInventory(Session.Economy); Save(); Notify("선택한 재고를 정리했어요."); UI.Refresh();
        }
        void SyncMode()
        {
            if (lastMode == Session.Mode) return; lastMode = Session.Mode; World.SetMode(lastMode);
            if (lastMode == GameMode.Results)
            {
                World.Kart.Stop(); World.UpdateThread(false); Audio.UpdateDriving(World.Kart, false); Audio.Play(2);
                World.CentralCandy.Show(Session.Result?.Samples);
                World.SetCandyQuality(Session.Result == null ? 0 : Session.Result.Quality);
                Notify(Session.Result == null ? "이번 주행은 미완주입니다. 다시 도전해보세요." : "완주! 솜사탕 보관 · 기록 보상 +" + Session.ResultBonus + " 코인");
                Save();
            }
            if (UI) UI.Refresh();
        }
        public void BuyUpgrade(int index)
        {
            if (Session.Mode != GameMode.Shop || Session.Paused || !Store.CanSave) return;
            if (Session.Economy.BuyUpgrade(index)) { Audio.Play(1); Notify("업그레이드 완료!"); Save(); }
            else Notify("코인이 부족하거나 최고 레벨이에요."); UI.Refresh();
        }
        public void BuyShelf()
        {
            if (Session.Mode != GameMode.Shop || Session.Paused || !Store.CanSave) return;
            if (Session.Economy.BuyShelf()) { World.ShowInventory(Session.Economy); Audio.Play(1); Notify("진열대 확장! 최대 " + Session.Economy.StockCapacity + "개를 보관해요."); Save(); }
            else Notify("확장할 코인이 부족하거나 최대 크기예요."); UI.Refresh();
        }
        public void TogglePause()
        {
            Session.Paused = !Session.Paused; World.AnimationPaused = Session.Paused; World.UpdateThread(false);
            World.Kart.SetEffects(!Session.Paused && Session.Mode == GameMode.Racing); Audio.UpdateDriving(World.Kart, !Session.Paused && Session.Mode == GameMode.Racing); UI.Refresh();
        }
        public void ToggleMute() { Audio.Toggle(); UI.Refresh(); }
        public void ResetSave()
        {
            if (Session.Mode != GameMode.Shop || !Store.ArchiveAndReset()) return;
            Initialize(Store.DirectoryPath); World.AnimationPaused = false; Notify("새로운 솜사탕 가게를 열었어요."); UI.Refresh();
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
