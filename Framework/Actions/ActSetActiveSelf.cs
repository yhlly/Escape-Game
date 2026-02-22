using UnityEngine;

/// <summary>
/// Sets this GameObject's active state.
/// Prefer this over Destroy for pickups so GameManager.Restart can re-enable the object.
/// </summary>
public class ActSetActiveSelf : PuzzleAction
{
    public bool active = false;

    public override void Execute(GameManager gm)
    {
        gameObject.SetActive(active);
    }
}
