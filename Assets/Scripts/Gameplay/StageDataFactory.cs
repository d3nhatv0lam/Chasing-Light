using System;
using UnityEngine;

namespace ChasingLight
{
    public static class StageDataFactory
    {
        /// <summary>Chuyển StageAsset thành StageData và kiểm tra cấu hình cơ bản.</summary>
        public static StageData Create(StageAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset), "StageAsset chưa được gán.");
            }

            StageData stage;
            try
            {
                stage = asset.ToData();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Không thể chuyển StageAsset thành StageData. Kiểm tra Cells/entities và các danh sách cơ chế trong Inspector.",
                    exception);
            }

            if (stage.Grid == null || stage.Grid.Width <= 0 || stage.Grid.Height <= 0)
            {
                throw new InvalidOperationException("StageAsset phải có Width và Height lớn hơn 0.");
            }

            int playerId = FindEntityId(stage, EntityType.Player);
            int shadowId = FindEntityId(stage, EntityType.Shadow);

            ValidateEntityPosition(stage, playerId, "Player");
            ValidateEntityPosition(stage, shadowId, "Shadow");

            EntityData player = FindEntity(stage, playerId);
            if (stage.Grid.GetCell(player.Position).Type != CellType.Start)
            {
                throw new InvalidOperationException(
                    "Ô đặt Player trong StageAsset phải có Cell Type = Start.");
            }

            bool hasGoal = false;
            for (int x = 0; x < stage.Grid.Width; x++)
            {
                for (int y = 0; y < stage.Grid.Height; y++)
                {
                    if (stage.Grid.GetCell(new Vector2Int(x, y)).Type == CellType.Goal)
                    {
                        hasGoal = true;
                        break;
                    }
                }

                if (hasGoal) break;
            }

            if (!hasGoal)
            {
                throw new InvalidOperationException(
                    "StageAsset cần ít nhất một Cell có Cell Type = Goal.");
            }

            return stage;
        }

        /// <summary>Lấy ID thực thể theo EntityType, không phụ thuộc ID cố định.</summary>
        public static int FindEntityId(StageData stage, EntityType type)
        {
            if (stage == null || stage.Entities == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            int foundId = -1;
            foreach (EntityData entity in stage.Entities)
            {
                if (entity.Type != type)
                {
                    continue;
                }

                if (foundId >= 0)
                {
                    throw new InvalidOperationException(
                        "StageAsset chỉ được có một thực thể loại " + type + ".");
                }

                foundId = entity.Id;
            }

            if (foundId < 0)
            {
                throw new InvalidOperationException(
                    "StageAsset thiếu thực thể loại " + type + ". Hãy thêm entity trong Inspector.");
            }

            return foundId;
        }

        private static EntityData FindEntity(StageData stage, int id)
        {
            foreach (EntityData entity in stage.Entities)
            {
                if (entity.Id == id) return entity;
            }

            throw new InvalidOperationException("Không tìm thấy entity ID " + id + ".");
        }

        private static void ValidateEntityPosition(StageData stage, int entityId, string entityName)
        {
            EntityData entity = FindEntity(stage, entityId);
            if (!stage.Grid.InBounds(entity.Position))
            {
                throw new InvalidOperationException(
                    entityName + " ở ngoài bản đồ: " + entity.Position + ".");
            }

            if (stage.Grid.GetCell(entity.Position).Type == CellType.Empty)
            {
                throw new InvalidOperationException(
                    entityName + " đang ở ô Empty " + entity.Position + ". Hãy đặt entity lên Start/Floor hoặc một ô đứng được.");
            }
        }
    }
}
