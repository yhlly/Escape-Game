using UnityEngine;

public class ActShowDocument : PuzzleAction
{
    public DocumentData document;
    public bool markReadOnOpen = true;

    public override void Execute(GameManager gm)
    {
        if (gm == null || document == null) return;

        gm.OpenDocument(document);

        if (markReadOnOpen && !string.IsNullOrWhiteSpace(document.id))
            gm.MarkDocumentRead(document.id);
    }
}
