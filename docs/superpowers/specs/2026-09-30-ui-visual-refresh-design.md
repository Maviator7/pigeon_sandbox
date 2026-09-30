# Unity UI Visual Refresh

## Goal

Make the existing Unity IMGUI feel more like a cozy town-building game through clearer button styling and better menu layouts, while preserving the current game screen content and behavior.

## Current context

The game uses Unity IMGUI in `SandboxApp.cs` and `SandboxUi.cs`. The current screen has a compact civic status header, floating map controls, a bottom tool dock, a build sheet, detail drawers, and a game menu. Most panels and buttons use a shared cream-and-green palette, rounded button textures, and Japanese text. The recent map-first layout is the baseline to refine.

## Design

- Keep the map-first screen, status values, labels, available actions, and existing menu sections.
- Refine the visual hierarchy of buttons with consistent padding, shape, contrast, and distinct normal, hover, pressed, selected, and disabled states.
- Give the build sheet and facility cards a more game-like presentation. Add small, restrained code-drawn pictograms for the existing facility types, with the existing name, use, and cost information.
- Reorganize spacing and button placement inside the build sheet, detail drawers, selection card, and game menu so headings, choices, and actions read as distinct groups.
- Keep panel colors within the existing warm paper and deep green palette. Use the bundled Noto Sans JP font and no external image or font assets.
- Use subtle native IMGUI feedback only where it improves legibility, such as a short panel entrance and clear pressed state. Avoid continuous decorative animation.

## Behavior and scope constraints

- Preserve all current controls, labels, click outcomes, camera gestures, overlay transitions, and build/selection flow.
- After placing a building, the game must continue to return to inspection as it does now.
- Do not add controls, actions, gameplay information, tutorial text, features, or new screen content.
- Do not change `TownSimulation`, save data, or the existing game rules.
- Restrict implementation to the runtime UI presentation and layout. Keep the existing map-first architecture.

## Acceptance criteria

- The HUD, build sheet, detail drawers, selection card, and game menu share a coherent game-like button and panel style.
- Existing actions remain reachable and keep their current results.
- Selected and disabled controls are visually distinguishable, with readable Japanese text.
- Facility pictograms fit beside existing text without changing the information shown.
- Layout adapts to the available window bounds without placing interactive controls off-screen or over the map hit area.
- The Core simulation and save format remain untouched.

## Verification

Review the edited UI at the existing desktop window size and a narrower window size. Confirm button bounds and map hit exclusion remain aligned with the visible UI. Do not add or run test commands as part of this task unless requested.
