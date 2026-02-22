using UnityEngine;

/// <summary>
/// Checks a boolean game flag stored in GameManager.
/// </summary>
public class CondFlag : PuzzleCondition
{
    public GameFlag flag;
    public bool expected = true;
    public bool invert;

    public override bool Check(GameManager gm)
    {
        if (gm == null) return false;

        bool value = false;
        switch (flag)
        {
            case GameFlag.DoorUnlocked: value = gm.doorUnlocked; break;
            case GameFlag.CircuitFixed: value = gm.circuitFixed; break;
            case GameFlag.InChase: value = gm.inChase; break;
            case GameFlag.IsHidden: value = gm.isHidden; break;
            case GameFlag.InTreatment: value = gm.inTreatment; break;
            case GameFlag.Screaming: value = gm.screaming; break;
            case GameFlag.GirlCalmed: value = gm.girlCalmed; break;

            case GameFlag.QuestionnairePassed: value = gm.questionnairePassed; break; // ✅ 新增
        }

        bool result = (value == expected);
        if (invert) result = !result;
        return result;
    }
}