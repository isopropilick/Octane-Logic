using System;
using System.Collections.Generic;
using System.Linq;
using Core.Factory;
using Core.Localization;
using Game.Content.Features.Signals;
using Game.Core.Content.Buildings;
using Game.Core.Coordinates;
using Game.Core.Map.Simulation;
using Game.Core.Simulation;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Hijack;
using ShapezShifter.Kit;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

namespace OctaneLogic
{
    /// <summary>
    /// Registers the first placeable Octane Logic devices. They operate on IntegerSignal values,
    /// deliberately complementing the boolean gates already provided by the base game.
    /// </summary>
    internal sealed class NumericGates
    {
        private readonly NumericGateBuilding[] _buildings;

        public NumericGates(ILogger logger)
        {
            _buildings = new[]
            {
                new NumericGateBuilding(logger, NumericGateOperation.Add, "adder", new Color(0.18f, 0.72f, 0.94f)),
                new NumericGateBuilding(logger, NumericGateOperation.Subtract, "subtractor", new Color(0.38f, 0.82f, 0.48f)),
                new NumericGateBuilding(logger, NumericGateOperation.Multiply, "multiplier", new Color(0.96f, 0.62f, 0.18f)),
                new NumericGateBuilding(logger, NumericGateOperation.Divide, "divider", new Color(0.82f, 0.38f, 0.96f)),
                new NumericGateBuilding(logger, NumericGateOperation.Modulo, "modulo", new Color(0.96f, 0.32f, 0.42f))
            };
        }

        public IEnumerable<ISimulationSystem> RegisterSimulationSystems(IEnumerable<ISimulationSystem> systems)
        {
            return systems.Concat(_buildings.Select(building => building.CreateSimulationSystem()));
        }
    }

    internal sealed class NumericGateBuilding
    {
        private readonly BuildingDefinitionId _definitionId;
        private readonly NumericGateOperation _operation;
        private readonly ILogger _logger;

        public NumericGateBuilding(ILogger logger, NumericGateOperation operation, string id, Color color)
        {
            _logger = logger;
            _operation = operation;
            _definitionId = new BuildingDefinitionId("octane-logic-" + id);

            IBuildingGroupBuilder group = BuildingGroup.Create(new BuildingDefinitionGroupId("octane-logic-" + id + "-group"))
                .WithTitle(("octane-logic." + id + ".title").T())
                .WithDescription(("octane-logic." + id + ".description").T())
                .WithIcon(NumericGateVisuals.CreateIcon(color))
                .AsNonTransportableBuilding()
                .WithPreferredPlacement(DefaultPreferredPlacementMode.Single)
                .AutoConnected();

            IBuildingConnectorData connectors = BuildingConnectors.SingleTile()
                .AddWireInput(WireConnectorConfig.CustomInput(TileDirection.West))
                .AddWireInput(WireConnectorConfig.CustomInput(TileDirection.South))
                .AddWireOutput(WireConnectorConfig.CustomOutput(TileDirection.East))
                .Build();

            var drawData = new NumericGateDrawData();
            IBuildingBuilder building = Building.Create(_definitionId)
                .WithConnectorData(connectors)
                .DynamicallyRendering<NumericGateSimulationRenderer, NumericGateSimulation, NumericGateDrawData>(drawData)
                .WithStaticDrawData(NumericGateVisuals.CreateDrawData(id, color, drawData))
                .WithoutSound()
                .WithoutSimulationConfiguration()
                .WithEfficiencyData(new BuildingEfficiencyData(1.0f, 1));

            AtomicBuildings.Extend()
                .AllScenarios()
                .WithBuilding(building, group)
                .UnlockedAtMilestone(new ByIndexMilestoneSelector(2))
                .WithDefaultPlacement()
                .InToolbar(ToolbarElementLocator.Root().ChildAt(2).ChildAt(6).ChildAt(^1).InsertAfter())
                .WithSimulation(new NumericGateFactoryBuilder(_operation), logger)
                .WithCustomModules(new NumericGateModuleDataProvider())
                .WithoutPrediction()
                .Build();
        }

        public ISimulationSystem CreateSimulationSystem()
        {
            return new AtomicStatefulBuildingSimulationSystem<NumericGateSimulation, LogicGate2In1OutSimulationState>(
                new NumericGateSimulationFactory(_operation), _definitionId, _logger);
        }
    }

    internal enum NumericGateOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo
    }

    internal sealed class NumericGateFactoryBuilder
        : IBuildingSimulationFactoryBuilder<NumericGateSimulation, LogicGate2In1OutSimulationState,
            EmptyCustomSimulationConfiguration>
    {
        private readonly NumericGateOperation _operation;

        public NumericGateFactoryBuilder(NumericGateOperation operation)
        {
            _operation = operation;
        }

        public IFactory<LogicGate2In1OutSimulationState, NumericGateSimulation> BuildFactory(
            SimulationSystemsDependencies dependencies,
            out EmptyCustomSimulationConfiguration configuration)
        {
            configuration = new EmptyCustomSimulationConfiguration();
            return new NumericGateSimulationFactory(_operation);
        }
    }

    internal sealed class NumericGateSimulationFactory : IFactory<LogicGate2In1OutSimulationState, NumericGateSimulation>
    {
        private readonly NumericGateOperation _operation;

        public NumericGateSimulationFactory(NumericGateOperation operation)
        {
            _operation = operation;
        }

        public NumericGateSimulation Produce(LogicGate2In1OutSimulationState state)
        {
            return new NumericGateSimulation(state, _operation);
        }
    }

    internal sealed class NumericGateSimulation : LogicGate2In1OutSimulation
    {
        private readonly NumericGateOperation _operation;

        public NumericGateSimulation(LogicGate2In1OutSimulationState state, NumericGateOperation operation)
            : base(state)
        {
            _operation = operation;
        }

        public override ISignal ComputeOutputSignal(ISignal left, ISignal right)
        {
            if (!(left is IntegerSignal leftInteger) || !(right is IntegerSignal rightInteger))
            {
                return NullSignal.Instance;
            }

            long a = leftInteger.Value;
            long b = rightInteger.Value;
            switch (_operation)
            {
                case NumericGateOperation.Add:
                    return IntegerSignal.Get(Clamp(a + b));
                case NumericGateOperation.Subtract:
                    return IntegerSignal.Get(Clamp(a - b));
                case NumericGateOperation.Multiply:
                    return IntegerSignal.Get(Clamp(a * b));
                case NumericGateOperation.Divide:
                    return b == 0 ? NullSignal.Instance : IntegerSignal.Get(Clamp(a / b));
                case NumericGateOperation.Modulo:
                    return b == 0 ? NullSignal.Instance : IntegerSignal.Get(Clamp(a % b));
                default:
                    return NullSignal.Instance;
            }
        }

        private static int Clamp(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }
    }

    internal sealed class NumericGateModuleDataProvider : SimulationBasedBuildingModuleDataProvider<NumericGateSimulation>
    {
        public override IEnumerable<IHUDSidePanelModuleData> GetSimulationModules(
            BuildingModel building,
            ILocalizedSimulation localizedSimulation,
            NumericGateSimulation simulation)
        {
            yield return new HUDSidePanelModuleWireInfo.Data("A", simulation.Input0Conductor);
            yield return new HUDSidePanelModuleWireInfo.Data("B", simulation.Input1Conductor);
            yield return new HUDSidePanelModuleWireInfo.Data("Result", simulation.OutputConductor);
        }
    }

    internal sealed class NumericGateSimulationRenderer
        : StatelessBuildingSimulationRenderer<NumericGateSimulation, NumericGateDrawData>
    {
        public NumericGateSimulationRenderer(IMapModel map) : base(map) { }
    }

    internal sealed class NumericGateDrawData : IBuildingCustomDrawData { }

    internal static class NumericGateVisuals
    {
        public static Sprite CreateIcon(Color color)
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        public static BuildingDrawData CreateDrawData(string id, Color color, NumericGateDrawData drawData)
        {
            Mesh mesh = CreateBoxMesh("OctaneLogic_" + id, color);
            LOD6Mesh lod = MeshLod.Create().AddLod0Mesh(mesh).BuildLod6Mesh();

            return new BuildingDrawData(
                renderVoidBelow: false,
                mainMeshPerLayer: new ILODMesh[] { lod, lod, lod },
                isolatedBlueprintMesh: lod,
                combinedBlueprintMesh: lod,
                previewMesh: lod.LODClose,
                glassMesh: new LODEmptyMesh(),
                colliders: BoundingBoxHelper.CreateBasicCollider(mesh),
                customDrawData: drawData,
                hasCustomOverviewMesh: false,
                customOverviewMesh: null,
                simulationRendererDrawsMainMesh: false);
        }

        private static Mesh CreateBoxMesh(string name, Color color)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                new Vector3(-0.42f, 0.0f, -0.42f), new Vector3(0.42f, 0.0f, -0.42f),
                new Vector3(0.42f, 0.0f, 0.42f), new Vector3(-0.42f, 0.0f, 0.42f),
                new Vector3(-0.42f, 0.42f, -0.42f), new Vector3(0.42f, 0.42f, -0.42f),
                new Vector3(0.42f, 0.42f, 0.42f), new Vector3(-0.42f, 0.42f, 0.42f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6, 3, 0, 4, 3, 4, 7
            };
            mesh.colors = new[] { color, color, color, color, color, color, color, color };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
