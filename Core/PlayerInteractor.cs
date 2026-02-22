using UnityEngine;

[RequireComponent(typeof(FPSController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public float interactDistance = 3.2f;
    public LayerMask mask = ~0;

    [Header("Input")]
    [Tooltip("Use left mouse click to interact.")]
    public bool useMouseClick = true;

    [Tooltip("Keep legacy key E interaction if needed.")]
    public bool allowKeyE = true;

    FPSController _fps;

    void Awake()
    {
        _fps = GetComponent<FPSController>();
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || _fps == null || _fps.cameraTransform == null) return;

        // UI 打开时不允许点击世界物体（保持你原框架习惯）
        if (gm.IsUiBlocking())
        {
            gm.SetHint("");
            return;
        }

        // ✅ 核心：Ray 从“真实鼠标指针位置”发射
        Camera cam = _fps.cameraTransform.GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, mask, QueryTriggerInteraction.Ignore))
        {
            var it = hit.collider.GetComponentInParent<IInteractable>();
            if (it != null && it.CanInteract(this))
            {
                // 提示文字（不改你的交互手感，只提示当前能交互）
                gm.SetHint(it.Prompt);

                bool clicked = useMouseClick && Input.GetMouseButtonDown(0);
                bool pressedE = allowKeyE && Input.GetKeyDown(KeyCode.E);

                if (clicked || pressedE)
                {
                    it.Interact(this);
                }
                return;
            }
        }

        gm.SetHint("");
    }
}