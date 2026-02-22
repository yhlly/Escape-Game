using UnityEngine;

public class ActOpenDoorSmooth : PuzzleAction
{
    public DoorSwing door;     // 拖门
    public bool alsoUnlock = true;

    public override void Execute(GameManager gm)
    {
        if (door == null)
        {
            Debug.LogError("[ActOpenDoorSmooth] door is null.");
            return;
        }

        if (alsoUnlock) door.Unlock();
        door.Open();
    }
}