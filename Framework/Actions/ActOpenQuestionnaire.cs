using UnityEngine;

public class ActOpenQuestionnaire : PuzzleAction
{
    public QuestionnairePanel panel;

    public override void Execute(GameManager gm)
    {
        if (panel == null)
        {
            Debug.LogError("[ActOpenQuestionnaire] panel is null.");
            return;
        }

        // find FPSController in scene (your player)
        var fps = FindObjectOfType<FPSController>();

        panel.Open(gm, fps);
    }
}
