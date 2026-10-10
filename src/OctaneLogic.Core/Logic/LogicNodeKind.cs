namespace OctaneLogic.Core.Logic
{
    public enum LogicNodeKind
    {
        Buffer,
        Not,
        And,
        Or,
        Xor,
        Nand,
        Nor,
        Xnor,
        MemoryLatch,
        Clock,
        RisingEdge,
        FallingEdge,
        Pulse,
        DelayedPulse,
        Toggle
    }
}
