using System;
using System.Collections.Generic;
using GameFramework.Event;
using GameFramework.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AiFriendlyFrame.Sample.CircuitPuzzle
{
    [Flags]
    internal enum CircuitDirection
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8,
    }

    internal enum CircuitGameState
    {
        Boot,
        Generating,
        Playing,
        Cleared,
        Paused,
    }

    internal enum CircuitSignal
    {
        BoardGenerated,
        NodeRotated,
        BoardCleared,
        LanguageChanged,
    }

    internal sealed class CircuitPuzzleEventArgs : GameEventArgs
    {
        internal static readonly int EventId = typeof(CircuitPuzzleEventArgs).GetHashCode();

        internal CircuitSignal Signal { get; private set; }
        internal int Seed { get; private set; }
        internal int Moves { get; private set; }

        public override int Id => EventId;

        internal static CircuitPuzzleEventArgs Create(CircuitSignal signal, int seed, int moves)
        {
            var args = GameFramework.ReferencePool.Acquire<CircuitPuzzleEventArgs>();
            args.Signal = signal;
            args.Seed = seed;
            args.Moves = moves;
            return args;
        }

        public override void Clear()
        {
            Signal = CircuitSignal.BoardGenerated;
            Seed = 0;
            Moves = 0;
        }
    }

    internal sealed class CircuitNode
    {
        internal CircuitDirection Connections;
        internal int Rotation;
        internal bool IsStart;
        internal bool IsEnd;
    }

    internal sealed class CircuitPuzzleBoard
    {
        private static readonly CircuitDirection[] Directions =
        {
            CircuitDirection.North,
            CircuitDirection.East,
            CircuitDirection.South,
            CircuitDirection.West,
        };

        private CircuitNode[,] _nodes;

        internal int Size { get; private set; }
        internal int Seed { get; private set; }
        internal int Moves { get; private set; }
        internal int CurrentEnergyCount { get; private set; }

        internal CircuitNode GetNode(int x, int y)
        {
            return _nodes[x, y];
        }

        internal void Generate(int seed, int size)
        {
            Seed = seed;
            Size = size;
            Moves = 0;
            _nodes = new CircuitNode[Size, Size];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    _nodes[x, y] = new CircuitNode();
                }
            }

            var random = new System.Random(Seed);
            int currentX = 0;
            int currentY = 0;
            _nodes[currentX, currentY].IsStart = true;
            while (currentX != Size - 1 || currentY != Size - 1)
            {
                bool goEast = currentX < Size - 1 && (currentY == Size - 1 || random.Next(0, 2) == 0);
                int nextX = goEast ? currentX + 1 : currentX;
                int nextY = goEast ? currentY : currentY + 1;
                CircuitDirection direction = goEast ? CircuitDirection.East : CircuitDirection.South;
                _nodes[currentX, currentY].Connections |= direction;
                _nodes[nextX, nextY].Connections |= Opposite(direction);
                currentX = nextX;
                currentY = nextY;
            }

            _nodes[Size - 1, Size - 1].IsEnd = true;
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    CircuitNode node = _nodes[x, y];
                    if (!node.IsStart && !node.IsEnd && node.Connections == CircuitDirection.None)
                    {
                        node.Connections = CreateDecorativeNode(random);
                    }

                    if (!node.IsStart && !node.IsEnd)
                    {
                        node.Rotation = random.Next(0, 4);
                    }
                }
            }

            while (IsConnected())
            {
                RotateFirstRotatableNode();
            }

            CurrentEnergyCount = CountEnergizedNodes();
        }

        internal bool TryRotate(int x, int y)
        {
            CircuitNode node = _nodes[x, y];
            if (node.IsStart || node.IsEnd)
            {
                return false;
            }

            node.Rotation = (node.Rotation + 1) % 4;
            Moves++;
            CurrentEnergyCount = CountEnergizedNodes();
            return true;
        }

        internal CircuitDirection GetRotatedConnections(int x, int y)
        {
            CircuitNode node = _nodes[x, y];
            CircuitDirection connections = node.Connections;
            for (int index = 0; index < node.Rotation; index++)
            {
                connections = RotateClockwise(connections);
            }

            return connections;
        }

        internal bool IsConnected()
        {
            return IsEnergized(Size - 1, Size - 1);
        }

        internal bool IsEnergized(int targetX, int targetY)
        {
            bool[,] visited = BuildEnergizedMap(out _);
            return visited[targetX, targetY];
        }

        private int CountEnergizedNodes()
        {
            BuildEnergizedMap(out int energizedCount);
            return energizedCount;
        }

        internal bool[,] BuildEnergizedMap(out int energizedCount)
        {
            var visited = new bool[Size, Size];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(Vector2Int.zero);
            visited[0, 0] = true;
            energizedCount = 0;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                energizedCount++;
                CircuitDirection connections = GetRotatedConnections(current.x, current.y);
                foreach (CircuitDirection direction in Directions)
                {
                    if ((connections & direction) == 0)
                    {
                        continue;
                    }

                    Vector2Int next = GetAdjacent(current, direction);
                    if (next.x < 0 || next.y < 0 || next.x >= Size || next.y >= Size || visited[next.x, next.y])
                    {
                        continue;
                    }

                    CircuitDirection nextConnections = GetRotatedConnections(next.x, next.y);
                    if ((nextConnections & Opposite(direction)) == 0)
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return visited;
        }

        private void RotateFirstRotatableNode()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    CircuitNode node = _nodes[x, y];
                    if (!node.IsStart && !node.IsEnd)
                    {
                        node.Rotation = (node.Rotation + 1) % 4;
                        return;
                    }
                }
            }
        }

        private static CircuitDirection CreateDecorativeNode(System.Random random)
        {
            switch (random.Next(0, 3))
            {
                case 0:
                    return CircuitDirection.North | CircuitDirection.South;
                case 1:
                    return CircuitDirection.North | CircuitDirection.East;
                default:
                    return CircuitDirection.North | CircuitDirection.East | CircuitDirection.West;
            }
        }

        private static CircuitDirection RotateClockwise(CircuitDirection value)
        {
            CircuitDirection result = CircuitDirection.None;
            if ((value & CircuitDirection.North) != 0) result |= CircuitDirection.East;
            if ((value & CircuitDirection.East) != 0) result |= CircuitDirection.South;
            if ((value & CircuitDirection.South) != 0) result |= CircuitDirection.West;
            if ((value & CircuitDirection.West) != 0) result |= CircuitDirection.North;
            return result;
        }

        private static CircuitDirection Opposite(CircuitDirection direction)
        {
            switch (direction)
            {
                case CircuitDirection.North: return CircuitDirection.South;
                case CircuitDirection.East: return CircuitDirection.West;
                case CircuitDirection.South: return CircuitDirection.North;
                case CircuitDirection.West: return CircuitDirection.East;
                default: return CircuitDirection.None;
            }
        }

        private static Vector2Int GetAdjacent(Vector2Int position, CircuitDirection direction)
        {
            switch (direction)
            {
                case CircuitDirection.North: return position + Vector2Int.up;
                case CircuitDirection.East: return position + Vector2Int.right;
                case CircuitDirection.South: return position + Vector2Int.down;
                case CircuitDirection.West: return position + Vector2Int.left;
                default: return position;
            }
        }
    }

    /// <summary>
    /// Standalone procedural sample. It remains playable without GF_X startup components and reports
    /// which optional framework services are available when opened inside a configured project context.
    /// </summary>
    public sealed class CircuitPuzzleBootstrap : MonoBehaviour
    {
        private const int BoardSize = 6;
        private const string BestMovesKey = "AI-Friendly-Project.Sample.CircuitPuzzle.BestMoves";
        private const string HighestLevelKey = "AI-Friendly-Project.Sample.CircuitPuzzle.HighestLevel";
        private const string DefaultLevelConfigKey = "Sample.CircuitPuzzle.DefaultLevel";
        private const string TitleTintConfigKey = "Sample.CircuitPuzzle.TitleTint";

        private readonly CircuitPuzzleBoard _board = new CircuitPuzzleBoard();
        private readonly List<Button> _nodeButtons = new List<Button>();
        private readonly List<Text> _nodeLabels = new List<Text>();
        private readonly List<PulseVisual> _activePulses = new List<PulseVisual>();
        private readonly Stack<PulseVisual> _pulsePool = new Stack<PulseVisual>();

        [SerializeField] private Font _font;
        private Text _headlineText;
        private Text _stateText;
        private Text _metricsText;
        private Text _frameworkText;
        private GameObject _frameworkPanel;
        private Button _resetButton;
        private Button _nextButton;
        private Button _pauseButton;
        private Button _languageButton;
        private Button _statusButton;
        private CircuitGameState _state;
        private int _level = 1;
        private int _lastElapsedSeconds = -1;
        private float _startedAt;
        private float _pausedAt;
        private bool _isChinese = true;
        private int _eventCount;
        private int _createdPulseCount;
        private int _recommendedMoves;
        private string _dataSourceStatus = "未接入框架数据，使用样例回退值";

        private void Awake()
        {
            if (_font == null)
            {
                Debug.LogError("[CircuitPuzzle] 未配置 SIMHEI.TTF 字体，无法创建界面。");
                enabled = false;
                return;
            }

            EnsureEventSystem();
            BuildInterface();
            _state = CircuitGameState.Boot;
        }

        private void Start()
        {
            int defaultLevel = 1;
            if (GFBuiltin.Config != null && GF.Config.HasConfig(DefaultLevelConfigKey))
            {
                int.TryParse(GF.Config.GetString(DefaultLevelConfigKey), out defaultLevel);
            }

            LoadLevel(Mathf.Max(defaultLevel, 1), GetInitialSeed());
        }

        private void Update()
        {
            if (_state == CircuitGameState.Playing)
            {
                int elapsedSeconds = Mathf.FloorToInt(Time.unscaledTime - _startedAt);
                if (elapsedSeconds != _lastElapsedSeconds)
                {
                    _lastElapsedSeconds = elapsedSeconds;
                    RefreshMetrics();
                }
            }

            for (int index = _activePulses.Count - 1; index >= 0; index--)
            {
                PulseVisual pulse = _activePulses[index];
                if (!pulse.Tick(Time.unscaledDeltaTime))
                {
                    pulse.Root.SetActive(false);
                    _pulsePool.Push(pulse);
                    _activePulses.RemoveAt(index);
                }
            }
        }

        private void OnDestroy()
        {
            _activePulses.Clear();
            _pulsePool.Clear();
        }

        private void GenerateBoard(int seed)
        {
            _state = CircuitGameState.Generating;
            _board.Generate(seed, BoardSize);
            _startedAt = Time.unscaledTime;
            _lastElapsedSeconds = -1;
            ClearNodeViews();
            CreateNodeViews();
            _state = CircuitGameState.Playing;
            RaiseSignal(CircuitSignal.BoardGenerated);
            RefreshAll();
        }

        private void LoadLevel(int requestedLevel, int fallbackSeed)
        {
            _level = requestedLevel;
            _recommendedMoves = 0;
            _dataSourceStatus = "未接入框架数据，使用样例回退值";
            int seed = fallbackSeed;

            if (GFBuiltin.DataTable != null)
            {
                CircuitLevelTable level = GF.DataTable.GetDataTable<CircuitLevelTable>()?.GetDataRow(requestedLevel);
                if (level != null)
                {
                    _level = level.Id;
                    seed = level.Seed;
                    _recommendedMoves = level.TargetMoves;
                    _dataSourceStatus = $"数据表：第 {level.Id} 关（棋盘 {level.BoardSize}×{level.BoardSize}）";
                }
            }

            GenerateBoard(seed);
        }

        private void RotateNode(int x, int y)
        {
            if (_state != CircuitGameState.Playing || !_board.TryRotate(x, y))
            {
                return;
            }

            SpawnPulse(_nodeButtons[GetViewIndex(x, y)].transform as RectTransform);
            TryPlaySound("Sample/CircuitPuzzle/Rotate.wav");
            RaiseSignal(CircuitSignal.NodeRotated);
            if (_board.IsConnected())
            {
                _state = CircuitGameState.Cleared;
                int bestMoves = PlayerPrefs.GetInt(BestMovesKey, int.MaxValue);
                if (_board.Moves < bestMoves)
                {
                    PlayerPrefs.SetInt(BestMovesKey, _board.Moves);
                }

                PlayerPrefs.SetInt(HighestLevelKey, Mathf.Max(_level, PlayerPrefs.GetInt(HighestLevelKey, 1)));
                PlayerPrefs.Save();
                if (GFBuiltin.Setting != null)
                {
                    GFBuiltin.Setting.SetInt(BestMovesKey, PlayerPrefs.GetInt(BestMovesKey));
                    GFBuiltin.Setting.SetInt(HighestLevelKey, PlayerPrefs.GetInt(HighestLevelKey));
                    GFBuiltin.Setting.Save();
                }
                TryPlaySound("Sample/CircuitPuzzle/Cleared.wav");
                RaiseSignal(CircuitSignal.BoardCleared);
            }

            RefreshAll();
        }

        private void NextLevel()
        {
            LoadLevel(_level + 1, unchecked(_board.Seed * 486187739 + 31));
        }

        private void ResetBoard()
        {
            GenerateBoard(_board.Seed);
        }

        private void ToggleLanguage()
        {
            _isChinese = !_isChinese;
            if (GFBuiltin.Localization != null)
            {
                GFBuiltin.Localization.Language = _isChinese ? Language.ChineseSimplified : Language.English;
            }

            RaiseSignal(CircuitSignal.LanguageChanged);
            RefreshAll();
        }

        private void TogglePause()
        {
            if (_state == CircuitGameState.Playing)
            {
                _state = CircuitGameState.Paused;
                _pausedAt = Time.unscaledTime;
            }
            else if (_state == CircuitGameState.Paused)
            {
                _state = CircuitGameState.Playing;
                _startedAt += Time.unscaledTime - _pausedAt;
            }

            RefreshAll();
        }

        private void ToggleFrameworkPanel()
        {
            _frameworkPanel.SetActive(!_frameworkPanel.activeSelf);
            RefreshFrameworkStatus();
        }

        private void RefreshAll()
        {
            bool[,] energized = _board.BuildEnergizedMap(out _);
            for (int x = 0; x < BoardSize; x++)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    int index = GetViewIndex(x, y);
                    CircuitNode node = _board.GetNode(x, y);
                    _nodeLabels[index].text = GetGlyph(_board.GetRotatedConnections(x, y), node);
                    ColorBlock colors = _nodeButtons[index].colors;
                    Color color = energized[x, y] ? new Color(0.15f, 0.9f, 0.75f, 1f) : new Color(0.22f, 0.29f, 0.43f, 1f);
                    colors.normalColor = color;
                    colors.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
                    _nodeButtons[index].colors = colors;
                }
            }

            _headlineText.text = GetLocalizedText("Sample.CircuitPuzzle.Title", "电路拼图");
            _stateText.text = GetStateText();
            SetButtonText(_resetButton, GetLocalizedText("Sample.CircuitPuzzle.Reset", "重置"));
            SetButtonText(_nextButton, GetLocalizedText("Sample.CircuitPuzzle.Next", "下一关"));
            SetButtonText(_pauseButton, _state == CircuitGameState.Paused
                ? GetLocalizedText("Sample.CircuitPuzzle.Resume", "继续")
                : GetLocalizedText("Sample.CircuitPuzzle.Pause", "暂停"));
            SetButtonText(_languageButton, "切换框架语言");
            SetButtonText(_statusButton, "框架状态");
            RefreshMetrics();
            RefreshFrameworkStatus();
        }

        private void RefreshMetrics()
        {
            int elapsedSeconds = Mathf.Max(_lastElapsedSeconds, 0);
            int bestMoves = PlayerPrefs.GetInt(BestMovesKey, 0);
            string targetMoves = _recommendedMoves > 0 ? $"   推荐 {_recommendedMoves}" : string.Empty;
            _metricsText.text = $"关卡 {_level}   种子 {_board.Seed}   步数 {_board.Moves}{targetMoves}   时间 {elapsedSeconds}s   最佳 {bestMoves}\n能量节点 {_board.CurrentEnergyCount}/{BoardSize * BoardSize}   脉冲 {_activePulses.Count}/{_createdPulseCount}\n{_dataSourceStatus}";
        }

        private void RefreshFrameworkStatus()
        {
            if (_frameworkText == null)
            {
                return;
            }

            string eventStatus = GFBuiltin.Event != null ? "GF 事件：已启用" : "GF 事件：独立运行回退";
            string settingStatus = GFBuiltin.Setting != null ? "GF 设置：已启用" : "设置：PlayerPrefs 回退";
            string localizationStatus = GFBuiltin.Localization != null ? $"GF 本地化：已启用（当前：{(_isChinese ? "简体中文" : "英语")}）" : "本地化：样例内置文案";
            string dataStatus = GFBuiltin.DataTable != null ? $"GF 数据表：{_dataSourceStatus}" : "数据表：样例种子配置回退";
            string configStatus = GFBuiltin.Config != null && GFBuiltin.Config.HasConfig(DefaultLevelConfigKey)
                ? $"GF 配置：默认关卡 {GFBuiltin.Config.GetString(DefaultLevelConfigKey)}"
                : "GF 配置：未加载，使用默认值";
            string resourceStatus = GFBuiltin.Resource == null
                ? "GF 资源：独立场景中不可用"
                : $"GF 资源：{GFBuiltin.Resource.HasAsset("Assets/Sample/CircuitPuzzle/Resources/Optional.asset")}";
            string soundStatus = GFBuiltin.Sound == null ? "声音扩展：独立场景中不可用" : "声音扩展：可安全调用可选资源";
            _frameworkText.text = "框架状态\n" +
                                  $"运行状态：{GetStateText()}\n{eventStatus}\n{settingStatus}\n{localizationStatus}\n{dataStatus}\n{configStatus}\n{resourceStatus}\n{soundStatus}\n" +
                                  $"样例事件计数：{_eventCount}\n" +
                                  "远程更新 / HybridCLR 发布：未配置";
        }

        private string GetStateText()
        {
            switch (_state)
            {
                case CircuitGameState.Cleared:
                    return "连通成功！点击下一关生成新的电路。";
                case CircuitGameState.Paused:
                    return "已暂停";
                default:
                    return GetLocalizedText("Sample.CircuitPuzzle.Instruction", "旋转节点，让能量从起点抵达终点。");
            }
        }

        private void RaiseSignal(CircuitSignal signal)
        {
            _eventCount++;
            if (GFBuiltin.Event != null)
            {
                GFBuiltin.Event.Fire(this, CircuitPuzzleEventArgs.Create(signal, _board.Seed, _board.Moves));
            }
        }

        private void TryPlaySound(string assetName)
        {
            if (GFBuiltin.Sound != null)
            {
                GFBuiltin.Sound.PlayEffect(assetName);
            }
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject("CircuitPuzzleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage(canvasObject.transform, "Background", new Color(0.04f, 0.07f, 0.13f, 1f));
            Stretch(background.rectTransform);

            _headlineText = CreateText(canvasObject.transform, "Headline", 42, TextAnchor.MiddleCenter, Color.white);
            SetRect(_headlineText.rectTransform, new Vector2(0f, 378f), new Vector2(760f, 72f));
            if (GFBuiltin.Config != null && GFBuiltin.Config.HasConfig(TitleTintConfigKey) &&
                ColorUtility.TryParseHtmlString(GFBuiltin.Config.GetString(TitleTintConfigKey), out Color titleTint))
            {
                _headlineText.color = titleTint;
            }
            _stateText = CreateText(canvasObject.transform, "State", 21, TextAnchor.MiddleCenter, new Color(0.64f, 0.75f, 0.92f, 1f));
            SetRect(_stateText.rectTransform, new Vector2(0f, 324f), new Vector2(860f, 42f));
            _metricsText = CreateText(canvasObject.transform, "Metrics", 16, TextAnchor.MiddleCenter, new Color(0.66f, 0.79f, 0.9f, 1f));
            SetRect(_metricsText.rectTransform, new Vector2(0f, -421f), new Vector2(980f, 46f));

            Image boardBackground = CreateImage(canvasObject.transform, "Board", new Color(0.08f, 0.13f, 0.23f, 0.98f));
            SetRect(boardBackground.rectTransform, new Vector2(0f, -15f), new Vector2(620f, 620f));
            GridLayoutGroup grid = boardBackground.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BoardSize;
            grid.cellSize = new Vector2(90f, 90f);
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(18, 18, 18, 18);

            GameObject pulseLayerObject = new GameObject("PulseLayer", typeof(RectTransform));
            pulseLayerObject.transform.SetParent(canvasObject.transform, false);
            _pulseRoot = pulseLayerObject.transform;
            SetRect(pulseLayerObject.GetComponent<RectTransform>(), new Vector2(0f, -15f), new Vector2(620f, 620f));

            _resetButton = CreateButton(canvasObject.transform, "ResetButton", new Vector2(-360f, -365f), new Vector2(136f, 46f), ResetBoard);
            _nextButton = CreateButton(canvasObject.transform, "NextButton", new Vector2(-180f, -365f), new Vector2(136f, 46f), NextLevel);
            _pauseButton = CreateButton(canvasObject.transform, "PauseButton", new Vector2(0f, -365f), new Vector2(136f, 46f), TogglePause);
            _languageButton = CreateButton(canvasObject.transform, "LanguageButton", new Vector2(180f, -365f), new Vector2(136f, 46f), ToggleLanguage);
            _statusButton = CreateButton(canvasObject.transform, "StatusButton", new Vector2(360f, -365f), new Vector2(136f, 46f), ToggleFrameworkPanel);

            _frameworkPanel = new GameObject("FrameworkStatusPanel", typeof(Image));
            _frameworkPanel.transform.SetParent(canvasObject.transform, false);
            Image frameworkBackground = _frameworkPanel.GetComponent<Image>();
            frameworkBackground.color = new Color(0.02f, 0.03f, 0.07f, 0.96f);
            RectTransform frameworkRect = frameworkBackground.rectTransform;
            SetRect(frameworkRect, new Vector2(440f, 68f), new Vector2(370f, 420f));
            _frameworkText = CreateText(_frameworkPanel.transform, "FrameworkText", 14, TextAnchor.UpperLeft, new Color(0.72f, 0.95f, 0.88f, 1f));
            Stretch(_frameworkText.rectTransform, 18f);
            _frameworkPanel.SetActive(false);

            _boardRoot = boardBackground.transform;
        }

        private Transform _boardRoot;
        private Transform _pulseRoot;

        private void CreateNodeViews()
        {
            for (int y = BoardSize - 1; y >= 0; y--)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    int capturedX = x;
                    int capturedY = y;
                    Button button = CreateNodeButton(_boardRoot, () => RotateNode(capturedX, capturedY), out Text label);
                    _nodeButtons.Add(button);
                    _nodeLabels.Add(label);
                }
            }
        }

        private void ClearNodeViews()
        {
            _nodeButtons.Clear();
            _nodeLabels.Clear();
            for (int index = _boardRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(_boardRoot.GetChild(index).gameObject);
            }
        }

        private void SpawnPulse(RectTransform target)
        {
            PulseVisual pulse;
            if (_pulsePool.Count > 0)
            {
                pulse = _pulsePool.Pop();
            }
            else
            {
                Image image = CreateImage(_pulseRoot, "EnergyPulse", new Color(0.2f, 1f, 0.82f, 0.65f));
                image.raycastTarget = false;
                pulse = new PulseVisual(image.gameObject, image.rectTransform, image);
                _createdPulseCount++;
            }

            pulse.Root.transform.SetParent(_pulseRoot, false);
            pulse.Rect.SetAsLastSibling();
            pulse.Rect.position = target.position;
            pulse.Begin();
            _activePulses.Add(pulse);
        }

        private Button CreateNodeButton(Transform parent, UnityEngine.Events.UnityAction onClick, out Text label)
        {
            GameObject nodeObject = new GameObject("CircuitNode", typeof(Image), typeof(Button));
            nodeObject.transform.SetParent(parent, false);
            Image image = nodeObject.GetComponent<Image>();
            image.color = new Color(0.22f, 0.29f, 0.43f, 1f);
            Button button = nodeObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            label = CreateText(nodeObject.transform, "Glyph", 55, TextAnchor.MiddleCenter, Color.white);
            Stretch(label.rectTransform);
            label.raycastTarget = false;
            return button;
        }

        private Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(name, typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.38f, 0.58f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            Text label = CreateText(buttonObject.transform, "Label", 16, TextAnchor.MiddleCenter, Color.white);
            label.text = name == "ResetButton" ? "重置" : name == "NextButton" ? "下一关" : name == "PauseButton" ? "暂停" : name == "LanguageButton" ? "切换框架语言" : "框架状态";
            Stretch(label.rectTransform);
            label.raycastTarget = false;
            SetRect(buttonObject.GetComponent<RectTransform>(), position, size);
            return button;
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = value;
            }
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        private static int GetInitialSeed()
        {
            int utcSeconds = (int)(DateTime.UtcNow - new DateTime(2020, 1, 1)).TotalSeconds;
            return Mathf.Abs(utcSeconds);
        }

        private static int GetViewIndex(int x, int y)
        {
            return x + (BoardSize - 1 - y) * BoardSize;
        }

        private static string GetLocalizedText(string key, string fallback)
        {
            if (GFBuiltin.Localization == null)
            {
                return fallback;
            }

            string value = GFBuiltin.Localization.GetString(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        private static string GetGlyph(CircuitDirection connections, CircuitNode node)
        {
            if (node.IsStart) return "◉";
            if (node.IsEnd) return "◎";
            switch (connections)
            {
                case CircuitDirection.North | CircuitDirection.South: return "│";
                case CircuitDirection.East | CircuitDirection.West: return "─";
                case CircuitDirection.North | CircuitDirection.East: return "└";
                case CircuitDirection.East | CircuitDirection.South: return "┌";
                case CircuitDirection.South | CircuitDirection.West: return "┐";
                case CircuitDirection.West | CircuitDirection.North: return "┘";
                case CircuitDirection.North | CircuitDirection.East | CircuitDirection.South: return "├";
                case CircuitDirection.East | CircuitDirection.South | CircuitDirection.West: return "┬";
                case CircuitDirection.South | CircuitDirection.West | CircuitDirection.North: return "┤";
                case CircuitDirection.West | CircuitDirection.North | CircuitDirection.East: return "┴";
                default: return "•";
            }
        }

        private sealed class PulseVisual
        {
            private float _remaining;

            internal PulseVisual(GameObject root, RectTransform rect, Image image)
            {
                Root = root;
                Rect = rect;
                Image = image;
            }

            internal GameObject Root { get; }
            internal RectTransform Rect { get; }
            internal Image Image { get; }

            internal void Begin()
            {
                _remaining = 0.42f;
                Root.SetActive(true);
                Rect.sizeDelta = new Vector2(20f, 20f);
                Image.color = new Color(0.2f, 1f, 0.82f, 0.72f);
            }

            internal bool Tick(float deltaTime)
            {
                _remaining -= deltaTime;
                float progress = 1f - Mathf.Clamp01(_remaining / 0.42f);
                Rect.sizeDelta = Vector2.one * Mathf.Lerp(20f, 88f, progress);
                Color color = Image.color;
                color.a = Mathf.Lerp(0.72f, 0f, progress);
                Image.color = color;
                return _remaining > 0f;
            }
        }
    }
}
