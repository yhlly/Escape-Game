using UnityEngine;

/// <summary>
/// PuzzleAction: Show a story overlay sequence (reusable narrative beats).
/// Attach to a PuzzleElement and wire via your Condition/Action framework.
/// </summary>
public class ActShowStorySequence : PuzzleAction
{
    public StorySequenceData sequence;

    public override void Execute(GameManager gm)
    {
        if (gm == null || sequence == null) return;
        gm.PlayStory(sequence);
    }
}
