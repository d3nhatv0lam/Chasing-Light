using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;

namespace ChasingLight
{
    public sealed class StageController : MonoBehaviour
    {
        [Header("Stage Configuration")] [SerializeField]
        private StageAsset currentStageAsset;

        public StageAsset CurrentStageAsset { get => currentStageAsset; set => currentStageAsset = value; }

        [SerializeField] private float stepDuration = 0.15f;

        [Header("Scene References")] [SerializeField]
        private GridView gridView;

        [SerializeField] private GameplayHUDView hudView;
        [SerializeField] private StageModalsView modalsView;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private StageCatalog stageCatalog;

        [Header("Audio")]
        [SerializeField] private StageAudioListener audioListener;

        [Header("Entity Prefabs")] [SerializeField]
        private EntityView playerPrefab;

        [SerializeField] private EntityView shadowPrefab;

        private StageState _stageState;
        private StageSimulation _stageSimulation;
        private readonly Dictionary<int, EntityView> _entityViews = new();

        private bool _isAnimating = false;
        private bool _isGameOver = false;
        private bool _isPaused = false;
        private float _stageTimer = 0f;

        private WaitForSeconds _stepWait;
        private WaitForSeconds _teleportWait;

        private void Awake()
        {
            _stepWait = new WaitForSeconds(stepDuration);
            _teleportWait = new WaitForSeconds(0.08f);
        }

        private void Start()
        {
            if (stageCamera == null)
                stageCamera = Camera.main;

            Debug.Assert(gridView != null, "[StageController] Chưa gán GridView trong Scene!");
            Debug.Assert(hudView != null, "[StageController] Chưa gán GameplayHUDView trong Scene!");
            Debug.Assert(modalsView != null, "[StageController] Chưa gán StageModalsView trong Scene!");
            Debug.Assert(stageCatalog != null, "[StageController] Chưa gán StageCatalog trong Scene!");

            // Màn được chọn từ scene DifficultySelect ưu tiên hơn asset gán sẵn trong scene.
            if (StageContext.SelectedStage != null)
            {
                currentStageAsset = StageContext.SelectedStage;
            }

            if (currentStageAsset != null)
            {
                InitializeStage(currentStageAsset);
            }
            else
            {
                Debug.LogWarning("[StageController] Chưa gán CurrentStageAsset!");
            }
        }

        private void OnEnable()
        {
            hudView.OnCommandInput += HandleCommand;
            hudView.OnPauseClicked += TogglePause;

            modalsView.OnResumeRequested += ResumeGame;
            modalsView.OnRestartRequested += RestartStage;
            modalsView.OnNextStageRequested += LoadNextStage;
            modalsView.OnExitMenuRequested += ExitToMenu;
            modalsView.OnHomeRequested += ReturnToHome;
        }

        private void OnDisable()
        {
            hudView.OnCommandInput -= HandleCommand;
            hudView.OnPauseClicked -= TogglePause;

            modalsView.OnResumeRequested -= ResumeGame;
            modalsView.OnRestartRequested -= RestartStage;
            modalsView.OnNextStageRequested -= LoadNextStage;
            modalsView.OnExitMenuRequested -= ExitToMenu;
            modalsView.OnHomeRequested -= ReturnToHome;
        }

        public void Update()
        {
            if (!_isGameOver && !_isPaused)
            {
                _stageTimer += Time.deltaTime;
                if (_stageState != null)
                    _stageState.ElapsedTimeSeconds = Mathf.FloorToInt(_stageTimer);
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Khi dang Pause: khoa phim R va di chuyen, chi bat phim ESC/P de Resume
            if (_isPaused)
            {
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
                    TogglePause();
                return;
            }

            // Khi Game Over: khong mo Pause
            if (_isGameOver)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                    RestartStage();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
            {
                TogglePause();
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartStage();
                return;
            }

            if (_isAnimating) return;

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                HandleCommand(EntityCommand.Up);
            }
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                HandleCommand(EntityCommand.Down);
            }
            else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                HandleCommand(EntityCommand.Left);
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                HandleCommand(EntityCommand.Right);
            }
        }

        public void InitializeStage(StageAsset stageAsset)
        {
            if (stageAsset == null) return;

            // Guard clause: Reset Time.timeScale de tranh dong bang scene
            Time.timeScale = 1f;
            _isPaused = false;
            _isGameOver = false;
            _isAnimating = false;
            _stageTimer = 0f;
            ClearEntities();

            modalsView.HideAll();
            hudView.SetInputInteractable(true);
            hudView.SetPauseInteractable(true);

            StageData stageData = stageAsset.ToData();
            _stageState = new StageState(stageData);
            _stageSimulation = new StageSimulation(_stageState);

            gridView.BuildMap(stageData);
            SpawnEntities(stageData);
            CenterCameraOnGrid(stageData.Grid);

            // Phát âm thanh nhân vật chính xuất hiện từ cổng lúc khởi tạo màn chơi
            audioListener?.PlayCharAppearanceSound();

            hudView.UpdateCommandTimeline(_stageState.History, stageData.ShadowSpawnDelaySteps);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void HandleCommand(EntityCommand command)
        {
            if (_isAnimating || _isGameOver || _stageSimulation == null) return;
            if (!_stageSimulation.CanExecuteStep(command))
            {
                Debug.Log($"<color=yellow>[StageController] Không thể di chuyển hướng {command} từ vị trí hiện tại.</color>");
                return;
            }

            var stepResult = _stageSimulation.ExecuteStep(command);
            StartCoroutine(PlayTurnCoroutine(stepResult));
        }

        private IEnumerator PlayTurnCoroutine(StepResult result)
        {
            _isAnimating = true;
            hudView?.SetInputInteractable(false);

            // Xuất hiện thực thể
            foreach (var spawn in result.Spawns)
            {
                if (_entityViews.TryGetValue(spawn.EntityId, out var entityView))
                {
                    entityView.SetGridPosition(spawn.Position);
                    entityView.gameObject.SetActive(true);
                }
            }
            if (result.Spawns.Count > 0)
                audioListener?.PlayCharAppearanceSound();

            foreach (var move in result.Movements)
            {
                if (_entityViews.TryGetValue(move.EntityId, out var entityView))
                {
                    if (!entityView.gameObject.activeSelf)
                        entityView.gameObject.SetActive(true);

                    entityView.MoveTo(move.To, stepDuration);
                }
            }
            if (result.Movements.Count > 0)
                audioListener?.PlayStepSound();
            yield return _stepWait;
            
            foreach (var fall in result.Falls)
            {
                if (_entityViews.TryGetValue(fall.EntityId, out var entityView))
                {
                    entityView.MoveTo(fall.To, stepDuration);
                }
            }
            if (result.Falls.Count > 0)
                yield return _stepWait;

            // Xác định các thực thể bị chết do kẹt vòng lặp cổng dịch chuyển
            var teleportOverloadDeadEntityIds = new HashSet<int>();
            foreach (var death in result.Deaths)
            {
                if (result.Teleports.Count(t => t.EntityId == death.EntityId) >= 2)
                {
                    teleportOverloadDeadEntityIds.Add(death.EntityId);
                }
            }

            // Các thực thể dịch chuyển bình thường (không bị kẹt vòng lặp)
            foreach (var teleport in result.Teleports)
            {
                if (teleportOverloadDeadEntityIds.Contains(teleport.EntityId))
                    continue;

                if (_entityViews.TryGetValue(teleport.EntityId, out var entityView))
                {
                    entityView.SetGridPosition(teleport.To);
                    audioListener?.PlayTeleportSound();
                    yield return _teleportWait;
                }
            }

            // animation cho các thực thể bị kẹt bẫy cổng
            foreach (var deadEntityId in teleportOverloadDeadEntityIds)
            {
                if (_entityViews.TryGetValue(deadEntityId, out var entityView))
                {
                    var entityTps = result.Teleports.Where(t => t.EntityId == deadEntityId).ToList();
                    var posA = entityTps[0].From;
                    var posB = entityTps[0].To;

                    yield return StartCoroutine(PlayTeleportOverloadCoroutine(entityView, posA, posB));
                }
            }


            // Cơ quan đổi trạng thái trước khi rơi (được phân loại từ Core Simulation)
            var preFallMechs = result.PreFallMechanismChanges;
            var postFallMechs = result.PostFallMechanismChanges;

            // Cơ quan đổi trạng thái trước khi rơi (do bước chân dẫm switch / teleport)
            if (preFallMechs.Count > 0)
            {
                ApplyMechanismChanges(preFallMechs);
                audioListener?.PlayMechanismChanges(preFallMechs);
                yield return _teleportWait;
            }
            
            // Cơ quan đổi trạng thái sau khi rơi (ví dụ rơi trúng Switch, Gate đổi trạng thái sau đó)
            if (postFallMechs.Count > 0)
            {
                ApplyMechanismChanges(postFallMechs);
                audioListener?.PlayMechanismChanges(postFallMechs);
                yield return _teleportWait;
            }
            
            foreach (var death in result.Deaths)
            {
                if (teleportOverloadDeadEntityIds.Contains(death.EntityId))
                    continue; // Đã chạy animation quá tải ở trên

                if (_entityViews.TryGetValue(death.EntityId, out var entityView))
                {
                    entityView.gameObject.SetActive(false);
                }
            }
            bool isCaught = result.EndEvents.Any(e => e is PlayerCaughtEvent);
            if (result.Deaths.Count > 0 || isCaught)
                audioListener?.PlayDieSound();

            // Pha 5: Cập nhật dải Timeline mũi tên & Trạng thái game
            hudView.UpdateCommandTimeline(_stageState.History, _stageState.Data.ShadowSpawnDelaySteps);

            if (result.Status == StageStatus.Won)
            {
                _isGameOver = true;
                hudView.SetPauseInteractable(false);
                gridView.PlayGoalWin();

                audioListener?.PlayGoalReachedSound();

                var nextStage = GetNextStage();
                yield return new WaitForSeconds(0.8f);

                modalsView.ShowVictory(
                    result.StarsEarned,
                    result.StepCount,
                    Mathf.FloorToInt(_stageTimer),
                    nextStage,
                    currentStageAsset != null ? currentStageAsset.Difficulty : Difficulty.Easy
                );
            }
            else if (result.Status == StageStatus.Lost)
            {
                _isGameOver = true;
                hudView.SetPauseInteractable(false);

                string reason = isCaught ? "Caught by the shadow!" : "Fell into the abyss!";
                yield return new WaitForSeconds(0.4f);

                modalsView.ShowDefeat(reason);
            }

            _isAnimating = false;
            hudView.SetInputInteractable(!_isGameOver);
        }

        private void ApplyMechanismChanges(IEnumerable<ISimulationEvent> mechEvents)
        {
            foreach (var mechEvent in mechEvents)
            {
                if (mechEvent is SwitchToggledEvent sw)
                {
                    gridView.UpdateSwitch(sw.SwitchId, sw.IsActivated);
                }
                else if (mechEvent is GateStateChangedEvent gate)
                {
                    gridView.UpdateGate(gate.GateId, gate.IsOpen);
                }
                else if (mechEvent is TeleportStateChangedEvent teleport)
                {
                    gridView.UpdateTeleport(teleport.TeleportId, teleport.IsEnabled);
                }
            }
        }

        private void RestartStage()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            StopAllCoroutines();
            _isAnimating = false;
            _isGameOver = false;

            if (currentStageAsset != null)
                InitializeStage(currentStageAsset);
        }

        public void TogglePause()
        {
            // Không bật pause khi game đã kết thúc
            if (_isGameOver) return;

            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;

            if (_isPaused)
                modalsView.ShowPause();
            else
                modalsView.HideAll();
        }

        private void ResumeGame()
        {
            if (_isPaused)
                TogglePause();
        }

        private void LoadNextStage()
        {
            Time.timeScale = 1f;
            _isPaused = false;

            var nextStage = GetNextStage();
            if (nextStage != null)
            {
                StageContext.SelectedStage = nextStage;
                StageContext.LastSelectedDifficulty = nextStage.Difficulty;
                currentStageAsset = nextStage;
                InitializeStage(nextStage);
            }
            else
            {
                ExitToMenu();
            }
        }

        private void ExitToMenu()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("DifficultySelect");
        }

        private void ReturnToHome()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private StageAsset GetNextStage()
        {
            if (stageCatalog == null || currentStageAsset == null) return null;

            var currentDiff = currentStageAsset.Difficulty;
            var currentStages = stageCatalog.GetStages(currentDiff).ToList();

            // Màn tiếp theo trong cùng độ khó
            int currentIndex = currentStages.IndexOf(currentStageAsset);
            if (currentIndex >= 0 && currentIndex + 1 < currentStages.Count)
                return currentStages[currentIndex + 1];

            // Tiến tới độ khó kế tiếp
            var difficulties = (Difficulty[])System.Enum.GetValues(typeof(Difficulty));
            int nextDiffIndex = System.Array.IndexOf(difficulties, currentDiff) + 1;

            while (nextDiffIndex < difficulties.Length)
            {
                var nextDiff = difficulties[nextDiffIndex];
                var nextStages = stageCatalog.GetStages(nextDiff).ToList();
                if (nextStages.Count > 0)
                    return nextStages[0];
                nextDiffIndex++;
            }

            return null; // Phá đảo toàn bộ game
        }

        private void CenterCameraOnGrid(GridData grid)
        {
            if (stageCamera == null || grid == null) return;

            // 1. Tìm vùng bao thực tế của các ô có nội dung trong màn chơi
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float bottomY = float.MaxValue;
            float topY = float.MinValue;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var cell = grid.GetCell(new Vector2Int(x, y));
                    if (cell.Type == CellType.Empty) continue;

                    if (x < minX) minX = x;
                    if (x + 1f > maxX) maxX = x + 1f;

                    float cellBottom = y;
                    // Các ô có cổng/nhân vật đứng trên bề mặt (y + 1) sẽ cao tới khoảng y + 2
                    float cellTop = y + 2f;

                    if (cellBottom < bottomY) bottomY = cellBottom;
                    if (cellTop > topY) topY = cellTop;
                }
            }

            // Fallback nếu màn trống
            if (minX > maxX)
            {
                minX = 0f;
                maxX = grid.Width;
                bottomY = 0f;
                topY = grid.Height;
            }

            // 2. Tính kích thước phóng to (Orthographic Size)
            // Chừa khoảng trống phía trên (topPadding) cho thanh Timeline HUD và 2 bên (paddingX)
            float topPadding = 1.2f;
            float paddingX = 0.8f;

            float contentHeight = (topY + topPadding) - bottomY;
            float contentWidth = (maxX - minX) + paddingX * 2f;

            float aspect = stageCamera.aspect;
            float sizeY = contentHeight * 0.5f;
            float sizeX = (contentWidth * 0.5f) / aspect;
            float orthoSize = Mathf.Max(sizeY, sizeX);

            stageCamera.orthographicSize = orthoSize;

            // 3. Căn giữa theo chiều ngang của các ô thực tế,
            //    và NEO MÉP DƯỚI CAMERA sát vào chân Pole (bottomY)
            float centerX = (minX + maxX) * 0.5f;
            float centerY = bottomY + orthoSize;

            stageCamera.transform.position = new Vector3(centerX, centerY, stageCamera.transform.position.z);
        }

        private void SpawnEntities(StageData stageData)
        {
            if (stageData?.Entities == null) return;

            foreach (var entity in stageData.Entities)
            {
                var prefabToSpawn = entity.Type == EntityType.Player
                    ? playerPrefab
                    : shadowPrefab;

                if (prefabToSpawn == null) continue;

                var entityObj = Instantiate(prefabToSpawn);
                entityObj.SetGridPosition(entity.Position);

                // Shadow khởi đầu chưa xuất hiện (chờ sau delay steps)
                if (entity.Type == EntityType.Shadow)
                    entityObj.gameObject.SetActive(false);

                _entityViews.Add(entity.Id, entityObj);
            }
        }

        
        private IEnumerator PlayTeleportOverloadCoroutine(EntityView entityView, Vector2Int posA, Vector2Int posB)
        {
            const int bounceCount = 8; // Giật qua lại 8 lần tạo cảm giác dồn dập
            for (int i = 0; i < bounceCount; i++)
            {
                var targetPos = (i % 2 == 0) ? posB : posA;
                entityView.SetGridPosition(targetPos);

                // Chớp màu đỏ cảnh báo quá tải cổng
                entityView.SetColor(i % 2 == 0 ? new Color(1f, 0.35f, 0.35f, 1f) : Color.white);

                // Tốc độ nhấp nháy nhanh dần đều (dồn dập từ 0.08s xuống 0.02s)
                float delay = Mathf.Lerp(0.08f, 0.02f, (float)i / bounceCount);
                yield return new WaitForSeconds(delay);
            }

            // Hiệu ứng co rút và tan biến vào tâm cổng
            Vector3 originalScale = entityView.transform.localScale;
            const float fadeDuration = 0.15f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                entityView.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                entityView.SetColor(new Color(1f, 0.35f, 0.35f, 1f - t));
                yield return null;
            }

            // Hoàn tất: Reset trạng thái và ẩn thực thể
            entityView.transform.localScale = originalScale;
            entityView.SetColor(Color.white);
            entityView.gameObject.SetActive(false);
        }
        private void ClearEntities()
        {
            foreach (var kvp in _entityViews)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }

            _entityViews.Clear();
        }
    }
}