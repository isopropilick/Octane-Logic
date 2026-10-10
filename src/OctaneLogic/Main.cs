using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using Game.Core.Simulation;
using Game.Orchestration;
using MonoMod.RuntimeDetour;
using OctaneLogic.Core.Persistence;
using ShapezShifter.Flow;
using ShapezShifter.SharpDetour;

namespace OctaneLogic
{
    /// <summary>
    /// Shapez 2 mod entry point. Physical building registration is intentionally kept separate from this save contract.
    /// </summary>
    public sealed class Main : IMod
    {
        private readonly ILogger _logger;
        private readonly Hook _simulationSystemsHook;
        private readonly NumericGates _numericGates;
        private bool _disposed;

        public Main(ILogger logger)
        {
            _logger = logger;
            this.AttachSaveData<OctaneLogicSaveData>();
            this.RegisterToAfterSaveDataDeserialized<OctaneLogicSaveData>(OnSaveDataLoaded);

            _numericGates = new NumericGates(logger);
            _simulationSystemsHook = DetourHelper
                .CreatePostfixHook<BuiltinSimulationSystems, IEnumerable<ISimulationSystem>>(
                    systems => systems.CreateSimulationSystems(),
                    AddSimulationSystems);

            _logger.Info?.Log("Octane Logic 0.2.0 initialized: numeric signal gates registered.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _simulationSystemsHook.Dispose();
            this.UnregisterToAfterSaveDataDeserialized<OctaneLogicSaveData>(OnSaveDataLoaded);
            this.DetachSaveData<OctaneLogicSaveData>();
        }

        private IEnumerable<ISimulationSystem> AddSimulationSystems(
            BuiltinSimulationSystems builtInSystems,
            IEnumerable<ISimulationSystem> systems)
        {
            return _numericGates.RegisterSimulationSystems(systems);
        }

        private void OnSaveDataLoaded(OctaneLogicSaveData saveData)
        {
            saveData.Normalize();
            _logger.Info?.Log($"Octane Logic loaded {saveData.Nodes.Count} persisted logic node(s), schema {saveData.SchemaVersion}.");
        }
    }
}
