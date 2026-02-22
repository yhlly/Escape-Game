using UnityEngine;

/// <summary>
/// Removes an item from the inventory via GameManager.
/// </summary>
public class ActRemoveItem : PuzzleAction
{
    public string itemId = "Doll";

    public override void Execute(GameManager gm)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        gm.RemoveItem(itemId);
    }
}

