using UnityEngine;

/// <summary>
/// Door lock migrated to Element + Condition + Action.
/// Keypad-driven (async). Opens the GameManager keypad, stores the entered code,
/// then uses Puzzle conditions/actions to branch into success/fail.
/// </summary>
public class DoorLock : PuzzleElement
{
    [Header("Door Open (Smooth)")]
    [Tooltip("Rotate this pivot to open the door, e.g. 'PatientRoom/DoorPivot'.")]
    public string doorPivotPath = "PatientRoom/DoorPivot";

    [Tooltip("Open angle around Y.")]
    public float openAngleY = 90f;

    [Tooltip("Open duration seconds.")]
    public float openDuration = 0.8f;

    [Tooltip("If door opens the wrong way, toggle this.")]
    public bool negativeDirection = false;

    void Awake()
    {
        prompt = "Enter password";
        requireAllConditions = true;
        allowInteractWhenConditionsFail = true; // wrong code should still run onFail

        // Condition evaluated AFTER keypad submit
        var cCode = EnsureCondition<CondKeypadPassword>();
        conditions = new PuzzleCondition[] { cCode };

        // ✅ Success: unlock flag + smooth open + toast (NO teleport, NO disable door)
        var aUnlock = EnsureActionSetFlag(GameFlag.DoorUnlocked, true);

        var aOpen = EnsureAction<ActOpenDoorSmoothByPath>();
        aOpen.pivotPath = doorPivotPath;
        aOpen.openAngleY = openAngleY;
        aOpen.duration = openDuration;
        aOpen.negativeDirection = negativeDirection;
        aOpen.useLocalRotation = true;

        var aToast = EnsureActionToast("Door unlocked.", 1.5f);

        onSuccess = new PuzzleAction[] { aUnlock, aOpen, aToast };

        // Fail: toast
        var aFail = EnsureActionToastFail("Wrong code.", 1.5f);
        onFail = new PuzzleAction[] { aFail };
    }

    public override bool CanInteract(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        return gm != null && !gm.doorUnlocked && !gm.IsUiBlocking();
    }

    public override void Interact(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return;

        // Clear previous entry so conditions are based solely on the new submission.
        gm.lastKeypadEntry = "";

        gm.OpenKeypad(code =>
        {
            gm.lastKeypadEntry = (code ?? string.Empty).Trim();

            // ✅ 你要的“输入面板关闭”：这里已经关了
            gm.CloseKeypad();

            // Run the condition/action pipeline.
            base.Interact(interactor);
        });
    }

    T EnsureCondition<T>() where T : PuzzleCondition
    {
        var c = GetComponent<T>();
        if (c != null) return c;
        return gameObject.AddComponent<T>();
    }

    T EnsureAction<T>() where T : PuzzleAction
    {
        var a = GetComponent<T>();
        if (a != null) return a;
        return gameObject.AddComponent<T>();
    }

    ActSetFlag EnsureActionSetFlag(GameFlag flag, bool value)
    {
        foreach (var a in GetComponents<ActSetFlag>())
        {
            if (a != null && a.flag == flag)
            {
                a.value = value;
                return a;
            }
        }
        var created = gameObject.AddComponent<ActSetFlag>();
        created.flag = flag;
        created.value = value;
        return created;
    }

    ActToast EnsureActionToast(string message, float duration)
    {
        foreach (var a in GetComponents<ActToast>())
        {
            if (a != null && a.message == message)
            {
                a.duration = duration;
                return a;
            }
        }
        var created = gameObject.AddComponent<ActToast>();
        created.message = message;
        created.duration = duration;
        return created;
    }

    ActToast EnsureActionToastFail(string message, float duration)
    {
        return EnsureActionToast(message, duration);
    }
}