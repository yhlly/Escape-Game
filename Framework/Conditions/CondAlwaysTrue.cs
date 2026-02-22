using UnityEngine;

/// <summary>
/// Condition that always returns true. Useful as a default.
/// </summary>
public class CondAlwaysTrue : PuzzleCondition
{
    public override bool Check(GameManager gm) => true;
}

