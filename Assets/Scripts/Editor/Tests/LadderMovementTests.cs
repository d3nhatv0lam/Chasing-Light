using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ChasingLight.Tests
{
    [TestFixture]
    public class LadderMovementTests
    {
        [Test]
        public void Ladder_FromFloor_CannotMoveDownIntoLadder()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.Floor),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 2))
            };

            var stageData = new StageData("test-ladder-floor", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            Assert.IsFalse(simulation.CanExecuteStep(EntityCommand.Down));
            var result = simulation.ExecuteStep(EntityCommand.Down);

            Assert.IsEmpty(result.Movements);
            Assert.AreEqual(new Vector2Int(1, 2), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_FromGate_CannotMoveDownIntoLadder()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.Gate),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 2))
            };
            var gates = new List<GateData>
            {
                new GateData(1, new Vector2Int(1, 2), new SwitchCondition(LinkLogic.All, new List<int>()), initialSolid: true)
            };

            var stageData = new StageData("test-ladder-gate", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), gates, new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            Assert.IsFalse(simulation.CanExecuteStep(EntityCommand.Down));
            var result = simulation.ExecuteStep(EntityCommand.Down);

            Assert.IsEmpty(result.Movements);
            Assert.AreEqual(new Vector2Int(1, 2), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_FromPoleTile_CannotMoveDownIntoLadder()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.PoleTile),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 2))
            };

            var stageData = new StageData("test-ladder-poletile", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            Assert.IsFalse(simulation.CanExecuteStep(EntityCommand.Down));
            var result = simulation.ExecuteStep(EntityCommand.Down);

            Assert.IsEmpty(result.Movements);
            Assert.AreEqual(new Vector2Int(1, 2), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_FromLadder_CanMoveDownToFloor()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };

            var stageData = new StageData("test-ladder-down", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            var result = simulation.ExecuteStep(EntityCommand.Down);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new Vector2Int(1, 0), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_FromSwitchOnTop_CanMoveDownToFloor()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.Switch),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 2))
            };
            var switches = new List<SwitchData>
            {
                new SwitchData(1, new Vector2Int(1, 2), SwitchMode.Hold, false)
            };

            var stageData = new StageData("test-ladder-switch", 1, Difficulty.Easy, grid, entities, 0,
                switches, new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            var result = simulation.ExecuteStep(EntityCommand.Down);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new Vector2Int(1, 0), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_FromStart_CannotMoveDownIntoLadder()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.Start),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 2))
            };

            var stageData = new StageData("test-ladder-start-down", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            Assert.IsFalse(simulation.CanExecuteStep(EntityCommand.Down));
            var result = simulation.ExecuteStep(EntityCommand.Down);

            Assert.IsEmpty(result.Movements);
            Assert.AreEqual(new Vector2Int(1, 2), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_ToStartAbove_CannotMoveUpThroughStart()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.Start),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };

            var stageData = new StageData("test-ladder-start-up", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            Assert.IsFalse(simulation.CanExecuteStep(EntityCommand.Up));
            var result = simulation.ExecuteStep(EntityCommand.Up);

            Assert.IsEmpty(result.Movements);
            Assert.AreEqual(new Vector2Int(1, 1), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_ToOpenGateAbove_CannotMoveUpThroughGate()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.Gate),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };
            var gates = new List<GateData>
            {
                new GateData(1, new Vector2Int(1, 2), new SwitchCondition(LinkLogic.All, new List<int>()), initialSolid: true)
            };

            var stageData = new StageData("test-ladder-open-gate-up", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), gates, new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            Assert.IsFalse(simulation.CanExecuteStep(EntityCommand.Up));
            var result = simulation.ExecuteStep(EntityCommand.Up);

            Assert.IsEmpty(result.Movements);
            Assert.AreEqual(new Vector2Int(1, 1), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_ToClosedGateAbove_CannotMoveUpThroughGate()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.Gate),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };
            var gates = new List<GateData>
            {
                new GateData(1, new Vector2Int(1, 2), new SwitchCondition(LinkLogic.All, new List<int>()), initialSolid: false)
            };

            var stageData = new StageData("test-ladder-closed-gate-up", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), gates, new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            Assert.IsFalse(simulation.CanExecuteStep(EntityCommand.Up));
            var result = simulation.ExecuteStep(EntityCommand.Up);

            Assert.IsEmpty(result.Movements);
            Assert.AreEqual(new Vector2Int(1, 1), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_ToPoleTileAbove_CannotMoveUpThroughPoleTile()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.PoleTile),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };

            var stageData = new StageData("test-ladder-poletile-up", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            Assert.IsFalse(simulation.CanExecuteStep(EntityCommand.Up));
            var result = simulation.ExecuteStep(EntityCommand.Up);

            Assert.IsEmpty(result.Movements);
            Assert.AreEqual(new Vector2Int(1, 1), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_FromPoleTileBelow_CanMoveUpIntoLadder()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.PoleTile),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 0))
            };

            var stageData = new StageData("test-ladder-from-poletile-below", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            var result = simulation.ExecuteStep(EntityCommand.Up);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new Vector2Int(1, 1), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_FromLadder_CanMoveDownToPoleTile()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.PoleTile),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };

            var stageData = new StageData("test-ladder-down-to-poletile", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), new List<TeleportData>(), new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            var result = simulation.ExecuteStep(EntityCommand.Down);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new Vector2Int(1, 0), state.Entities[1].Position);
        }

        [Test]
        public void Ladder_ToTeleportAbove_CanMoveUpOntoMechanism()
        {
            var cells = new List<CellData>
            {
                new CellData(new Vector2Int(1, 0), CellType.Floor),
                new CellData(new Vector2Int(1, 1), CellType.Ladder),
                new CellData(new Vector2Int(1, 2), CellType.Teleport),
                new CellData(new Vector2Int(2, 2), CellType.Teleport),
            };

            var grid = new GridData(3, 3, cells);
            var entities = new List<EntityData>
            {
                new EntityData(1, EntityType.Player, new Vector2Int(1, 1))
            };
            var teleports = new List<TeleportData>
            {
                new TeleportData(1, new Vector2Int(1, 2), new Vector2Int(2, 2), new SwitchCondition(LinkLogic.All, new List<int>()), initialEnabled: false)
            };

            var stageData = new StageData("test-ladder-teleport-up", 1, Difficulty.Easy, grid, entities, 0,
                new List<SwitchData>(), new List<GateData>(), teleports, new List<StateStar>());

            var state = new StageState(stageData);
            var simulation = new StageSimulation(state);

            var result = simulation.ExecuteStep(EntityCommand.Up);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new Vector2Int(1, 2), state.Entities[1].Position);
        }
    }
}
