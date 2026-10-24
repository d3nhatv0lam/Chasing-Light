using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Động cơ điều phối lượt chơi (Turn Engine).
    /// Nhận lệnh từ người chơi, vận hành theo Pipeline nhân quả, cập nhật StageState và sinh StepResult cho View.
    /// </summary>
    public sealed class StageSimulation
    {
        public StageState State { get; }
        public GameRules Rules { get; }
        public int PlayerId { get; }

        public StageSimulation(StageState state, GameRules rules = null, int? playerId = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Rules = rules ?? new GameRules();

            if (playerId.HasValue)
            {
                PlayerId = playerId.Value;
            }
            else
            {
                PlayerId = FindDefaultPlayerId(state);
            }
        }

        private static int FindDefaultPlayerId(StageState state)
        {
            foreach (var entity in state.Data.Entities)
            {
                if (entity.Type == EntityType.Player)
                    return entity.Id;
            }

            return 1;
        }

        public bool CanExecuteStep(EntityCommand command)
        {
            var playerId = State.Data.Entities.First(x => x.Type == EntityType.Player).Id;
            return Rules.CanMove(State, playerId,command);
        }

        /// <summary>
        /// Thực thi 1 lượt chơi với lệnh điều khiển từ Player.
        /// </summary>
        public StepResult ExecuteStep(EntityCommand playerCommand)
        {
            // 0. Kiểm tra trạng thái ván đấu
            if (State.Status != StageStatus.Playing)
            {
                return StepResult.Failed(
                    playerCommand,
                    "Màn chơi đã kết thúc, không nhận thêm lệnh.",
                    State.Status,
                    State.StepCount
                );
            }

            // Lưu lại vị trí cũ của tất cả Entity trước
            var prevPositions = new Dictionary<int, Vector2Int>();
            foreach (var kvp in State.Entities)
            {
                prevPositions[kvp.Key] = kvp.Value.Position;
            }

            var movements = new List<EntityMovedEvent>();
            var teleports = new List<EntityTeleportedEvent>();
            var mechanismChanges = new List<ISimulationEvent>();
            var falls = new List<EntityFellEvent>();
            var endEvents = new List<ISimulationEvent>();
            var allEvents = new List<ISimulationEvent>();

            State.History.Add(playerCommand);
            State.StepCount++;

            if (Rules.ShouldSpawnShadow(State))
            {
                foreach (var entityData in State.Data.Entities)
                {
                    if (entityData.Type == EntityType.Shadow &&
                        State.Entities.TryGetValue(entityData.Id, out var entityState))
                    {
                        entityState.Active = true;
                    }
                }
            }

            var shadowCommand = Rules.GetShadowCommand(State);
            ExecuteMovements(playerCommand, shadowCommand, prevPositions, movements, allEvents);

            var teleportedEntities = new HashSet<int>();
            ExecuteTeleports(teleports, teleportedEntities, allEvents);

            // Vòng lặp giải quyết tương tác Cơ chế & Trọng lực (Mechanisms <-> Gravity)
            // Di chuyển/Teleport -> kích hoạt Switch/Gate -> rơi tự do -> rơi chạm Switch -> kích hoạt tiếp...
            var currentPrevPositions = prevPositions;
            const int maxIterations = 10;
            int iteration = 0;

            while (iteration < maxIterations)
            {
                iteration++;

                ExecuteMechanisms(currentPrevPositions, mechanismChanges, allEvents);

                bool entitiesFell = ExecuteGravity(falls, allEvents, out var fallenPrevPositions);
                if (!entitiesFell)
                {
                    break;
                }

                foreach (var ent in State.Entities.Values)
                {
                    if (!fallenPrevPositions.ContainsKey(ent.Id))
                    {
                        fallenPrevPositions[ent.Id] = ent.Position;
                    }
                }

                currentPrevPositions = fallenPrevPositions;
            }

            int starsEarned = ExecuteCollisionsAndEndGame(prevPositions, teleportedEntities, endEvents, allEvents);

            return new StepResult(
                success: true,
                message: string.Empty,
                playerCommand: playerCommand,
                shadowCommand: shadowCommand,
                status: State.Status,
                stepCount: State.StepCount,
                starsEarned: starsEarned,
                movements: movements,
                teleports: teleports,
                mechanismChanges: mechanismChanges,
                falls: falls,
                endEvents: endEvents,
                allEvents: allEvents
            );
        }

        private void ExecuteMovements(
            EntityCommand playerCommand,
            EntityCommand shadowCommand,
            Dictionary<int, Vector2Int> prevPositions,
            List<EntityMovedEvent> movements,
            List<ISimulationEvent> allEvents)
        {
            // Di chuyển Player
            if (Rules.CanMove(State, PlayerId, playerCommand))
            {
                var target = Rules.GetTargetPosition(State, PlayerId, playerCommand);
                bool isLadder = Rules.IsLadderCell(State, prevPositions[PlayerId]);

                State.Entities[PlayerId].Position = target;

                var moveEvt = new EntityMovedEvent(PlayerId, prevPositions[PlayerId], target, isLadder);
                movements.Add(moveEvt);
                allEvents.Add(moveEvt);
            }

            // Di chuyển Shadow / Kẻ địch
            foreach (var entityData in State.Data.Entities)
            {
                if (entityData.Id == PlayerId) continue;

                if (!State.Entities.TryGetValue(entityData.Id, out var enemyState)
                    || !enemyState.Active
                    || !enemyState.Alive) continue;
                
                if (!Rules.CanMove(State, entityData.Id, shadowCommand)) continue;
                    
                var target = Rules.GetTargetPosition(State, entityData.Id, shadowCommand);
                bool isLadder = Rules.IsLadderCell(State, prevPositions[entityData.Id]);

                enemyState.Position = target;

                var moveEvt = new EntityMovedEvent(entityData.Id, prevPositions[entityData.Id], target,
                    isLadder);
                
                movements.Add(moveEvt);
                allEvents.Add(moveEvt);
            }
        }

        private void ExecuteTeleports(
            List<EntityTeleportedEvent> teleports,
            HashSet<int> teleportedEntities,
            List<ISimulationEvent> allEvents)
        {
            foreach (var entity in State.Entities.Select(kvp => kvp.Value).Where(entity => entity.Active && entity.Alive))
            {
                if (!Rules.TryTeleport(State, entity.Position, out var destination)) continue;
                
                var from = entity.Position;
                entity.Position = destination;

                teleportedEntities.Add(entity.Id);

                var tpEvt = new EntityTeleportedEvent(entity.Id, from, destination);
                teleports.Add(tpEvt);
                allEvents.Add(tpEvt);
            }
        }

        private void ExecuteMechanisms(
            Dictionary<int, Vector2Int> prevPositions,
            List<ISimulationEvent> mechanismChanges,
            List<ISimulationEvent> allEvents)
        {
          
            foreach (var sw in State.Data.Switches)
            {
                bool currentSwState = State.SwitchStates.TryGetValue(sw.Id, out var cur) && cur;
                bool newSwState = currentSwState;

                if (sw.Mode == SwitchMode.Hold)
                {
                    newSwState = Rules.ShouldSwitchBeActive(State, sw.Id);
                }
                else if (sw.Mode == SwitchMode.Latching)
                {
                    // Latching: Dẫm vào thì kích hoạt
                    // Bước ra ngoài hoặc dẫm lại vẫn giữ nguyên ON (chỉ bị tắt bởi cơ chế/sự kiện khác).
                    var steppedOnThisTurn = false;
                    foreach (var ent in State.Entities.Values.Where(ent => ent.Active && ent.Alive && ent.Position == sw.Position))
                    {
                        if (prevPositions.TryGetValue(ent.Id, out var prev) && prev != sw.Position)
                        {
                            steppedOnThisTurn = true;
                            break;
                        }
                    }

                    if (steppedOnThisTurn)
                    {
                        newSwState = true;
                    }
                }

                if (newSwState != currentSwState)
                {
                    State.SwitchStates[sw.Id] = newSwState;
                    var swEvent = new SwitchToggledEvent(sw.Id, newSwState);
                    mechanismChanges.Add(swEvent);
                    allEvents.Add(swEvent);
                }
            }

            // Cập nhật Gate dựa trên điều kiện Switch
            foreach (var gate in State.Data.Gates)
            {
                if (gate.Condition?.SwitchIds == null || gate.Condition.SwitchIds.Count == 0)
                    continue;
                
                bool isPowered = Rules.IsConditionMet(State, gate.Condition);
                // Nếu ban đầu Đóng (!InitialOpened) -> Mở (true).
                // Nếu ban đầu Mở (InitialOpened) -> Đóng (false).
                bool shouldOpen = isPowered ^ gate.InitialOpened;

                bool currentGateState = State.GateStates.TryGetValue(gate.Id, out var gateCur) && gateCur;
                if (shouldOpen != currentGateState)
                {
                    State.GateStates[gate.Id] = shouldOpen;
                    var gateEvent = new GateStateChangedEvent(gate.Id, shouldOpen);
                    mechanismChanges.Add(gateEvent);
                    allEvents.Add(gateEvent);
                }
            }

            // Cập nhật Teleport bật/tắt dựa trên điều kiện Switch
            foreach (var tp in State.Data.Teleports)
            {
                if (tp.Condition?.SwitchIds == null || tp.Condition.SwitchIds.Count == 0)
                    continue;

                bool isPowered = Rules.IsConditionMet(State, tp.Condition);
                bool shouldEnable = isPowered ^ tp.InitialEnabled;

                bool currentTpState = State.TeleportStates.TryGetValue(tp.Id, out var tCur) && tCur;
                if (shouldEnable != currentTpState)
                {
                    State.TeleportStates[tp.Id] = shouldEnable;
                    var tpEvt = new TeleportStateChangedEvent(tp.Id, shouldEnable);
                    mechanismChanges.Add(tpEvt);
                    allEvents.Add(tpEvt);
                }
            }
        }

        private bool ExecuteGravity(
            List<EntityFellEvent> falls,
            List<ISimulationEvent> allEvents,
            out Dictionary<int, Vector2Int> fallenPrevPositions)
        {
            fallenPrevPositions = new Dictionary<int, Vector2Int>();
            bool anyFell = false;

            foreach (var entity in State.Entities.Values.Where(e => e.Active && e.Alive))
            {
                var fallResult = Rules.CalculateFallDestination(State, entity.Position);
                if (fallResult.FinalPosition == entity.Position) continue;
                
                var from = entity.Position;
                entity.Position = fallResult.FinalPosition;

                if (fallResult.IsFatal)
                {
                    entity.Alive = false;
                }

                fallenPrevPositions[entity.Id] = from;
                anyFell = true;

                var fellEvent = new EntityFellEvent(entity.Id, from, fallResult.FinalPosition, fallResult.IsFatal);
                falls.Add(fellEvent);
                allEvents.Add(fellEvent);
            }

            return anyFell;
        }

        private int ExecuteCollisionsAndEndGame(
            Dictionary<int, Vector2Int> prevPositions,
            HashSet<int> teleportedEntities,
            List<ISimulationEvent> endEvents,
            List<ISimulationEvent> allEvents)
        {
            bool playerTeleported = teleportedEntities.Contains(PlayerId);
            bool isCaught = Rules.TryGetCatchingEnemy(
                State,
                PlayerId,
                prevPositions[PlayerId],
                prevPositions,
                playerTeleported,
                teleportedEntities,
                out var caughtByEnemyId
            );

            if (isCaught)
            {
                var caughtEvt = new PlayerCaughtEvent(PlayerId, caughtByEnemyId);
                endEvents.Add(caughtEvt);
                allEvents.Add(caughtEvt);
            }

            // thắng/thua
            int starsEarned = 0;
            if (Rules.IsLost(State, PlayerId, isCaught))
            {
                State.Status = StageStatus.Lost;
                var completedEvt = new StageCompletedEvent(StageStatus.Lost, 0, State.StepCount);
                endEvents.Add(completedEvt);
                allEvents.Add(completedEvt);
            }
            else if (Rules.IsWon(State, PlayerId))
            {
                State.Status = StageStatus.Won;
                starsEarned = Rules.CalculateStars(State, PlayerId);
                var completedEvt = new StageCompletedEvent(StageStatus.Won, starsEarned, State.StepCount);
                endEvents.Add(completedEvt);
                allEvents.Add(completedEvt);
            }

            return starsEarned;
        }

        /// <summary>
        /// Cập nhật thời gian trôi qua trong màn chơi (gọi từ timer hoặc Update của Unity).
        /// </summary>
        public void TickTime(int deltaSeconds)
        {
            if (State.Status == StageStatus.Playing)
            {
                State.ElapsedTimeSeconds += deltaSeconds;
            }
        }
    }
}