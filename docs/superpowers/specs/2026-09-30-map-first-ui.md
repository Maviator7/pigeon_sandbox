# Map-first Unity UI

The user supplied `pigeon_city_ui_refactor_prompt.md` and `pigeon_city_ui_mock.html` and confirmed that they should guide an implementation in the Unity game. This design adapts the web mock to the existing Unity IMGUI screen without changing the town simulation.

## Layout

- A compact cream HUD spans the top. It keeps the title, budget, happiness, satisfaction, residents and visitors, time, pause, and speed visible.
- The camera viewport fills the remaining screen. Transient controls float above it: small zoom and district controls at the upper left, requests/notices/encyclopedia at the upper right, a tool dock at the bottom, and a menu button at the lower right.
- The build catalog opens as a bottom sheet. It includes all eight existing facility types, with category filters and cost/effect text. Choosing one closes the sheet and shows a small placement card; placing or cancelling closes that card.
- Existing requests, encyclopedia, policies, festivals, and facility actions are shown in contextual drawers or cards. This preserves bird focus, renaming, relocation, upgrade, removal, festival, policy, district expansion, and save actions.
- Operation results and events appear as brief toasts. The notices drawer retains recent messages so transient notices can be revisited.

## Scope choices

The mock contains roads, restart, title screen, and settings, which have no corresponding feature in the current Unity game. The user chose an existing-function UI refactor. These entries are omitted rather than shown as inactive controls. `TownSimulation`, save data, town economics, and bird behavior are unchanged.

## Interaction and accessibility

Only one large overlay is open at a time. Escape and the visible close button dismiss it. Map input is ignored while an overlay is open. Existing text styles retain the bundled Japanese font. Buttons have consistent minimum hit targets and a selected state. The drawer and build sheet clamp to the available viewport at narrower sizes.

## Verification

Editor checks cover map viewport size, overlay hit exclusion, catalog membership, notices, and control bounds. The existing C# verification suite must compile. The Mac player will be rebuilt and visually checked at desktop sizes.
