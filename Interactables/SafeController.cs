using UnityEngine;

public class SafeController : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private string prompt = "Use Safe";
    public string Prompt => prompt;

    [Header("Unlock Type")]
    public bool usePassword = true;
    public string correctPassword = "0420";

    public bool requireQuestionnairePassed = false; // 可选：问卷通过才能用
    public string needQuestionnaireToast = "Please complete the questionnaire first.";

    [Header("Animator")]
    public Animator animator;
    public string openTriggerName = "Open"; // Animator trigger 参数名

    [Header("Loot (reveal)")]
    public GameObject lootRoot;                 // 保险箱内部物品父物体
    public bool revealLootOnOpen = true;

    [Header("State")]
    public bool unlocked = false;
    public bool opened = false;

    public bool CanInteract(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return false;
        if (gm.IsUiBlocking()) return false;
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return;

        // 可选：问卷门槛
        if (requireQuestionnairePassed && !gm.questionnairePassed)
        {
            gm.Toast(needQuestionnaireToast, 2.0f);
            return;
        }

        if (opened)
        {
            gm.Toast("It's already open.", 1.5f);
            return;
        }

        // 已解锁但还没开门
        if (unlocked)
        {
            Open(gm);
            return;
        }

        // 密码解锁（用你现成的 Keypad UI）
        if (usePassword)
        {
            gm.lastKeypadEntry = "";
            gm.OpenKeypad(code =>
            {
                gm.lastKeypadEntry = (code ?? "").Trim();

                // ✅ 你要的：输入结束先关面板
                gm.CloseKeypad();

                if (gm.lastKeypadEntry == correctPassword)
                {
                    unlocked = true;
                    gm.Toast("Unlocked.", 1.2f);
                    Open(gm);
                }
                else
                {
                    gm.Toast("Wrong code.", 1.5f);
                }
            });

            return;
        }

        gm.Toast("Nothing happens.", 1.5f);
    }

    void Open(GameManager gm)
    {
        if (opened) return;
        opened = true;

        // 播放开门动画
        if (animator != null && !string.IsNullOrWhiteSpace(openTriggerName))
        {
            animator.ResetTrigger(openTriggerName);
            animator.SetTrigger(openTriggerName);
        }

        // 展示箱内物品
        if (revealLootOnOpen && lootRoot != null)
            lootRoot.SetActive(true);
    }
}