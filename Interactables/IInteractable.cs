using UnityEngine;

public interface IInteractable
{
    string Prompt { get; }
    bool CanInteract(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
}
