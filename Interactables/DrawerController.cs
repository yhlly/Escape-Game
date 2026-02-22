using System.Collections;
using UnityEngine;

/// <summary>
/// Controls drawer smooth open/close by moving localPosition.
/// Keeps its own open state (low coupling).
/// </summary>
public class DrawerController : MonoBehaviour
{
    [Header("Motion")]
    public Vector3 localOpenOffset = new Vector3(0f, 0f, 0.35f);
    public float moveSeconds = 0.25f;

    [Header("State (read-only)")]
    [SerializeField] private bool isOpen;

    public bool IsOpen => isOpen;

    private Vector3 _closedLocal;
    private Vector3 _openLocal;
    private bool _inited;
    private Coroutine _co;

    void Awake()
    {
        InitIfNeeded();
    }

    void OnValidate()
    {
        // 方便你在编辑器里调 offset
        if (Application.isPlaying) return;
        _closedLocal = transform.localPosition;
        _openLocal = _closedLocal + localOpenOffset;
    }

    private void InitIfNeeded()
    {
        if (_inited) return;
        _closedLocal = transform.localPosition;
        _openLocal = _closedLocal + localOpenOffset;
        _inited = true;
    }

    public void Toggle()
    {
        InitIfNeeded();
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        InitIfNeeded();

        if (_co != null) StopCoroutine(_co);
        isOpen = open;

        Vector3 from = transform.localPosition;
        Vector3 to = isOpen ? _openLocal : _closedLocal;

        _co = StartCoroutine(TweenLocal(from, to, moveSeconds));
    }

    private IEnumerator TweenLocal(Vector3 from, Vector3 to, float seconds)
    {
        if (seconds <= 0f)
        {
            transform.localPosition = to;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / seconds;
            float eased = SmoothStep01(t);
            transform.localPosition = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }
        transform.localPosition = to;
    }

    private float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
