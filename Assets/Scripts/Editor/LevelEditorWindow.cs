using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ChasingLight.Editor
{
    /// <summary>
    /// Visual Level Editor tool for Chasing Light.
    /// Allows painting grid tiles (Floor, Ladder, Start, Goal), placing mechanisms (Switch, Gate, Teleport),
    /// configuring star completion conditions, and validating stage rules.
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        public enum BrushMode
        {
            Empty,
            Floor,
            Ladder,
            Start,
            Goal,
            Switch,
            Gate,
            Teleport
        }

        private enum TabView
        {
            GridEditor,
            Mechanisms,
            Stars,
            Validation
        }

        [SerializeField] private StageAsset _targetAsset;

        // Stage Metadata
        private string _stageId = "";
        private int _stageNumber = 1;
        private Difficulty _difficulty = Difficulty.Easy;
        private int _width = 10;
        private int _height = 8;
        private int _shadowDelaySteps = 5;

        // Working Grid Data (Width x Height)
        private CellType[,] _grid = new CellType[10, 8];

        // Working Mechanisms Data
        private List<SwitchDefinition> _switches = new();
        private List<GateDefinition> _gates = new();
        private List<TeleportDefinition> _teleports = new();
        private List<StarCondition> _starConditions = new();
        private List<EntityDefinition> _entities = new();

        // UI State
        private BrushMode _currentBrush = BrushMode.Floor;
        private TabView _currentTab = TabView.GridEditor;
        private Vector2 _gridScrollPos;
        private Vector2 _mechanismsScrollPos;
        private float _cellSize = 36f;
        private Vector2Int? _pendingTeleportPosA;
        private string _statusMessage = "Ready to edit.";
        private MessageType _statusMessageType = MessageType.Info;

        [MenuItem("Tools/Chasing Light/Level Editor %#l")]
        [MenuItem("Window/Chasing Light/Level Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(900, 650);
            window.Show();
        }

        public static void OpenWithAsset(StageAsset asset)
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(900, 650);
            window.LoadFromAsset(asset);
            window.Show();
        }

        private void OnEnable()
        {
            if (_targetAsset != null && _grid != null && _grid.GetLength(0) == _width && _grid.GetLength(1) == _height)
                return;

            if (_targetAsset != null)
            {
                LoadFromAsset(_targetAsset);
            }
            else
            {
                InitializeNewStage(10, 8, Difficulty.Easy, 1);
            }
        }

        private void OnGUI()
        {
            DrawTopToolbar();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            // Left sidebar: Stage Metadata & Tool Palette
            DrawSidebar();

            EditorGUILayout.Space(6);

            // Right main area: Tabs (Grid / Mechanisms / Stars / Validation)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawTabsHeader();
            EditorGUILayout.Space(4);

            switch (_currentTab)
            {
                case TabView.GridEditor:
                    DrawGridEditorView();
                    break;
                case TabView.Mechanisms:
                    DrawMechanismsView();
                    break;
                case TabView.Stars:
                    DrawStarsView();
                    break;
                case TabView.Validation:
                    DrawValidationView();
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            DrawBottomStatusBar();
        }

        #region Top Toolbar

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            var newTarget = (StageAsset)EditorGUILayout.ObjectField(
                "Stage Asset:",
                _targetAsset,
                typeof(StageAsset),
                false,
                GUILayout.Width(280));
            if (EditorGUI.EndChangeCheck())
            {
                if (newTarget != null)
                {
                    LoadFromAsset(newTarget);
                }
            }

            if (GUILayout.Button("New Stage", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Create a new stage? Any unsaved changes will be lost.", "Yes", "Cancel"))
                {
                    InitializeNewStage(_width, _height, _difficulty, _stageNumber);
                }
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                SaveCurrentStage();
            }

            if (GUILayout.Button("Save As...", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                SaveAsNewAsset();
            }

            if (GUILayout.Button("▶ Playtest", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                SaveCurrentStage();
                if (_targetAsset != null)
                {
                    StageAssetCustomEditor.PlayStage(_targetAsset);
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("✖ Close", EditorStyles.toolbarButton, GUILayout.Width(65)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Sidebar: Settings & Tool Palette

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(260));

            EditorGUILayout.LabelField("⚙ Stage Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            _stageNumber = EditorGUILayout.IntField("Stage Number (#):", _stageNumber);
            _difficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty:", _difficulty);
            _shadowDelaySteps = EditorGUILayout.IntField("Shadow Delay (Steps):", _shadowDelaySteps);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("📐 Grid Dimensions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            int newWidth = EditorGUILayout.IntField("Width:", _width);
            int newHeight = EditorGUILayout.IntField("Height:", _height);
            EditorGUILayout.EndHorizontal();

            if (newWidth != _width || newHeight != _height)
            {
                if (newWidth >= 3 && newHeight >= 3 && newWidth <= 50 && newHeight <= 50)
                {
                    ResizeGrid(newWidth, newHeight);
                }
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("Clear Grid", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Clear Grid", "Are you sure you want to reset all cells to Empty?", "Clear", "Cancel"))
                    ClearAllCells();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🖌 Tools", EditorStyles.boldLabel);

            DrawBrushButton(BrushMode.Floor, "🟫 Floor [1]", new Color(0.6f, 0.45f, 0.35f));
            DrawBrushButton(BrushMode.Ladder, "🪜 Ladder [2]", new Color(0.85f, 0.55f, 0.2f));
            DrawBrushButton(BrushMode.Start, "🟢 Start [3]", new Color(0.2f, 0.75f, 0.3f));
            DrawBrushButton(BrushMode.Goal, "🚩 Goal [4]", new Color(0.95f, 0.75f, 0.15f));
            DrawBrushButton(BrushMode.Switch, "🔘 Switch [5]", new Color(0.65f, 0.3f, 0.8f));
            DrawBrushButton(BrushMode.Gate, "🚪 Gate [6]", new Color(0.85f, 0.25f, 0.25f));
            DrawBrushButton(BrushMode.Teleport, "🌀 Teleport [7]", new Color(0.2f, 0.7f, 0.8f));
            DrawBrushButton(BrushMode.Empty, "🧹 Eraser [E/0]", new Color(0.4f, 0.4f, 0.4f));

            if (_pendingTeleportPosA.HasValue)
            {
                EditorGUILayout.HelpBox($"Placing Teleport: Click a second cell for Portal B (Portal A at {_pendingTeleportPosA.Value})", MessageType.Warning);
                if (GUILayout.Button("Cancel Teleport", EditorStyles.miniButton))
                {
                    _pendingTeleportPosA = null;
                }
            }

            EditorGUILayout.Space(8);
            _cellSize = EditorGUILayout.Slider("Cell Size (Zoom):", _cellSize, 24f, 60f);

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("Tips:\n• Left-Click: Paint cell\n• Left-Drag: Continuous painting\n• Right-Click: Quick erase\n• Shortcuts: [1..7], [E/0]", MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawBrushButton(BrushMode mode, string label, Color color)
        {
            var oldColor = GUI.backgroundColor;
            if (_currentBrush == mode)
            {
                GUI.backgroundColor = color * 1.3f;
            }

            if (GUILayout.Button(label, GUILayout.Height(28)))
            {
                _currentBrush = mode;
                if (mode != BrushMode.Teleport)
                    _pendingTeleportPosA = null;
            }

            GUI.backgroundColor = oldColor;
        }

        #endregion

        #region Tabs Header

        private void DrawTabsHeader()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(_currentTab == TabView.GridEditor, "🎨 Grid Editor", EditorStyles.toolbarButton))
                _currentTab = TabView.GridEditor;

            if (GUILayout.Toggle(_currentTab == TabView.Mechanisms, "⚙ Mechanisms", EditorStyles.toolbarButton))
                _currentTab = TabView.Mechanisms;

            if (GUILayout.Toggle(_currentTab == TabView.Stars, "⭐ Stars", EditorStyles.toolbarButton))
                _currentTab = TabView.Stars;

            var isValid = CheckStageValidation(out var errors, out _);
            string valLabel = isValid ? "✅ Valid Stage" : $"❌ Issues ({errors.Count})";
            if (GUILayout.Toggle(_currentTab == TabView.Validation, valLabel, EditorStyles.toolbarButton))
                _currentTab = TabView.Validation;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 1: 2D Grid Visual Editor

        private void DrawGridEditorView()
        {
            _gridScrollPos = EditorGUILayout.BeginScrollView(_gridScrollPos);

            float totalWidth = _width * _cellSize + 60f;
            float totalHeight = _height * _cellSize + 50f;
            Rect canvasRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

            GUI.Box(canvasRect, GUIContent.none, EditorStyles.textArea);

            HandleGridKeyboardShortcuts();

            Event currentEvent = Event.current;
            Vector2 mousePos = currentEvent.mousePosition;

            float originX = canvasRect.x + 40f;
            float originY = canvasRect.y + 10f;

            // Coordinates: Y = 0 is bottom row, Y = Height - 1 is top row.
            for (int y = _height - 1; y >= 0; y--)
            {
                int visualRow = (_height - 1) - y;
                float cellY = originY + visualRow * _cellSize;

                // Y-coordinate label on the left
                Rect yLabelRect = new Rect(canvasRect.x + 5f, cellY + (_cellSize - 16f) * 0.5f, 30f, 16f);
                GUI.Label(yLabelRect, $"Y:{y}", EditorStyles.miniLabel);

                for (int x = 0; x < _width; x++)
                {
                    float cellX = originX + x * _cellSize;
                    Rect cellRect = new Rect(cellX, cellY, _cellSize - 2f, _cellSize - 2f);

                    var cellPos = new Vector2Int(x, y);
                    var cellType = _grid[x, y];

                    DrawCellVisual(cellRect, cellPos, cellType);

                    // Mouse interactions
                    bool isMouseOver = cellRect.Contains(mousePos);
                    if (isMouseOver)
                    {
                        // Hover highlight border
                        Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(1f, 1f, 1f, 0.4f));

                        // Left click or drag
                        if ((currentEvent.type == EventType.MouseDown && currentEvent.button == 0) ||
                            (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0))
                        {
                            ApplyBrushToCell(cellPos);
                            currentEvent.Use();
                            GUI.changed = true;
                        }
                        // Right click: Erase
                        else if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
                        {
                            EraseCell(cellPos);
                            currentEvent.Use();
                            GUI.changed = true;
                        }
                    }
                }
            }

            // X-coordinate labels at the bottom
            float xLabelsY = originY + _height * _cellSize + 4f;
            for (int x = 0; x < _width; x++)
            {
                float cellX = originX + x * _cellSize;
                Rect xLabelRect = new Rect(cellX, xLabelsY, _cellSize, 16f);
                GUI.Label(xLabelRect, $"{x}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCellVisual(Rect rect, Vector2Int pos, CellType type)
        {
            Color bgColor = GetCellColor(type);
            EditorGUI.DrawRect(rect, bgColor);

            string label = "";
            switch (type)
            {
                case CellType.Start:
                    label = "IN\n";
                    break;
                case CellType.Goal:
                    label = "OUT\n";
                    break;
                case CellType.Floor:
                    label = "F";
                    break;
                case CellType.Ladder:
                    label = "🪜";
                    break;
                case CellType.Switch:
                    var sw = _switches.FirstOrDefault(s => s.position == pos);
                    label = sw != null ? $"SW\n#{sw.id}" : "SW";
                    break;
                case CellType.Gate:
                    var gt = _gates.FirstOrDefault(g => g.position == pos);
                    label = gt != null ? $"GT\n#{gt.id}" : "GT";
                    break;
                case CellType.Teleport:
                    var tp = _teleports.FirstOrDefault(t => t.positionA == pos || t.positionB == pos);
                    if (tp != null)
                    {
                        string sub = tp.positionA == pos ? "A" : "B";
                        label = $"TP\n#{tp.id}{sub}";
                    }
                    else
                    {
                        label = "TP";
                    }
                    break;
                case CellType.Empty:
                default:
                    label = "·";
                    break;
            }

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = _cellSize > 34f ? 11 : 9,
                normal = { textColor = type == CellType.Empty ? new Color(1f, 1f, 1f, 0.2f) : Color.white }
            };

            GUI.Label(rect, label, style);
        }

        private Color GetCellColor(CellType type)
        {
            return type switch
            {
                CellType.Start => new Color(0.15f, 0.55f, 0.25f, 0.9f),
                CellType.Goal => new Color(0.75f, 0.55f, 0.1f, 0.9f),
                CellType.Floor => new Color(0.42f, 0.32f, 0.24f, 0.9f),
                CellType.Ladder => new Color(0.7f, 0.42f, 0.12f, 0.9f),
                CellType.Switch => new Color(0.5f, 0.2f, 0.65f, 0.9f),
                CellType.Gate => new Color(0.7f, 0.2f, 0.2f, 0.9f),
                CellType.Teleport => new Color(0.12f, 0.55f, 0.65f, 0.9f),
                _ => new Color(0.14f, 0.14f, 0.14f, 0.9f)
            };
        }

        private void HandleGridKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.Alpha1:
                    _currentBrush = BrushMode.Floor;
                    Repaint();
                    break;
                case KeyCode.Alpha2:
                    _currentBrush = BrushMode.Ladder;
                    Repaint();
                    break;
                case KeyCode.Alpha3:
                    _currentBrush = BrushMode.Start;
                    Repaint();
                    break;
                case KeyCode.Alpha4:
                    _currentBrush = BrushMode.Goal;
                    Repaint();
                    break;
                case KeyCode.Alpha5:
                    _currentBrush = BrushMode.Switch;
                    Repaint();
                    break;
                case KeyCode.Alpha6:
                    _currentBrush = BrushMode.Gate;
                    Repaint();
                    break;
                case KeyCode.Alpha7:
                    _currentBrush = BrushMode.Teleport;
                    Repaint();
                    break;
                case KeyCode.Alpha0:
                case KeyCode.E:
                    _currentBrush = BrushMode.Empty;
                    Repaint();
                    break;
            }
        }

        private void ApplyBrushToCell(Vector2Int pos)
        {
            switch (_currentBrush)
            {
                case BrushMode.Start:
                    // Remove any existing Start cell to guarantee exactly 1 Start
                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            if (_grid[x, y] == CellType.Start)
                                _grid[x, y] = CellType.Empty;
                        }
                    }
                    _grid[pos.x, pos.y] = CellType.Start;
                    SyncEntitiesWithStart(pos);
                    _statusMessage = $"Placed Start (IN) at ({pos.x}, {pos.y}).";
                    _statusMessageType = MessageType.Info;
                    break;

                case BrushMode.Goal:
                    // Remove any existing Goal cell to guarantee exactly 1 Goal
                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            if (_grid[x, y] == CellType.Goal)
                                _grid[x, y] = CellType.Empty;
                        }
                    }
                    _grid[pos.x, pos.y] = CellType.Goal;
                    _statusMessage = $"Placed Goal (OUT) at ({pos.x}, {pos.y}).";
                    _statusMessageType = MessageType.Info;
                    break;

                case BrushMode.Floor:
                    ClearMechanismAt(pos);
                    _grid[pos.x, pos.y] = CellType.Floor;
                    break;

                case BrushMode.Ladder:
                    ClearMechanismAt(pos);
                    _grid[pos.x, pos.y] = CellType.Ladder;
                    break;

                case BrushMode.Switch:
                    _grid[pos.x, pos.y] = CellType.Switch;
                    if (!_switches.Any(s => s.position == pos))
                    {
                        int nextId = _switches.Count > 0 ? _switches.Max(s => s.id) + 1 : 1;
                        _switches.Add(new SwitchDefinition(nextId, pos, SwitchMode.Hold, false));
                        _statusMessage = $"Created Switch #{nextId} at ({pos.x}, {pos.y}).";
                        _statusMessageType = MessageType.Info;
                    }
                    break;

                case BrushMode.Gate:
                    _grid[pos.x, pos.y] = CellType.Gate;
                    if (!_gates.Any(g => g.position == pos))
                    {
                        int nextId = _gates.Count > 0 ? _gates.Max(g => g.id) + 1 : 1;
                        _gates.Add(new GateDefinition(nextId, pos, false, new SwitchConditionDefinition(LinkLogic.All)));
                        _statusMessage = $"Created Gate #{nextId} at ({pos.x}, {pos.y}).";
                        _statusMessageType = MessageType.Info;
                    }
                    break;

                case BrushMode.Teleport:
                    if (!_pendingTeleportPosA.HasValue)
                    {
                        _pendingTeleportPosA = pos;
                        _grid[pos.x, pos.y] = CellType.Teleport;
                        _statusMessage = $"Selected Portal A at ({pos.x}, {pos.y}). Click a second cell to place Portal B.";
                        _statusMessageType = MessageType.Warning;
                    }
                    else
                    {
                        Vector2Int posA = _pendingTeleportPosA.Value;
                        if (posA == pos)
                        {
                            _statusMessage = "Portal B cannot be at the same position as Portal A!";
                            _statusMessageType = MessageType.Error;
                            return;
                        }

                        _grid[pos.x, pos.y] = CellType.Teleport;
                        int nextId = _teleports.Count > 0 ? _teleports.Max(t => t.id) + 1 : 1;
                        _teleports.Add(new TeleportDefinition(nextId, posA, pos, true, new SwitchConditionDefinition(LinkLogic.All)));
                        _pendingTeleportPosA = null;
                        _statusMessage = $"Created Teleport pair #{nextId} between ({posA.x}, {posA.y}) and ({pos.x}, {pos.y}).";
                        _statusMessageType = MessageType.Info;
                    }
                    break;

                case BrushMode.Empty:
                    EraseCell(pos);
                    break;
            }
        }

        private void EraseCell(Vector2Int pos)
        {
            ClearMechanismAt(pos);
            _grid[pos.x, pos.y] = CellType.Empty;
        }

        private void ClearMechanismAt(Vector2Int pos)
        {
            _switches.RemoveAll(s => s.position == pos);
            _gates.RemoveAll(g => g.position == pos);
            _teleports.RemoveAll(t => t.positionA == pos || t.positionB == pos);
        }

        #endregion

        #region Tab 2: Mechanisms Configuration

        private void DrawMechanismsView()
        {
            _mechanismsScrollPos = EditorGUILayout.BeginScrollView(_mechanismsScrollPos);

            // Switches section
            EditorGUILayout.LabelField($"🔘 Switches ({_switches.Count})", EditorStyles.boldLabel);
            if (_switches.Count == 0)
            {
                EditorGUILayout.HelpBox("No switches placed. Use the Switch brush [5] on the grid.", MessageType.None);
            }
            else
            {
                const float Gap = 25f;

                float idW = EditorStyles.boldLabel.CalcSize(new GUIContent("Switch #9")).x;
                float posW = EditorStyles.label.CalcSize(new GUIContent("Position: (9, 9)")).x;
                float modeW = EditorStyles.label.CalcSize(new GUIContent("Mode:")).x;
                float dropW = EditorStyles.popup.CalcSize(new GUIContent("Latching")).x;
                float initActiveW = EditorStyles.toggle.CalcSize(new GUIContent("")).x + EditorStyles.label.CalcSize(new GUIContent("Init Active")).x;
                float delW = EditorStyles.miniButton.CalcSize(new GUIContent("Delete")).x;

                for (int i = 0; i < _switches.Count; i++)
                {
                    var sw = _switches[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    var idLabel = new GUIContent($"Switch #{sw.id}");
                    var posLabel = new GUIContent($"Position: ({sw.position.x}, {sw.position.y})");
                    
                    EditorGUILayout.LabelField($"Switch #{sw.id}", EditorStyles.boldLabel, GUILayout.Width(idW));
                    GUILayout.Space(Gap);
                    EditorGUILayout.LabelField($"Position: ({sw.position.x}, {sw.position.y})", GUILayout.Width(posW));
                    GUILayout.Space(Gap);
                    EditorGUILayout.LabelField("Mode:", GUILayout.Width(modeW));
                    sw.mode = (SwitchMode)EditorGUILayout.EnumPopup(sw.mode, GUILayout.Width(dropW));
                    GUILayout.Space(Gap);
                    sw.initialActivated = EditorGUILayout.ToggleLeft("Init Active", sw.initialActivated, GUILayout.Width(initActiveW));
                    GUILayout.Space(Gap);

                    if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(delW)))
                    {
                        _grid[sw.position.x, sw.position.y] = CellType.Empty;
                        _switches.RemoveAt(i);
                        i--;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space(8);

            // Gates section
            EditorGUILayout.LabelField($"🚪 Gates / Bridges ({_gates.Count})", EditorStyles.boldLabel);
            if (_gates.Count == 0)
            {
                EditorGUILayout.HelpBox("No gates placed. Use the Gate brush [6] on the grid.", MessageType.None);
            }
            else
            {
                for (int i = 0; i < _gates.Count; i++)
                {
                    var gt = _gates[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Gate #{gt.id}", EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Position: ({gt.position.x}, {gt.position.y})", GUILayout.Width(110));
                    gt.initialOpened = EditorGUILayout.ToggleLeft("Init Opened", gt.initialOpened, GUILayout.Width(120));

                    if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(55)))
                    {
                        _grid[gt.position.x, gt.position.y] = CellType.Empty;
                        _gates.RemoveAt(i);
                        i--;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                    EditorGUILayout.EndHorizontal();

                    // Switch link conditions
                    gt.switchCondition ??= new SwitchConditionDefinition();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Open Logic:", GUILayout.Width(80));
                    gt.switchCondition.logic = (LinkLogic)EditorGUILayout.EnumPopup(gt.switchCondition.logic, GUILayout.Width(70));

                    EditorGUILayout.LabelField("Linked Switches:", GUILayout.Width(110));
                    if (_switches.Count == 0)
                    {
                        EditorGUILayout.LabelField("(No Switches available)", EditorStyles.miniLabel);
                    }
                    else
                    {
                        foreach (var sw in _switches)
                        {
                            bool isLinked = gt.switchCondition.switchIds.Contains(sw.id);
                            bool toggle = EditorGUILayout.ToggleLeft($"SW #{sw.id}", isLinked, GUILayout.Width(65));
                            if (toggle != isLinked)
                            {
                                if (toggle) gt.switchCondition.switchIds.Add(sw.id);
                                else gt.switchCondition.switchIds.Remove(sw.id);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space(8);

            // Teleports section
            EditorGUILayout.LabelField($"🌀 Teleport Portals ({_teleports.Count})", EditorStyles.boldLabel);
            if (_teleports.Count == 0)
            {
                EditorGUILayout.HelpBox("No teleports placed. Use the Teleport brush [7] and click 2 cells to place Portals A and B.", MessageType.None);
            }
            else
            {
                for (int i = 0; i < _teleports.Count; i++)
                {
                    var tp = _teleports[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Teleport #{tp.id}", EditorStyles.boldLabel, GUILayout.Width(90));
                    EditorGUILayout.LabelField($"A: ({tp.positionA.x},{tp.positionA.y}) ⇄ B: ({tp.positionB.x},{tp.positionB.y})", GUILayout.Width(180));
                    tp.initialEnabled = EditorGUILayout.ToggleLeft("Init Enabled", tp.initialEnabled, GUILayout.Width(120));

                    if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(55)))
                    {
                        _grid[tp.positionA.x, tp.positionA.y] = CellType.Empty;
                        _grid[tp.positionB.x, tp.positionB.y] = CellType.Empty;
                        _teleports.RemoveAt(i);
                        i--;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                    EditorGUILayout.EndHorizontal();

                    // Switch link conditions
                    tp.switchCondition ??= new SwitchConditionDefinition();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Trigger Logic:", GUILayout.Width(90));
                    tp.switchCondition.logic = (LinkLogic)EditorGUILayout.EnumPopup(tp.switchCondition.logic, GUILayout.Width(70));

                    EditorGUILayout.LabelField("Linked Switches:", GUILayout.Width(110));
                    if (_switches.Count == 0)
                    {
                        EditorGUILayout.LabelField("(No Switches available)", EditorStyles.miniLabel);
                    }
                    else
                    {
                        foreach (var sw in _switches)
                        {
                            bool isLinked = tp.switchCondition.switchIds.Contains(sw.id);
                            bool toggle = EditorGUILayout.ToggleLeft($"SW #{sw.id}", isLinked, GUILayout.Width(65));
                            if (toggle != isLinked)
                            {
                                if (toggle) tp.switchCondition.switchIds.Add(sw.id);
                                else tp.switchCondition.switchIds.Remove(sw.id);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Tab 3: Stars

        private void DrawStarsView()
        {
            EditorGUILayout.LabelField("Star Rating Conditions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Default Rules: 1 Star for stage completion, 2 Stars for under 7 minutes (420s), 3 Stars for under 3 minutes (180s).", MessageType.Info);

            if (GUILayout.Button("Apply Default Stars", GUILayout.Height(28)))
            {
                SetupDefaultStars();
            }

            EditorGUILayout.Space(6);
            EnsureThreeStarConditions();

            for (int i = 0; i < 3; i++)
            {
                var star = _starConditions[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Star {i + 1} Milestone", EditorStyles.boldLabel);

                star.hasTimeLimit = EditorGUILayout.Toggle("Has Time Limit:", star.hasTimeLimit);
                if (star.hasTimeLimit)
                {
                    star.maxTimeSeconds = EditorGUILayout.IntField("Max Time (Seconds):", star.maxTimeSeconds);
                    int minutes = star.maxTimeSeconds / 60;
                    int seconds = star.maxTimeSeconds % 60;
                    EditorGUILayout.LabelField($"(= {minutes}m {seconds}s)", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("Stage completion with no time limit.", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }
        }

        #endregion

        #region Tab 4: Validation & Compliance

        private void DrawValidationView()
        {
            EditorGUILayout.LabelField("Stage Validation Report", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            bool isValid = CheckStageValidation(out var errors, out var warnings);

            if (isValid)
            {
                EditorGUILayout.HelpBox("Stage is valid and ready to play!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"{errors.Count} error(s) must be resolved before using this stage:", MessageType.Error);
                foreach (var err in errors)
                {
                    EditorGUILayout.LabelField($"• {err}", EditorStyles.wordWrappedLabel);
                }
            }

            if (warnings.Count > 0)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox($"Warnings ({warnings.Count}):", MessageType.Warning);
                foreach (var warn in warnings)
                {
                    EditorGUILayout.LabelField($"• {warn}", EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Rules Checklist:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Start Point (IN): {(FindStartCell().HasValue ? "✅ Exactly 1 point" : "❌ Missing or duplicate")}");
            EditorGUILayout.LabelField($"Goal Point (OUT): {(FindGoalCell().HasValue ? "✅ Exactly 1 point" : "❌ Missing or duplicate")}");
            EditorGUILayout.LabelField($"3 Star Milestones: {(_starConditions.Count == 3 ? "✅ Configured" : "❌ Incomplete")}");
        }

        #endregion

        #region Bottom Status Bar

        private void DrawBottomStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            string assetInfo = _targetAsset != null ? _targetAsset.name : "(Unsaved Stage)";
            EditorGUILayout.LabelField($"Editing: {assetInfo} | {_difficulty} - Stage {_stageNumber} | Grid: {_width}x{_height}", GUILayout.Width(360));

            GUIContent statusContent = new GUIContent(_statusMessage);
            var icon = _statusMessageType switch
            {
                MessageType.Error => EditorGUIUtility.IconContent("console.erroricon.sml")?.image,
                MessageType.Warning => EditorGUIUtility.IconContent("console.warnicon.sml")?.image,
                _ => EditorGUIUtility.IconContent("console.infoicon.sml")?.image
            };
            if (icon != null) statusContent.image = icon;

            EditorGUILayout.LabelField(statusContent, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Stage Management & File Operations

        private void InitializeNewStage(int width, int height, Difficulty diff, int stageNum)
        {
            _targetAsset = null;
            _stageId = Guid.NewGuid().ToString("D");
            _stageNumber = stageNum;
            _difficulty = diff;
            _width = Mathf.Clamp(width, 3, 50);
            _height = Mathf.Clamp(height, 3, 50);
            _shadowDelaySteps = 5;

            _grid = new CellType[_width, _height];
            _switches = new List<SwitchDefinition>();
            _gates = new List<GateDefinition>();
            _teleports = new List<TeleportDefinition>();
            _entities = new List<EntityDefinition>();

            SetupDefaultStars();

            if (_width >= 2 && _height >= 2)
            {
                _grid[1, 1] = CellType.Start;
                _grid[_width - 2, 1] = CellType.Goal;
                SyncEntitiesWithStart(new Vector2Int(1, 1));
            }

            _statusMessage = "Initialized new stage.";
            _statusMessageType = MessageType.Info;
            Repaint();
        }

        public void LoadFromAsset(StageAsset asset)
        {
            if (asset == null) return;

            _targetAsset = asset;
            _stageId = string.IsNullOrEmpty(asset.Id) ? Guid.NewGuid().ToString("D") : asset.Id;
            _stageNumber = asset.StageNumber > 0 ? asset.StageNumber : 1;
            _difficulty = asset.Difficulty;
            _width = Mathf.Max(3, asset.Width);
            _height = Mathf.Max(3, asset.Height);
            _shadowDelaySteps = asset.ShadowSpawnDelaySteps > 0 ? asset.ShadowSpawnDelaySteps : 5;

            _grid = new CellType[_width, _height];

            // Populate grid from asset.Cells
            if (asset.Cells != null)
            {
                foreach (var c in asset.Cells)
                {
                    if (c.position.x >= 0 && c.position.x < _width && c.position.y >= 0 && c.position.y < _height)
                    {
                        _grid[c.position.x, c.position.y] = c.type;
                    }
                }
            }

            // Copy mechanisms
            _switches = asset.Switches != null
                ? asset.Switches.Select(s => new SwitchDefinition(s.id, s.position, s.mode, s.initialActivated)).ToList()
                : new List<SwitchDefinition>();

            _gates = asset.Gates != null
                ? asset.Gates.Select(g => new GateDefinition(
                    g.id,
                    g.position,
                    g.initialOpened,
                    new SwitchConditionDefinition(g.switchCondition != null ? g.switchCondition.logic : LinkLogic.All, g.switchCondition?.switchIds))).ToList()
                : new List<GateDefinition>();

            _teleports = asset.Teleports != null
                ? asset.Teleports.Select(t => new TeleportDefinition(
                    t.id,
                    t.positionA,
                    t.positionB,
                    t.initialEnabled,
                    new SwitchConditionDefinition(t.switchCondition != null ? t.switchCondition.logic : LinkLogic.All, t.switchCondition?.switchIds))).ToList()
                : new List<TeleportDefinition>();

            _starConditions = asset.StarConditions != null && asset.StarConditions.Count > 0
                ? asset.StarConditions.Select(st => new StarCondition(st.hasTimeLimit, st.maxTimeSeconds)).ToList()
                : new List<StarCondition>();

            if (_starConditions.Count == 0)
                SetupDefaultStars();

            _entities = asset.Entities != null
                ? asset.Entities.Select(e => new EntityDefinition(e.id, e.type, e.position)).ToList()
                : new List<EntityDefinition>();

            var startPos = FindStartCell();
            if (startPos.HasValue)
                SyncEntitiesWithStart(startPos.Value);

            _statusMessage = $"Loaded stage '{asset.name}' successfully.";
            _statusMessageType = MessageType.Info;
            Repaint();
        }

        private void SaveCurrentStage()
        {
            if (_targetAsset == null)
            {
                SaveAsNewAsset();
                return;
            }

            ApplyDataToAsset(_targetAsset);
            EditorUtility.SetDirty(_targetAsset);
            AssetDatabase.SaveAssets();

            _statusMessage = $"Saved '{_targetAsset.name}' at {DateTime.Now:HH:mm:ss}.";
            _statusMessageType = MessageType.Info;
            ShowNotification(new GUIContent($"Saved {_targetAsset.name}"));
        }

        private void SaveAsNewAsset()
        {
            string folderPath = $"Assets/Data/Levels/{_difficulty}";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string defaultFileName = $"Stage_{_stageNumber:D2}.asset";
            string fullPath = EditorUtility.SaveFilePanelInProject(
                "Save Stage As New Asset",
                defaultFileName,
                "asset",
                "Choose destination StageAsset path",
                folderPath);

            if (string.IsNullOrEmpty(fullPath)) return;

            var newAsset = CreateInstance<StageAsset>();
            ApplyDataToAsset(newAsset);

            AssetDatabase.CreateAsset(newAsset, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _targetAsset = newAsset;
            _statusMessage = $"Created and saved new asset: {newAsset.name}";
            _statusMessageType = MessageType.Info;
            ShowNotification(new GUIContent($"Saved {newAsset.name}"));
        }

        private void ApplyDataToAsset(StageAsset asset)
        {
            asset.Id = string.IsNullOrEmpty(_stageId) ? Guid.NewGuid().ToString("D") : _stageId;
            asset.StageNumber = _stageNumber;
            asset.Difficulty = _difficulty;
            asset.Width = _width;
            asset.Height = _height;
            asset.ShadowSpawnDelaySteps = _shadowDelaySteps;

            // Pack only non-empty cells
            var cellsList = new List<CellDefinition>();
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (_grid[x, y] != CellType.Empty)
                    {
                        cellsList.Add(new CellDefinition(new Vector2Int(x, y), _grid[x, y]));
                    }
                }
            }
            asset.Cells = cellsList;

            var startPos = FindStartCell();
            if (startPos.HasValue)
                SyncEntitiesWithStart(startPos.Value);

            asset.Entities = new List<EntityDefinition>(_entities);
            asset.Switches = new List<SwitchDefinition>(_switches);
            asset.Gates = new List<GateDefinition>(_gates);
            asset.Teleports = new List<TeleportDefinition>(_teleports);
            asset.StarConditions = new List<StarCondition>(_starConditions);

            asset.EnsureAllIds();
        }

        #endregion

        #region Helpers & Grid Operations

        private void ResizeGrid(int newWidth, int newHeight)
        {
            var newGrid = new CellType[newWidth, newHeight];
            int copyW = Math.Min(_width, newWidth);
            int copyH = Math.Min(_height, newHeight);

            for (int y = 0; y < copyH; y++)
            {
                for (int x = 0; x < copyW; x++)
                {
                    newGrid[x, y] = _grid[x, y];
                }
            }

            _width = newWidth;
            _height = newHeight;
            _grid = newGrid;

            // Remove out-of-bounds mechanisms
            _switches.RemoveAll(s => s.position.x >= _width || s.position.y >= _height);
            _gates.RemoveAll(g => g.position.x >= _width || g.position.y >= _height);
            _teleports.RemoveAll(t => t.positionA.x >= _width || t.positionA.y >= _height || t.positionB.x >= _width || t.positionB.y >= _height);

            _statusMessage = $"Resized grid to {_width}x{_height}.";
            Repaint();
        }

        private void ClearAllCells()
        {
            _grid = new CellType[_width, _height];
            _switches.Clear();
            _gates.Clear();
            _teleports.Clear();
            _pendingTeleportPosA = null;
            _statusMessage = "Cleared all cells to Empty.";
            Repaint();
        }

        private Vector2Int? FindStartCell()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (_grid[x, y] == CellType.Start)
                        return new Vector2Int(x, y);
                }
            }
            return null;
        }

        private Vector2Int? FindGoalCell()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (_grid[x, y] == CellType.Goal)
                        return new Vector2Int(x, y);
                }
            }
            return null;
        }

        private void SyncEntitiesWithStart(Vector2Int startPos)
        {
            _entities.Clear();
            _entities.Add(new EntityDefinition(1, EntityType.Player, startPos));
            _entities.Add(new EntityDefinition(2, EntityType.Shadow, startPos));
        }

        private void SetupDefaultStars()
        {
            _starConditions = new List<StarCondition>
            {
                new StarCondition(false, 0),
                new StarCondition(true, 420),
                new StarCondition(true, 180)
            };
        }

        private void EnsureThreeStarConditions()
        {
            while (_starConditions.Count < 3)
            {
                if (_starConditions.Count == 0) _starConditions.Add(new StarCondition(false, 0));
                else if (_starConditions.Count == 1) _starConditions.Add(new StarCondition(true, 420));
                else if (_starConditions.Count == 2) _starConditions.Add(new StarCondition(true, 180));
            }
        }

        private bool CheckStageValidation(out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            int startCount = 0;
            int goalCount = 0;

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (_grid[x, y] == CellType.Start) startCount++;
                    if (_grid[x, y] == CellType.Goal) goalCount++;
                }
            }

            if (startCount == 0)
                errors.Add("Missing Start point (IN).");
            else if (startCount > 1)
                errors.Add($"Found {startCount} Start points! Exactly 1 Start point is allowed.");

            if (goalCount == 0)
                errors.Add("Missing Goal point (OUT).");
            else if (goalCount > 1)
                errors.Add($"Found {goalCount} Goal points! Exactly 1 Goal point is allowed.");

            var switchIds = new HashSet<int>(_switches.Select(s => s.id));
            foreach (var gt in _gates)
            {
                if (gt.switchCondition != null)
                {
                    foreach (var sid in gt.switchCondition.switchIds)
                    {
                        if (!switchIds.Contains(sid))
                            errors.Add($"Gate #{gt.id} links to non-existent Switch #{sid}.");
                    }
                }
            }

            foreach (var tp in _teleports)
            {
                if (tp.switchCondition != null)
                {
                    foreach (var sid in tp.switchCondition.switchIds)
                    {
                        if (!switchIds.Contains(sid))
                            errors.Add($"Teleport #{tp.id} links to non-existent Switch #{sid}.");
                    }
                }
            }

            if (_shadowDelaySteps <= 0)
                warnings.Add("Shadow delay steps should be greater than 0.");

            return errors.Count == 0;
        }

        #endregion
    }
}
