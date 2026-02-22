using UnityEngine;

/// <summary>
/// Shows a short toast message via GameManager.
/// </summary>
public class ActToast : PuzzleAction
{
    [TextArea]
    public string message = "Toast";
    public float duration = 2f;

    public override void Execute(GameManager gm)
    {
        if (string.IsNullOrEmpty(message)) return;
        gm.Toast(message, duration);
    }
}

