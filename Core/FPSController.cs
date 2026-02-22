using UnityEngine;

/// <summary>
/// 简单FPS控制器 - 按住鼠标左键控制视角
/// 
/// 核心逻辑：
/// - 按住鼠标左键：旋转视角
/// - 松开鼠标左键：视角固定，鼠标可以自由移动
/// - 光标始终可见，不锁定
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2.0f;
    public float pitchMin = -75f;
    public float pitchMax = 75f;

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float sprintMultiplier = 1.6f;
    public float gravity = -15f;

    [Header("Body")]
    public float controllerHeight = 1.8f;
    public float controllerRadius = 0.35f;
    public float cameraHeight = 1.6f;

    CharacterController _cc;
    float _yaw;
    float _pitch;
    float _vy;
    bool _frozen;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        _cc.height = controllerHeight;
        _cc.radius = controllerRadius;
        _cc.center = new Vector3(0f, controllerHeight * 0.5f, 0f);

        if (cameraTransform == null)
        {
            var cam = transform.Find("Main Camera");
            if (cam != null) cameraTransform = cam;
            else if (Camera.main != null) cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
            cameraTransform.localPosition = new Vector3(0f, cameraHeight, 0f);

        var rootCam = GetComponent<Camera>();
        if (rootCam != null) rootCam.enabled = false;

        // ✅ 光标始终可见，不锁定
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (_frozen) return;

        Look();
        Move();
    }

    void Look()
    {
        if (cameraTransform == null) return;

        // ✅ 核心：只有按住鼠标左键时才旋转视角
        if (Input.GetMouseButton(0)) // 0 = 鼠标左键
        {
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

            _yaw += mx;
            _pitch -= my;
            _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
        // 松开左键时，视角保持不动
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dir = (transform.right * h + transform.forward * v).normalized;
        float sp = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        if (_cc.isGrounded && _vy < 0f) _vy = -2f;
        _vy += gravity * Time.deltaTime;

        Vector3 vel = dir * sp + Vector3.up * _vy;
        _cc.Move(vel * Time.deltaTime);
    }

    public void SetFrozen(bool frozen)
    {
        _frozen = frozen;
    }

    public void TeleportTo(Vector3 pos, float yaw)
    {
        _cc.enabled = false;
        transform.position = pos;
        _cc.enabled = true;

        _yaw = yaw;
        _pitch = 0f;
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
        }
    }

    public void LockCursor(bool locked)
    {
        // 保持兼容性，但实际上始终不锁定
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}