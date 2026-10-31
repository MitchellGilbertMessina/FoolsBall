using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drive one rod at a time with the mouse, laid out for the top-down camera.
///
///   Mouse up/down     slide the rod (rods run up/down the screen)
///   Mouse left/right  spin: the figures' feet follow the mouse, so a quick
///                     sideways flick toward the opponent's goal is a kick
///   Left click        grab your back row (goalie)
///   Right click       grab your front row (strikers)
///   Up / Down         toggle back/front row
///   Left / Right      switch to the team on that side of the screen
///   Space             spawn a ball for the team you're holding
///   Tab               release / grab the cursor
///
/// Rods you aren't holding stay where you left them.
/// </summary>
[DefaultExecutionOrder(-200)]
public class MouseRodSource : RodInputSource
{
    [Header("Feel")]
    [Tooltip("Slide change per pixel of vertical mouse movement. Full travel is 2 (-1..1).")]
    public float slideSensitivity = 0.004f;

    [Tooltip("Degrees of spin per pixel of horizontal mouse movement.")]
    public float rotationSensitivity = 0.8f;

    [Tooltip("Degrees of spin per unit of scroll. 0 = off (mouse wheels feel notched; trackpads can be nice).")]
    public float scrollSensitivity = 0f;

    public bool invertSlide = false;
    [Tooltip("Flip if the feet move the opposite way to the mouse.")]
    public bool invertRotation = false;

    [Header("Screen layout")]
    [Tooltip("Which team plays on the left of the screen (Left arrow selects it).")]
    public FoosTeam leftTeam = FoosTeam.Blue;

    [Header("Cursor")]
    [Tooltip("Lock and hide the cursor while driving a rod, so movement never hits the screen edge.")]
    public bool captureCursor = true;

    readonly Dictionary<int, RodState> states = new Dictionary<int, RodState>();
    FoosTeam selectedTeam = FoosTeam.Blue;
    FoosRow selectedRow = FoosRow.Back;
    bool isActive;
    bool selectionInitialised;

    public override string DisplayName => "Mouse / keyboard";
    public override bool IsAvailable => Mouse.current != null;

    public bool CursorCaptured => !captureCursor || Cursor.lockState == CursorLockMode.Locked;

    public int SelectedRodId
    {
        get
        {
            var rod = FindSelectedRod();
            return rod != null ? rod.rodId : -1;
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

        if (!selectionInitialised)
        {
            selectedTeam = rods[0].team;
            selectedRow = rods[0].row;
            selectionInitialised = true;
        }

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        FoosballController previous = FindSelectedRod();

        // ---- selection ----
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
                selectedRow = selectedRow == FoosRow.Back ? FoosRow.Front : FoosRow.Back;
            if (kb.leftArrowKey.wasPressedThisFrame) selectedTeam = leftTeam;
            if (kb.rightArrowKey.wasPressedThisFrame) selectedTeam = Other(leftTeam);
        }
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame) selectedRow = FoosRow.Back;
            if (mouse.rightButton.wasPressedThisFrame) selectedRow = FoosRow.Front;
        }

        FoosballController rod = FindSelectedRod();
        if (rod == null) return;

        // Let go of the button on a rod we just switched away from.
        if (previous != null && previous != rod && states.TryGetValue(previous.rodId, out RodState prev))
        {
            prev.button = false;
            states[previous.rodId] = prev;
        }

        int id = rod.rodId;
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
                if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
                    CaptureCursor();
                states[id] = s;
                return;
            }
        }

        // ---- movement ----
        Vector2 delta = mouse.delta.ReadValue();
        float scroll = mouse.scroll.ReadValue().y;

        // Screen-up moves the rod toward +slide.
        s.slide += delta.y * slideSensitivity * (invertSlide ? -1f : 1f);
        s.slide = Mathf.Clamp(s.slide, -1f, 1f);

        // Feet follow the mouse horizontally on screen, for either team:
        // the rod's own rotationDirection is undone here so both sides feel the same.
        float spin = (-delta.x * rotationSensitivity + scroll * scrollSensitivity) * rod.rotationDirection;
        s.angle = Mathf.DeltaAngle(0f, s.angle + spin * (invertRotation ? -1f : 1f));

        // ---- button ----
        s.button = kb != null && kb.spaceKey.isPressed;

        states[id] = s;
    }

    FoosballController FindSelectedRod()
    {
        var rods = RodInputHub.Rods;
        FoosballController teamFallback = null;

        foreach (var r in rods)
        {
            if (r.team != selectedTeam) continue;
            if (r.row == selectedRow) return r;
            if (teamFallback == null) teamFallback = r;
        }

        if (teamFallback != null) return teamFallback;
        return rods.Count > 0 ? rods[0] : null;
    }

    static FoosTeam Other(FoosTeam t) => t == FoosTeam.Red ? FoosTeam.Blue : FoosTeam.Red;

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
