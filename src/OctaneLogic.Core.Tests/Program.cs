using System;
using OctaneLogic.Core.Logic;
using OctaneLogic.Core.Persistence;

static class Program
{
    static int Main()
    {
        try
        {
            ClockIsDeterministic();
            LatchAndTogglePersistState();
            PulsesHaveTheConfiguredWidth();
            DelayedPulseWaitsBeforeEmitting();
            SaveDataRepairsInvalidValuesWithoutDroppingNodes();
            Console.WriteLine("Octane Logic core checks passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void ClockIsDeterministic()
    {
        var clock = new LogicNodeState { Kind = LogicNodeKind.Clock, PeriodTicks = 4, HighTicks = 2, PhaseTicks = 1 };
        Assert(LogicNodeEvaluator.Evaluate(clock, default, 0));
        Assert(!LogicNodeEvaluator.Evaluate(clock, default, 1));
        Assert(!LogicNodeEvaluator.Evaluate(clock, default, 2));
        Assert(LogicNodeEvaluator.Evaluate(clock, default, 3));
    }

    private static void LatchAndTogglePersistState()
    {
        var latch = new LogicNodeState { Kind = LogicNodeKind.MemoryLatch };
        Assert(LogicNodeEvaluator.Evaluate(latch, new LogicInputs(false, set: true), 0));
        Assert(LogicNodeEvaluator.Evaluate(latch, default, 1));
        Assert(!LogicNodeEvaluator.Evaluate(latch, new LogicInputs(false, reset: true), 2));

        var toggle = new LogicNodeState { Kind = LogicNodeKind.Toggle };
        Assert(LogicNodeEvaluator.Evaluate(toggle, new LogicInputs(true), 0));
        Assert(LogicNodeEvaluator.Evaluate(toggle, new LogicInputs(true), 1));
        Assert(!LogicNodeEvaluator.Evaluate(toggle, new LogicInputs(false), 2));
        Assert(!LogicNodeEvaluator.Evaluate(toggle, new LogicInputs(true), 3));
    }

    private static void PulsesHaveTheConfiguredWidth()
    {
        var pulse = new LogicNodeState { Kind = LogicNodeKind.Pulse, PulseWidthTicks = 2 };
        Assert(LogicNodeEvaluator.Evaluate(pulse, new LogicInputs(true), 0));
        Assert(LogicNodeEvaluator.Evaluate(pulse, new LogicInputs(false), 1));
        Assert(!LogicNodeEvaluator.Evaluate(pulse, default, 2));
    }

    private static void DelayedPulseWaitsBeforeEmitting()
    {
        var pulse = new LogicNodeState { Kind = LogicNodeKind.DelayedPulse, DelayTicks = 2, PulseWidthTicks = 1 };
        Assert(!LogicNodeEvaluator.Evaluate(pulse, new LogicInputs(true), 0));
        Assert(!LogicNodeEvaluator.Evaluate(pulse, new LogicInputs(false), 1));
        Assert(LogicNodeEvaluator.Evaluate(pulse, default, 2));
    }

    private static void SaveDataRepairsInvalidValuesWithoutDroppingNodes()
    {
        var data = new OctaneLogicSaveData
        {
            SchemaVersion = 0,
            Nodes = { new LogicNodeState { NodeId = "duplicate", PeriodTicks = 0 }, new LogicNodeState { NodeId = "duplicate" } }
        };
        data.Normalize();
        Assert(data.SchemaVersion == OctaneLogicSaveData.CurrentSchemaVersion);
        Assert(data.Nodes.Count == 2);
        Assert(data.Nodes[0].NodeId != data.Nodes[1].NodeId);
        Assert(data.Nodes[0].PeriodTicks == 1);
    }

    private static void Assert(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Core check failed.");
    }
}
