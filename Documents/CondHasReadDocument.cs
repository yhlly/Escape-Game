using UnityEngine;

public class CondHasReadDocument : PuzzleCondition
{
    public string documentId;
    public bool expected = true;

    public override bool Check(GameManager gm)
    {
        if (gm == null) return false;
        bool has = gm.HasReadDocument(documentId);
        return has == expected;
    }
}
