using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Single point gameplay asks for rod input. Picks which source is active
/// (Arduino rods or mouse/keyboard), keeps the list of rods in the scene,
/// and turns per-rod buttons into per-team "pressed this frame" events.
///
/// You don't have to put this in the scene: if none exists, one is created
/// automatically with default settings. Add it to a GameObject yourself when
/// you want to tweak the serial mapping or mouse feel in the Inspector.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RodInputHub : MonoBehaviour
{
    public enum Mode { Auto, Serial, MouseKeyboard }

    public static RodInputHub Instance { get; private set; }

    [Tooltip("Auto = Arduino rods when connected, otherwise mouse/keyboard. F1 cycles modes in play.")]
    public Mode mode = Mode.Auto;

    public SerialRodSource serialSource;
    public MouseRodSource mouseSource;

    [Header("HUD")]
    [Tooltip("On-screen input info. H toggles it in play.")]
    public bool showHud = true;

    // ---------------- ROD REGISTRY ----------------
    static readonly List<FoosballController> rods = new List<FoosballController>();
    public static IReadOnlyList<FoosballController> Rods => rods;

    public static void Register(FoosballController rod)
    {
        if (rods.Contains(rod)) return;

        foreach (var other in rods)
            if (other.rodId == rod.rodId)
                Debug.LogWarning($"Rods '{other.name}' and '{rod.name}' share rodId {rod.rodId}; they will move together.");

        rods.Add(rod);
        rods.Sort((a, b) => a.rodId.CompareTo(b.rodId));
    }

    public static void Unregister(FoosballController rod) => rods.Remove(rod);

    // ---------------- STATE ----------------
    RodInputSource active;
    readonly bool[] teamButton = new bool[2];
    readonly bool[] teamButtonPrev = new bool[2];
    GUIStyle hudStyle;

    public RodInputSource ActiveSource => active;

    // ---------------- BOOTSTRAP ----------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        rods.Clear();
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (FindAnyObjectByType<RodInputHub>() != null) return;
        new GameObject("RodInput (auto)").AddComponent<RodInputHub>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // (TryGetComponent rather than ??: Unity's fake-null objects don't work with ??)
        if (serialSource == null && !TryGetComponent(out serialSource))
            serialSource = gameObject.AddComponent<SerialRodSource>();
        if (mouseSource == null && !TryGetComponent(out mouseSource))
            mouseSource = gameObject.AddComponent<MouseRodSource>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------- UPDATE ----------------
    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f1Key.wasPressedThisFrame) mode = (Mode)(((int)mode + 1) % 3);
            if (kb.hKey.wasPressedThisFrame) showHud = !showHud;
        }

        RodInputSource next = ResolveSource();
        if (next != active)
        {
            if (active != null) active.SetActive(false);
            active = next;
            if (active != null) active.SetActive(true);
        }

        // Team buttons: any rod on a team pressing its button counts for that team.
        for (int t = 0; t < teamButton.Length; t++)
        {
            teamButtonPrev[t] = teamButton[t];
            teamButton[t] = false;
        }

        foreach (var rod in rods)
        {
            if (active != null && active.TryGetRod(rod.rodId, out RodState s) && s.button)
                teamButton[(int)rod.team] = true;
        }
    }

    RodInputSource ResolveSource()
    {
        switch (mode)
        {
            case Mode.Serial: return serialSource;
            case Mode.MouseKeyboard: return mouseSource;
            default:
                if (serialSource != null && serialSource.IsAvailable) return serialSource;
                return mouseSource;
        }
    }

    // ---------------- PUBLIC API ----------------

    public static RodState GetRod(int rodId)
    {
        var hub = Instance;
        if (hub != null && hub.active != null && hub.active.TryGetRod(rodId, out RodState s))
            return s;
        return RodState.Neutral;
    }

    /// <summary>True on the frame any rod of this team first presses its button.</summary>
    public static bool GetButtonDown(FoosTeam team)
    {
        var hub = Instance;
        if (hub == null) return false;
        int t = (int)team;
        return hub.teamButton[t] && !hub.teamButtonPrev[t];
    }

    // ---------------- HUD ----------------
    void OnGUI()
    {
        if (!showHud) return;

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                richText = true,
                padding = new RectOffset(10, 10, 8, 8)
            };
        }

        var sb = new StringBuilder();
        string activeName = active != null ? active.DisplayName : "none";
        sb.Append($"<b>Input:</b> {activeName}   <color=#aaaaaa>[{mode} · F1]</color>");

        if (active == mouseSource && mouseSource != null)
        {
            int selected = mouseSource.SelectedRodId;
            sb.Append("\n");
            foreach (var rod in rods)
            {
                bool isSel = rod.rodId == selected;
                string col = rod.team == FoosTeam.Red ? "#ff6b6b" : "#6bb7ff";
                sb.Append(isSel ? "\n<b>> " : "\n   ");
                sb.Append($"<color={col}>{rod.rodId}: {rod.name}</color>");
                if (isSel) sb.Append("</b>");
            }
            sb.Append("\n\n<color=#aaaaaa>Up/Down: rod · mouse X slide · mouse Y / scroll spin\nclick or space: ball · Tab: free cursor · Enter: power-up · H: hide</color>");
            if (!mouseSource.CursorCaptured)
                sb.Append("\n<color=#ffd166>Click the game view to grab the rod</color>");
        }

        var content = new GUIContent(sb.ToString());
        Vector2 size = hudStyle.CalcSize(content);
        GUI.Box(new Rect(10, 10, size.x, size.y), content, hudStyle);
    }
}
