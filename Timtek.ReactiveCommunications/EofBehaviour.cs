namespace Timtek.ReactiveCommunications;

/// <summary>Options for specifying how to handle DataReceived event with type=EOF.</summary>
public enum EofBehaviour
{
    /// <summary>Legacy behaviour - treat EOF as end-of-stream (OnCompleted).</summary>
    Complete,

    /// <summary>New default behaviour: treat EOF as a communications error (OnError).</summary>
    Error
}