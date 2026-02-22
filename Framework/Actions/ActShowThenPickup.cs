using UnityEngine;

/// <summary>
/// Click item -> show document -> player clicks "Pick up" in document UI -> add to inventory.
/// </summary>
public class ActShowThenPickup : PuzzleAction
{
    [Header("Show")]
    public DocumentData document;

    [Header("Collect")]
    public string itemId = "Photo";
    public bool destroyOnCollect = true;

    [Header("Safety")]
    public bool blockWhileOpen = true;

    public override void Execute(GameManager gm)
    {
        if (gm == null)
        {
            Debug.LogError("[ActShowThenPickup] GameManager is null.");
            return;
        }

        if (blockWhileOpen && gm.documentOpen) return;

        if (document == null)
        {
            Debug.LogError("[ActShowThenPickup] document is null.");
            return;
        }

        gm.OpenDocumentForPickup(document, itemId, gameObject, destroyOnCollect);
    }
}