using System;

namespace OctaneLogic.Core.Logic
{
    /// <summary>
    /// Deterministic evaluator. Call once per simulation tick for each node.
    /// </summary>
    public static class LogicNodeEvaluator
    {
    public static bool Evaluate(LogicNodeState node, LogicInputs input, long simulationTick)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        node.Normalize();

        return node.Kind switch
        {
            LogicNodeKind.MemoryLatch => EvaluateMemoryLatch(node, input),
            LogicNodeKind.Clock => EvaluateClock(node, simulationTick),
            LogicNodeKind.RisingEdge => EvaluateRisingEdge(node, input),
            LogicNodeKind.Pulse => EvaluatePulse(node, input),
            LogicNodeKind.DelayedPulse => EvaluateDelayedPulse(node, input),
            LogicNodeKind.Toggle => EvaluateToggle(node, input),
            _ => false
        };
    }

    private static bool EvaluateMemoryLatch(LogicNodeState node, LogicInputs input)
    {
        // Reset intentionally wins if both control inputs are high.
        if (input.Reset) node.StoredValue = false;
        else if (input.Set) node.StoredValue = true;
        else if (input.Enable) node.StoredValue = input.Data;
        return node.StoredValue;
    }

    private static bool EvaluateClock(LogicNodeState node, long simulationTick)
    {
        long phase = PositiveModulo(simulationTick + node.PhaseTicks, node.PeriodTicks);
        return phase < node.HighTicks;
    }

    private static bool EvaluateRisingEdge(LogicNodeState node, LogicInputs input)
    {
        bool result = input.Data && !node.PreviousInput;
        node.PreviousInput = input.Data;
        return result;
    }

    private static bool EvaluatePulse(LogicNodeState node, LogicInputs input)
    {
        if (input.Data && !node.PreviousInput)
        {
            node.PulseTicksRemaining = node.PulseWidthTicks;
        }

        node.PreviousInput = input.Data;
        return ConsumePulse(node);
    }

    private static bool EvaluateDelayedPulse(LogicNodeState node, LogicInputs input)
    {
        if (input.Data && !node.PreviousInput)
        {
            node.DelayTicksRemaining = node.DelayTicks;
        }

        node.PreviousInput = input.Data;
        if (node.DelayTicksRemaining >= 0)
        {
            if (node.DelayTicksRemaining == 0)
            {
                node.DelayTicksRemaining = -1;
                node.PulseTicksRemaining = node.PulseWidthTicks;
            }
            else
            {
                node.DelayTicksRemaining--;
            }
        }

        return ConsumePulse(node);
    }

    private static bool EvaluateToggle(LogicNodeState node, LogicInputs input)
    {
        if (input.Data && !node.PreviousInput)
        {
            node.StoredValue = !node.StoredValue;
        }

        node.PreviousInput = input.Data;
        return node.StoredValue;
    }

    private static bool ConsumePulse(LogicNodeState node)
    {
        if (node.PulseTicksRemaining <= 0) return false;
        node.PulseTicksRemaining--;
        return true;
    }

    private static long PositiveModulo(long value, long divisor)
    {
        long result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
    }
}
