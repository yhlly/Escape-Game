using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScreamTrigger : MonoBehaviour
{
    [Header("Timer")]
    public float timeLimitSeconds = 12f;

    [Header("Behavior")]
    public bool triggerOnce = true;
    public bool autoSetInTreatment = true;

    [Header("UI")]
    public bool showCountdownOnScreen = true;
    public string toastOnScream = "A piercing scream echoes!";
    public string countdownLabel = "Distance to being found: ";

    [Header("Fail")]
    public bool failOnTimeout = true;
    public string timeoutReason = "You failed to calm her down in time.";

    // ===== static global timer API (for other scripts like EndingButton / Girl) =====
    static bool _globalRunning = false;
    static float _globalEndTime = 0f;

    bool _triggered = false;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        if (!_globalRunning) return;

        var gm = GameManager.I;
        if (gm == null)
        {
            _globalRunning = false;
            return;
        }

        // Calmed or scream stopped -> stop timer
        if (gm.girlCalmed || !gm.screaming)
        {
            _globalRunning = false;
            return;
        }

        if (Time.time >= _globalEndTime)
        {
            _globalRunning = false;

            if (failOnTimeout)
            {
                gm.Toast(timeoutReason, 2.0f);
                gm.Fail(timeoutReason);
            }
            else
            {
                gm.screaming = false;
                gm.Toast(timeoutReason, 2.0f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<FPSController>() == null) return;

        var gm = GameManager.I;
        if (gm == null) return;

        if (autoSetInTreatment)
            gm.inTreatment = true;

        if (gm.girlCalmed) return;

        if (triggerOnce && _triggered) return;
        _triggered = true;

        gm.screaming = true;
        gm.Toast(toastOnScream, 2.0f);

        StartGlobalTimer(timeLimitSeconds);
    }

    void OnGUI()
    {
        if (!showCountdownOnScreen) return;

        var gm = GameManager.I;
        if (gm == null) return;
        if (!_globalRunning) return;
        if (!gm.screaming) return;
        if (gm.girlCalmed) return;

        float left = RemainingSeconds();
        GUI.skin.box.fontSize = 18;
        GUI.Box(new Rect(12, 80, 520, 34), $"{countdownLabel}{left:0.0}s");
    }

    // ===== public static API used by other scripts =====
    public static void StartGlobalTimer(float seconds)
    {
        _globalRunning = true;
        _globalEndTime = Time.time + Mathf.Max(0.1f, seconds);
    }

    public static void StopGlobalTimer()
    {
        _globalRunning = false;
    }

    public static bool IsTimerRunning()
    {
        return _globalRunning;
    }

    public static float RemainingSeconds()
    {
        if (!_globalRunning) return 0f;
        return Mathf.Max(0f, _globalEndTime - Time.time);
    }
}