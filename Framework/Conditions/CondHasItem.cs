using UnityEngine;

/// <summary>
/// Checks whether the player has (or does not have) a given item in inventory.
/// </summary>
public class CondHasItem : PuzzleCondition
{
    public string itemId = "Doll";
    public bool shouldHaveItem = true;

    public override bool Check(GameManager gm)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return true;
        bool has = gm.HasItem(itemId);
        return shouldHaveItem ? has : !has;
    }
}

