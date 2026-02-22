using System;
using UnityEngine;

/// <summary>
/// True if the last keypad entry matches the current GameManager.doorPassword.
/// Intended for DoorLock / keypad-driven elements.
/// </summary>
public class CondKeypadPassword : PuzzleCondition
{
    public override bool Check(GameManager gm)
    {
        if (gm == null) return false;
        string a = (gm.lastKeypadEntry ?? string.Empty).Trim();
        string b = (gm.doorPassword ?? string.Empty).Trim();
        return string.Equals(a, b, StringComparison.Ordinal);
    }
}
