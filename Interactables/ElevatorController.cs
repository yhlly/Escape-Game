using System.Collections;
using UnityEngine;

/// <summary>
/// Elevator minimal flow:
/// - After circuitFixed becomes true: door auto opens once (optional)
/// - Remote click / panel click triggers:
///   - If door closed -> open
///   - If door open and player on platform -> close -> move platform (carry player) -> open
/// - IMPORTANT:
///   - While chase is active, standing on platform immediately stops chase (inChase=false) and hides player.
///   - Platform detection uses a downward Raycast from CharacterController foot area (robust; not camera-based).
/// </summary>
public class ElevatorController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The moving door object (your Wall that carries the Panel).")]
    public Transform door; // wall + panel together

    [Tooltip("The moving platform/floor that carries the player.")]
    public Transform platform; // floor plate (prefer the root that contains the standing collider)

    [Tooltip("Player root transform (recommend: object with FPSController + CharacterController).")]
    public Transform playerRoot;

    [Header("Door Poses (LOCAL)")]
    public Vector3 doorClosedLocalPos;
    public Vector3 doorClosedLocalEuler;
    public Vector3 doorOpenLocalPos;
    public Vector3 doorOpenLocalEuler;

    [Header("Platform Stops (WORLD)")]
    public Transform topStop;
    public Transform bottomStop;

    [Header("Timings")]
    public float doorDuration = 0.6f;
    public float travelDuration = 2.5f;
    public float settleDelay = 0.1f;

    [Header("Rules")]
    public bool autoOpenDoorWhenCircuitFixed = true;

    [Tooltip("Require player to be on platform to travel (recommended true).")]
    public bool requirePlayerOnPlatform = true;

    [Tooltip("If false, ElevatorPanelClickRelay will not trigger this elevator.")]
    public bool remoteClickEnabled = true;

    [Header("Chase Stop On Platform")]
    [Tooltip("If true: when inChase and player stands on platform, immediately stop chase and mark hidden.")]
    public bool stopChaseWhenOnPlatform = true;

    [Header("Raycast Standing Check")]
    [Tooltip("How high above foot point to start the ray (avoid starting inside floor).")]
    public float rayStartUp = 0.2f;

    [Tooltip("How far downward to raycast from the foot-start point.")]
    public float rayDownDistance = 1.2f;

    [Tooltip("Raycast layer mask (default = everything).")]
    public LayerMask groundMask = ~0;

    [Tooltip("If your platform standing collider is Trigger, set this true (otherwise keep false).")]
    public bool includeTriggers = false;

    [Header("UX Toasts")]
    public string toastNeedCircuit = "Please connect the circuit first.";
    public string toastNeedStandOnPlatform = "Stand on the elevator platform first.";

    bool _autoOpenedOnce = false;
    bool _isDoorOpen = false;
    bool _isMoving = false;
    bool _atTop = true;

    Coroutine _doorCo;

    void Start()
    {
        ResolvePlayer();
        InferStartFloor();
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null) return;

        // ✅ Stand on platform -> stop chase immediately
        if (stopChaseWhenOnPlatform && gm.inChase && IsPlayerOnPlatform())
        {
            gm.isHidden = true;
            gm.inChase = false;
        }

        // ✅ Circuit fixed -> auto open door once
        if (autoOpenDoorWhenCircuitFixed && gm.circuitFixed && !_autoOpenedOnce)
        {
            _autoOpenedOnce = true;
            OpenDoor();
        }
    }

    void ResolvePlayer()
    {
        if (playerRoot != null) return;

        var fps = FindObjectOfType<FPSController>();
        if (fps != null)
        {
            playerRoot = fps.transform;
            return;
        }

        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            playerRoot = p.transform;
            return;
        }

        // IMPORTANT: do NOT fallback to Camera.main.transform for standing checks
    }

    void InferStartFloor()
    {
        if (platform == null || topStop == null || bottomStop == null) return;

        float dt = Vector3.Distance(platform.position, topStop.position);
        float db = Vector3.Distance(platform.position, bottomStop.position);
        _atTop = dt <= db;
    }

    /// <summary>
    /// Call this from your panel click / relay click.
    /// </summary>
    public void OnPanelTriggered()
    {
        var gm = GameManager.I;
        if (gm == null) return;

        if (_isMoving) return;

        if (!gm.circuitFixed)
        {
            gm.Toast(toastNeedCircuit, 2.0f);
            return;
        }

        // Door closed -> open
        if (!_isDoorOpen)
        {
            OpenDoor();
            return;
        }

        // Door open -> request travel
        if (requirePlayerOnPlatform && !IsPlayerOnPlatform())
        {
            gm.Toast(toastNeedStandOnPlatform, 2.0f);
            return;
        }

        bool goDown = _atTop;
        StartCoroutine(CoTravel(goDown));
    }

    public void OnTravelButtonPressed()
    {
        var gm = GameManager.I;
        if (gm == null) return;

        if (_isMoving) return;

        if (!gm.circuitFixed)
        {
            gm.Toast(toastNeedCircuit, 2.0f);
            return;
        }

        // ✅ 直接尝试“关门+移动”，不再做“门关了就先开门”的逻辑
        if (requirePlayerOnPlatform && !IsPlayerOnPlatform())
        {
            gm.Toast(toastNeedStandOnPlatform, 2.0f);
            return;
        }

        bool goDown = _atTop;
        StartCoroutine(CoTravel(goDown));
    }

    /// <summary>
    /// Robust standing detection:
    /// Raycast downward from CharacterController foot area.
    /// Returns true if the first hit collider belongs to platform (platform itself or its child/parent).
    /// </summary>
    bool IsPlayerOnPlatform()
    {
        if (platform == null) return false;

        // 1) Find CharacterController (even if playerRoot is camera child, we can find in parent)
        CharacterController cc = null;
        if (playerRoot != null)
            cc = playerRoot.GetComponent<CharacterController>() ?? playerRoot.GetComponentInParent<CharacterController>();

        if (cc == null)
        {
            // Fallback: distance check (less reliable)
            if (playerRoot == null) return false;
            return Vector3.Distance(playerRoot.position, platform.position) < 1.2f;
        }

        // 2) Compute "foot area" world point
        Vector3 ccCenterWorld = cc.transform.TransformPoint(cc.center);
        float footY = ccCenterWorld.y - (cc.height * 0.5f) + cc.radius;
        Vector3 origin = new Vector3(ccCenterWorld.x, footY + rayStartUp, ccCenterWorld.z);

        // 3) Raycast down
        QueryTriggerInteraction q = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDownDistance, groundMask, q))
        {
            // Hit object belongs to platform hierarchy?
            if (hit.transform == platform || hit.transform.IsChildOf(platform))
                return true;

            // platform might be a child of the collider root
            if (platform.IsChildOf(hit.transform))
                return true;

            return false;
        }

        return false;
    }

    IEnumerator CoTravel(bool goDown)
    {
        _isMoving = true;

        // 1) Close door
        yield return CoDoor(false);
        yield return new WaitForSeconds(settleDelay);

        // 2) Move platform (carry player)
        if (platform == null || topStop == null || bottomStop == null)
        {
            Debug.LogError("[ElevatorController] platform/topStop/bottomStop not assigned.");
            _isMoving = false;
            yield break;
        }

        Transform targetStop = goDown ? bottomStop : topStop;

        // Parent player to platform so they move together
        Transform originalParent = null;
        if (playerRoot != null)
        {
            originalParent = playerRoot.parent;
            playerRoot.SetParent(platform, true);
        }

        Vector3 startPos = platform.position;
        Vector3 endPos = targetStop.position;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, travelDuration);
            platform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        platform.position = endPos;

        // Restore parent
        if (playerRoot != null)
            playerRoot.SetParent(originalParent, true);

        _atTop = !goDown;

        yield return new WaitForSeconds(settleDelay);

        // 3) Open door
        yield return CoDoor(true);

        _isMoving = false;
    }

    public void OpenDoor() => StartDoor(true);
    public void CloseDoor() => StartDoor(false);

    void StartDoor(bool open)
    {
        if (door == null)
        {
            Debug.LogError("[ElevatorController] door is null.");
            return;
        }

        if (_doorCo != null) StopCoroutine(_doorCo);
        _doorCo = StartCoroutine(CoDoor(open));
    }

    IEnumerator CoDoor(bool open)
    {
        if (door == null) yield break;

        Vector3 startPos = door.localPosition;
        Quaternion startRot = door.localRotation;

        Vector3 targetPos = open ? doorOpenLocalPos : doorClosedLocalPos;
        Quaternion targetRot = Quaternion.Euler(open ? doorOpenLocalEuler : doorClosedLocalEuler);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, doorDuration);
            door.localPosition = Vector3.Lerp(startPos, targetPos, t);
            door.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        door.localPosition = targetPos;
        door.localRotation = targetRot;

        _isDoorOpen = open;
        _doorCo = null;
    }

    // --- Editor convenience: capture door poses ---
    [ContextMenu("Capture CLOSED from current (door local)")]
    public void CaptureClosedFromCurrent()
    {
        if (door == null) return;
        doorClosedLocalPos = door.localPosition;
        doorClosedLocalEuler = door.localEulerAngles;
    }

    [ContextMenu("Capture OPEN from current (door local)")]
    public void CaptureOpenFromCurrent()
    {
        if (door == null) return;
        doorOpenLocalPos = door.localPosition;
        doorOpenLocalEuler = door.localEulerAngles;
    }
}