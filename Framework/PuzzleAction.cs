using UnityEngine;

public abstract class PuzzleAction : MonoBehaviour
{
    /// <summary>
    /// Execute this action using the shared game manager.
    /// </summary>
    public abstract void Execute(GameManager gm);
}

