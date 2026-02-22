using System.Collections;
using UnityEngine;

public class DoorSwing : MonoBehaviour
{
    [Header("Open Settings")]
    public float openAngle = 90f;       // 开门角度
    public float duration = 0.8f;       // 开门时间
    public bool negativeDirection = false; // 反方向开门

    [Header("Lock State")]
    public bool isLocked = true;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip openClip;

    Coroutine _co;
    bool _opened;

    public void Unlock() => isLocked = false;

    public void Open()
    {
        if (isLocked || _opened) return;
        StartRotate(true);
    }

    void StartRotate(bool open)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoRotate(open));
    }

    IEnumerator CoRotate(bool open)
    {
        float sign = negativeDirection ? -1f : 1f;
        float targetY = open ? sign * openAngle : 0f;

        Quaternion start = transform.localRotation;
        Quaternion end = Quaternion.Euler(0f, targetY, 0f);

        if (open && openClip != null && audioSource != null)
            audioSource.PlayOneShot(openClip);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duration);
            transform.localRotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }

        transform.localRotation = end;
        _opened = open;
        _co = null;
    }
}