using UnityEngine;

/// <summary>
/// Legacy hiding spot migrated to Element + Condition + Action.
/// Keeps the original hard gate (must have fixed circuit and not already in treatment),
/// but uses the puzzle framework to branch between success (inChase) and fail (not inChase).
/// </summary>
public class HidingSpot : PuzzleElement
{
    void Awake()
    {
        prompt = "Enter closet (hide)";
        requireAllConditions = true;
        allowInteractWhenConditionsFail = true; // allow E to show fail toast when not in chase

        // Soft condition: inChase == true
        var cChase = EnsureCondFlag(GameFlag.InChase, expected: true);
        conditions = new PuzzleCondition[] { cChase };

        // Success actions (same as legacy)
        var aHidden = EnsureActionSetFlag(GameFlag.IsHidden, true);
        var aStopChase = EnsureActionSetFlag(GameFlag.InChase, false);
        var aInTreat = EnsureActionSetFlag(GameFlag.InTreatment, true);

        var aTp = EnsureAction<ActTeleport>();
        aTp.target = HE_TeleportTarget.TreatmentRoom;

        var aScream = EnsureActionSetFlag(GameFlag.Screaming, true);
        var aToast = EnsureActionToast("A piercing scream echoes!", 2.2f);

        onSuccess = new PuzzleAction[] { aHidden, aStopChase, aInTreat, aTp, aScream, aToast };

        // Fail action (original text)
        var aFailToast = EnsureActionToastFail("You feel unsafe. Fix circuit first to trigger the patrol.", 2f);
        onFail = new PuzzleAction[] { aFailToast };
    }

    // Hard gate preserved from legacy: only after circuit fixed, and before going to treatment.
    public override bool CanInteract(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return false;
        if (gm.IsUiBlocking()) return false;
        return gm.circuitFixed && !gm.inTreatment;
    }

    CondFlag EnsureCondFlag(GameFlag flag, bool expected)
    {
        foreach (var c in GetComponents<CondFlag>())
        {
            if (c != null && c.flag == flag && c.expected == expected && !c.invert)
                return c;
        }
        var created = gameObject.AddComponent<CondFlag>();
        created.flag = flag;
        created.expected = expected;
        created.invert = false;
        return created;
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
        // Prefer an existing ActToast with matching message (so we can have separate success/fail toasts if desired)
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
