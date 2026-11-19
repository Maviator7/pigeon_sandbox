# 街の大型プロジェクト Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add repeatable, saved town projects that combine facility layouts and observed pigeon/human activity, with decorative building rewards.

**Architecture:** Keep project rules and saved progress in a new Unity-independent `TownProjects.cs` partial. `TownSimulation` owns save validation and building unlocks; `TownWorld` renders three decorative facilities; `SandboxUi` exposes a compact project drawer. Pure simulation checks cover stage transitions, event observation, rewards, unlocks, and migrations.

**Tech Stack:** Unity 6000.3.11f1, C#, IMGUI, Python-driven C# verification.

---

### Task 1: Add persistent project progression

**Files:**
- Create: `unity/PigeonSandbox/Assets/PigeonSandbox/Core/TownProjects.cs`
- Create: `unity/PigeonSandbox/Assets/PigeonSandbox/Core/TownProjects.cs.meta`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Core/TownSimulation.cs`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Core/TownDistricts.cs`
- Modify: `unity/PigeonSandbox/Tests/TownChecks.cs`
- Create: `unity/PigeonSandbox/Tests/ProjectChecks.cs`

- [ ] Write failing checks for project selection, facility readiness, unique pigeon observations, visits and purchases at the matching venue only, progression switching, target scaling from 2 to the cap of 7, cycle rewards, and invalid save rejection that leaves the current town unchanged.
- [ ] Run `python3 unity/PigeonSandbox/Tests/verify.py` and confirm the new project checks fail before implementation.
- [ ] Add project definitions/state, stage advancement, activity witnesses tied to the currently valid matching layout, deep-copy save handling, validation, old-save migration, and a save-version increment.
- [ ] Run the C# checks again and confirm all project and existing checks pass.

### Task 2: Add project-unlocked decorative facilities

**Files:**
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Core/TownSimulation.cs`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Core/TownProjects.cs`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Core/TownProjects.cs.meta`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/TownWorld.cs`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxApp.cs`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxUi.cs`
- Modify: `unity/PigeonSandbox/Tests/ProjectChecks.cs`
- Modify: `unity/PigeonSandbox/Tests/BenchmarkChecks.cs` only if enum coverage exposes a regression.

- [ ] Add three facility kinds at the end of the enum to preserve existing save values; update ordinal-indexed `Cost`, `NameOf`, `Tips`, and build-catalog `uses` entries; lock construction until the matching project is completed.
- [ ] Add compact, smooth landmark meshes and keep these buildings out of economy, cleanliness, happiness, human visitor routing, and pigeon target selection.
- [ ] Test locked construction, first-clear unlocks, building placement, upgrade/move/remove, save roundtrips, unchanged simulation metrics, and unchanged bird/human routing.
- [ ] Run `python3 unity/PigeonSandbox/Tests/verify.py` and confirm the full checks pass.

### Task 3: Add the project panel and entry point

**Files:**
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxApp.cs`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxUi.cs`
- Modify: `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxApp.cs.meta` only if Unity requires metadata updates.

- [ ] Add a “街の計画” quick action and project overlay using the existing drawer and safe close behavior.
- [ ] Show each project’s layout hint, bird and human progress, reward, current cycle, selection state, and next-cycle action.
- [ ] Save immediately when a project cycle completes without forcing the user away from the map.
- [ ] Run `python3 unity/PigeonSandbox/Tools/format.py --check` and `python3 unity/PigeonSandbox/Tests/verify.py`.

### Task 4: Refresh font, documentation, and final build

**Files:**
- Regenerate: `unity/PigeonSandbox/Assets/Resources/NotoSansJP.ttf`
- Modify: `README.md`
- Modify: `AGENTS.md`
- Modify: `docs/superpowers/specs/2026-09-30-city-projects-design.md`
- Modify: `docs/superpowers/plans/2026-09-30-city-projects.md`

- [ ] Run `python3 unity/PigeonSandbox/Tools/subset_font.py` after adding the UI text.
- [ ] Document the project loop and three decorative rewards in the Unity README and project structure instructions.
- [ ] Run `python3 unity/PigeonSandbox/Tools/format.py --check`, `python3 unity/PigeonSandbox/Tests/verify.py`, and the Unity Mac batch build.
- [ ] Inspect the final diff and report any remaining visual checks that could not be verified.
