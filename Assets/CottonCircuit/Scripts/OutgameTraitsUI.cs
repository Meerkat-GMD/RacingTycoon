using System.Collections.Generic;
using UnityEngine;

namespace CottonCircuit
{
    public partial class GameUI
    {
        const float TraitMapWidth = 1048, TraitMapHeight = 550;
        UpgradeGraphGraphic upgradeEdges;
        UnityEngine.UI.ScrollRect upgradeScroll;
        RectTransform upgradeMap;
        UnityEngine.UI.Text graphZoomLabel;
        float graphZoom = 1, graphFitZoom = 1;
        int traitTab;
        string highlightedTrait;
        readonly List<UpgradeNodeView> upgradeViews = new List<UpgradeNodeView>();
        readonly List<TraitRequirementView> traitRequirements = new List<TraitRequirementView>();
        UnityEngine.UI.Button[] traitTabs;

        sealed class UpgradeNodeView
        {
            public UpgradeNode Node;
            public int Tab;
            public UnityEngine.UI.Button Button;
            public ProgressionArtGraphic Disc;
            public UnityEngine.UI.Image Art;
            public GameObject Highlight;
            public UnityEngine.UI.Text Rank, Name;
        }
        sealed class TraitRequirementView
        {
            public string Parent;
            public int Tab;
            public UnityEngine.UI.Button Button;
            public UnityEngine.UI.Text Text;
        }

        void BuildUpgradeGraph(RectTransform p)
        {
            traitTab = 0; highlightedTrait = null; traitRequirements.Clear();
            Box(p, "GraphPaper", 0, 0, 1080, 742, PrepWhite);
            Label(p, "성장 지도", 27, 21, 240, 43, 25, PrepInk, FontStyle.Bold);
            preparationCount = Label(p, "", 638, 28, 415, 30, 14, PrepMuted, FontStyle.Normal, TextAnchor.MiddleRight);
            traitTabs = new UnityEngine.UI.Button[UpgradeTreeLayout.Tabs.Length];
            for (int i = 0; i < traitTabs.Length; i++)
            {
                int index = i;
                var tab = UpgradeTreeLayout.Tabs[i];
                traitTabs[i] = ButtonAt(p, tab.Name, 19 + i * 174, 77, 166, 43, PrepPaper, PrepInk,
                    () => ShowTraitTab(index), 16);
                traitTabs[i].name = "TraitTab_" + tab.Id;
            }

            var scrollRoot = Rect(p, "UpgradeGraphScroll", 16, 138, 1048, 548);
            var navigation = scrollRoot.gameObject.AddComponent<UpgradeGraphScroll>();
            upgradeScroll = navigation;
            navigation.Zoom = delta => SetGraphZoom(graphZoom * Mathf.Pow(1.15f, delta));
            var viewport = Box(scrollRoot, "Viewport", 0, 0, 1048, 548, PrepWhite, false);
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = Rect(viewport.rectTransform, "Content", 0, 0, TraitMapWidth, TraitMapHeight);
            upgradeMap = Rect(content, "TraitMap", 0, 0, TraitMapWidth, TraitMapHeight);
            upgradeScroll.viewport = viewport.rectTransform; upgradeScroll.content = content;
            upgradeScroll.horizontal = true; upgradeScroll.vertical = true;
            upgradeScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            upgradeScroll.inertia = true; upgradeScroll.decelerationRate = .08f;
            upgradeScroll.onValueChanged.AddListener(_ => HoverHint.HideAll());
            upgradeEdges = Rect(upgradeMap, "UpgradeConnections", 0, 0, TraitMapWidth, TraitMapHeight)
                .gameObject.AddComponent<UpgradeGraphGraphic>();
            upgradeEdges.raycastTarget = false;

            foreach (var node in Progression.Nodes)
            {
                var n = node;
                var position = UpgradeTreeLayout.Find(node.Id);
                var view = new UpgradeNodeView { Node = node, Tab = UpgradeTreeLayout.TabOf(node.Id) };
                var nodeRoot = Rect(upgradeMap, "UpgradeNode_" + node.Id, position.X - 88, position.Y - 32, 176, 64);
                view.Highlight = PrepArt(nodeRoot, "NavigationHighlight", 51, -5, 74, 74, "disc", Palette.Hex("D6A853")).gameObject;
                view.Disc = PrepArt(nodeRoot, "NodeCircle", 56, 0, 64, 64, "disc", CategoryColor(node.Category));
                view.Disc.raycastTarget = true;
                view.Button = nodeRoot.gameObject.AddComponent<UnityEngine.UI.Button>();
                view.Button.targetGraphic = view.Disc;
                view.Button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                var colors = view.Button.colors;
                colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f); colors.pressedColor = new Color(.87f, .87f, .87f);
                colors.disabledColor = Color.white; view.Button.colors = colors;
                view.Button.onClick.AddListener(() =>
                {
                    int before = Progression.Level(game.Session.Economy, n.Id); game.PurchaseNode(n.Id);
                    if (Progression.Level(game.Session.Economy, n.Id) > before)
                    { speechOverride = n.Name + ", 준비됐어요!"; speechUntil = Time.unscaledTime + 5; }
                    highlightedTrait = n.Id; upgradeEdges.PinnedNode = n.Id;
                    outgameFingerprint = int.MinValue;
                });
                view.Art = PrepSprite(nodeRoot, "NodeIcon", 67, 8, 42, 42, UiArt.TraitIcon(node.Id));
                var badge = Box(nodeRoot, "RankBadge", 70, 49, 36, 19, PrepInk);
                view.Rank = Label(badge.rectTransform, "", 0, 0, 36, 19, 11, PrepWhite, FontStyle.Bold, TextAnchor.MiddleCenter);
                Box(nodeRoot, "NodeCaptionPaper", 0, 71, 176, 27, PrepWhite, false);
                view.Name = Label(nodeRoot, node.Name, 0, 71, 176, 27, 16, PrepInk, FontStyle.Bold, TextAnchor.UpperCenter);
                var focus = nodeRoot.gameObject.AddComponent<UpgradeGraphFocus>(); focus.Graph = upgradeEdges; focus.NodeId = node.Id;
                HoverHint.Attach(nodeRoot.gameObject, () => UpgradeDetails(n), true);

                int row = 0;
                foreach (string parentId in node.Parents)
                {
                    if (UpgradeTreeLayout.TabOf(parentId) == view.Tab) continue;
                    string parent = parentId;
                    // Keep prerequisite controls beside the trait in the hierarchy so hover ownership is unambiguous.
                    var requirement = ButtonAt(upgradeMap, "", position.X - 88, position.Y + 69 + row++ * 23, 176, 21, PrepPaper, PrepInk,
                        () => ShowTraitTab(UpgradeTreeLayout.TabOf(parent), parent), 13);
                    requirement.name = "TraitRequirement_" + node.Id + "_" + parent;
                    requirement.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                    traitRequirements.Add(new TraitRequirementView { Parent = parent, Tab = view.Tab, Button = requirement,
                        Text = requirement.GetComponentInChildren<UnityEngine.UI.Text>() });
                    HoverHint.Attach(requirement.gameObject, () => RequirementDetails(parent));
                }
                upgradeViews.Add(view);
            }

            graphZoomLabel = Label(p, "", 28, 700, 65, 25, 12, PrepMuted);
            Label(p, "○ 필요 특성    ✓ 완료", 112, 698, 310, 27, 13, PrepMuted);
            var zoomOut = ButtonAt(p, "−", 837, 697, 42, 32, PrepPaper, PrepInk, () => SetGraphZoom(graphZoom / 1.25f), 19);
            zoomOut.name = "ZoomGraphOut";
            var zoomIn = ButtonAt(p, "+", 887, 697, 42, 32, PrepPaper, PrepInk, () => SetGraphZoom(graphZoom * 1.25f), 19);
            zoomIn.name = "ZoomGraphIn";
            var reset = ButtonAt(p, "전체", 937, 697, 113, 32, PrepPaper, PrepInk, () => SetGraphZoom(graphFitZoom, true), 12);
            reset.name = "ResetGraphView";
            graphZoomLabel.raycastTarget = true;
            HoverHint.Attach(graphZoomLabel.gameObject, "드래그로 이동 · 휠로 확대/축소");
            graphFitZoom = Mathf.Min(1, Mathf.Min(1048 / TraitMapWidth, 548 / TraitMapHeight));
            ShowTraitTab(0);
        }

        void ShowTraitTab(int index, string target = null)
        {
            traitTab = Mathf.Clamp(index, 0, UpgradeTreeLayout.Tabs.Length - 1);
            highlightedTrait = target; HoverHint.HideAll();
            foreach (var view in upgradeViews) view.Button.gameObject.SetActive(view.Tab == traitTab);
            foreach (var requirement in traitRequirements) requirement.Button.gameObject.SetActive(requirement.Tab == traitTab);
            for (int i = 0; i < traitTabs.Length; i++) traitTabs[i].image.color = i == traitTab ? PrepMint : PrepPaper;
            upgradeEdges.TabIndex = traitTab; upgradeEdges.PinnedNode = target; upgradeEdges.Focus(null);
            SetGraphZoom(graphFitZoom, true); outgameFingerprint = int.MinValue;
            if (game.HasProgression) RefreshUpgradeGraph(game.Session.Economy);
        }

        void SetGraphZoom(float zoom, bool fit = false)
        {
            if (!upgradeMap || !upgradeScroll) return;
            HoverHint.HideAll(); upgradeScroll.StopMovement();
            var content = upgradeScroll.content; var viewport = upgradeScroll.viewport;
            Vector2 center = new Vector2(viewport.rect.width * .5f, -viewport.rect.height * .5f);
            Vector2 focalPoint = (center - content.anchoredPosition - upgradeMap.anchoredPosition) / graphZoom;
            graphZoom = Mathf.Clamp(zoom, graphFitZoom, 1.7f);
            Vector2 mapSize = upgradeMap.sizeDelta * graphZoom;
            content.sizeDelta = new Vector2(Mathf.Max(viewport.rect.width, mapSize.x), Mathf.Max(viewport.rect.height, mapSize.y));
            upgradeMap.localScale = Vector3.one * graphZoom;
            upgradeMap.anchoredPosition = new Vector2((content.sizeDelta.x - mapSize.x) * .5f, -(content.sizeDelta.y - mapSize.y) * .5f);
            Vector2 position = fit ? Vector2.zero : center - focalPoint * graphZoom - upgradeMap.anchoredPosition;
            content.anchoredPosition = new Vector2(Mathf.Clamp(position.x, viewport.rect.width - content.sizeDelta.x, 0),
                Mathf.Clamp(position.y, 0, content.sizeDelta.y - viewport.rect.height));
            if (graphZoomLabel) graphZoomLabel.text = Mathf.RoundToInt(graphZoom * 100) + "%";
        }

        void RefreshUpgradeGraph(Economy economy)
        {
            int bought = 0, total = 0;
            foreach (var view in upgradeViews)
            {
                var node = view.Node; int rank = Progression.Level(economy, node.Id);
                bool available = Progression.CanBuy(economy, node.Id), reachable = true;
                if (view.Tab == traitTab) { total++; if (rank > 0) bought++; }
                foreach (var parent in node.Parents) if (Progression.Level(economy, parent) == 0) reachable = false;
                view.Button.interactable = available;
                view.Disc.color = rank > 0 || available ? CategoryColor(node.Category) : reachable
                    ? Color.Lerp(CategoryColor(node.Category), PrepWhite, .38f) : Palette.Hex("E7E9E2");
                view.Disc.SetVerticesDirty(); view.Art.canvasRenderer.SetAlpha(rank > 0 || reachable ? 1 : .55f);
                view.Rank.text = rank + "/" + node.MaxLevel;
                view.Name.color = rank > 0 || reachable ? PrepInk : Palette.Hex("65736D");
                view.Highlight.SetActive(highlightedTrait == node.Id);
            }
            foreach (var requirement in traitRequirements)
            {
                bool met = Progression.Level(economy, requirement.Parent) > 0;
                requirement.Text.text = (met ? "✓ " : "○ ") + Progression.Find(requirement.Parent).Name + "  ↗";
                requirement.Button.image.color = met ? Palette.Hex("DDEDE4") : Palette.Hex("F4E3D3");
            }
            preparationCount.text = bought + " / " + total + " 해금";
            upgradeEdges.Economy = economy; upgradeEdges.RefreshEdges();
        }

        string RequirementDetails(string id)
        {
            var parent = Progression.Find(id);
            bool met = Progression.Level(game.Session.Economy, id) > 0;
            return "필요 특성  ·  " + UpgradeTreeLayout.Tabs[UpgradeTreeLayout.TabOf(id)].Name + "\n" + parent.Name +
                (met ? "  ✓ 완료" : "  1단계 필요") + "\n클릭하면 해당 특성으로 이동합니다.";
        }

        string UpgradeDetails(UpgradeNode node)
        {
            var economy = game.Session.Economy; int level = Progression.Level(economy, node.Id);
            string value = node.Name + "   " + level + "/" + node.MaxLevel + "\n" + node.Description;
            string effect = Progression.EffectSummary(economy, node.Id);
            if (effect != node.Description) value += "\n" + effect;
            bool complete = level >= node.MaxLevel;
            int cost = Progression.Cost(economy, node.Id);
            value += complete ? "\n모든 단계 완료" : "\n비용  " + cost.ToString("N0") + " C";
            if (node.Parents.Length > 0)
            {
                value += "\n필요 특성  ·  각각 1단계";
                foreach (string parent in node.Parents)
                    value += "\n" + (Progression.Level(economy, parent) > 0 ? "✓ " : "○ ") + Progression.Find(parent).Name;
            }
            if (!complete && economy.Coins < cost) value += "\n" + (cost - economy.Coins).ToString("N0") + " C 부족";
            return value;
        }
    }
}
