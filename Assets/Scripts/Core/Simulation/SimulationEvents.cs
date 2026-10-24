using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Giao diện đánh dấu cho tất cả các sự kiện diễn ra trong 1 bước tính toán của Simulation.
    /// </summary>
    public interface ISimulationEvent
    {
    }
    
    /// <summary>
    /// Sự kiện thực thể được tạo ra.
    /// </summary>
    public sealed class EntitySpawnedEvent : ISimulationEvent
    {
        public int EntityId { get; }
        public Vector2Int Position { get; }
        public EntitySpawnedEvent(int entityId, Vector2Int position)
        {
            EntityId = entityId;
            Position = position;
        }
    }
    
    /// <summary>
    ///  Sự kiện thực thể chết.
    /// </summary>
    public sealed class EntityDiedEvent : ISimulationEvent
    {
        public int EntityId { get; }
        public EntityDiedEvent(int entityId) => EntityId = entityId;
        public override string ToString() => $"[EntityDied] Entity {EntityId} has died.";
    }

    /// <summary>
    /// Sự kiện thực thể di chuyển bước đi (bước ngang hoặc leo thang).
    /// </summary>
    public sealed class EntityMovedEvent : ISimulationEvent
    {
        public int EntityId { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }
        public bool IsLadder { get; }

        public EntityMovedEvent(int entityId, Vector2Int from, Vector2Int to, bool isLadder)
        {
            EntityId = entityId;
            From = from;
            To = to;
            IsLadder = isLadder;
        }

        public override string ToString() => $"[EntityMoved] Entity {EntityId}: {From} -> {To} (Ladder: {IsLadder})";
    }

    /// <summary>
    /// Sự kiện thực thể bước qua Cổng dịch chuyển (Teleport).
    /// </summary>
    public sealed class EntityTeleportedEvent : ISimulationEvent
    {
        public int EntityId { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }

        public EntityTeleportedEvent(int entityId, Vector2Int from, Vector2Int to)
        {
            EntityId = entityId;
            From = from;
            To = to;
        }

        public override string ToString() => $"[EntityTeleported] Entity {EntityId}: {From} -> {To}";
    }

    /// <summary>
    /// Sự kiện Công tắc (Switch) thay đổi trạng thái bật / tắt.
    /// </summary>
    public sealed class SwitchToggledEvent : ISimulationEvent
    {
        public int SwitchId { get; }
        public bool IsActivated { get; }

        public SwitchToggledEvent(int switchId, bool isActivated)
        {
            SwitchId = switchId;
            IsActivated = isActivated;
        }

        public override string ToString() => $"[SwitchToggled] Switch {SwitchId} => {(IsActivated ? "ON" : "OFF")}";
    }

    /// <summary>
    /// Sự kiện Cổng (Gate) thay đổi trạng thái Mở (thành lỗ hổng) hoặc Đóng (thành nền đứng).
    /// </summary>
    public sealed class GateStateChangedEvent : ISimulationEvent
    {
        public int GateId { get; }
        public bool IsOpen { get; }

        public GateStateChangedEvent(int gateId, bool isOpen)
        {
            GateId = gateId;
            IsOpen = isOpen;
        }

        public override string ToString() => $"[GateStateChanged] Gate {GateId} => {(IsOpen ? "OPEN (Hole)" : "CLOSED (Floor)")}";
    }

    /// <summary>
    /// Sự kiện Cổng dịch chuyển (Teleport) được bật / tắt tính năng.
    /// </summary>
    public sealed class TeleportStateChangedEvent : ISimulationEvent
    {
        public int TeleportId { get; }
        public bool IsEnabled { get; }

        public TeleportStateChangedEvent(int teleportId, bool isEnabled)
        {
            TeleportId = teleportId;
            IsEnabled = isEnabled;
        }

        public override string ToString() => $"[TeleportStateChanged] Teleport {TeleportId} => {(IsEnabled ? "ENABLED" : "DISABLED")}";
    }

    /// <summary>
    /// Sự kiện thực thể bị rơi tự do do không có chỗ đứng (bước hụt chân hoặc Gate mở dưới chân).
    /// </summary>
    public sealed class EntityFellEvent : ISimulationEvent
    {
        public int EntityId { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }
        public bool IsFatal { get; }

        public EntityFellEvent(int entityId, Vector2Int from, Vector2Int to, bool isFatal)
        {
            EntityId = entityId;
            From = from;
            To = to;
            IsFatal = isFatal;
        }

        public override string ToString() => $"[EntityFell] Entity {EntityId}: {From} -> {To} (Fatal: {IsFatal})";
    }

    /// <summary>
    /// Sự kiện Player bị bắt bởi Shadow / Kẻ địch.
    /// </summary>
    public sealed class PlayerCaughtEvent : ISimulationEvent
    {
        public int PlayerId { get; }
        public int EnemyId { get; }

        public PlayerCaughtEvent(int playerId, int enemyId)
        {
            PlayerId = playerId;
            EnemyId = enemyId;
        }

        public override string ToString() => $"[PlayerCaught] Player {PlayerId} caught by Enemy {EnemyId}";
    }

    /// <summary>
    /// Sự kiện màn chơi kết thúc (Thắng hoặc Thua).
    /// </summary>
    public sealed class StageCompletedEvent : ISimulationEvent
    {
        public StageStatus Status { get; }
        public int StarsEarned { get; }
        public int FinalSteps { get; }

        public StageCompletedEvent(StageStatus status, int starsEarned, int finalSteps)
        {
            Status = status;
            StarsEarned = starsEarned;
            FinalSteps = finalSteps;
        }

        public override string ToString() => $"[StageCompleted] Status: {Status}, Stars: {StarsEarned}, Steps: {FinalSteps}";
    }
}
