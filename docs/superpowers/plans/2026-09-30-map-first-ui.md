# Map-first Unity UI Implementation Plan

> **For agentic workers:** Keep the Unity town simulation and save format unchanged. Execute the tasks below in order and verify after each meaningful change.

**Goal:** Make the map the primary surface while moving existing controls into contextual Unity IMGUI overlays.

**Architecture:** Keep `SandboxApp` as the event owner to preserve its existing town and camera calls. Replace the static three-column layout with computed overlay geometry, reuse `RightContent` for detailed drawers, and show transient results as toasts.

**Tech Stack:** Unity 6000.3.11f1, C# IMGUI, Unity Editor checks, Python C# compilation harness.

---

### Task 1: Layout and input boundaries

**Files:** `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxApp.cs`, `unity/PigeonSandbox/Assets/PigeonSandbox/Editor/MapFirstUiChecks.cs`

- [ ] Add failing Editor checks for a near-full-width camera viewport and map input exclusion over toolbar, drawers, and sheet.
- [ ] Run the Editor check and confirm the expected failure.
- [ ] Implement compact layout geometry and route camera input only to uncovered map positions.
- [ ] Run the Editor check and C# compilation harness.

### Task 2: HUD, toolbar, and build sheet

**Files:** `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxApp.cs`, `unity/PigeonSandbox/Assets/PigeonSandbox/Editor/MapFirstUiChecks.cs`

- [ ] Add failing checks for all eight existing facilities being available in catalog categories and for placement/cancellation state.
- [ ] Implement compact HUD, bottom toolbar, category-filtered facility cards, and floating placement card.
- [ ] Verify build, selection, placement, and zoom in the player.

### Task 3: Contextual drawers, facility actions, and toasts

**Files:** `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxApp.cs`, `unity/PigeonSandbox/Assets/PigeonSandbox/Editor/MapFirstUiChecks.cs`, `unity/PigeonSandbox/Assets/PigeonSandbox/Editor/StatusPanelChecks.cs`

- [ ] Add failing checks for overlay transitions, Escape dismissal, and notice retention.
- [ ] Move existing request, encyclopedia, policy, festival, expansion, save, and facility-action UI into drawers/cards.
- [ ] Replace the persistent footer notice display with a toast and recent-notices view.
- [ ] Update the old footer-specific checks to verify the new presentation, then run Editor checks.

### Task 4: Visual and regression verification

**Files:** No game logic files.

- [ ] Run `python3 unity/PigeonSandbox/Tests/verify.py`.
- [ ] Rebuild the Mac player using the project's Editor build command.
- [ ] Inspect at desktop and narrow window sizes, then exercise the existing actions listed in the design.
- [ ] Review `git diff` to confirm `Core` logic and save format are untouched.
