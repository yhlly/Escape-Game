using UnityEngine;

/// <summary>
/// Shows an ending text via GameManager.End().
/// </summary>
public class ActShowEnding : PuzzleAction
{
    [TextArea]
    public string endingText = "ENDING";

    public override void Execute(GameManager gm)
    {
        gm.End(endingText);
    }
}

