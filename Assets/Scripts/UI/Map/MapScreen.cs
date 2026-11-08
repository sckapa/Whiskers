using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Whiskers.Core.Events;
using Whiskers.Run;
using Whiskers.Run.Map;
using Random = System.Random;

namespace Whiskers.UI.Map
{
    // Draws the run map (bottom to top, boss on top) and lets the player pick the next node.
    // Nodes and lines are created at runtime from the prefabs, or as plain placeholders without them.
    public sealed class MapScreen : MonoBehaviour
    {
        private const string HealthFormat = "HP {0} / {1}";
        private const float Half = 0.5f;
        private const int JitterSeedMultiplier = 7919;

        [Serializable]
        private sealed class NodeLook
        {
            public MapNodeType type = MapNodeType.Combat;
            public Sprite icon = null;
            public Color color = Color.white;
            [Tooltip("Shown when there is no icon")]
            public string letter = "?";
        }

        [Header("Layout")]
        [Tooltip("Content of a vertical ScrollRect (or any RectTransform). Nodes are placed from its bottom")]
        [SerializeField] private RectTransform content = null;
        [SerializeField] private ScrollRect scrollRect = null;
        [SerializeField] private Vector2 cellSize = new Vector2(160f, 150f);
        [SerializeField] private float padding = 120f;
        [Tooltip("Random offset of each node, share of a cell")]
        [SerializeField, Range(0f, 0.5f)] private float jitter = 0.2f;

        [Header("Prefabs (optional)")]
        [SerializeField] private MapNodeView nodePrefab = null;
        [Tooltip("Stretched and rotated between two nodes")]
        [SerializeField] private Image linePrefab = null;
        [SerializeField] private Vector2 placeholderNodeSize = new Vector2(80f, 80f);
        [SerializeField] private float bossScale = 1.6f;
        [SerializeField, Min(1f)] private float lineThickness = 6f;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color travelledLineColor = new Color(1f, 0.85f, 0.4f, 0.9f);

        [Header("Node looks")]
        [SerializeField] private List<NodeLook> looks = new List<NodeLook>
        {
            new NodeLook { type = MapNodeType.Combat, color = new Color(0.85f, 0.85f, 0.85f), letter = "C" },
            new NodeLook { type = MapNodeType.Elite, color = new Color(1f, 0.45f, 0.35f), letter = "E" },
            new NodeLook { type = MapNodeType.Event, color = new Color(0.6f, 0.75f, 1f), letter = "?" },
            new NodeLook { type = MapNodeType.Shop, color = new Color(1f, 0.85f, 0.35f), letter = "$" },
            new NodeLook { type = MapNodeType.Rest, color = new Color(0.5f, 1f, 0.55f), letter = "R" },
            new NodeLook { type = MapNodeType.Boss, color = new Color(0.8f, 0.3f, 1f), letter = "B" }
        };

        [Header("HUD (optional)")]
        [SerializeField] private TMP_Text playerHealthLabel = null;
        [SerializeField] private Button abandonRunButton = null;

        private readonly Dictionary<int, MapNodeView> viewsById = new Dictionary<int, MapNodeView>();
        private readonly List<MapLine> lines = new List<MapLine>();
        private RectTransform linesRoot;
        private RectTransform nodesRoot;

        private void Awake()
        {
            if (abandonRunButton != null)
            {
                abandonRunButton.onClick.AddListener(OnAbandonRun);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RunMapChangedEvent>(OnMapChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RunMapChangedEvent>(OnMapChanged);
        }

        private void Start()
        {
            RunManager run = RunManager.Instance;

            // No run when the scene is played directly instead of from Bootstrap
            if (run == null || !run.IsRunActive || content == null)
            {
                return;
            }

            Build(run);
            Refresh(run);
            ScrollToAvailable(run);
        }

        private void OnMapChanged(RunMapChangedEvent changed)
        {
            if (RunManager.Instance != null && RunManager.Instance.IsRunActive && linesRoot != null)
            {
                Refresh(RunManager.Instance);
            }
        }

        private void Build(RunManager run)
        {
            RunMap map = run.Map;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, map.RowCount * cellSize.y + padding * 2f);

            linesRoot = CreateLayer("Lines");
            nodesRoot = CreateLayer("Nodes");

            foreach (MapNode node in map.Nodes)
            {
                viewsById[node.Id] = CreateNode(node);
            }

            foreach (MapNode node in map.Nodes)
            {
                foreach (int nextId in node.NextIds)
                {
                    CreateLine(node, map.Get(nextId));
                }
            }
        }

        private void Refresh(RunManager run)
        {
            foreach (MapNodeView view in viewsById.Values)
            {
                view.SetState(GetState(view.Node, run));
            }

            foreach (MapLine line in lines)
            {
                bool travelled = run.IsCompleted(line.From) && (run.IsCompleted(line.To) || run.ActiveNode == line.To);
                line.Image.color = travelled ? travelledLineColor : lineColor;
            }

            if (playerHealthLabel != null)
            {
                playerHealthLabel.text = string.Format(HealthFormat, run.Player.Health.Current, run.Player.Health.Max);
            }
        }

        private static MapNodeState GetState(MapNode node, RunManager run)
        {
            if (run.CurrentNode == node)
            {
                return MapNodeState.Current;
            }

            if (run.IsCompleted(node))
            {
                return MapNodeState.Completed;
            }

            return run.IsAvailable(node) ? MapNodeState.Available : MapNodeState.Locked;
        }

        private MapNodeView CreateNode(MapNode node)
        {
            MapNodeView view = nodePrefab != null ? Instantiate(nodePrefab, nodesRoot) : CreatePlaceholderNode();
            RectTransform rect = (RectTransform)view.transform;
            rect.anchorMin = new Vector2(Half, 0f);
            rect.anchorMax = new Vector2(Half, 0f);
            rect.anchoredPosition = GetPosition(node);
            view.name = node.ToString();

            if (node.Type == MapNodeType.Boss)
            {
                rect.localScale *= bossScale;
            }

            NodeLook look = looks.Find(candidate => candidate.type == node.Type) ?? new NodeLook { type = node.Type };
            view.Bind(node, look.icon, look.color, look.letter, OnNodeClicked);
            return view;
        }

        private void CreateLine(MapNode from, MapNode to)
        {
            Image line = linePrefab != null ? Instantiate(linePrefab, linesRoot) : CreatePlaceholderLine();
            line.raycastTarget = false;

            Vector2 start = GetPosition(from);
            Vector2 end = GetPosition(to);
            Vector2 delta = end - start;

            RectTransform rect = line.rectTransform;
            rect.anchorMin = new Vector2(Half, 0f);
            rect.anchorMax = new Vector2(Half, 0f);
            rect.pivot = new Vector2(Half, Half);
            rect.sizeDelta = new Vector2(delta.magnitude, lineThickness);
            rect.anchoredPosition = start + delta * Half;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            lines.Add(new MapLine(line, from, to));
        }

        // x relative to the content centre, y from its bottom; the boss sits in the middle
        private Vector2 GetPosition(MapNode node)
        {
            RunMap map = RunManager.Instance.Map;
            float centerColumn = (map.ColumnCount - 1) * Half;
            Vector2 offset = Vector2.zero;

            if (node.Type != MapNodeType.Boss)
            {
                Random nodeRandom = new Random(node.Id * JitterSeedMultiplier);
                offset = new Vector2((float)(nodeRandom.NextDouble() * 2d - 1d), (float)(nodeRandom.NextDouble() * 2d - 1d));
                offset = Vector2.Scale(offset * jitter, cellSize);
            }

            float column = node.Type == MapNodeType.Boss ? centerColumn : node.Column;
            return new Vector2((column - centerColumn) * cellSize.x, padding + node.Row * cellSize.y) + offset;
        }

        private void ScrollToAvailable(RunManager run)
        {
            if (scrollRect == null)
            {
                return;
            }

            int row = run.CurrentNode != null ? run.CurrentNode.Row : 0;
            scrollRect.verticalNormalizedPosition = run.Map.RowCount > 1 ? Mathf.Clamp01((float)row / (run.Map.RowCount - 1)) : 0f;
        }

        private void OnNodeClicked(MapNode node)
        {
            RunManager.Instance.ChooseNode(node);
        }

        private static void OnAbandonRun()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.EndRun();
            }
        }

        private RectTransform CreateLayer(string layerName)
        {
            RectTransform layer = new GameObject(layerName, typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(content, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            return layer;
        }

        private MapNodeView CreatePlaceholderNode()
        {
            GameObject node = new GameObject("Node", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapNodeView));
            node.transform.SetParent(nodesRoot, false);
            ((RectTransform)node.transform).sizeDelta = placeholderNodeSize;

            Image image = node.GetComponent<Image>();
            Button button = node.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(node.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.black;
            label.fontSize = placeholderNodeSize.y * Half;
            label.raycastTarget = false;

            MapNodeView view = node.GetComponent<MapNodeView>();
            view.Configure(button, image, label);
            return view;
        }

        private readonly struct MapLine
        {
            public Image Image { get; }
            public MapNode From { get; }
            public MapNode To { get; }

            public MapLine(Image image, MapNode from, MapNode to)
            {
                Image = image;
                From = from;
                To = to;
            }
        }

        private Image CreatePlaceholderLine()
        {
            GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(linesRoot, false);
            return line.GetComponent<Image>();
        }
    }
}
