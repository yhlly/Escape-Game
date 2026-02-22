using UnityEngine;

public enum HE_TeleportTarget
{
    PatientRoom,
    Office,
    TreatmentRoom
}

/// <summary>
/// Teleports the player to a room using GameManager teleport helpers.
/// </summary>
public class ActTeleport : PuzzleAction
{
    public HE_TeleportTarget target;

    public override void Execute(GameManager gm)
    {
        switch (target)
        {
            case HE_TeleportTarget.PatientRoom:
                gm.TeleportToPatient();
                break;
            case HE_TeleportTarget.Office:
                gm.TeleportToOffice();
                break;
            case HE_TeleportTarget.TreatmentRoom:
                gm.TeleportToTreatment();
                break;
        }
    }
}

