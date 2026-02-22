using UnityEngine;

/// <summary>
/// Destroys the current GameObject. Useful for pickups.
/// </summary>
public class ActDestroySelf : PuzzleAction
{
    public override void Execute(GameManager gm)
    {
        Destroy(gameObject);
    }
}

