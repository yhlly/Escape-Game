Hospital Escape HE (Greybox-style, reliable inventory + hints)

This pack uses a Greybox-like architecture:
- One GameManager does: inventory, UI (OnGUI), keypad, toasts, objectives, timers, endings.
- One PlayerInteractor does: raycast from camera center, shows hint, press E to interact.
- Interactables are small components (DoorLock / ElectricalBox / PickupItem / HidingSpot / Girl / EndingButton).

Why this fixes your issue:
- Inventory is stored in a single manager; UI reads directly from it (no duplicate InventoryManagers).
- Interactions don't depend on trigger volumes; only need colliders (your cubes already have).

INSTALL
1) Delete/disable older scripts you imported from me before (to avoid duplicates).
2) Import folder: Assets/HospitalEscapeHE into your project Assets/
3) Wait for 0 red errors.
4) Menu: Hospital Escape HE -> Auto Setup Scene (adds scripts by your hierarchy names)
5) Play.

Controls
- WASD Move, Mouse Look
- E Interact
- I Backpack
- ESC Toggle cursor lock
- R Restart (on ending screen)

Hierarchy names expected (same as your screenshot):
Player
GameManager
PatientRoom/Door/Lock
Closet/CircultBox
Office/Doll
Closet
TreatmentRoom/Girl
TreatmentRoom/RedButton
TreatmentRoom/BlueButton
TreatmentRoom/ScreamTrigger (optional, but supported)

Password default: 251012
Doctor timer default: 10-20s
