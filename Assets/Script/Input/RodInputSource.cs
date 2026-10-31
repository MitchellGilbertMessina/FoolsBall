using UnityEngine;

/// <summary>
/// Base class for anything that can drive rods. Sources update their state in their
/// own Update (they run early via DefaultExecutionOrder) and answer per-rod queries.
/// </summary>
public abstract class RodInputSource : MonoBehaviour
{
    public abstract string DisplayName { get; }

    /// <summary>True when this source can currently provide data (e.g. Arduino connected).</summary>
    public abstract bool IsAvailable { get; }

    /// <summary>Returns false if this source has nothing for that rod.</summary>
    public abstract bool TryGetRod(int rodId, out RodState state);

    /// <summary>Called by the hub when this source becomes / stops being the active one.</summary>
    public virtual void SetActive(bool active) { }
}
