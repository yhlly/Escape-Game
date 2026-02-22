using UnityEngine;

/// <summary>
/// Attach to any scene item:
/// - Click to open document (image+text)
/// - Pick up inside document
/// - Prevent re-pickup if already in inventory
/// </summary>
public class InspectablePickupItem : PuzzleElement
{
    [Header("Item")]
    public string itemId = "Photo";
    public DocumentData document;
    public bool destroyOnPickup = true;

    void Awake()
    {
        prompt = $"Inspect {itemId}";
        requireAllConditions = true;
        allowInteractWhenConditionsFail = false;

        // Condition: !HasItem(itemId)
        var cNoItem = EnsureCondHasItem(itemId, shouldHave: false);
        conditions = new PuzzleCondition[] { cNoItem };

        // Action: show document with pickup button
        var a = EnsureAction<ActShowThenPickup>();
        a.document = document;
        a.itemId = itemId;
        a.destroyOnCollect = destroyOnPickup;

        onSuccess = new PuzzleAction[] { a };
        onFail = null;
    }

    void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(itemId))
            prompt = $"Inspect {itemId}";
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