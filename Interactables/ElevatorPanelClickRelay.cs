using UnityEngine;

/// <summary>
/// Remote click relay: when enabled, any left click triggers elevator panel.
/// Put this on Player (or Main Camera).
/// </summary>
public class ElevatorPanelClickRelay : MonoBehaviour
{
    public ElevatorController elevator;

    [Tooltip("Only relay clicks when GameManager exists and UI is not blocking.")]
    public bool respectUiBlocking = true;

    void Awake()
    {
        if (elevator == null)
            elevator = FindObjectOfType<ElevatorController>();
    }

    void Update()
    {
        if (elevator == null) return;
        if (!elevator.remoteClickEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            var gm = GameManager.I;
            if (respectUiBlocking && gm != null && gm.IsUiBlocking()) return;

            elevator.OnPanelTriggered();
        }
    }
}