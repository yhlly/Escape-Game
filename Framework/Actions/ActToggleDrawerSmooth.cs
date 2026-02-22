using UnityEngine;

/// <summary>
/// PuzzleAction: toggles a DrawerController.
/// Attach this to the same object as PuzzleElement (Drawer front).
/// </summary>
public class ActToggleDrawerSmooth : PuzzleAction
{
    public DrawerController drawer; // assign, or auto-find on same object

    public override void Execute(GameManager gm)
    {
        if (drawer == null)
            drawer = GetComponent<DrawerController>();

        if (drawer == null)
        {
            Debug.LogError("[ActToggleDrawerSmooth] No DrawerController found.");
            return;
        }

        drawer.Toggle();
    }
}
