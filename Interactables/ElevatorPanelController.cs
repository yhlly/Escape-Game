using System.Collections;
using UnityEngine;

public class ElevatorPanelController : MonoBehaviour
{
    [Header("Panel (the moving part)")]
    public Transform panel;

    [Header("Closed Pose (LOCAL)")]
    public Vector3 closedLocalPos;
    public Vector3 closedLocalEuler;

    [Header("Open Pose (LOCAL)")]
    public Vector3 openLocalPos;
    public Vector3 openLocalEuler;

    [Header("Motion")]
    public float duration = 0.6f;

    public bool IsOpen => _isOpen;
    bool _isOpen;
    Coroutine _co;

    void Reset()
    {
        panel = transform;
        CaptureClosedFromCurrent();
        openLocalPos = closedLocalPos + new Vector3(0f, 1.0f, 0f);
        openLocalEuler = closedLocalEuler;
    }

    [ContextMenu("Capture CLOSED from current")]
    public void CaptureClosedFromCurrent()
    {
        if (panel == null) panel = transform;
        closedLocalPos = panel.localPosition;
        closedLocalEuler = panel.localEulerAngles;
    }

    [ContextMenu("Capture OPEN from current")]
    public void CaptureOpenFromCurrent()
    {
        if (panel == null) panel = transform;
        openLocalPos = panel.localPosition;
        openLocalEuler = panel.localEulerAngles;
    }

    public void Open() => StartMove(true);
    public void Close() => StartMove(false);
    public void Toggle() => StartMove(!_isOpen);

    void StartMove(bool open)
    {
        if (panel == null)
        {
            Debug.LogError("[ElevatorPanelController] panel is null.");
            return;
        }

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoMove(open));
    }

    IEnumerator CoMove(bool open)
    {
        Vector3 startPos = panel.localPosition;
        Quaternion startRot = panel.localRotation;

        Vector3 targetPos = open ? openLocalPos : closedLocalPos;
        Quaternion targetRot = Quaternion.Euler(open ? openLocalEuler : closedLocalEuler);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duration);
            panel.localPosition = Vector3.Lerp(startPos, targetPos, t);
            panel.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        panel.localPosition = targetPos;
        panel.localRotation = targetRot;
        _isOpen = open;
        _co = null;
    }
}