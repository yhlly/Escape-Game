using UnityEngine;

public class ElevatorPanelInteractable : MonoBehaviour, IInteractable
{
    [Header("Controller")]
    public ElevatorPanelController controller;

    [Header("Prompts")]
    [SerializeField] string promptLocked = "Fix the circuit first";
    [SerializeField] string promptToggle = "Toggle panel";
    public string Prompt => (CanUse() ? promptToggle : promptLocked);

    [Header("Auto Open")]
    public bool autoOpenWhenCircuitFixed = true;

    [Header("Toast")]
    public string toastLocked = "Please fix the circuit first.";
    public float toastLockedDuration = 2.0f;

    bool _autoOpenedOnce = false;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<ElevatorPanelController>();
    }

    void Update()
    {
        if (!autoOpenWhenCircuitFixed) return;

        var gm = GameManager.I;
        if (gm == null) return;

        // ✅ ElectricalBox 成功后会把 gm.circuitFixed 设为 true
        if (gm.circuitFixed && !_autoOpenedOnce)
        {
            _autoOpenedOnce = true;
            if (controller != null && !controller.IsOpen)
                controller.Open();
        }
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return false;
        if (gm.IsUiBlocking()) return false;
        return true; // 允许没修好也能点，用于提示
    }

    public void Interact(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return;

        if (!gm.circuitFixed)
        {
            gm.Toast(toastLocked, toastLockedDuration);
            return;
        }

        if (controller == null)
        {
            Debug.LogError("[ElevatorPanelInteractable] controller is null.");
            return;
        }

        // ✅ 修好电路后：再次点击就关上；再点再开
        controller.Toggle();
    }

    bool CanUse()
    {
        var gm = GameManager.I;
        return gm != null && gm.circuitFixed;
    }
}