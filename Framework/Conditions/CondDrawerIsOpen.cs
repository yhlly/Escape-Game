using UnityEngine;

/// <summary>
/// Condition: true only if the referenced drawer is open.
/// Low coupling: does not require GameFlag / GameManager changes.
/// </summary>
public class CondDrawerIsOpen : PuzzleCondition
{
    public DrawerController drawer;
    public bool expectedOpen = true;

    public override bool Check(GameManager gm)
    {
        if (drawer == null)
            drawer = GetComponentInParent<DrawerController>();

        if (drawer == null) return false;
        return drawer.IsOpen == expectedOpen;
    }
}
