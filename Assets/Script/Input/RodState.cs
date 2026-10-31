public enum FoosTeam { Red = 0, Blue = 1 }

/// <summary>
/// Hardware-independent snapshot of one rod. Every input source (serial rods,
/// mouse/keyboard, future network players) produces these; gameplay only reads these.
/// </summary>
public struct RodState
{
    /// <summary>Position along the rod's travel, -1 (one end) to +1 (other end).</summary>
    public float slide;

    /// <summary>Rod angle in degrees, -180..180. 0 = figure hanging upright.</summary>
    public float angle;

    /// <summary>The rod's button (currently: spawn a ball for this rod's team).</summary>
    public bool button;

    public static readonly RodState Neutral = new RodState();
}
