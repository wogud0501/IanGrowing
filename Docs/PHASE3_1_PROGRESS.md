# Phase 3.1 — Character life presentation

2026-11-17 · Unity 6000.6.8f1 · in progress

## Implemented
- Existing Rigidbody/input, authoritative age, gameplay activity services and scene-door system retained.
- `PlayerAnimationPresentation` owns frame selection, distance-driven locomotion, restrained idle/status variation and shadow presentation. `PlayerControlLocks` provides independently owned leases for actions and growth effects.
- Young-adult bank retains official four-direction idle/walk/run. New body-only seated/eat/drink/typing/talk/reach/phone/status atlas is sliced by actual transparent gutters, with one PPU and head-centred pivots. Original official design and adult bank are preserved.
- Existing life-action adapter now keeps the real adult player visible for bed/table/desk actions. Physical approach/alignment/exit, reservation release, cancellation, blanket, real chair, food and computer prop hooks are connected.
- Purchased furniture copies authored interaction anchors. Independent starter home remains bed-only; QA buys and places furniture through the existing economy.
- Home computer uses existing rest action. School education and workplace salary remain owned by existing services.
- Equipment overlays gain action bindings and per-growth-profile anchor offsets. Missing costume art keeps the original outfit.
- Existing distinct growth banks retained; age-derived visual/collider updates do not create another player or save a second authoritative age.
- 16 existing room scenes receive presentation/lock bindings; school and office public desks receive seat adapters. Door logic is retained.
- Conversation presentation faces player and NPC toward each other and restores NPC pose after the existing action.

## Assets and authoring
- New atlas: `Assets/_Game/Art/Phase31/Ian_YoungAdult_Life.png` (64 body frames).
- Props: `Assets/_Game/Art/Phase31/LifeProps.png` (6 props).
- Sprite subassets and animation bank: `Assets/_Game/Data/Phase31/`.
- Original lying body reused with centred pivot; the two-frame sleep hold is not new sleep animation art. Left seated actions mirror right art. Some directions and ages retain previous art/fallbacks.
- Editor authoring: `Phase31Setup*.cs`; source and prompt provenance: `Docs/Phase31/ART_PROMPTS.md`.

## Verification so far
- Isolated Play Mode run: 109 assertions passed before final seat/bed alignment polish, including sleep, fridge, meal, home computer, actual door to alley and return, growth/age rules, equipped headphone save/read/restore and cancelled-sleep cleanup.
- Screen inspection found nonuniform atlas gutters causing adjacent-frame fragments; corrected slicing. Desk height and sleep centre have also been adjusted and are under final verification.
- Before/after screenshots: `Docs/Phase31/Before/`, `Docs/Phase31/Screenshots/`.
- Extended Play Mode run for directional movement, menu, status, school/work/talk and stage colliders is running.
- MCP transport unavailable; existing project-scoped Editor bridge performs real scene authoring and Play Mode runs.

## Save and limits
- Save schema unchanged. Age/equipment remain authoritative; transient blink/frame/action pose is not persisted.
- Production saves are guarded during QA; baseline hashes in `Docs/Phase31/SAVE_BASELINE.json`. Final restoration/hash audit pending.
- New atlas needs further hand-authored frame timing/pose review before claiming production-final character art. Other-age full action sets and per-pose headphone variants are not fully newly authored.
- Android/iOS/device, performance profiling and all interaction/direction combinations are not yet verified for this phase.
