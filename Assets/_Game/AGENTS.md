# Character art instructions
The final project direction is 2D, Unity 6.6.
The user-uploaded image at Art/Characters/Ian/Reference/Ian_OfficialDesign.png is Ian's authoritative official basic design.
Read CHARACTER_DESIGN.md before editing or generating player sprites, UI portraits, expressions, states, event art, or cutscene art.
Preserve tousled black hair, half-closed tired eyes, lethargic expression, black headphones around the neck, black short sleeve top, black cargo pants, slippers, and dark desaturated palette.
Use Data/IanOfficialIdentity.asset as the shared identity reference. The uploaded original takes precedence over generated derivatives.
The old Art/2D/Ian_* sprites are archived placeholders and must not become the default again.
Do not overwrite the official original. Keep gameplay/save/event systems intact when changing presentation.

# C# source readability
Do not minify project C# source. Do not place multiple methods or executable statements on one line.
Use four-space indentation, Allman braces, one field per declaration, and a blank line between methods.
Wrap long conditions, calls, LINQ chains and nested ternaries naturally, targeting 120 characters.
Preserve serialized/public names, expression grouping, literals, coroutine order and existing gameplay behavior.
Follow `/Docs/CODE_STYLE.md` and the root `.editorconfig` for all new and modified project C# files.
External vendor/package code and genuinely generated `.g.cs` / `.generated.cs` files are excluded.

# Project file organization
Follow `/Docs/PROJECT_STRUCTURE.md` for project-owned scripts and assets.
Use Unity AssetDatabase to move assets while preserving original GUIDs and `.meta` files.
Preserve existing Editor/test assembly boundaries, Resources keys and scene IDs.
Do not automatically delete apparently unused assets; record uncertain candidates for review.
