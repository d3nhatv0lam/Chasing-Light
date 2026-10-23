using System;
using System.Collections.Generic;
using ChasingLight;

namespace ChasingLight
{
    /// <summary>
    /// Gói kết quả trả về sau khi Simulation thực thi 1 bước.
    /// </summary>
    public sealed class StepResult
    {
        public bool Success { get; }
        public string Message { get; }
        public EntityCommand PlayerCommand { get; }
        public EntityCommand ShadowCommand { get; }
        public StageStatus Status { get; }
        public int StepCount { get; }
        public int StarsEarned { get; }

        /// <summary>
        /// Nhịp 1 (~0.12s - 0.15s): Các thực thể di chuyển bước đi đồng thời.
        /// </summary>
        public IReadOnlyList<EntityMovedEvent> Movements { get; }

        /// <summary>
        /// Nhịp 2 (Tức thời ~0.10s): Dịch chuyển qua cổng Teleport.
        /// </summary>
        public IReadOnlyList<EntityTeleportedEvent> Teleports { get; }

        /// <summary>
        /// Nhịp 3 (~0.05s): Thay đổi trạng thái Switch và Gate (diễn ra cùng lúc chân chạm đích).
        /// </summary>
        public IReadOnlyList<ISimulationEvent> MechanismChanges { get; }

        /// <summary>
        /// Nhịp 4 (~0.15s): Rơi tự do nếu bước hụt chân hoặc Gate bên dưới sập mở.
        /// </summary>
        public IReadOnlyList<EntityFellEvent> Falls { get; }

        /// <summary>
        /// Nhịp 5: Kết thúc lượt (Bị bắt, Thắng/Thua).
        /// </summary>
        public IReadOnlyList<ISimulationEvent> EndEvents { get; }

        /// <summary>
        /// Toàn bộ sự kiện theo thứ tự xuất hiện (phục vụ debug/test).
        /// </summary>
        public IReadOnlyList<ISimulationEvent> AllEvents { get; }

        public StepResult(
            bool success,
            string message,
            EntityCommand playerCommand,
            EntityCommand shadowCommand,
            StageStatus status,
            int stepCount,
            int starsEarned,
            IReadOnlyList<EntityMovedEvent> movements,
            IReadOnlyList<EntityTeleportedEvent> teleports,
            IReadOnlyList<ISimulationEvent> mechanismChanges,
            IReadOnlyList<EntityFellEvent> falls,
            IReadOnlyList<ISimulationEvent> endEvents,
            IReadOnlyList<ISimulationEvent> allEvents)
        {
            Success = success;
            Message = message ?? string.Empty;
            PlayerCommand = playerCommand;
            ShadowCommand = shadowCommand;
            Status = status;
            StepCount = stepCount;
            StarsEarned = starsEarned;
            Movements = movements ?? Array.Empty<EntityMovedEvent>();
            Teleports = teleports ?? Array.Empty<EntityTeleportedEvent>();
            MechanismChanges = mechanismChanges ?? Array.Empty<ISimulationEvent>();
            Falls = falls ?? Array.Empty<EntityFellEvent>();
            EndEvents = endEvents ?? Array.Empty<ISimulationEvent>();
            AllEvents = allEvents ?? Array.Empty<ISimulationEvent>();
        }

        public static StepResult Failed(EntityCommand command, string reason, StageStatus status, int stepCount)
        {
            return new StepResult(
                success: false,
                message: reason,
                playerCommand: command,
                shadowCommand: EntityCommand.None,
                status: status,
                stepCount: stepCount,
                starsEarned: 0,
                movements: null,
                teleports: null,
                mechanismChanges: null,
                falls: null,
                endEvents: null,
                allEvents: null
            );
        }
    }
}
