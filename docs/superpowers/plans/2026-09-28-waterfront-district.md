# Waterfront District Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Add one unlockable 7×7 waterfront district east of the existing Unity town, using the same economy, calendar, pigeons and facilities.

**Architecture:** Keep a single `TownSimulation`; plot coordinates identify the district. A focused `TownDistricts.cs` partial owns unlock, bounds, and gentle district requests. Existing save, AI and visitor code call this API. `TownWorld` renders the annex and river only when unlocked; `SandboxApp` supplies an unlock action and camera shortcuts.

**Tech Stack:** Unity 6000.3.11f1, pure C# simulation tests, Unity Editor rendering checks, macOS benchmark and build.

---

All paths below are relative to `unity/PigeonSandbox` unless noted. Start from the verified clean `codex/waterfront-district` worktree and the before-change Mac benchmark; preserve the old web prototype.

### Task 1: Bounds, unlock and save compatibility

**Files:** `Tests/DistrictChecks.cs` (new), `Tests/TownChecks.cs`, `Assets/PigeonSandbox/Core/TownDistricts.cs` (new), `Assets/PigeonSandbox/Core/TownSimulation.cs`.

- [x] Write failing `DistrictChecks` for: cannot unlock before expansion level 4; cost exactly ¥3,600; unlock once; main bounds unchanged; annex accepts only x=9..15 and z=-3..3; buildings move across the seam; capture/restore preserves unlock, facilities and bird at x>8; version-2 save remains locked; invalid version-3 save fails without mutating the live town. Hook it into `TownChecks.RunAll()`.
- [x] Run `python3 unity/PigeonSandbox/Tests/verify.py` and confirm failure is due to missing district APIs.
- [x] Add `WaterfrontUnlocked`, `WaterfrontCost`, `UnlockWaterfront()`, `IsWaterfrontPlot(x,z)`, and `IsPlayablePlot(x,z)` in `TownDistricts.cs`. Use x=9..15, z=-3..3 and require the full station expansion. `UnlockWaterfront()` subtracts money only once and calls `Changed`.
- [x] Add save fields, bump `CurrentSaveVersion` 2→3, and validate unlocked/expansion/coordinates/request payload before changing state. Restore old saves as locked. Update the bird-position clamp to allow the annex in unlocked saves. Run the full C# suite and confirm green.

### Task 2: District requests and actual town activity

**Files:** `Tests/DistrictChecks.cs`, `Assets/PigeonSandbox/Core/TownDistricts.cs`, `Assets/PigeonSandbox/Core/TownSimulation.cs`.

- [x] Add failing tests for three one-time requests: both tree+park and fountain+park within 2.1 cells **inside** waterfront; waterfront bakery+plaza within 2.1 plus actual visitor purchase; no cross-seam completion; exact one-time rewards; completion survives save and restore.
- [x] Add `WaterfrontRequests` and `EvaluateWaterfrontRequests()` in the partial; call from `Recalculate()` after existing rewards. Call `WitnessWaterfrontPurchase(target)` only after a real bakery purchase. Save three completion flags and reject malformed version-3 arrays. Run tests green.
- [x] Add failing deterministic tests for water-loving pigeon preference and ability to arrive in annex despite distance; for east-side visitor spawn only when an eligible waterfront venue exists, local destination/exit (including a destination removed mid-trip), and a real waterfront purchase. No eastern visitors when only housing/tree exists.
- [x] Add a bounded score bonus to `Select`, extend decision time only when necessary to reach a far target, and give visitors a transient east-entry flag plus local destination search and east exit. Keep old-town behavior unchanged before unlock. Run all C# checks.

### Task 3: Visual district and navigation

**Files:** `Assets/PigeonSandbox/Runtime/TownWorld.cs`, `Assets/PigeonSandbox/Runtime/SandboxApp.cs`, `Assets/PigeonSandbox/Editor/WorldSyncChecks.cs`, `Assets/PigeonSandbox/Core/TownBenchmark.cs`.

- [x] Add Editor checks that unlocked terrain includes 49 waterfront plots and a river, unlock rebuilds terrain exactly once, facility models stay baked, and seasonal color transitions still rebuild incrementally. Run the Editor check red.
- [x] Extend `ResizeTerrain` cache key with unlock state. Bake the annex ground, 49 plots, a tapered landscaped shoulder covering diagonal paths from the north-east/south-east station plots, promenade and decorative river in the existing seasonal palette; never draw water on buildable plots. Keep facility batching and original ground unchanged when locked.
- [x] In the existing left expansion slot, show unlock after full station expansion and an opened-state label afterward. In the map toolbar, show compact `駅前`/`水辺` focus buttons after unlock; keep pan, zoom, follow and `街全体`. Make `GridPoint` and pan limits use new asymmetric bounds. Add the three district requests to the right-hand `お願い` scroll content.
- [x] Add a separate waterfront benchmark setup/flag so the normal 289-plot baseline stays comparable. Build and run the Editor checks and Mac benchmark, including a seasonal boundary at 338 built plots; inspect the F3/Player.log frame maxima.

### Task 4: Documentation, visual QA and completion

**Files:** `README.md`, `unity/PigeonSandbox/README.md`, font subset `Assets/Resources/NotoSansJP.ttf`.

- [x] Update READMEs with cost, unlock condition, district controls, visits, requests and save compatibility. Run `python3 unity/PigeonSandbox/Tools/subset_font.py` for new Japanese UI text.
- [x] Run `python3 unity/PigeonSandbox/Tests/verify.py`, `python3 unity/PigeonSandbox/Tools/format.py --check`, Unity `WorldSyncChecks.Verify`, and Mac batch build. Confirm `PIGEON VERIFICATION PASSED`, `Build Finished, Result: Success.`, `git diff --check`, and no generated scene/settings diff.
- [x] Visually inspect the unlocked annex in spring and winter at a 1280×800 Mac window, including the HUD, build controls, district buttons and scrollable requests. Verify camera shortcuts and locked terrain in Editor checks.
- [x] Request independent code review, fix important findings, and rerun affected checks. Commit on `codex/waterfront-district` and integrate locally into clean `main` after the final review. Leave push/release to a later request.
