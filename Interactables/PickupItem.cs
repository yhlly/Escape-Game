using UnityEngine;

/// <summary>
/// Legacy pickup migrated to Element + Condition + Action.
/// Uses disable (SetActive false) instead of Destroy so GameManager.Restart can restore it.
/// </summary>
public class PickupItem : PuzzleElement
{
    public string itemId = "Doll";

    void Awake()
    {
        prompt = $"Pick up {itemId}";
        requireAllConditions = true;
        allowInteractWhenConditionsFail = false;

        // Conditions: doorUnlocked == true AND !HasItem(itemId)
        var cDoor = EnsureCondFlag(GameFlag.DoorUnlocked, expected: true);
        var cNoItem = EnsureCondHasItem(itemId, shouldHave: false);
        conditions = new PuzzleCondition[] { cDoor, cNoItem };

        // Actions: AddItem + disable self
        var aAdd = EnsureAction<ActAddItem>();
        aAdd.itemId = itemId;

        var aDisable = EnsureAction<ActSetActiveSelf>();
        aDisable.active = false;

        onSuccess = new PuzzleAction[] { aAdd, aDisable };
        onFail = null;
    }

    void OnValidate()
    {
        // Keep prompt synced in editor.
        prompt = $"Pick up {itemId}";
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

    CondHasItem EnsureCondHasItem(string id, bool shouldHave)
    {
        foreach (var c in GetComponents<CondHasItem>())
        {
            if (c != null && string.Equals(c.itemId, id) && c.shouldHaveItem == shouldHave)
                return c;
        }
        var created = gameObject.AddComponent<CondHasItem>();
        created.itemId = id;
        created.shouldHaveItem = shouldHave;
        return created;
    }

    T EnsureAction<T>() where T : PuzzleAction
    {
        var a = GetComponent<T>();
        if (a != null) return a;
        return gameObject.AddComponent<T>();
    }
}
