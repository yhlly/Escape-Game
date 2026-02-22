using UnityEngine;

/// <summary>
/// Adds an item to the inventory via GameManager.
/// </summary>
public class ActAddItem : PuzzleAction
{
    public string itemId = "Doll";

    public override void Execute(GameManager gm)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        gm.AddItem(itemId);
    }
}

