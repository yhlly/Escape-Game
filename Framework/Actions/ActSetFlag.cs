using UnityEngine;

/// <summary>
/// Sets a boolean flag in GameManager.
/// </summary>
public class ActSetFlag : PuzzleAction
{
    public GameFlag flag;
    public bool value = true;

    public override void Execute(GameManager gm)
    {
        if (gm == null) return;

        switch (flag)
        {
            case GameFlag.DoorUnlocked: gm.doorUnlocked = value; break;
            case GameFlag.CircuitFixed: gm.circuitFixed = value; break;
            case GameFlag.InChase: gm.inChase = value; break;
            case GameFlag.IsHidden: gm.isHidden = value; break;
            case GameFlag.InTreatment: gm.inTreatment = value; break;
            case GameFlag.Screaming: gm.screaming = value; break;
            case GameFlag.GirlCalmed: gm.girlCalmed = value; break;

            case GameFlag.QuestionnairePassed: gm.questionnairePassed = value; break; // ✅ 新增
        }
    }
}