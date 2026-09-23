using System;

namespace OctaneLogic.Core.Logic;

/// <summary>
/// Persisted state and configuration for one logical node. It has no game-object references.
/// </summary>
public sealed class LogicNodeState
{
    public string NodeId { get; set; } = Guid.NewGuid().ToString("N");
    public LogicNodeKind Kind { get; set; }

    // Clock configuration.
    public int PeriodTicks { get; set; } = 60;
    public int HighTicks { get; set; } = 30;
    public int PhaseTicks { get; set; }

    // Pulse and delayed-pulse configuration.
    public int PulseWidthTicks { get; set; } = 1;
    public int DelayTicks { get; set; }

    // Runtime state. These fields are persisted to ensure deterministic save/reload behaviour.
    public bool StoredValue { get; set; }
    public bool PreviousInput { get; set; }
    public int PulseTicksRemaining { get; set; }
    public int DelayTicksRemaining { get; set; } = -1;

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(NodeId))
        {
            NodeId = Guid.NewGuid().ToString("N");
        }

        PeriodTicks = Math.Max(1, PeriodTicks);
        HighTicks = Math.Clamp(HighTicks, 0, PeriodTicks);
        PhaseTicks = Math.Max(0, PhaseTicks);
        PulseWidthTicks = Math.Max(1, PulseWidthTicks);
        DelayTicks = Math.Max(0, DelayTicks);
        PulseTicksRemaining = Math.Max(0, PulseTicksRemaining);
        DelayTicksRemaining = Math.Max(-1, DelayTicksRemaining);
    }
}
