using UnityEngine;
namespace CottonCircuit
{
    public partial class GameController : MonoBehaviour
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
        public bool ContinuousMode { get; private set; }
        public bool RaceVisible => ContinuousMode || Session?.Mode == GameMode.Racing;
        public double RunProgress => Session == null || Session.ProductionWaiting || World.Kart.DriveModel == null
            ? 0 : World.Kart.DriveModel.Sample.Progress / World.Kart.DriveModel.Course.Length;
        bool ShopActionsAllowed => Session != null && !Session.Paused && (Shift == null || Shift.IsOpen) && (ContinuousMode || Session.Mode == GameMode.Shop);
        public CustomerOrder SelectedOrder => Session?.Economy.Orders.Find(o => o.Id == SelectedOrderId);
        public Product SelectedProduct => Session?.Economy.Inventory.Find(p => p.Id == SelectedProductId);
        public MusicScene MusicScene => new MusicScene
        {
            Title = titleScreen, Story = titleScreen && titleScreen.StoryShowing, InGame = Session != null,
            Tutorial = TutorialActive, Progression = HasProgression, ShiftExists = Shift != null,
            ShiftOpen = Shift != null && Shift.DayRunning,
            Phase = HasProgression ? Session.Economy.Progression.Phase : BusinessPhase.Operating,
            Mode = Session != null ? Session.Mode : GameMode.Shop,
            Machine = HasProgression ? Shift.SelectedMachine : 0,
            RemainingSeconds = Shift != null ? Shift.State.RemainingSeconds : 0
        };
        float noticeTimer, discardConfirmUntil, abortConfirmUntil;
        string discardCandidate;
        int renderedSamples = -1, orderRevision;
        GameMode lastMode;
        TitleScreenUI titleScreen;
        public void ShowTitle(string saveDirectory)
        {
            if (Session != null || titleScreen) return;
            if (World) World.gameObject.SetActive(false);
            if (!titleScreen)
            {
                var titleObject = new GameObject("Title Screen");
                titleObject.transform.SetParent(transform, false);
                titleScreen = titleObject.AddComponent<TitleScreenUI>();
            }
            titleScreen.Show(this, saveDirectory);
        }
        public void ReturnToTitle()
        {
            if (Session == null || Store == null || titleScreen) return;
            if (UI) UI.CancelShiftDrag();
            // Keep the live session if a writable save fails. A protected, unreadable
            // save can return to the title without replacing its original bytes.
            if (Store.CanSave && !Save()) { if (UI) UI.Refresh(); return; }
            string directory = Store.DirectoryPath;
            CloseTutorialUI();
            if (World)
            {
                World.Kart.Stop(); World.Kart.SetEffects(false);
                World.UpdateThread(false); World.AnimationPaused = true;
                if (Audio) Audio.UpdateDriving(World.Kart, false);
            }
            if (UI) UI.HideForTitle();
            Session = null; Shift = null; Orders = null;
            SelectedOrderId = SelectedProductId = null;
            Notice = null; noticeTimer = 0;
            ContinuousMode = false;
            ShowTitle(directory);
        }
        public void Initialize(string saveDirectory, bool continuous = true, bool businessDay = true, bool progression = true, bool tutorial = false)
        {
            if (titleScreen) { titleScreen.Close(); titleScreen = null; }
            CloseTutorialUI();
            if (World) World.gameObject.SetActive(true);
            if (UI) UI.CancelShiftDrag();
            Notice = null; noticeTimer = 0;
            Store = new SaveStore(saveDirectory); Session = new GameSession(Store.Load());
            machineDrives = new ArcadeDrive[3];
            if (continuous && businessDay && progression && Session.Economy.Progression == null)
            {
                Progression.Enable(Session.Economy);
                Session.Economy.Business = null; Session.Economy.Inventory.Clear(); Session.Economy.CompletedIds.Clear();
            }
            ContinuousMode = continuous;
            Shift = continuous && businessDay ? new ShopShift(Session.Economy) : null;
            if (tutorial && Shift != null) Shift.BeginTutorial();
            Orders = Shift == null ? new OrderManager(Session.Economy) : null;
            orderRevision = Orders == null ? 0 : Orders.Revision;
            lastMode = Session.Mode; PreparedMap = PreparedFlavor = 0; RunFlavor = 0;
            PreparedStyle = RunStyle = Shift != null ? DrivingStyle.Downhill : DrivingStyle.Kart;
            if (HasProgression) PreparedStyle = RunStyle = Session.Economy.Progression.CartStyle == 0 ? DrivingStyle.Kart : DrivingStyle.Downhill;
            SelectedOrderId = SelectedProductId = null;
            if (World)
            {
                World.Initialize(); World.SetContinuousMode(continuous); World.SelectCourse(0);
                World.SetMode(GameMode.Shop); World.Kart.ResetPosition();
                World.ShowInventory(Session.Economy); World.UpdateOrders(Session.Economy, 0);
            }
            if (Shift != null) InitializeShiftDriving();
            else if (Orders.Orders.Count > 0) SelectOrder(Orders.Orders[0].Id);
            if (HasProgression) ApplyMachineCourse(false);
            if (UI) UI.Initialize(this);
            PrepareTutorialUI();
            if (ContinuousMode && Shift == null) BeginContinuousRecipe(true);
            if (Store.Error != null) Notify(Store.Error); else Save();
        }
        void Start() { if (Session == null && !titleScreen) ShowTitle(Application.persistentDataPath); }
        void Update()
        {
            if (Session == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (Session.Paused) { UI.Refresh(); return; }
            if (Shift != null && Input.GetMouseButtonDown(1)) EmptySugar();
            if (Shift != null && Input.GetKeyDown(KeyCode.F)) ExtractCandy();
            if (!ContinuousMode && Input.GetKeyDown(KeyCode.Return))
            {
                if (Session.Mode == GameMode.Shop) StartRun();
                else if (Session.Mode == GameMode.Racing) FinishRun();
                else ReturnToShop();
            }
            if (Session.Mode == GameMode.Racing && !SelectedMachineHasWorker && (Shift == null || Shift.IsOpen) && Input.GetKeyDown(KeyCode.R))
            { World.Kart.Recover(); Notify(Shift == null ? "코스에 복귀했어요. 결승선까지 한 바퀴 완주하세요." : "코스에 복귀했어요. 복귀 이동은 생산 거리에 포함되지 않아요."); }
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
                if (Shift != null && !Shift.IsOpen) break;
            }
        }
        void Step(float throttle, float steering, bool brake, bool drift, float dt, bool boost)
        {
            if (noticeTimer > 0) { noticeTimer -= dt; if (noticeTimer <= 0) Notice = null; }
            if (Shift != null) { StepShift(throttle, steering, brake, drift, dt, boost); return; }
            if (Session.Mode != GameMode.Results)
            {
                int missed = Session.Economy.MissedOrders;
                Orders.Tick(dt);
                if (missed != Session.Economy.MissedOrders) Notify("기다리던 손님이 떠났어요. 만든 솜사탕은 재고에 남아요.");
            }
            if (Session.Mode == GameMode.Racing)
            {
                World.Kart.MaximumSpeed = Session.Economy.MaxSpeed;
                int boosts = World.Kart.DriveModel.BoostCount, hits = World.Kart.DriveModel.WallHits;
                int drifts = World.Kart.DriveModel.DriftCount;
                int previousLaps = World.Kart.DriveModel.Laps, previousRecipes = Session.CompletedRecipes;
                double delta = World.Kart.Drive(throttle, steering, brake, dt, drift, boost);
                if (World.Kart.DriveModel.BoostCount > boosts) Audio.PlayBoost(World.Kart.DriveModel.BoostTier);
                if (World.Kart.DriveModel.WallHits > hits) Audio.Play(Sound.WallHit);
                if (World.Kart.DriveModel.DriftCount > drifts) Notify(RunStyle == DrivingStyle.Kart
                    ? "드리프트 성공! 부스터 " + World.Kart.DriveModel.StoredBoosts + "/2 · 가속이 끝나면 Shift로 사용"
                    : "드리프트 성공! 엑셀을 유지해 속도를 더 올리세요.");
                if (Store.CanSave) Session.TickRecipe(dt, delta * RunTargetGrams / 40.0 * 1.18 * (1 + Session.Economy.Levels[1] * .15), World.Kart.Radius, World.Kart.DriveModel.Laps, World.Kart.DriveModel.SkillCount, World.Kart.DriveModel.WallHits);
                if (ContinuousMode && previousRecipes != Session.CompletedRecipes)
                {
                    if (SelectedProduct == null) SelectedProductId = Session.Result.Id;
                    World.ShowInventory(Session.Economy); Audio.Play(Sound.CandyExtract); Save();
                    Notify("완주! 솜사탕 보관 · 기록 보상 +" + Session.ResultBonus + " 코인" +
                        (Session.ProductionWaiting ? " · 진열대가 가득 차 생산을 기다려요." : " · 다음 솜사탕을 만들어요."));
                    renderedSamples = -1;
                }
                if (ContinuousMode && World.Kart.DriveModel.Laps > previousLaps &&
                    (RunMap != PreparedMap || RunFlavor != PreparedFlavor || RunStyle != PreparedStyle))
                    BeginContinuousRecipe(false);
                if (Session.Production.Samples.Count != renderedSamples)
                {
                    renderedSamples = Session.Production.Samples.Count; World.CentralCandy.Show(Session.Production.Samples);
                }
            }
            SyncMode();
            World.UpdateOrders(Session.Economy, dt);
            World.UpdateThread(Session.Mode == GameMode.Racing && !Session.ProductionWaiting && World.Kart.IsCollecting);
            Audio.UpdateDriving(World.Kart, Session.Mode == GameMode.Racing);
            if (Orders.Revision != orderRevision) { orderRevision = Orders.Revision; Save(); }
            UI.Refresh();
        }
        public void SelectOrder(string id)
        {
            if (Shift != null) return;
            if (!ShopActionsAllowed) return;
            var order = Session.Economy.Orders.Find(o => o.Id == id); if (order == null) return;
            SelectedOrderId = id; PreparedMap = order.Size; PreparedFlavor = order.Flavor;
            var match = Session.Economy.Inventory.Find(p => CandyRecipe.Matches(p, order));
            SelectedProductId = match?.Id; if (UI) UI.Refresh();
        }
        public void SelectProduct(string id)
        {
            if (!ShopActionsAllowed) return;
            if (Session.Economy.Inventory.Exists(p => p.Id == id)) SelectedProductId = id;
            discardCandidate = null; UI.Refresh();
        }
        public void SetSize(int size) { SetMap(size); }
        public void SetMap(int map)
        {
            if (!ShopActionsAllowed || map < 0 || map > 1) return;
            PreparedMap = map; SelectedOrderId = null; UI.Refresh();
        }
        public void SetFlavor(int flavor)
        {
            if (!ShopActionsAllowed || flavor < 0 || flavor > 2) return;
            PreparedFlavor = flavor; SelectedOrderId = null; UI.Refresh();
        }
        public void SetStyle(int style)
        {
            if (!ShopActionsAllowed || style < 0 || style > 1) return;
            PreparedStyle = (DrivingStyle)style; UI.Refresh();
        }
        public void PrepareStock() { if (!ShopActionsAllowed) return; SelectedOrderId = null; StartRun(); }
        public void MakeOrder(string id) { SelectOrder(id); if (SelectedOrderId == id) StartRun(); }
        public void StartRun()
        {
            if (ContinuousMode)
            {
                if (Session.Paused) return;
                Notify("다음 바퀴부터 " + RaceRecipe.Name(PreparedMap) + " · " + Palette.FlavorName(PreparedFlavor) + " 솜사탕을 만들어요.");
                return;
            }
            if (Session.Paused || Session.Mode != GameMode.Shop) return;
            if (!Store.CanSave) { Notify(Store.Error + "  Esc → 타이틀 화면으로 → 새 게임"); return; }
            RunMap = PreparedMap; RunFlavor = PreparedFlavor; RunStyle = PreparedStyle;
            RunTargetGrams = RaceRecipe.TargetGrams(RunMap);
            if (!Session.StartRecipe(RunMap, RunFlavor)) { Notify("진열대가 가득 찼어요. 손님에게 건네거나 재고를 정리하세요."); return; }
            World.SelectCourse(RunMap); World.Kart.ConfiguredFlavor = RunFlavor;
            World.Kart.SetStyle(RunStyle, World.Assets.DownhillCoupe);
            World.Kart.MaximumSpeed = Session.Economy.MaxSpeed; World.Kart.ResetPosition(); renderedSamples = -1; abortConfirmUntil = 0;
            World.CentralCandy.Show(null); Audio.Play(Sound.EngineStart); SyncMode();
            Notify(RaceRecipe.Name(RunMap) + " · " + Palette.FlavorName(RunFlavor) + " 설정! 한 바퀴 완주하면 완성됩니다.");
        }
        void BeginContinuousRecipe(bool forceReset)
        {
            bool reset = forceReset || RunMap != PreparedMap || RunStyle != PreparedStyle;
            RunMap = PreparedMap; RunFlavor = PreparedFlavor; RunStyle = PreparedStyle;
            RunTargetGrams = RaceRecipe.TargetGrams(RunMap);
            if (reset)
            {
                World.SelectCourse(RunMap);
                World.Kart.SetStyle(RunStyle, World.Assets.DownhillCoupe);
                World.Kart.MaximumSpeed = Session.Economy.MaxSpeed; World.Kart.ResetPosition();
            }
            World.Kart.ConfiguredFlavor = RunFlavor;
            var drive = World.Kart.DriveModel;
            Session.StartContinuousRecipe(RunMap, RunFlavor, drive.Laps, drive.SkillCount, drive.WallHits);
            renderedSamples = -1; abortConfirmUntil = 0;
            World.CentralCandy.Show(null); SyncMode();
        }
        public void FinishRun()
        {
            if (ContinuousMode || Session.Mode != GameMode.Racing || Session.Paused) return;
            if (Time.unscaledTime >= abortConfirmUntil)
            { abortConfirmUntil = Time.unscaledTime + 5; Notify("솜사탕 없이 돌아가려면 '주행 포기' 또는 Enter를 한 번 더 누르세요."); return; }
            Session.FinishRun(); SyncMode();
        }
        public void ReturnToShop()
        {
            if (Session.Paused) return;
            if (ContinuousMode) { UI.Refresh(); return; }
            Session.ReturnToShop(); SyncMode(); World.ShowInventory(Session.Economy);
            if (Session.Result != null) SelectedProductId = Session.Result.Id;
            if (SelectedOrder == null) SelectedOrderId = null;
            World.UpdateOrders(Session.Economy, 0); UI.Refresh();
        }
        public bool Serve(string orderId)
        {
            if (Shift != null) return DeliverCandy(SelectedProductId, orderId) == DeliveryResult.Sold;
            if (!ShopActionsAllowed || !Store.CanSave) return false;
            var product = SelectedProduct; int slot = Session.Economy.Orders.FindIndex(o => o.Id == orderId);
            var receipt = Orders.Serve(orderId, SelectedProductId);
            if (receipt == null) { Notify("재고에서 주문의 맛과 크기에 맞는 솜사탕을 선택하세요."); return false; }
            World.HandOver(product, slot); SelectedProductId = null;
            Audio.PlaySale(false); Notify("직접 전달했어요!  +" + receipt.Price + " 코인 · 팁 +" + receipt.Tip);
            orderRevision = Orders.Revision; Save(); World.ShowInventory(Session.Economy); World.UpdateOrders(Session.Economy, 0); UI.Refresh(); return true;
        }
        public void DiscardSelected()
        {
            if (!ShopActionsAllowed || !Store.CanSave || SelectedProduct == null) return;
            if (discardCandidate != SelectedProductId || Time.unscaledTime > discardConfirmUntil)
            { discardCandidate = SelectedProductId; discardConfirmUntil = Time.unscaledTime + 5; Notify("선택한 솜사탕을 버리려면 '재고 정리'를 한 번 더 누르세요."); return; }
            Session.Economy.Discard(SelectedProductId); Audio.Play(Sound.Trash); SelectedProductId = null; discardCandidate = null;
            World.ShowInventory(Session.Economy); Save(); Notify("선택한 재고를 정리했어요."); UI.Refresh();
        }
        void SyncMode()
        {
            if (lastMode == Session.Mode) return; lastMode = Session.Mode; World.SetMode(lastMode);
            if (lastMode == GameMode.Results)
            {
                World.Kart.Stop(); World.UpdateThread(false); Audio.UpdateDriving(World.Kart, false); Audio.Play(Sound.ClosingJingle);
                World.CentralCandy.Show(Session.Result?.Samples);
                World.SetCandyQuality(Session.Result == null ? 0 : Session.Result.Quality);
                Notify(Session.Result == null ? "이번 주행은 미완주입니다. 다시 도전해보세요." : "완주! 솜사탕 보관 · 기록 보상 +" + Session.ResultBonus + " 코인");
                Save();
            }
            if (UI) UI.Refresh();
        }
        public void BuyUpgrade(int index)
        {
            if (!ShopActionsAllowed || !Store.CanSave) return;
            if (Session.Economy.BuyUpgrade(index)) { Audio.Play(Sound.Purchase); Notify("업그레이드 완료!"); Save(); }
            else Notify("코인이 부족하거나 최고 레벨이에요."); UI.Refresh();
        }
        public void BuyShelf()
        {
            if (!ShopActionsAllowed || !Store.CanSave) return;
            if (Session.Economy.BuyShelf()) { World.ShowInventory(Session.Economy); Audio.Play(Sound.Purchase); Notify("진열대 확장! 최대 " + Session.Economy.StockCapacity + "개를 보관해요."); Save(); }
            else Notify("확장할 코인이 부족하거나 최대 크기예요."); UI.Refresh();
        }
        public void TogglePause()
        {
            if (Session == null) return;
            Session.Paused = !Session.Paused; World.AnimationPaused = Session.Paused; World.UpdateThread(false);
            if (Shift != null)
            {
                Shift.Paused = Session.Paused;
                if (UI) UI.CancelShiftDrag();
                World.AnimationPaused = Session.Paused || !Shift.IsOpen;
                World.Kart.SetEffects(!World.AnimationPaused); Audio.UpdateDriving(World.Kart, !World.AnimationPaused);
                Save(); UI.Refresh(); return;
            }
            World.Kart.SetEffects(!Session.Paused && Session.Mode == GameMode.Racing); Audio.UpdateDriving(World.Kart, !Session.Paused && Session.Mode == GameMode.Racing); UI.Refresh();
        }
        public void ToggleMute() { Audio.Toggle(); UI.Refresh(); }
        public void ResetSave()
        {
            if ((!ContinuousMode && Session.Mode != GameMode.Shop) || !Store.ArchiveAndReset()) return;
            Initialize(Store.DirectoryPath, ContinuousMode, Shift != null, HasProgression);
            World.AnimationPaused = Shift != null && !Shift.IsOpen; Notify("새로운 솜사탕 가게를 열었어요."); UI.Refresh();
        }
        bool Save()
        {
            if (Shift != null) Shift.SyncActive();
            bool saved = Store.Save(Session.Economy);
            if (!saved && Store.Error != null) Notify(Store.Error);
            return saved;
        }
        public void Notify(string message) { Notice = message; noticeTimer = 5; }
        void OnApplicationQuit() { if (Session != null && Store != null) Save(); }
        void OnApplicationFocus(bool focused)
        {
            if (enabled && !focused && Session != null && Session.Mode == GameMode.Racing && !Application.isBatchMode)
            {
                Session.Paused = true;
                if (Shift != null) { Shift.Paused = true; if (UI) UI.CancelShiftDrag(); Save(); }
                World.AnimationPaused = true; World.Kart.SetEffects(false); World.UpdateThread(false); Audio.UpdateDriving(World.Kart, false); if (UI) UI.Refresh();
            }
        }
    }
}
