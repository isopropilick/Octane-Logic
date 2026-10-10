using System;
using System.Collections.Generic;
using System.Linq;
using OctaneLogic.Core.Logic;

namespace OctaneLogic.Core.Persistence
{
    /// <summary>
    /// Mod-owned save blob. Keep this independent of Shapez 2 map entities and runtime objects.
    /// </summary>
    public sealed class OctaneLogicSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public List<LogicNodeState> Nodes { get; set; } = new List<LogicNodeState>();

        public void Normalize()
        {
            if (SchemaVersion <= 0) SchemaVersion = CurrentSchemaVersion;
            Nodes ??= new List<LogicNodeState>();

            foreach (LogicNodeState node in Nodes.Where(node => node != null))
            {
                node.Normalize();
            }

            // Preserve the first stable ID and only repair duplicates. Never discard saved nodes.
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (LogicNodeState node in Nodes.Where(node => node != null))
            {
                if (!usedIds.Add(node.NodeId))
                {
                    node.NodeId = Guid.NewGuid().ToString("N");
                    usedIds.Add(node.NodeId);
                }
            }
        }
    }
}
