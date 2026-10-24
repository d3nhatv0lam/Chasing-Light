using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Kết quả tính toán rơi tự do.
    /// </summary>
    public readonly struct FallResult
    {
        public readonly Vector2Int FinalPosition { get; }
        public readonly bool IsFatal { get; } // true nếu rơi khỏi sàn đấu (y < 0)

        public FallResult(Vector2Int finalPosition, bool isFatal)
        {
            FinalPosition = finalPosition;
            IsFatal = isFatal;
        }
    }

    /// <summary>
    /// chỉ trả lời câu hỏi
    /// Quy ước toạ độ: y tăng = đi lên, y < 0 = rơi khỏi màn chơi.
    /// </summary>
    public sealed class GameRules
    {
        #region Hàm Cơ bản

        public static Vector2Int ToDelta(EntityCommand command)
        {
            return command switch
            {
                EntityCommand.Left => Vector2Int.left,
                EntityCommand.Right => Vector2Int.right,
                EntityCommand.Up => Vector2Int.up,
                EntityCommand.Down => Vector2Int.down,
                _ => Vector2Int.zero,
            };
        }

        public bool CanStandOn(CellType cell)
        {
            return cell switch
            {
                CellType.Empty => false,
                _ => true,
            };
        }

        public bool IsLadderCell(StageState stage, Vector2Int position)
        {
            var grid = stage.Data.Grid;
            return grid.InBounds(position) && grid.GetCell(position).Type == CellType.Ladder;
        }

        public bool IsOpenGate(StageState stage, Vector2Int position)
        {
            foreach (var gate in stage.Data.Gates)
            {
                if (gate.Position == position && stage.GateStates.TryGetValue(gate.Id, out var isOpen) && isOpen)
                    return true;
            }

            return false;
        }

        public bool IsEnemy(EntityType type)
        {
            return type switch
            {
                EntityType.Player => false,
                _ => true,
            };
        }

        #endregion

        #region Hàm Tính Toán

        public bool CanStandAt(StageState stage, Vector2Int position)
        {
            if (!stage.Data.Grid.InBounds(position)) return false;
            
            var type = stage.Data.Grid.GetCell(position).Type;

            if (type == CellType.Gate)
                return IsOpenGate(stage, position);

            return CanStandOn(type);
        }

        /// <summary>
        /// Kiểm tra điều kiện Switch liên kết (All hoặc Any) có được thỏa mãn không.
        /// </summary>
        public bool IsConditionMet(StageState stage, SwitchCondition condition)
        {
            if (condition?.SwitchIds == null || condition.SwitchIds.Count == 0)
                return false;

            if (condition.Logic == LinkLogic.All)
            {
                return condition.SwitchIds.All(id => stage.SwitchStates.TryGetValue(id,out var switchState) && switchState);
            }
            else // any
            {
                return condition.SwitchIds.Any(id => stage.SwitchStates.TryGetValue(id,out var switchState) && switchState);
            }
        }

        /// <summary>
        /// Kiểm tra Switch có nên được kích hoạt dựa trên vị trí hiện tại của các thực thể không.
        /// </summary>
        public bool ShouldSwitchBeActive(StageState stage, int switchId)
        {
            var targetSwitch = stage.Data.Switches.FirstOrDefault(sw => sw.Id == switchId);

            if (targetSwitch == null) return false;

            // Chế độ Hold: chỉ bật khi có thực thể đang dẫm lên
            if (targetSwitch.Mode == SwitchMode.Hold)
            {
                return stage.Entities.Values
                    .Any(e => e.Active 
                              && e.Alive 
                              && e.Position == targetSwitch.Position);
            }

            // Với Latching, giữ nguyên trạng thái nếu không có can thiệp
            return stage.SwitchStates.TryGetValue(switchId, out var current) && current;
        }

        /// <summary>
        /// Tìm điểm đến của thang khi leo Up/Down (di chuyển 1 mạch từ đầu đến cuối thang).
        /// </summary>
        public bool TryGetLadderDestination(StageState stage, Vector2Int from, EntityCommand command,
            out Vector2Int destination)
        {
            destination = from;
            if (command is not (EntityCommand.Up or EntityCommand.Down))
                return false;

            var grid = stage.Data.Grid;
            var stepY = command == EntityCommand.Up ? 1 : -1;
            var current = from;

            var next = current + new Vector2Int(0, stepY);
            var fromIsLadder = IsLadderCell(stage, current);
            var nextIsLadder = IsLadderCell(stage, next);

            if (!fromIsLadder && !nextIsLadder)
                return false;

            if (nextIsLadder)
            {
                current = next;
            }

            while (IsLadderCell(stage, current + new Vector2Int(0, stepY)))
            {
                current += new Vector2Int(0, stepY);
            }

            // Kiểm tra sàn đón ở đầu ra của thang nếu có
            if (command == EntityCommand.Up)
            {
                var above = current + Vector2Int.up;
                if (grid.InBounds(above) && CanStandAt(stage, above))
                {
                    destination = above;
                    return true;
                }
            }
            else if (command == EntityCommand.Down)
            {
                var below = current + Vector2Int.down;
                if (grid.InBounds(below) && CanStandAt(stage, below))
                {
                    destination = below;
                    return true;
                }
            }

            destination = current;
            return destination != from;
        }

        /// <summary>
        /// Tính toán tọa độ đích của một lệnh di chuyển (tính cả thang nếu có).
        /// </summary>
        public Vector2Int GetTargetPosition(StageState stage, int entityId, EntityCommand command)
        {
            if (!stage.Entities.TryGetValue(entityId, out var entity))
                return Vector2Int.zero;

            var from = entity.Position;
            if (command is EntityCommand.Up or EntityCommand.Down)
            {
                if (TryGetLadderDestination(stage, from, command, out var ladderDest))
                    return ladderDest;
                return from;
            }

            return from + ToDelta(command);
        }

        /// <summary>
        /// Tính toán điểm tiếp đất khi rơi tự do.
        /// </summary>
        public FallResult CalculateFallDestination(StageState stage, Vector2Int startPosition)
        {
            var current = startPosition;
            while (!CanStandAt(stage, current))
            {
                current += Vector2Int.down;
                if (current.y < 0)
                {
                    return new FallResult(current, isFatal: true);
                }
            }

            return new FallResult(current, isFatal: false);
        }

        public bool TryTeleport(StageState stage, Vector2Int position, out Vector2Int destination)
        {
            foreach (var tp in stage.Data.Teleports)
            {
                if (!stage.TeleportStates.TryGetValue(tp.Id, out var enabled) || !enabled)
                    continue;

                if (tp.PositionA == position)
                {
                    destination = tp.PositionB;
                    return true;
                }

                if (tp.PositionB == position)
                {
                    destination = tp.PositionA;
                    return true;
                }
            }

            destination = default;
            return false;
        }

        /// <summary>
        /// Kiểm tra sự giao cắt đối đầu của 2 đoạn thẳng có hướng trên không gian 1 chiều (1D Directed Segments).
        /// Trả về true nếu hai đoạn có phần giao nhau VÀ chuyển động ngược hướng (hoặc có điểm đứng yên).
        /// </summary>
        private static bool Has1DDirectedOverlap(int startA, int endA, int startB, int endB)
        {
            int minA = Mathf.Min(startA, endA);
            int maxA = Mathf.Max(startA, endA);
            int minB = Mathf.Min(startB, endB);
            int maxB = Mathf.Max(startB, endB);

            bool hasIntervalOverlap = Mathf.Max(minA, minB) <= Mathf.Min(maxA, maxB);
            int vectorA = endA - startA;
            int vectorB = endB - startB;
            bool isOppositeOrStationary = (vectorA * vectorB) <= 0;

            return hasIntervalOverlap && isOppositeOrStationary;
        }

        /// <summary>
        /// Kiểm tra xem quỹ đạo di chuyển của Player và Enemy có giao cắt/đâm vào nhau không.
        /// Bao gồm: cùng ô cuối lượt, đi bộ đâm nhau, leo thang đối đầu, và rơi xuyên qua người nhau.
        /// </summary>
        public bool IsTrajectoryIntersecting(
            Vector2Int pFrom, Vector2Int pTo,
            Vector2Int eFrom, Vector2Int eTo,
            bool isTeleport = false)
        {
            if (isTeleport) return false;

            // 1. Chạm cùng một ô
            if (pTo == eTo) return true;

            // 2. Va chạm trên cùng một cột X (Leo thang đối đầu hoặc Rơi xuyên qua nhau)
            if (pFrom.x == pTo.x && eFrom.x == eTo.x && pTo.x == eTo.x)
                return Has1DDirectedOverlap(pFrom.y, pTo.y, eFrom.y, eTo.y);

            // 3. Va chạm trên cùng một hàng Y (Đi bộ bước ngang đâm vào nhau)
            if (pFrom.y == pTo.y && eFrom.y == eTo.y && pTo.y == eTo.y)
                return Has1DDirectedOverlap(pFrom.x, pTo.x, eFrom.x, eTo.x);

            return false;
        }

        public bool ShouldSpawnShadow(StageState stage)
        {
            return stage.StepCount >= stage.Data.ShadowSpawnDelaySteps;
        }

        public EntityCommand GetShadowCommand(StageState stage)
        {
            if (stage.StepCount <= stage.Data.ShadowSpawnDelaySteps)
                return EntityCommand.None;
            return stage.History[stage.StepCount - stage.Data.ShadowSpawnDelaySteps - 1];
        }

        #endregion

        #region Public APIs

        public bool CanMove(StageState stage, int id, EntityCommand command)
        {
            if (command == EntityCommand.None) return false;
            if (!stage.Entities.TryGetValue(id, out var entity)) return false;

            var grid = stage.Data.Grid;
            var from = entity.Position;

            // Đi thang (Up / Down)
            if (command is EntityCommand.Up or EntityCommand.Down)
            {
                return TryGetLadderDestination(stage, from, command, out var ladderDest)
                       && grid.InBounds(ladderDest)
                       && CanStandAt(stage, ladderDest);
            }

            // Đi ngang (Left / Right)
            var to = from + ToDelta(command);
            if (!grid.InBounds(to)) return false;

            // Chỉ được bước ngang nếu chân đang đứng vững trên nền hoặc đang bám thang
            bool hasFooting = CanStandAt(stage, from) || IsLadderCell(stage, from);
            return hasFooting;
        }

        public bool IsWon(StageState stage, int playerId)
        {
            if (!stage.Entities.TryGetValue(playerId, out var player)) return false;
            return player.Alive && stage.Data.Grid.GetCell(player.Position).Type == CellType.Goal;
        }

        /// <summary>
        /// Kiểm tra xem Player có bị bất kỳ Enemy/Shadow nào bắt theo quỹ đạo không (quét 1 lượt duy nhất).
        /// Nếu bị bắt, trả về true và gán caughtByEnemyId; nếu không bị bắt trả về false.
        /// </summary>
        public bool TryGetCatchingEnemy(
            StageState stage, int playerId,
            Vector2Int playerPrevPos,
            IReadOnlyDictionary<int, Vector2Int> enemyPrevPositions,
            bool playerTeleported,
            ISet<int> teleportedEnemies,
            out int caughtByEnemyId)
        {
            caughtByEnemyId = -1;
            if (!stage.Entities.TryGetValue(playerId, out var player)) return false;

            foreach (var entity in stage.Data.Entities)
            {
                if (playerId == entity.Id) continue;

                if (stage.Entities.TryGetValue(entity.Id, out var entityState)
                    && entityState.Active
                    && entityState.Alive
                    && IsEnemy(entity.Type))
                {
                    var enemyPrevPos =
                        enemyPrevPositions != null && enemyPrevPositions.TryGetValue(entity.Id, out var prev)
                            ? prev
                            : entityState.Position;

                    var enemyTeleported = teleportedEnemies != null && teleportedEnemies.Contains(entity.Id);
                    var anyTeleported = playerTeleported || enemyTeleported;

                    if (IsTrajectoryIntersecting(
                            playerPrevPos,
                            player.Position,
                            enemyPrevPos,
                            entityState.Position,
                            anyTeleported))
                    {
                        caughtByEnemyId = entity.Id;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra Player có bị bắt khi xét toàn bộ quỹ đạo di chuyển từ lượt trước sang lượt này hay không.
        /// </summary>
        public bool IsCaughtWithTrajectory(
            StageState stage, int playerId,
            Vector2Int playerPrevPos,
            IReadOnlyDictionary<int, Vector2Int> enemyPrevPositions,
            bool playerTeleported = false,
            ISet<int> teleportedEnemies = null)
        {
            return TryGetCatchingEnemy(
                stage,
                playerId,
                playerPrevPos,
                enemyPrevPositions,
                playerTeleported,
                teleportedEnemies,
                out _
            );
        }

        public bool IsLost(StageState stage, int playerId, bool isCaught)
        {
            if (!stage.Entities.TryGetValue(playerId, out var player)) return true;
            return !player.Alive || isCaught;
        }

        public bool IsGameOver(StageState stage, int playerId, bool isCaught)
        {
            return IsWon(stage, playerId) || IsLost(stage, playerId, isCaught);
        }

        /// <summary>
        /// Đánh giá số sao đạt được (0 đến 3 sao) dựa vào thời gian và các điều kiện hoàn thành.
        /// </summary>
        public int CalculateStars(StageState stage, int playerId)
        {
            if (!IsWon(stage, playerId)) return 0;

            int stars = 1; // Thắng mặc định đạt 1 sao

            var starConditions = stage.Data.StateStars;
            if (starConditions is { Count: > 0 })
            {
                stars += starConditions.Count(star =>
                    star.hasTimeLimit && stage.ElapsedTimeSeconds <= star.maxTimeSeconds);
            }

            return Mathf.Clamp(stars, 1, 3);
        }

        #endregion
    }
}