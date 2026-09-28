# Status Panel Clarity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Separate the Unity game's current action from town messages in the bottom panel and make the accounting period clear in the header.

**Architecture:** Change only `SandboxApp` UI state and drawing. Preserve `TownSimulation` notifications and existing button handlers. Classify existing `message` assignments as guidance or feedback, compose a deduplicated notice view, and bound the two bottom columns with scroll views.

**Tech Stack:** Unity 6000.3.11f1, IMGUI, Editor smoke checks, Mac build.

**Spec:** `docs/superpowers/specs/2026-09-28-status-panel-clarity-design.md`.

---

### Task 1: Message composition

**Files:** `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxApp.cs`, `unity/PigeonSandbox/Assets/PigeonSandbox/Editor/StatusPanelChecks.cs` (new), `unity/PigeonSandbox/Assets/PigeonSandbox/Editor/BuildMac.cs`.

- [ ] Add Editor checks first for: guidance from selecting a facility is omitted from the notice column; operation feedback is shown; distinct non-accounting `town.Notice` events are preserved; matching feedback/notice is shown once; accounting `town.Notice` is omitted; welcome/build-failure notices are marked as hints. Run `PigeonSandbox.Editor.StatusPanelChecks.Verify` and confirm the test fails because the view composition API is missing.
- [ ] Add a small UI-only message category alongside `message`, set it at existing assignment sites, and expose an internal/private composition method called by the footer and checks. Call `StatusPanelChecks.Verify()` from `BuildMac.Verify()`. Do not alter town events, budget arithmetic, or save data. Re-run the Editor check until green.

### Task 2: Two-column layout and header

**Files:** `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime/SandboxApp.cs`, `unity/PigeonSandbox/Assets/PigeonSandbox/Editor/StatusPanelChecks.cs`.

- [ ] Add a geometry check for two non-overlapping, in-bounds footer columns, a reserved full-width button row in facility management, and fixed-height/scrollable text areas. Verify it fails against the old layout API.
- [ ] Render the left 「いまの操作」 and right 「街のお知らせ」 columns in all modes. Keep management buttons and callbacks unchanged in the full-width bottom row. Make each column's body scroll within the existing 192-unit footer; show 「操作結果」/「ヒント」/「できごと」 small labels where appropriate.
- [ ] Add no-wrap formatting for all three lines in the 245-unit budget region: 「街の予算」、 「20秒ごと 収入」、 and 「維持費」. Test normal and large Money/Income/Upkeep values; use a readable compact 万-unit display if full numbers do not fit. Preserve underlying values.
- [ ] Run `python3 unity/PigeonSandbox/Tools/subset_font.py` for new labels, then `python3 unity/PigeonSandbox/Tests/verify.py` and `python3 unity/PigeonSandbox/Tools/format.py --check`.

### Task 3: Visual and build verification

**Files:** UI and Editor files above; no simulation changes.

- [ ] Build Mac app with `-executeMethod PigeonSandbox.Editor.BuildMac.Build` and confirm `PIGEON VERIFICATION PASSED` and `Build Finished, Result: Success.` in the log.
- [ ] At 1280×800, visually inspect build, inspect, and facility-management modes. Check a deliberately long save-error message in both the normal and facility-management footer, scroll each to the end, and confirm full text remains readable without crossing the management buttons. Verify no duplicated notice appears and the header stays readable with large Money/Income/Upkeep values.
- [ ] Run `git diff --check`, review only intended UI/font/docs changes, commit on `codex/status-panel-clarity`, then integrate locally into a clean `main`. Do not push or release unless requested.
