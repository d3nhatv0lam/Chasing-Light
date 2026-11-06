using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ChasingLight.Tests
{
    [TestFixture]
    public class StageSimulationTests
    {
        [Test]
        public void Coop_GhostStepsOnSwitch_PlayerStepsOnGate_PlayerStands()
        {
            // Grid 5x5:
            // y=1: (1,1) Floor, (2,1) Gate 1 (initially closed, controlled by Switch 1)
            // y=2: (1,2) Floor, (2,2) Switch 1 (Hold mode)
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 1), CellType.Floor),
                new CellData(new Vector2Int(2, 1), CellType.Gate),
                new CellData(new Vector2Int(1, 2), CellType.Floor),
                new CellData(new Vector2Int(2, 2), CellType.Switch),
            };

            var grid = new GridData(5, 5, cells);

            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1)),
                new EntityData(2, EntityType.Shadow, new Vector2Int(1, 2))
            };

            var switches = new List<SwitchData>
            {
                new SwitchData(1, new Vector2Int(2, 2), SwitchMode.Hold, initialActivated: false)
            };

            var gates = new List<GateData>
            {
                new GateData(1, new Vector2Int(2, 1), new SwitchCondition(LinkLogic.All, new List<int> { 1 }), initialOpened: false)
            };

            var stageData = new StageData(
                id: "test-coop",
                stageNumber: 1,
                difficulty: Difficulty.Easy,
                grid: grid,
                entities: entities,
                shadowSpawnDelaySteps: 1,
                switches: switches,
                gates: gates,
                teleports: new List<TeleportData>(),
                stateStars: new List<StateStar>()
            );

            var state = new StageState(stageData);
            state.Entities[2].Active = true;
            state.History.Add(EntityCommand.Right); // Shadow will execute this command
            state.StepCount = 1;

            var simulation = new StageSimulation(state);

            // Execute: Player moves Right to Gate (2,1), Shadow moves Right to Switch (2,2)
            var result = simulation.ExecuteStep(EntityCommand.Right);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new Vector2Int(2, 1), state.Entities[1].Position, "Player phải ở vị trí Gate (2,1)");
            Assert.AreEqual(new Vector2Int(2, 2), state.Entities[2].Position, "Ghost phải ở vị trí Switch (2,2)");
            Assert.IsTrue(state.SwitchStates[1], "Switch 1 phải bật khi Ghost dẫm lên");
            Assert.IsTrue(state.GateStates[1], "Gate 1 phải mở thành sàn vững");
            Assert.IsTrue(state.Entities[1].Alive, "Player phải đứng vững, KHÔNG được rơi chết!");
            Assert.AreEqual(0, result.Falls.Count, "Không có thực thể nào bị rơi trong lượt phối hợp này");
        }

        [Test]
        public void FallOntoSwitch_ActivatesGateInNextIteration()
        {
            // Player tại (1, 3). Ô (2, 3) trống.
            // Dưới (2, 3) là (2, 1) có Switch 1 (Hold mode).
            // Switch 1 mở Gate 1 tại (4, 1).
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 3), CellType.Floor),
                new CellData(new Vector2Int(2, 1), CellType.Switch),
                new CellData(new Vector2Int(4, 1), CellType.Gate),
            };

            var grid = new GridData(5, 5, cells);

            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 3))
            };

            var switches = new List<SwitchData>
            {
                new SwitchData(1, new Vector2Int(2, 1), SwitchMode.Hold, initialActivated: false)
            };

            var gates = new List<GateData>
            {
                new GateData(1, new Vector2Int(4, 1), new SwitchCondition(LinkLogic.All, new List<int> { 1 }), initialOpened: false)
            };

            var stageData = new StageData(
                id: "test-fall-switch",
                stageNumber: 1,
                difficulty: Difficulty.Easy,
                grid: grid,
                entities: entities,
                shadowSpawnDelaySteps: 5,
                switches: switches,
                gates: gates,
                teleports: new List<TeleportData>(),
                stateStars: new List<StateStar>()
            );

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            // Player bước sang phải vào khoảng không (2, 3) -> Rơi tự do xuống (2, 1)
            var result = simulation.ExecuteStep(EntityCommand.Right);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new Vector2Int(2, 1), state.Entities[1].Position, "Player phải tiếp đất tại vị trí Switch (2,1)");
            Assert.IsTrue(state.Entities[1].Alive, "Player còn sống khi tiếp đất an toàn trên sàn Switch");
            Assert.IsTrue(state.SwitchStates[1], "Switch 1 phải được kích hoạt sau khi rơi chạm vào");
            Assert.IsTrue(state.GateStates[1], "Gate 1 phải được mở ra ở iteration kế tiếp");
            Assert.AreEqual(1, result.Falls.Count, "Ghi nhận 1 sự kiện rơi");
        }

        [Test]
        public void SimultaneousTeleport_BothEntitiesSwapPositions()
        {
            // Cổng Teleport 1 kết nối A (2, 1) và B (4, 1).
            // Player tại (1, 1) bước Right sang A (2, 1).
            // Ghost tại (5, 1) bước Left sang B (4, 1).
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 1), CellType.Floor),
                new CellData(new Vector2Int(2, 1), CellType.Teleport),
                new CellData(new Vector2Int(4, 1), CellType.Teleport),
                new CellData(new Vector2Int(5, 1), CellType.Floor),
            };

            var grid = new GridData(3, 7, cells);

            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1)),
                new EntityData(2, EntityType.Shadow, new Vector2Int(5, 1))
            };

            var teleports = new List<TeleportData>
            {
                new TeleportData(1, new Vector2Int(2, 1), new Vector2Int(4, 1), condition: null, initialEnabled: true)
            };

            var stageData = new StageData(
                id: "test-teleport-swap",
                stageNumber: 1,
                difficulty: Difficulty.Easy,
                grid: grid,
                entities: entities,
                shadowSpawnDelaySteps: 1,
                switches: new List<SwitchData>(),
                gates: new List<GateData>(),
                teleports: teleports,
                stateStars: new List<StateStar>()
            );

            var state = new StageState(stageData);
            state.Entities[2].Active = true;
            state.History.Add(EntityCommand.Left); // Shadow will execute Left this turn
            state.StepCount = 1;

            var simulation = new StageSimulation(state);

            // Player moves Right to (2, 1), Ghost moves Left to (4, 1)
            var result = simulation.ExecuteStep(EntityCommand.Right);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new Vector2Int(4, 1), state.Entities[1].Position, "Player phải dịch chuyển từ A sang B (4,1)");
            Assert.AreEqual(new Vector2Int(2, 1), state.Entities[2].Position, "Ghost phải dịch chuyển từ B sang A (2,1)");
            Assert.IsFalse(state.TeleportStates[1], "Cổng 1 phải đóng sau khi cả hai đã sử dụng");
            Assert.AreEqual(2, result.Teleports.Count, "Cả hai thực thể đều phải có sự kiện Teleport");
        }

        [Test]
        public void StepOffHoldSwitch_GateClosesAndPlayerFalls()
        {
            // Player tại (1, 1) (Switch 1 Hold). Gate 1 tại (2, 1) ban đầu đóng.
            // Dưới (2, 1) là ô trống (vực).
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 1), CellType.Switch),
                new CellData(new Vector2Int(2, 1), CellType.Gate),
            };

            var grid = new GridData(3, 4, cells);

            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };

            var switches = new List<SwitchData>
            {
                new SwitchData(1, new Vector2Int(1, 1), SwitchMode.Hold, initialActivated: true)
            };

            var gates = new List<GateData>
            {
                new GateData(1, new Vector2Int(2, 1), new SwitchCondition(LinkLogic.All, new List<int> { 1 }), initialOpened: false)
            };

            var stageData = new StageData(
                id: "test-stepoff",
                stageNumber: 1,
                difficulty: Difficulty.Easy,
                grid: grid,
                entities: entities,
                shadowSpawnDelaySteps: 5,
                switches: switches,
                gates: gates,
                teleports: new List<TeleportData>(),
                stateStars: new List<StateStar>()
            );

            var state = new StageState(stageData);
            // Player đang đứng đè switch
            state.SwitchStates[1] = true;
            state.GateStates[1] = true;

            var simulation = new StageSimulation(state);

            // Player bước sang phải vào Gate (2, 1). Vì rời switch nên cổng sập và rơi!
            var result = simulation.ExecuteStep(EntityCommand.Right);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(state.SwitchStates[1], "Switch 1 phải tắt vì Player rời đi");
            Assert.IsFalse(state.GateStates[1], "Gate 1 phải đóng lại");
            Assert.IsFalse(state.Entities[1].Alive, "Player phải rơi xuống vực và chết");
            Assert.AreEqual(StageStatus.Lost, state.Status, "Trạng thái game chuyển sang Lost");
        }
        [Test]
        public void SimultaneousFall_BothInAir_ShadowActivatesSwitch_PlayerLandsOnGate_PlayerSurvives()
        {
            // Tái hiện chính xác bug kịch bản trong ảnh NewStage:
            // Nhánh trái: Shadow tại (2, 3), bước Right ra không trung (3, 3), dưới là Switch 3 tại (3, 2).
            // Nhánh phải: Player tại (7, 2), bước Left ra không trung (6, 2), dưới là Gate 1 tại (6, 1).
            // Cả 2 cùng lơ lửng trên không và cần trọng lực kéo xuống 1 ô.
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(2, 3), CellType.Floor),
                new CellData(new Vector2Int(3, 2), CellType.Switch),
                new CellData(new Vector2Int(7, 2), CellType.Floor),
                new CellData(new Vector2Int(6, 1), CellType.Gate),
            };

            var grid = new GridData(8, 11, cells);

            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(7, 2)),
                new EntityData(2, EntityType.Shadow, new Vector2Int(2, 3))
            };

            var switches = new List<SwitchData>
            {
                new SwitchData(3, new Vector2Int(3, 2), SwitchMode.Hold, initialActivated: false)
            };

            var gates = new List<GateData>
            {
                new GateData(1, new Vector2Int(6, 1),
                    new SwitchCondition(LinkLogic.All, new List<int> { 3 }),
                    initialOpened: false)
            };

            var stageData = new StageData(
                id: "test-simultaneous-fall",
                stageNumber: 1,
                difficulty: Difficulty.Easy,
                grid: grid,
                entities: entities,
                shadowSpawnDelaySteps: 1,
                switches: switches,
                gates: gates,
                teleports: new List<TeleportData>(),
                stateStars: new List<StateStar>()
            );

            var state = new StageState(stageData);
            state.Entities[2].Active = true;
            state.History.Add(EntityCommand.Right); // Shadow will execute Right
            state.StepCount = 1;

            var simulation = new StageSimulation(state);

            // Execute: Player moves Left, Shadow moves Right
            var result = simulation.ExecuteStep(EntityCommand.Left);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(state.SwitchStates[3], "Switch 3 phải ON khi Shadow rơi chạm đất");
            Assert.IsTrue(state.GateStates[1], "Gate 1 phải mở để đón chân Player");
            Assert.IsTrue(state.Entities[1].Alive, "Player KHÔNG được chết — Gate 1 phải đỡ được Player!");
            Assert.AreEqual(new Vector2Int(6, 1), state.Entities[1].Position, "Player tiếp đất an toàn tại Gate 1 (6, 1)");
            Assert.AreEqual(new Vector2Int(3, 2), state.Entities[2].Position, "Shadow tiếp đất an toàn tại Switch 3 (3, 2)");
            Assert.AreEqual(StageStatus.Playing, state.Status);
        }

        [Test]
        public void CliffFall_WalksIntoEndlessAbyss_DiesPromptlyWithoutInfiniteLoop()
        {
            // Player tại (1, 1). Bấm Right sang (2, 1) là khoảng không vô tận.
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 1), CellType.Floor),
            };

            var grid = new GridData(5, 5, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };

            var stageData = new StageData("test-cliff", 1, Difficulty.Easy, grid, entities, 5,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());
            var state = new StageState(stageData);

            var simulation = new StageSimulation(state);
            var result = simulation.ExecuteStep(EntityCommand.Right);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(state.Entities[1].Alive, "Player phải chết vì rơi xuống vực không có sàn");
            Assert.AreEqual(new Vector2Int(2, -1), state.Entities[1].Position, "Tọa độ Player ở ngoài biên y=-1");
            Assert.AreEqual(StageStatus.Lost, state.Status);
        }
    }
}
