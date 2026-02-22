using UnityEngine;

public abstract class PuzzleCondition : MonoBehaviour
{
    /// <summary>
    /// Return true if this condition is satisfied in the current game state.
    /// </summary>
    public abstract bool Check(GameManager gm);
}

