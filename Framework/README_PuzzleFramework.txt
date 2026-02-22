Hospital Escape HE – Component-based Puzzle Framework (Element + Condition + Action)

This folder adds a small puzzle framework on top of the original HospitalEscapeHE package.
You can keep using the old HE_* scripts, but for new puzzles we recommend:
- Attach HE_PuzzleElement to any interactable object instead of a custom HE_* script.
- Add one or more HE_PuzzleCondition components to define WHEN the puzzle can be solved.
- Add one or more HE_PuzzleAction components to define WHAT happens when it is solved.

The GameManager (HE_GameManager) stays the main state holder and "context".
The Element + Condition + Action pattern is entirely in the scene through components.

