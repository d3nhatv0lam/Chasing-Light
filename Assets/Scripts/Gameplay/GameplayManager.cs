using System;
using System.Collections;
using System.Collections.Generic;
using ChasingLight.Core.Actions;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Điều phối lượt chơi giữa Core (StageState/GameRules) và các controller hiển thị của Unity.
    /// </summary>
    public sealed class GameplayManager : MonoBehaviour
    {
        [Header("Stage")]
        [SerializeField] private StageAsset _stageAsset;
        [SerializeField] private bool _initializeOnStart = true;

        [Header("Scene References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private ShadowController _shadow;
        [SerializeField] private GridManager _gridManager;

        private const int InvalidEntityId = -1;
        private readonly GameRules _rules = new GameRules();

        private StageState _stage;
        private int _playerId = InvalidEntityId;
        private int _shadowId = InvalidEntityId;
        private int _shadowReplayCount;
        private bool _isResolvingTurn;
        private float _elapsedSecondAccumulator;
        private bool _initialized;

        /// <summary>Trạng thái runtime hiện tại của màn chơi.</summary>
        public StageState CurrentStage => _stage;

        /// <summary>Kiểm tra Player có thể gửi một lệnh mới hay không.</summary>
        public bool CanAcceptInput =>
            _initialized &&
            _stage != null &&
            _stage.Status == StageStatus.Playing &&
            !_isResolvingTurn &&
            _stage.Entities.ContainsKey(_playerId) &&
            _stage.Entities[_playerId].Alive &&
            _stage.Entities[_playerId].Active;

        private void Start()
        {
            if (_initializeOnStart && !_initialized)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (_stage == null || _stage.Status != StageStatus.Playing)
            {
                return;
            }

            _elapsedSecondAccumulator += Time.deltaTime;
            while (_elapsedSecondAccumulator >= 1f)
            {
                _stage.ElapsedTimeSeconds++;
                _elapsedSecondAccumulator -= 1f;
            }
        }

        /// <summary>
        /// Khởi tạo màn từ StageAsset được gán trong Inspector.
        /// </summary>
        public void Initialize()
        {
            if (_stageAsset == null)
            {
                Debug.LogError("GameplayManager: Hãy gán StageAsset vào trường Stage Asset.", this);
                return;
            }

            if (_player == null || _shadow == null || _gridManager == null)
            {
                Debug.LogError("GameplayManager: Hãy gán Player, Shadow và Grid Manager trong Inspector.", this);
                return;
            }

            try
            {
                StageData stageData = StageDataFactory.Create(_stageAsset);
                _playerId = StageDataFactory.FindEntityId(stageData, EntityType.Player);
                _shadowId = StageDataFactory.FindEntityId(stageData, EntityType.Shadow);
                _stage = new StageState(stageData);
            }
            catch (Exception exception)
            {
                Debug.LogError("GameplayManager: Không thể khởi tạo màn. " + exception.Message, this);
                _stage = null;
                _initialized = false;
                return;
            }

            _gridManager.Initialize(_stage.Data.Grid);
            _shadowReplayCount = 0;
            _isResolvingTurn = false;
            _elapsedSecondAccumulator = 0f;

            EntityState playerState = _stage.Entities[_playerId];
            EntityState shadowState = _stage.Entities[_shadowId];

            _player.Initialize(playerState.Position);
            _shadow.Initialize(shadowState.Position);

            RefreshHoldSwitchStates();
            RecalculateMechanismStates();
            ResolveGravityForActiveEntities();

            // Đồng bộ lại hình ảnh sau khi áp dụng trạng thái ban đầu của cơ chế.
            _player.Initialize(playerState.Position);
            _shadow.Initialize(shadowState.Position);

            _initialized = true;
            Debug.Log("GameplayManager: Đã khởi tạo stage '" + _stage.Data.Id + "'.", this);
        }

        /// <summary>
        /// Dùng khi một bộ tải màn khác muốn truyền StageAsset trực tiếp.
        /// </summary>
        public void Initialize(StageAsset stageAsset)
        {
            _stageAsset = stageAsset;
            _initialized = false;
            Initialize();
        }

        /// <summary>
        /// Nhận lệnh Player. Chỉ Core mới quyết định lệnh có hợp lệ hay không.
        /// </summary>
        public void SubmitPlayerCommand(EntityCommand command)
        {
            if (!CanAcceptInput || command == EntityCommand.None)
            {
                return;
            }

            StartCoroutine(ProcessPlayerTurn(command));
        }

        private IEnumerator ProcessPlayerTurn(EntityCommand command)
        {
            _isResolvingTurn = true;

            Dictionary<int, Vector2Int> positionsBeforePlayerTurn = CaptureEntityPositions();
            EntityState player = _stage.Entities[_playerId];
            Vector2Int playerFrom = player.Position;

            // Core quy định History lưu mọi lệnh được nhấn, kể cả lệnh bị chặn.
            _stage.History.Add(command);
            _stage.StepCount++;

            Vector2Int attemptedPosition;
            bool accepted = ApplyCommand(_playerId, command, out attemptedPosition);

            if (accepted)
            {
                HandleSwitchEntry(_playerId, playerFrom, player.Position);
                RecalculateMechanismStates();
                ResolveGravityForActiveEntities();

                yield return _player.AnimateCommand(
                    playerFrom,
                    attemptedPosition,
                    player.Position,
                    command);
            }
            else
            {
                _player.SetIdle(command);
            }

            // Nếu một Gate vừa mở khiến Shadow đang đứng trên Gate rơi xuống,
            // hoạt họa thay đổi vị trí phụ đó sau khi Player hoàn thành bước.
            yield return AnimateSecondaryEntityIfMoved(
                _shadowId,
                positionsBeforePlayerTurn,
                true);

            if (FinishIfPlayerDead())
            {
                _isResolvingTurn = false;
                yield break;
            }

            if (CheckCaught())
            {
                FinishLost("Player chạm Shadow.");
                _isResolvingTurn = false;
                yield break;
            }

            if (_rules.IsWon(_stage, _playerId))
            {
                FinishWon();
                _isResolvingTurn = false;
                yield break;
            }

            // Theo quy ước gameplay: với delay n, Shadow xuất hiện khi Player
            // hoàn tất bước n+1 và replay lệnh đầu tiên ngay trong lượt đó.
            EntityState shadow = _stage.Entities[_shadowId];
            if (!shadow.Active && _stage.StepCount > _stage.Data.ShadowSpawnDelaySteps)
            {
                shadow.Active = true;
                _shadow.ShowAt(shadow.Position);
                RefreshHoldSwitchStates();
                RecalculateMechanismStates();

                if (CheckCaught())
                {
                    FinishLost("Player chạm Shadow ngay khi Shadow xuất hiện.");
                    _isResolvingTurn = false;
                    yield break;
                }
            }

            // Replay đúng n lệnh đầu tiên. Sau n lượt replay, Shadow đứng yên.
            if (shadow.Active && shadow.Alive &&
                _shadowReplayCount < _stage.Data.ShadowSpawnDelaySteps)
            {
                EntityCommand shadowCommand = _rules.GetShadowCommand(_stage);
                if (shadowCommand != EntityCommand.None)
                {
                    Dictionary<int, Vector2Int> positionsBeforeShadowTurn = CaptureEntityPositions();
                    Vector2Int shadowFrom = shadow.Position;
                    Vector2Int playerPositionBeforeShadowTurn = player.Position;

                    Vector2Int shadowAttempted;
                    bool shadowAccepted = ApplyCommand(_shadowId, shadowCommand, out shadowAttempted);

                    if (shadowAccepted)
                    {
                        HandleSwitchEntry(_shadowId, shadowFrom, shadow.Position);
                        RecalculateMechanismStates();
                        ResolveGravityForActiveEntities();

                        yield return _shadow.AnimateCommand(
                            shadowFrom,
                            shadowAttempted,
                            shadow.Position,
                            shadowCommand);
                    }
                    else
                    {
                        _shadow.SetIdle(shadowCommand);
                    }

                    // Cập nhật hình ảnh Player nếu trạng thái Gate làm Player rơi
                    // trong lúc Shadow hoàn thành lượt của nó.
                    yield return AnimatePlayerIfMovedByEnvironment(
                        playerPositionBeforeShadowTurn);

                    _shadowReplayCount++;

                    if (!shadow.Alive || !shadow.Active)
                    {
                        shadow.Active = false;
                        _shadow.Hide();
                    }

                    if (FinishIfPlayerDead())
                    {
                        _isResolvingTurn = false;
                        yield break;
                    }

                    bool crossedPaths =
                        positionsBeforeShadowTurn.ContainsKey(_playerId) &&
                        positionsBeforeShadowTurn[_playerId] == shadow.Position &&
                        player.Position == positionsBeforeShadowTurn[_shadowId];

                    if (CheckCaught() || crossedPaths)
                    {
                        FinishLost("Player va chạm Shadow trong lượt replay.");
                        _isResolvingTurn = false;
                        yield break;
                    }

                    if (_rules.IsWon(_stage, _playerId))
                    {
                        FinishWon();
                        _isResolvingTurn = false;
                        yield break;
                    }
                }
                else
                {
                    // Một slot replay được tiêu thụ kể cả khi lệnh lịch sử là None.
                    _shadowReplayCount++;
                }
            }

            _isResolvingTurn = false;
        }

        /// <summary>
        /// Áp dụng lệnh bằng GameRules lên StageState, không điều khiển Transform ở đây.
        /// </summary>
        private bool ApplyCommand(
            int entityId,
            EntityCommand command,
            out Vector2Int attemptedPosition)
        {
            EntityState entity;
            if (!_stage.Entities.TryGetValue(entityId, out entity) ||
                !entity.Alive || !entity.Active)
            {
                attemptedPosition = entity != null ? entity.Position : Vector2Int.zero;
                return false;
            }

            Vector2Int from = entity.Position;
            attemptedPosition = from + GameRules.ToDelta(command);

            if (!_rules.CanMove(_stage, entityId, command))
            {
                attemptedPosition = from;
                return false;
            }

            entity.Position = attemptedPosition;
            _rules.ResolveFall(_stage, entityId);

            if (!entity.Alive)
            {
                entity.Active = false;
                return true;
            }

            Vector2Int teleportDestination;
            if (_rules.TryTeleport(_stage, entity.Position, out teleportDestination))
            {
                entity.Position = teleportDestination;
                _rules.ResolveFall(_stage, entityId);

                if (!entity.Alive)
                {
                    entity.Active = false;
                }
            }

            return true;
        }

        private Dictionary<int, Vector2Int> CaptureEntityPositions()
        {
            Dictionary<int, Vector2Int> positions = new Dictionary<int, Vector2Int>();
            foreach (KeyValuePair<int, EntityState> pair in _stage.Entities)
            {
                positions[pair.Key] = pair.Value.Position;
            }
            return positions;
        }

        private IEnumerator AnimateSecondaryEntityIfMoved(
            int entityId,
            Dictionary<int, Vector2Int> beforePositions,
            bool shadowEntity)
        {
            if (entityId == _playerId || !beforePositions.ContainsKey(entityId))
            {
                yield break;
            }

            EntityState entity = _stage.Entities[entityId];
            if (!entity.Active && entity.Alive)
            {
                yield break;
            }

            Vector2Int from = beforePositions[entityId];
            if (from == entity.Position)
            {
                yield break;
            }

            if (shadowEntity)
            {
                if (entity.Alive)
                {
                    yield return _shadow.AnimateCommand(from, from, entity.Position, EntityCommand.Down);
                }
                else
                {
                    yield return _shadow.AnimateCommand(from, from, entity.Position, EntityCommand.Down);
                    _shadow.Hide();
                }
            }
        }

        private IEnumerator AnimatePlayerIfMovedByEnvironment(Vector2Int previousPosition)
        {
            EntityState player = _stage.Entities[_playerId];
            if (player.Position == previousPosition)
            {
                yield break;
            }

            yield return _player.AnimateCommand(
                previousPosition,
                previousPosition,
                player.Position,
                EntityCommand.Down);
        }

        private void HandleSwitchEntry(
            int entityId,
            Vector2Int previousPosition,
            Vector2Int finalPosition)
        {
            if (previousPosition == finalPosition)
            {
                return;
            }

            foreach (SwitchData switchData in _stage.Data.Switches)
            {
                if (switchData.Mode == SwitchMode.Latching &&
                    switchData.Position == finalPosition &&
                    _stage.SwitchStates.ContainsKey(switchData.Id))
                {
                    _stage.SwitchStates[switchData.Id] =
                        !_stage.SwitchStates[switchData.Id];
                }
            }

            RefreshHoldSwitchStates();
        }

        private void RefreshHoldSwitchStates()
        {
            foreach (SwitchData switchData in _stage.Data.Switches)
            {
                if (switchData.Mode != SwitchMode.Hold ||
                    !_stage.SwitchStates.ContainsKey(switchData.Id))
                {
                    continue;
                }

                bool occupied = false;
                foreach (KeyValuePair<int, EntityState> pair in _stage.Entities)
                {
                    EntityState entity = pair.Value;
                    if (entity.Alive && entity.Active && entity.Position == switchData.Position)
                    {
                        occupied = true;
                        break;
                    }
                }

                _stage.SwitchStates[switchData.Id] = occupied;
            }
        }

        private void RecalculateMechanismStates()
        {
            foreach (GateData gate in _stage.Data.Gates)
            {
                if (gate.Condition != null &&
                    gate.Condition.SwitchIds != null &&
                    gate.Condition.SwitchIds.Count > 0)
                {
                    _stage.GateStates[gate.Id] = EvaluateSwitchCondition(gate.Condition);
                }
            }

            foreach (TeleportData teleport in _stage.Data.Teleports)
            {
                if (teleport.Condition != null &&
                    teleport.Condition.SwitchIds != null &&
                    teleport.Condition.SwitchIds.Count > 0)
                {
                    _stage.TeleportStates[teleport.Id] = EvaluateSwitchCondition(teleport.Condition);
                }
            }
        }

        private bool EvaluateSwitchCondition(SwitchCondition condition)
        {
            if (condition == null || condition.SwitchIds == null || condition.SwitchIds.Count == 0)
            {
                return false;
            }

            bool allActive = true;
            bool anyActive = false;

            foreach (int switchId in condition.SwitchIds)
            {
                bool active;
                if (!_stage.SwitchStates.TryGetValue(switchId, out active))
                {
                    active = false;
                }

                allActive &= active;
                anyActive |= active;
            }

            return condition.Logic == LinkLogic.All ? allActive : anyActive;
        }

        private void ResolveGravityForActiveEntities()
        {
            foreach (KeyValuePair<int, EntityState> pair in _stage.Entities)
            {
                EntityState entity = pair.Value;
                if (!entity.Alive || !entity.Active)
                {
                    continue;
                }

                _rules.ResolveFall(_stage, pair.Key);
                if (!entity.Alive)
                {
                    entity.Active = false;
                }
            }
        }

        private bool FinishIfPlayerDead()
        {
            EntityState player = _stage.Entities[_playerId];
            if (player.Alive)
            {
                return false;
            }

            player.Active = false;
            FinishLost("Player rơi khỏi màn chơi.");
            return true;
        }

        private bool CheckCaught()
        {
            return _rules.IsCaught(_stage, _playerId);
        }

        private void FinishWon()
        {
            if (_stage == null || _stage.Status != StageStatus.Playing)
            {
                return;
            }

            _stage.Status = StageStatus.Won;
            Debug.Log("Stage completed in " + _stage.ElapsedTimeSeconds + " seconds.", this);

            // StageState là nguồn trạng thái duy nhất trong Core mới.
            // UI có thể đọc CurrentStage.Status để hiển thị màn hình thắng.
        }

        private void FinishLost(string reason)
        {
            if (_stage == null || _stage.Status != StageStatus.Playing)
            {
                return;
            }

            _stage.Status = StageStatus.Lost;
            Debug.Log(reason, this);

            EntityState shadow;
            if (_stage.Entities.TryGetValue(_shadowId, out shadow))
            {
                shadow.Active = false;
            }

            if (_shadow != null)
            {
                _shadow.Hide();
            }

            // StageState là nguồn trạng thái duy nhất trong Core mới.
            // UI có thể đọc CurrentStage.Status để hiển thị màn hình thua.
        }
    }
}
