using UnityEngine;

/// <summary>
/// Legacy puzzle migrated to Element + Condition + Action.
/// Scene keeps the ElectricalBox component; it now configures itself as a PuzzleElement.
/// </summary>
public class ElectricalBox : PuzzleElement
{
    void Awake()
    {
        prompt = "Connect circuit";
        requireAllConditions = true;
        allowInteractWhenConditionsFail = false; // don't allow interaction once conditions fail (e.g. already fixed)

        // Conditions: doorUnlocked == true AND circuitFixed == false
        var cDoor = EnsureCondFlag(GameFlag.DoorUnlocked, expected: true);
        var cNotFixed = EnsureCondFlag(GameFlag.CircuitFixed, expected: false);
        conditions = new PuzzleCondition[] { cDoor, cNotFixed };

        // Actions on success
        var aFix = EnsureActionSetFlag(GameFlag.CircuitFixed, true);
        var aToast = EnsureActionToast("Circuit connected.", 1.6f);
        var aChase = EnsureAction<ActStartChase>();
        onSuccess = new PuzzleAction[] { aFix, aToast, aChase };
        onFail = null;
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
        var a = EnsureAction<ActToast>();
        a.message = message;
        a.duration = duration;
        return a;
    }
}
