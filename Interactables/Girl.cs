using UnityEngine;

/// <summary>
/// Treatment-room girl interaction migrated to Element + Condition + Action.
/// - Hard gate: only in treatment, not already calmed.
/// - Success requires having the calming item (default: GirlRelevant) and sets GirlCalmed.
/// - Fail shows a hint toast.
/// </summary>
public class Girl : PuzzleElement
{
    // The inventory item required to calm the girl.
    // You said the Doll is removed; use the PatientRoom item instead.
    public string requiredItem = "GirlRelevant";

    void Awake()
    {
        prompt = "Calm the girl";
        requireAllConditions = true;
        allowInteractWhenConditionsFail = true; // allow interaction to show fail hint when missing item

        var cHas = EnsureCondition<CondHasItem>();
        cHas.itemId = requiredItem;
        cHas.shouldHaveItem = true;
        conditions = new PuzzleCondition[] { cHas };

        // Do NOT consume the item so the player can still View it later in inventory.
        var aStopScream = EnsureActionSetFlag(GameFlag.Screaming, false);
        var aFlag = EnsureActionSetFlag(GameFlag.GirlCalmed, true);
        var aToast = EnsureActionToast("She focuses on the object and slowly calms down...", 2.2f);

        onSuccess = new PuzzleAction[] { aStopScream, aFlag, aToast };

        var aFail = EnsureActionToastFail("You need something to calm her down.", 2f);
        onFail = new PuzzleAction[] { aFail };
    }

    public override bool CanInteract(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return false;
        if (gm.IsUiBlocking()) return false;

        // Only meaningful in treatment, and only before calmed.
        return gm.inTreatment && !gm.girlCalmed;
    }

    T EnsureCondition<T>() where T : PuzzleCondition
    {
        var c = GetComponent<T>();
        if (c != null) return c;
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