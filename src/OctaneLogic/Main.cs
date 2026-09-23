using Core.Logging;
using OctaneLogic.Core.Persistence;
using ShapezShifter.Flow;

namespace OctaneLogic
{
    /// <summary>
    /// Shapez 2 mod entry point. Physical building registration is intentionally kept separate from this save contract.
    /// </summary>
    public sealed class Main : IMod
    {
        private readonly ILogger _logger;
        private bool _disposed;

        public Main(ILogger logger)
        {
            _logger = logger;
            this.AttachSaveData<OctaneLogicSaveData>();
            this.RegisterToAfterSaveDataDeserialized<OctaneLogicSaveData>(OnSaveDataLoaded);
            _logger.Info?.Log("Octane Logic 0.1.0 initialized: deterministic logic core and isolated save data enabled.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            this.UnregisterToAfterSaveDataDeserialized<OctaneLogicSaveData>(OnSaveDataLoaded);
            this.DetachSaveData<OctaneLogicSaveData>();
        }

        private void OnSaveDataLoaded(OctaneLogicSaveData saveData)
        {
            saveData.Normalize();
            _logger.Info?.Log($"Octane Logic loaded {saveData.Nodes.Count} persisted logic node(s), schema {saveData.SchemaVersion}.");
        }
    }
}
