using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EndingButton : MonoBehaviour, IInteractable
{
    public enum EndingType { Success, Fail }

    [Header("Type")]
    public bool autoDetectFromName = true;
    public EndingType type = EndingType.Success;

    [Header("Gating")]
    public bool requireTreatment = true;
    public bool requireGirlCalmed = true;

    [Header("Prompt")]
    public string successPrompt = "Press (Success)";
    public string failPrompt = "Press (Fail)";
    public string Prompt => (GetTypeResolved() == EndingType.Success) ? successPrompt : failPrompt;

    [Header("Ending Text")]
    [TextArea] public string successText = "SUCCESS: You escaped.";
    [TextArea] public string failText = "FAIL: You pressed the wrong button.";

    [Header("Locked Feedback")]
    public string toastNeedCalm = "Calm her down first.";
    public string toastNeedTreatment = "You are not in the Treatment Room.";

    void Awake()
    {
        if (!autoDetectFromName) return;

        // ✅ 自动判定：只要名字里包含 "fail"/"red" 就当失败；包含 "success"/"blue" 就当成功
        // 你可以按你自己的命名习惯扩展关键字
        var n = gameObject.name.ToLowerInvariant();

        if (n.Contains("fail") || n.Contains("red"))
            type = EndingType.Fail;
        else if (n.Contains("success") || n.Contains("blue"))
            type = EndingType.Success;
        // else: keep whatever you set in Inspector
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return false;
        if (gm.IsUiBlocking()) return false;

        // ✅ 为了避免“没反应”，这里永远允许点击，锁定逻辑放在 Interact 里 Toast
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return;

        if (requireTreatment && !gm.inTreatment)
        {
            gm.Toast(toastNeedTreatment, 2.0f);
            return;
        }

        if (requireGirlCalmed && !gm.girlCalmed)
        {
            gm.Toast(toastNeedCalm, 2.0f);
            return;
        }

        // stop scream timer if your ScreamTrigger has it
        ScreamTrigger.StopGlobalTimer();

        if (GetTypeResolved() == EndingType.Success)
            gm.End(successText);
        else
            gm.End(failText);
    }

    EndingType GetTypeResolved() => type;
}