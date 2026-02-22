using UnityEngine;

public class PuzzleElement : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] protected string prompt = "Interact";

    [Header("Conditions")]
    [Tooltip("If true: all conditions must pass. If false: any one condition passing is enough.")]
    public bool requireAllConditions = true;

    [SerializeField] protected PuzzleCondition[] conditions;

    [Header("Actions")]
    [SerializeField] protected PuzzleAction[] onSuccess;
    [SerializeField] protected PuzzleAction[] onFail;

    [Header("Interaction")]
    [Tooltip("If true, player can still press E when conditions fail, and onFail actions will run.")]
    public bool allowInteractWhenConditionsFail = true;

    public string Prompt => prompt;

    // 关键：给子类 override 用
    protected virtual void Awake()
    {
        // Auto-fill arrays if not manually assigned.
        if (conditions == null || conditions.Length == 0)
            conditions = GetComponents<PuzzleCondition>();

        // 注意：onSuccess 如果你不在 Inspector 手动配，也可以自动抓取
        // 但很多项目里 success/fail 都需要你明确配置，这里只在为空时抓取自身上的 PuzzleAction。
        if (onSuccess == null || onSuccess.Length == 0)
            onSuccess = GetComponents<PuzzleAction>();

        // onFail 不自动抓（避免把成功动作也抓进去导致两边都执行）
    }

    public virtual bool CanInteract(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return false;
        if (gm.IsUiBlocking()) return false;

        // 允许条件失败也能交互：用于 DoorLock 错码提示、Girl 没道具提示等
        if (allowInteractWhenConditionsFail) return true;

        return EvaluateConditions(gm);
    }

    public virtual void Interact(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return;

        bool ok = EvaluateConditions(gm);
        if (ok) ExecuteActions(onSuccess, gm);
        else ExecuteActions(onFail, gm);
    }

    protected bool EvaluateConditions(GameManager gm)
    {
        // 没有条件：默认通过
        if (conditions == null || conditions.Length == 0)
            return true;

        if (requireAllConditions)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                var c = conditions[i];
                if (c == null) continue;
                if (!c.Check(gm)) return false;
            }
            return true;
        }
        else
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                var c = conditions[i];
                if (c == null) continue;
                if (c.Check(gm)) return true;
            }
            return false;
        }
    }

    protected void ExecuteActions(PuzzleAction[] actions, GameManager gm)
    {
        if (actions == null || actions.Length == 0) return;

        for (int i = 0; i < actions.Length; i++)
        {
            var a = actions[i];
            if (a == null) continue;

            try
            {
                a.Execute(gm);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PuzzleElement] Action threw exception on '{name}': {a.GetType().Name}\n{e}");
            }
        }
    }

#if UNITY_EDITOR
    // 关键：给子类 override 用
    protected virtual void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(prompt))
            prompt = "Interact";
    }
#endif
}
