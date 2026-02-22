using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    [Header("Refs")]
    public Transform followTarget;   // 通常是玩家头部/相机挂点（没有就用 Player）
    public LayerMask collisionMask = ~0;

    [Header("Tuning")]
    public float sphereRadius = 0.2f;
    public float minDistance = 0.05f;
    public float smooth = 20f;

    Vector3 _localOffset;
    float _currentDist;

    void Start()
    {
        if (followTarget == null)
        {
            var player = FindObjectOfType<FPSController>();
            if (player) followTarget = player.transform;
        }

        _localOffset = transform.localPosition; // 相机相对挂点的本地偏移
        _currentDist = _localOffset.magnitude;
    }

    void LateUpdate()
    {
        if (followTarget == null) return;

        // 目标世界坐标（未碰撞时相机想去的位置）
        Vector3 desiredWorld = followTarget.TransformPoint(_localOffset);
        Vector3 origin = followTarget.position;

        Vector3 dir = (desiredWorld - origin);
        float desiredDist = dir.magnitude;
        if (desiredDist < 0.0001f) return;
        dir /= desiredDist;

        float hitDist = desiredDist;

        if (Physics.SphereCast(origin, sphereRadius, dir, out RaycastHit hit, desiredDist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            hitDist = Mathf.Max(minDistance, hit.distance);
        }

        _currentDist = Mathf.Lerp(_currentDist, hitDist, Time.deltaTime * smooth);
        Vector3 finalPos = origin + dir * _currentDist;

        transform.position = finalPos;
        transform.rotation = followTarget.rotation; // 保持跟随旋转（如果你相机旋转是别处控制，可删这行）
    }
}