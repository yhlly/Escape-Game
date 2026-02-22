using UnityEngine;

/// <summary>
/// Starts the doctor patrol countdown using GameManager.StartChaseTimer().
/// </summary>
public class ActStartChase : PuzzleAction
{
    public override void Execute(GameManager gm)
    {
        gm.StartChaseTimer();
    }
}

