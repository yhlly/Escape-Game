using UnityEngine;

/// <summary>
/// Finds a GameObject by scene path (e.g. "PatientRoom/Door") and sets its active state.
/// Useful to replace legacy hard-coded SetActive calls.
/// </summary>
public class ActSetActiveByPath : PuzzleAction
{
    [Tooltip("Scene path for GameObject.Find, e.g. PatientRoom/Door")]
    public string path = "";

    public bool active = true;

    public override void Execute(GameManager gm)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var go = GameObject.Find(path.Trim());
        if (go != null) go.SetActive(active);
    }
}
