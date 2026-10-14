using System;
using ChasingLight.Core.Actions;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Luật thuần: chỉ trả lời "được / không được", không đổi state (trừ <see cref="ResolveFall"/>).
    /// Quy ước toạ độ: y tăng = đi lên, y &lt; 0 = rơi khỏi màn chơi.
    /// </summary>
    public sealed class GameRules
    {
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

        public bool CanMove(StageState stage, int id, EntityCommand command)
        {
            if (command == EntityCommand.None) return false;

            var grid = stage.Data.Grid;
            var from = stage.Entities[id].Position;
            var to = from + ToDelta(command);

            // rìa màn chơi
            if (!grid.InBounds(to)) return false;

            // Lên / xuống cầu thang
            if (command is EntityCommand.Up or EntityCommand.Down)
            {
                return (grid.GetCell(from).Type == CellType.Ladder 
                        || grid.GetCell(to).Type == CellType.Ladder)
                       && CanStandAt(stage, to);
            }
            
            return true;
        }
        
        public bool CanStandAt(StageState stage, Vector2Int position)
        {
            var type = stage.Data.Grid.GetCell(position).Type;
            
            if (type == CellType.Gate)
                return !IsOpenGate(stage, position);
            
            return CanStandOn(type);
        }

        /// <summary>Gate là ô nền: đóng = đứng được, mở = thành lỗ (rơi xuống).</summary>
        public bool IsOpenGate(StageState stage, Vector2Int position)
        {
            foreach (var gate in stage.Data.Gates)
            {
                if (gate.Position == position && stage.GateStates[gate.Id]) return true;
            }

            return false;
        }
        

        /// <summary>Rơi đến khi chạm ô đứng được; rơi qua y &lt; 0 thì chết.</summary>
        public void ResolveFall(StageState stage, int id)
        {
            var entity = stage.Entities[id];

            while (!CanStandAt(stage, entity.Position))
            {
                entity.Position += Vector2Int.down;
                if (entity.Position.y < 0)
                {
                    entity.Alive = false;
                    return;
                }
            }
        }

        public bool TryTeleport(StageState stage, Vector2Int position, out Vector2Int destination)
        {
            foreach (var tp in stage.Data.Teleports)
            {
                if (!stage.TeleportStates[tp.Id]) continue;

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
        ///  Tất cả các Switch liên kết có được kích hoạt không
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        public bool IsPoweredBySwitches(StageState stage, MechanismType targetType, int targetId)
        {
            throw new NotImplementedException();
        }

        public bool IsWon(StageState stage, int playerId)
        {
            var player = stage.Entities[playerId];
            return player.Alive && stage.Data.Grid.GetCell(player.Position).Type == CellType.Goal;
        }
        
        public bool IsCaught(StageState stage, int playerId)
        {
            var player = stage.Entities[playerId];
            foreach (var entity in stage.Data.Entities)
            {
                if (playerId == entity.Id) continue;
                
                var entityState = stage.Entities[entity.Id];
                if (entityState.Active 
                    && IsEnemy(entity.Type) 
                    && entityState.Position == player.Position)
                {
                    return true;
                }
            }
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

        private bool IsEnemy(EntityType type)
        {
            return type switch
            {
                EntityType.Player => false,
                _ => true,
            };
        }
        
        private bool CanStandOn(CellType cell)
        {
            return cell switch
            {
                CellType.Empty => false,
                _ => true,
            };
        }
    }
}