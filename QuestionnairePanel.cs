using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Questionnaire UI panel (TMP / uGUI).
/// Goal: after submit (success or fail), the PANEL closes; on success it can optionally
/// disable/destroy the in-world "questionnaire paper" so it disappears permanently.
/// This script keeps the same public API: Open(GameManager gm, FPSController fps)
/// </summary>
public class QuestionnairePanel : MonoBehaviour
{
    [Header("UI Refs")]
    public TextMeshProUGUI titleText;
    public TMP_InputField nameInput;
    public TMP_InputField ageInput;
    public TMP_InputField wardInput;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI hintText;
    public Button submitButton;
    public Button closeButton;

    [Header("Config")]
    public QuestionnaireSpec spec;

    [Header("Outcome")]
    [Tooltip("Bool field/property name on GameManager to mark questionnaire success.")]
    public string successFlagName = "questionnairePassed";
    public string failReason = "Nurse found you. Game Over.";

    [Header("One-time / World Object")]
    [Tooltip("Optional: the in-world object that triggers the questionnaire (e.g., paper/clipboard). Will be disabled on success.")]
    public GameObject worldObjectToDisableOnSuccess;
    [Tooltip("If true, destroy the world object instead of disabling it.")]
    public bool destroyWorldObjectOnSuccess = false;
    [Tooltip("If true, once passed, the questionnaire cannot be opened again (Open will early-return).")]
    public bool oneTimeOnly = true;

    private GameManager _gm;
    private FPSController _fps;
    private float _t;
    private bool _running;

    public void Open(GameManager gm, FPSController fps)
    {
        if (spec == null)
        {
            Debug.LogError("[QuestionnairePanel] Spec is null.");
            return;
        }

        _gm = gm;
        _fps = fps;

        // If already passed and one-time, don't open again.
        if (oneTimeOnly && TryGetBoolOnGM(_gm, successFlagName))
        {
            _gm?.Toast("You already completed the form.", 1.5f);
            return;
        }

        // Block all world interaction while this panel is open
        _gm?.PushUiBlock();

        gameObject.SetActive(true);
        _running = true;
        _t = spec.timeLimitSeconds;

        // freeze player + unlock cursor
        if (_fps != null)
        {
            _fps.SetFrozen(true);
            _fps.LockCursor(false);
        }

        // reset UI
        if (titleText) titleText.text = "Patient Admission Form";
        if (hintText) hintText.text = "Fill correctly before time runs out.";
        if (nameInput) nameInput.text = "";
        if (ageInput) ageInput.text = "";
        if (wardInput) wardInput.text = "";

        if (submitButton)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(Submit);
            submitButton.interactable = true;
        }
        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            // requirement: unfinished = fail
            closeButton.onClick.AddListener(() => Fail("You left the form unfinished."));
            closeButton.interactable = true;
        }

        UpdateTimerText();
    }

    private void Update()
    {
        if (!_running) return;

        _t -= Time.deltaTime;
        if (_t <= 0f)
        {
            _t = 0f;
            UpdateTimerText();
            Fail("Time is up.");
            return;
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText)
            timerText.text = $"Time: {_t:0.0}s";
    }

    private void Submit()
    {
        if (!_running) return;

        // Validate
        string name = nameInput ? nameInput.text.Trim() : "";
        string ageStr = ageInput ? ageInput.text.Trim() : "";
        string ward = wardInput ? wardInput.text.Trim() : "";

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ageStr) || (spec.requireWard && string.IsNullOrEmpty(ward)))
        {
            Fail("Form incomplete.");
            return;
        }

        if (!int.TryParse(ageStr, out int age))
        {
            Fail("Age must be a number.");
            return;
        }

        // Correctness check (case-insensitive for name/ward)
        bool okName = string.Equals(name, spec.expectedName, StringComparison.OrdinalIgnoreCase);
        bool okAge = age == spec.expectedAge;
        bool okWard = !spec.requireWard || string.Equals(ward, spec.expectedWard, StringComparison.OrdinalIgnoreCase);

        if (!okName || !okAge || !okWard)
        {
            Fail("Incorrect information.");
            return;
        }

        Success();
    }

    private void Success()
    {
        _running = false;

        // prevent double-submit spam
        if (submitButton) submitButton.interactable = false;
        if (closeButton) closeButton.interactable = false;

        // set a flag in GameManager
        TrySetBoolOnGM(_gm, successFlagName, true);

        _gm?.Toast("Form accepted! You may proceed.", 2.5f);

        // IMPORTANT: remove the in-world questionnaire so it "disappears" after completion
        if (worldObjectToDisableOnSuccess != null)
        {
            if (destroyWorldObjectOnSuccess) Destroy(worldObjectToDisableOnSuccess);
            else worldObjectToDisableOnSuccess.SetActive(false);
        }

        ClosePanel();
    }

    private void Fail(string detail)
    {
        _running = false;

        if (submitButton) submitButton.interactable = false;
        if (closeButton) closeButton.interactable = false;

        if (_gm != null)
        {
            _gm.Toast($"FAILED: {detail}", 3.0f);
            _gm.Fail($"{failReason} ({detail})");
        }

        ClosePanel();
    }

    private void ClosePanel()
    {
        // Unblock world interaction (if no other panels are open)
        _gm?.PopUiBlock();

        // lock cursor back + unfreeze (cursor lock depends on whether any UI is still open)
        if (_fps != null)
        {
            bool shouldLock = !(_gm != null && _gm.IsUiBlocking());
            _fps.LockCursor(shouldLock);
            _fps.SetFrozen(false);
        }

        gameObject.SetActive(false);
    }

    // ====== Low coupling helpers ======

    private bool TryGetBoolOnGM(GameManager gm, string name)
    {
        if (gm == null || string.IsNullOrWhiteSpace(name)) return false;

        var t = gm.GetType();
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(gm);

        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(bool) && p.CanRead) return (bool)p.GetValue(gm);

        return false;
    }

    private void TrySetBoolOnGM(GameManager gm, string name, bool value)
    {
        if (gm == null || string.IsNullOrWhiteSpace(name)) return;

        var t = gm.GetType();
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool)) { f.SetValue(gm, value); return; }

        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(bool) && p.CanWrite) { p.SetValue(gm, value); }
    }
}
