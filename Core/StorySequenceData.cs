using UnityEngine;

[CreateAssetMenu(menuName = "Escape/Story Sequence", fileName = "StorySequence")]
public class StorySequenceData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string title;

    [Header("Lines (each entry is one screen)")]
    [TextArea(2, 8)]
    public string[] lines;

    [Header("Behavior")]
    [Tooltip("If > 0, auto-advance to next line after this many seconds. 0 = manual.")]
    public float autoAdvanceSeconds = 0f;

    [Tooltip("Allow player to press Esc to skip/close.")]
    public bool allowSkip = true;

    [Tooltip("Freeze movement & block interactions while story is showing.")]
    public bool blockInput = true;

    [Header("Visual")]
    [Range(0f, 1f)]
    [Tooltip("Fullscreen black overlay alpha.")]
    public float overlayAlpha = 0.86f;
}
