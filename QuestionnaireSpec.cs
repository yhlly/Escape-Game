using UnityEngine;

[CreateAssetMenu(menuName = "HospitalEscape/Questionnaire Spec")]
public class QuestionnaireSpec : ScriptableObject
{
    [Header("Correct Answers (simple)")]
    public string expectedName = "L. Zhang";
    public int expectedAge = 19;
    public string expectedWard = "302";

    [Header("Rules")]
    public float timeLimitSeconds = 20f; // 10~20 秒都可以
    public bool requireWard = true;
}
