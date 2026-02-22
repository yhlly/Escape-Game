using UnityEngine;

public class ElevatorTravelButtonInteractable : MonoBehaviour, IInteractable
{
    public ElevatorController elevator;

    [Header("Prompt")]
    public string prompt = "Start elevator";
    public string Prompt => prompt;

    [Header("Optional: allow click even if locked, to show toast")]
    public bool allowWhenLocked = true;

    void Awake()
    {
        if (elevator == null)
            elevator = FindObjectOfType<ElevatorController>();
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        var gm = GameManager.I;
        if (gm == null) return false;
        if (gm.IsUiBlocking()) return false;

        // 允许没修电路也能点，用于弹提示
        if (allowWhenLocked) return true;

        return gm.circuitFixed;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (elevator == null)
        {
            Debug.LogError("[ElevatorTravelButtonInteractable] elevator is null.");
            return;
        }

        elevator.OnTravelButtonPressed();
    }
}