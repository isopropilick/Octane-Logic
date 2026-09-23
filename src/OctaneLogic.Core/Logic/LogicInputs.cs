namespace OctaneLogic.Core.Logic;

/// <summary>
/// Normalized inputs for all Octane Logic nodes. Unused inputs are ignored by a node.
/// </summary>
public readonly struct LogicInputs
{
    public LogicInputs(bool data, bool enable = false, bool set = false, bool reset = false)
    {
        Data = data;
        Enable = enable;
        Set = set;
        Reset = reset;
    }

    public bool Data { get; }
    public bool Enable { get; }
    public bool Set { get; }
    public bool Reset { get; }
}
