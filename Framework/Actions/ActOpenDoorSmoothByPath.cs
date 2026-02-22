using System.Collections;
using UnityEngine;

/// <summary>
/// Smoothly opens a door by rotating a pivot transform found by scene path.
/// Designed to be used inside the Puzzle Action pipeline.
/// </summary>
public class ActOpenDoorSmoothByPath : PuzzleAction
{
    [Tooltip("Scene path for the hinge pivot to rotate, e.g. 'PatientRoom/DoorPivot'. If you don't have a pivot, you can put Door itself.")]
    public string pivotPath = "PatientRoom/DoorPivot";

    [Header("Motion")]
    public float openAngleY = 90f;
    public float duration = 0.8f;
    public bool negativeDirection = false;

    [Header("Optional")]
    public bool useLocalRotation = true;

    Transform _pivot;
    bool _opened;
    bool _running;

    public override void Execute(GameManager gm)
    {
        if (gm == null) return;

        if (_opened || _running) return;

        if (_pivot == null)
        {
            var go = GameObject.Find(pivotPath);
            if (go == null)
            {
                Debug.LogError($"[ActOpenDoorSmoothByPath] Cannot find pivot by path: '{pivotPath}'.");
                return;
            }
            _pivot = go.transform;
        }

        gm.StartCoroutine(CoOpen());
    }

    IEnumerator CoOpen()
    {
        _running = true;

        float sign = negativeDirection ? -1f : 1f;
        float targetY = sign * openAngleY;

        Quaternion start = useLocalRotation ? _pivot.localRotation : _pivot.rotation;
        Quaternion end = useLocalRotation
            ? Quaternion.Euler(0f, targetY, 0f)
            : Quaternion.Euler(0f, targetY, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duration);
            var q = Quaternion.Slerp(start, end, t);

            if (useLocalRotation) _pivot.localRotation = q;
            else _pivot.rotation = q;

            yield return null;
        }

        if (useLocalRotation) _pivot.localRotation = end;
        else _pivot.rotation = end;

        _opened = true;
        _running = false;
    }
}