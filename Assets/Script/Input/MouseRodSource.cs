using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drive one rod at a time with the mouse; Up/Down arrows pick which rod.
/// Rods you aren't holding stay where you left them.
///
///   Mouse X        slide
///   Mouse Y        spin (flick up to kick)
///   Scroll         spin
///   Click / Space  rod button (spawns a ball for that rod's team)
///   Tab            release / grab the cursor
/// </summary>
[DefaultExecutionOrder(-200)]
public class MouseRodSource : RodInputSource
{
    [Header("Feel")]
    [Tooltip("Slide change per pixel of horizontal mouse movement. Full travel is 2 (-1..1).")]
    public float slideSensitivity = 0.004f;

    [Tooltip("Degrees of spin per pixel of vertical mouse movement.")]
    public float rotationSensitivity = 0.8f;

    [Tooltip("Degrees of spin per unit of scroll.")]
    public float scrollSensitivity = 10f;

    public bool invertSlide = false;
    public bool invertRotation = false;

    [Header("Cursor")]
    [Tooltip("Lock and hide the cursor while driving a rod, so movement never hits the screen edge.")]
    public bool captureCursor = true;

    readonly Dictionary<int, RodState> states = new Dictionary<int, RodState>();
    int selectedIndex;
    bool isActive;
    bool waitForClickRelease;

    public override string DisplayName => "Mouse / keyboard";
    public override bool IsAvailable => Mouse.current != null;

    public bool CursorCaptured => !captureCursor || Cursor.lockState == CursorLockMode.Locked;

    public int SelectedRodId
    {
        get
        {
            var rods = RodInputHub.Rods;
            if (rods.Count == 0) return -1;
            return rods[Mod(selectedIndex, rods.Count)].rodId;
        }
    }

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

        var rods = RodInputHub.Rods;
        if (rods.Count == 0) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // ---- rod selection ----
        int previousId = SelectedRodId;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame) selectedIndex++;
            if (kb.downArrowKey.wasPressedThisFrame) selectedIndex--;
        }
        selectedIndex = Mod(selectedIndex, rods.Count);
        int id = rods[selectedIndex].rodId;

        // Release the button on a rod we just switched away from.
        if (previousId != id && previousId >= 0 && states.TryGetValue(previousId, out RodState prev))
        {
            prev.button = false;
            states[previousId] = prev;
        }

        if (!states.TryGetValue(id, out RodState s)) s = RodState.Neutral;
        s.button = false;

        if (mouse == null)
        {
            states[id] = s;
            return;
        }

        // ---- cursor capture ----
        if (captureCursor)
        {
            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked) ReleaseCursor();
                else CaptureCursor();
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                // First click only grabs the rod; it shouldn't also spawn a ball.
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    CaptureCursor();
                    waitForClickRelease = true;
                }
                states[id] = s;
                return;
            }
        }

        // ---- movement ----
        Vector2 delta = mouse.delta.ReadValue();
        float scroll = mouse.scroll.ReadValue().y;

        s.slide += delta.x * slideSensitivity * (invertSlide ? -1f : 1f);
        s.slide = Mathf.Clamp(s.slide, -1f, 1f);

        float spin = delta.y * rotationSensitivity + scroll * scrollSensitivity;
        s.angle = Mathf.DeltaAngle(0f, s.angle + spin * (invertRotation ? -1f : 1f));

        // ---- button ----
        if (waitForClickRelease && !mouse.leftButton.isPressed) waitForClickRelease = false;
        bool click = mouse.leftButton.isPressed && !waitForClickRelease;
        bool space = kb != null && kb.spaceKey.isPressed;
        s.button = click || space;

        states[id] = s;
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

    static int Mod(int a, int n) => ((a % n) + n) % n;
}
