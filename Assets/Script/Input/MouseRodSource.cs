using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drive one rod at a time with the mouse, laid out for the top-down camera.
///
///   Mouse up/down     slide the rod
///   Scroll            spin, with momentum: scroll up kicks toward the
///                     opponent's goal, a fast scroll spins hard and coasts
///   Left click        next rod, stepping left → right across the screen
///   Left / Right      previous / next rod
///   Space             spawn a ball for the team you're holding
///   Tab               release / grab the cursor
///
/// Rods you aren't holding stay where you left them (a spinning one coasts to a stop).
/// </summary>
[DefaultExecutionOrder(-200)]
public class MouseRodSource : RodInputSource
{
    [Header("Slide")]
    [Tooltip("Slide change per pixel of vertical mouse movement. Full travel is 2 (-1..1).")]
    public float slideSensitivity = 0.004f;
    public bool invertSlide = false;

    [Header("Spin (scroll)")]
    [Tooltip("Spin speed added per unit of scroll (degrees/second). One wheel notch ≈ 1 unit.")]
    public float scrollImpulse = 400f;
    [Tooltip("How quickly spin coasts to a stop. Higher = stops sooner, lower = more flywheel.")]
    public float spinDamping = 7f;
    [Tooltip("Cap on spin speed (degrees/second).")]
    public float maxSpinSpeed = 2000f;
    [Tooltip("Flip if scrolling up spins the wrong way (e.g. macOS natural scrolling).")]
    public bool invertScroll = false;

    [Header("Spin (mouse, optional)")]
    [Tooltip("Degrees of spin per pixel of horizontal mouse movement. 0 = off.")]
    public float rotationSensitivity = 0f;

    [Header("Screen layout")]
    [Tooltip("Which team plays on the left of the screen (it kicks toward the right).")]
    public FoosTeam leftTeam = FoosTeam.Blue;

    [Header("Cursor")]
    [Tooltip("Lock and hide the cursor while driving a rod, so movement never hits the screen edge.")]
    public bool captureCursor = true;

    readonly Dictionary<int, RodState> states = new Dictionary<int, RodState>();
    readonly Dictionary<int, float> spinVelocity = new Dictionary<int, float>();
    readonly List<FoosballController> screenOrder = new List<FoosballController>();
    int selectedRodId = int.MinValue;
    bool isActive;

    public override string DisplayName => "Mouse / keyboard";
    public override bool IsAvailable => Mouse.current != null;

    public bool CursorCaptured => !captureCursor || Cursor.lockState == CursorLockMode.Locked;
    public int SelectedRodId => selectedRodId;

    /// <summary>Rods in the order the mouse steps through them: left to right on screen.</summary>
    public IReadOnlyList<FoosballController> ScreenOrder => screenOrder;

    public override bool TryGetRod(int rodId, out RodState state)
    {
        // Rods we've never touched sit centred and upright.
        if (!states.TryGetValue(rodId, out state)) state = RodState.Neutral;
        return true;
    }

    public override void SetActive(bool active)
    {
        isActive = active;
        if (!active) ReleaseCursor();
    }

    void OnDisable() => ReleaseCursor();

    void Update()
    {
        if (!isActive) return;

        RebuildScreenOrder();
        if (screenOrder.Count == 0) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        int index = screenOrder.FindIndex(r => r.rodId == selectedRodId);
        if (index < 0) index = 0;

        // ---- cursor capture (first click only grabs; it doesn't switch rods) ----
        bool captured = true;
        if (captureCursor && mouse != null)
        {
            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked) ReleaseCursor();
                else CaptureCursor();
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                captured = false;
                if (mouse.leftButton.wasPressedThisFrame) CaptureCursor();
            }
        }

        // ---- selection ----
        if (captured && mouse != null && mouse.leftButton.wasPressedThisFrame) index++;
        if (kb != null && kb.rightArrowKey.wasPressedThisFrame) index++;
        if (kb != null && kb.leftArrowKey.wasPressedThisFrame) index--;
        index = ((index % screenOrder.Count) + screenOrder.Count) % screenOrder.Count;

        FoosballController rod = screenOrder[index];
        if (rod.rodId != selectedRodId)
        {
            // Let go of the button on the rod we're leaving.
            if (states.TryGetValue(selectedRodId, out RodState prev))
            {
                prev.button = false;
                states[selectedRodId] = prev;
            }
            selectedRodId = rod.rodId;
        }

        // ---- input for the held rod ----
        float spinInput = 0f;  // degrees/second added, in "toward the opponent" terms
        float spinDirect = 0f; // degrees, from optional mouse-X spin

        if (!states.TryGetValue(rod.rodId, out RodState s)) s = RodState.Neutral;
        s.button = false;

        if (captured && mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float scroll = mouse.scroll.ReadValue().y * (invertScroll ? -1f : 1f);

            s.slide += delta.y * slideSensitivity * (invertSlide ? -1f : 1f);
            s.slide = Mathf.Clamp(s.slide, -1f, 1f);

            spinInput = scroll * scrollImpulse;
            spinDirect = delta.x * rotationSensitivity * ForwardSign(rod); // in 'forward' terms, so mouse right = feet right

            s.button = kb != null && kb.spaceKey.isPressed;
        }
        states[rod.rodId] = s;

        // ---- spin momentum for every rod (so a released rod coasts) ----
        float dt = Time.deltaTime;
        foreach (var r in screenOrder)
        {
            spinVelocity.TryGetValue(r.rodId, out float v);
            if (r == rod) v += spinInput;
            v = Mathf.Clamp(v, -maxSpinSpeed, maxSpinSpeed);
            v *= Mathf.Exp(-spinDamping * dt);
            spinVelocity[r.rodId] = v;

            float forward = v * dt + (r == rod ? spinDirect : 0f);
            if (forward == 0f) continue;

            if (!states.TryGetValue(r.rodId, out RodState rs)) rs = RodState.Neutral;
            rs.angle = Mathf.DeltaAngle(0f, rs.angle + ToRodAngle(r, forward));
            states[r.rodId] = rs;
        }
    }

    /// <summary>+1 for the team on the left (kicks right), -1 for the team on the right.</summary>
    float ForwardSign(FoosballController rod) => rod.team == leftTeam ? 1f : -1f;

    /// <summary>
    /// Converts "degrees toward the opponent's goal" into the rod's own angle.
    /// With the top-down camera, a positive rod angle swings the feet toward
    /// screen-left; the rod's rotationDirection is undone so every rod matches.
    /// </summary>
    float ToRodAngle(FoosballController rod, float forwardDegrees)
    {
        float footRight = forwardDegrees * ForwardSign(rod); // + = feet move toward screen-right
        return -footRight * rod.rotationDirection;
    }

    void RebuildScreenOrder()
    {
        screenOrder.Clear();
        screenOrder.AddRange(RodInputHub.Rods);

        Camera cam = Camera.main;
        if (cam != null)
            screenOrder.Sort((a, b) =>
                cam.WorldToScreenPoint(a.transform.position).x.CompareTo(
                cam.WorldToScreenPoint(b.transform.position).x));
    }

    void CaptureCursor()
    {
        if (!captureCursor) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
