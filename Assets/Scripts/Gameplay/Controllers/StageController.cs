using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChasingLight
{
    public sealed class StageController : MonoBehaviour
    {
        [Header("Stage Configuration")] [SerializeField]
        private StageAsset currentStageAsset;

        [SerializeField] private float stepDuration = 0.15f;

        [Header("Scene References")] [SerializeField]
        private GridView gridView;

        [SerializeField] private GameplayHUDView hudView;
        [SerializeField] private Camera stageCamera;

        [Header("Entity Prefabs")] [SerializeField]
        private EntityView playerPrefab;

        [SerializeField] private EntityView shadowPrefab;

        private StageState _stageState;
        private StageSimulation _stageSimulation;
        private readonly Dictionary<int, EntityView> _entityViews = new();

        private bool _isAnimating = false;
        private bool _isGameOver = false;

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
            if (hudView != null)
            {
                hudView.OnCommandInput += HandleCommand;
            }
        }

        private void OnDisable()
        {
            if (hudView != null)
            {
                hudView.OnCommandInput -= HandleCommand;
            }
        }

        public void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartStage();
                return;
            }

            if (_isAnimating || _isGameOver) return;

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
            if (stageAsset is null) return;

            _isGameOver = false;
            _isAnimating = false;
            ClearEntities();

            StageData stageData = stageAsset.ToData();
            _stageState = new StageState(stageData);
            _stageSimulation = new StageSimulation(_stageState);

            gridView?.BuildMap(stageData);
            SpawnEntities(stageData);
            CenterCameraOnGrid(stageData.Grid);

            if (hudView is not null)
            {
                hudView.SetInputInteractable(true);
                hudView.UpdateCommandTimeline(_stageState.History, stageData.ShadowSpawnDelaySteps);
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void HandleCommand(EntityCommand command)
        {
            if (_isAnimating || _isGameOver || _stageSimulation == null) return;
            if (!_stageSimulation.CanExecuteStep(command)) return;

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

            foreach (var move in result.Movements)
            {
                if (_entityViews.TryGetValue(move.EntityId, out var entityView))
                {
                    if (!entityView.gameObject.activeSelf)
                        entityView.gameObject.SetActive(true);

                    entityView.MoveTo(move.To, stepDuration);
                }
            }

            yield return _stepWait;

            foreach (var teleport in result.Teleports)
            {
                if (_entityViews.TryGetValue(teleport.EntityId, out var entityView))
                {
                    entityView.SetGridPosition(teleport.To);
                    yield return _teleportWait;
                }
            }


            // Phân loại cơ quan đổi trạng thái trước khi rơi và sau khi rơi (ví dụ: rơi chạm switch)
            var preFallMechs = new List<ISimulationEvent>();
            var postFallMechs = new List<ISimulationEvent>();
            bool hasSeenFall = false;

            if (result.AllEvents != null)
            {
                foreach (var evt in result.AllEvents)
                {
                    if (evt is EntityFellEvent)
                    {
                        hasSeenFall = true;
                    }
                    else if (evt is SwitchToggledEvent or GateStateChangedEvent or TeleportStateChangedEvent)
                    {
                        if (hasSeenFall)
                            postFallMechs.Add(evt);
                        else
                            preFallMechs.Add(evt);
                    }
                }
            }
            else
            {
                preFallMechs.AddRange(result.MechanismChanges);
            }

            // Pha 3: Cơ quan đổi trạng thái trước khi rơi (do bước chân dẫm switch / teleport)
            if (gridView is not null && preFallMechs.Count > 0)
            {
                ApplyMechanismChanges(preFallMechs);
                yield return _teleportWait;
            }


            foreach (var fall in result.Falls)
            {
                if (_entityViews.TryGetValue(fall.EntityId, out var entityView))
                {
                    entityView.MoveTo(fall.To, stepDuration);
                }
            }

            if (result.Falls.Count > 0)
                yield return _stepWait;

            // Pha 4.5: Cơ quan đổi trạng thái sau khi rơi (ví dụ rơi trúng Switch, Gate đổi trạng thái sau đó)
            if (gridView is not null && postFallMechs.Count > 0)
            {
                ApplyMechanismChanges(postFallMechs);
                yield return _teleportWait;
            }

            // Pha 5: Cập nhật dải Timeline mũi tên & Trạng thái game
            if (hudView is not null)
                hudView.UpdateCommandTimeline(_stageState.History, _stageState.Data.ShadowSpawnDelaySteps);

            if (result.Status == StageStatus.Won)
            {
                _isGameOver = true;
                Debug.Log(
                    $"<color=green>[StageController] CHIẾN THẮNG! Số bước: {result.StepCount}, Sao: {result.StarsEarned}</color>");
            }
            else if (result.Status == StageStatus.Lost)
            {
                _isGameOver = true;
                Debug.Log("<color=red>[StageController] GAME OVER! Bấm R để chơi lại.</color>");
            }

            _isAnimating = false;
            hudView?.SetInputInteractable(!_isGameOver);
        }

        private void ApplyMechanismChanges(IEnumerable<ISimulationEvent> mechEvents)
        {
            if (gridView == null) return;

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
            StopAllCoroutines(); // Dừng toàn bộ hoạt họa đang chạy dở
            _isAnimating = false;
            _isGameOver = false;

            if (currentStageAsset != null)
                InitializeStage(currentStageAsset);
        }

        private void CenterCameraOnGrid(GridData grid)
        {
            if (stageCamera is null || grid == null) return;


            float paddingY = 0.8f;
            float paddingX = 0.8f;
   
            float centerX = (grid.Width - 1) * 0.5f;
            float centerY = (grid.Height - 1) * 0.5f + 0.7f;
            stageCamera.transform.position = new Vector3(centerX, centerY, stageCamera.transform.position.z);
            
            // Tự động scale camera ôm khít theo lưới mà không bị thu quá nhỏ
            float aspect = stageCamera.aspect;
            float sizeY = grid.Height * 0.5f + paddingY;
            float sizeX = (grid.Width * 0.5f + paddingX) / aspect;
            stageCamera.orthographicSize = Mathf.Max(sizeY, sizeX);
        }

        private void SpawnEntities(StageData stageData)
        {
            if (stageData?.Entities == null) return;

            foreach (var entity in stageData.Entities)
            {
                var prefabToSpawn = entity.Type == EntityType.Player
                    ? playerPrefab
                    : shadowPrefab;

                if (prefabToSpawn is null) continue;

                var entityObj = Instantiate(prefabToSpawn);
                entityObj.SetGridPosition(entity.Position);

                // Shadow khởi đầu chưa xuất hiện (chờ sau delay steps)
                if (entity.Type == EntityType.Shadow)
                    entityObj.gameObject.SetActive(false);

                _entityViews.Add(entity.Id, entityObj);
            }
        }

        private void ClearEntities()
        {
            foreach (var kvp in _entityViews)
            {
                if (kvp.Value is not null)
                    Destroy(kvp.Value.gameObject);
            }

            _entityViews.Clear();
        }
    }
}