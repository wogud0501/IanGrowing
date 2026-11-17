# Phase 3.1 — Character life presentation

2026-11-17 · Unity 6000.6.8f1 · **Young-adult vertical slice implemented and Play Mode verified. Full production-art acceptance remains open.**

## Actual implementation
- `PlayerAnimationPresentation` receives observed movement from the existing Rigidbody motor and owns frame selection, restrained idle/status reactions, distance-driven locomotion and shadow presentation. The existing acceleration, braking, diagonal normalization and focus-loss input reset remain in the motor.
- `PlayerControlLocks` provides independently owned leases for life actions and growth presentation. Ending/cancelling one owner cannot unlock another owner. Scene disable invalidates outstanding leases.
- Young-adult bank retains official four-direction idle/walk/run. New body-only seated/eat/drink/typing/talk/reach/phone/status frames use transparent-gutter slicing, one atlas PPU and head-centred pivots. The official source design and previous adult bank remain intact.
- Existing life-action adapter keeps the actual adult body visible for bed/table/chair/desk actions. It performs physical approach/alignment/exit, reservation, blanket and furniture foreground sorting, then releases input/collider/occupancy on normal exit or cancellation.
- Refrigerator retains existing appliance door frames; the player faces the appliance with an authored upward reaching pose. Scene door mechanics were preserved and actual outward/return travel was tested.
- Purchased furniture copies authored anchors. The first independent home remains bed-only. QA purchased fridge/table/desk/chair through the existing economy and placed them through the existing placement service.
- Home computer uses the existing rest activity. Education and salary remain owned by existing education/job services. A chair can also be used independently.
- Conversation faces both actors toward each other and restores the NPC pose when the existing family action ends.
- Inventory consumption publishes `ItemConsumed` only after a successful existing transaction. A drink event triggers presentation; animation never grants inventory, recovery, salary or mission rewards.
- Headphones use per-growth-profile neck offsets and display heights; action bindings retain the current equipment. Missing costume art preserves the base outfit. Existing equipment/stat/save services were not replaced.
- Age-derived appearance and collider changes retain the same player during growth, with a short control lease. Existing detailed 11 visual profiles remain authoritative; walking at 1+, independent outdoor travel at 7+ and optional independence at 19+ remain unchanged.

## Saved assets, scene and animation structure
- `Assets/_Game/Art/Phase31/Ian_YoungAdult_Life.png`: 64 body frames, 1254×1254.
- `Assets/_Game/Art/Phase31/LifeProps.png`: six food/book/monitor props, 1536×1024.
- `Assets/_Game/Data/Phase31/`: standalone sprites and the combined **69-clip** young-adult bank. These are the existing custom sprite-sequence clips, not a replacement giant Animator Controller.
- `PlayerLife.spriteatlasv2`: Point, no mipmaps, no rotation, 4px padding, 2048 maximum page, uncompressed RGBA32. Source textures do not retain CPU-readable copies.
- Six existing headphone definitions receive action bindings. Existing character hierarchy and appearance layer are retained.
- Sixteen existing room scenes save motor/presentation/control-lock bindings. Public school/work desks and existing home furniture receive adapters/anchors. No new gameplay scene or prefab was required; existing scene IDs and transitions remain intact.
- Authoring lives in `Phase31Setup*.cs` and `Phase31Atlas.cs`. Full implementation file list: [CHANGED_FILES.txt](Phase31/CHANGED_FILES.txt). Generation provenance: [ART_PROMPTS.md](Phase31/ART_PROMPTS.md).

## Actual Play Mode results — 1280×720
| Scenario | Assertions passed | Coverage |
|---|---:|---|
| [Vertical slice](Phase31/VERTICAL_SLICE_RESULTS.json) | 124 | Independent home bed-only start; sleep/wake; physical walking; bought/placed furniture; refrigerator prepares once; meal consumes once; computer; independent chair; actual door to alley and return; growth boundary/rules; equipment save/read/restore; cancellation; one manager/player |
| [Extended](Phase31/EXTENDED_RESULTS.json) | 72 | Actual four-direction walking frames/facing/stop; menu input gate; drink/phone/sick/exhausted/stress/happy reaction frames; real inventory drink; qualified child enrollment/study; one real job shift/salary; mutually facing family talk; seven age/profile/collider captures |
| [Final equipment correction](Phase31/GEAR_RESULTS.json) | 78 | Six headphones × four directions; overlays remain below face; seven action poses; direct UI action lock; actual equipment checkpoint/read/restore |

274 assertions include repeated setup checks; this is not 273 unique test cases. The equipment scenario was run after the final neck-size correction. All three completed reports have no reported UI issues. The prior full vertical-slice report predates that last equipment-only data correction; the final equipment scenario covers it directly.

The first failed checks were corrected: wrong expected door destination (`residence`, actual `alley`); test waiting before a deferred restore transition started; using a friend-making action instead of the existing family-talk action. Screenshot review also caught adjacent-frame fragments, a low desk seat, a misplaced sleep centre and oversized headphones. These were corrected rather than accepted from boolean tests alone.

Actual captures: [before](Phase31/Before/Home.png), [sleep](Phase31/Screenshots/01-Sleep.png), [refrigerator](Phase31/Screenshots/03-Fridge.png), [meal](Phase31/Screenshots/04-Meal.png), [computer](Phase31/Screenshots/05-Computer.png), [chair](Phase31/Screenshots/05B-Chair.png), [outside](Phase31/Screenshots/07-Outside.png), [study](Phase31/Screenshots/10-Study.png), [work](Phase31/Screenshots/11-Work.png), [final headphones](Phase31/Screenshots/Equipment-Final.png). `Age-0/1/7/13/19/30/60.png` are isolated development fixtures, not new-start screens.

MCP transport was unavailable; the existing project-scoped Editor command bridge performed real scene authoring, imports, compilation and Play Mode execution.

## Save, restoration and checkpoint
- No Save schema change. Authoritative age/equipment remain in existing data; temporary walk/blink/frame times are not persisted. Temporary action coordinates are not saved as the normal player position.
- Three original production save files are byte-identical: [SAVE_PRESERVATION.json](Phase31/SAVE_PRESERVATION.json). QA used isolated save slots and restored model/consent state.
- Play Mode stopped; `FamilyHome_IanBedroom` restored, clean. Original fast Play setting restored (`DisableDomainReload`); target remains `StandaloneWindows64`. Automated save protection was released for normal manual Play.
- [Final asset audit](Phase31/FINAL_ASSET_AUDIT.json): 69 clips with no missing frame references.
- Initial Git baseline `d01a288`; checkpoint `c4d15e1` created during work was preserved. Final continuation fixes and evidence are saved in the subsequent scoped checkpoint.

## Not complete / not verified
- This is **not full Phase 3.1 commercial-art sign-off**. New frames still need hand-authored pixel cleanup and a thorough frame-timing/pivot review. Left seated actions mirror right art; some motion/direction combinations use existing explicit fallbacks.
- Sleep reuses the original centred lying body and a two-frame hold; it is not newly authored multi-frame sleeping art. Full dedicated new action sets for every childhood/teen/middle/elder profile and every headphone/pose combination remain incomplete.
- Actual costume resources were not fabricated. Existing costume hooks/fallback remain. Sick/phone reactions are presentation hooks; this phase does not add a new sickness gameplay system.
- WASD/diagonal/acceleration/focus-loss code was retained; this phase's movement tests use actual Rigidbody scripted traversal, not an OS-level held-key/Alt-Tab test. OS focus-loss/manual keyboard QA is NOT VERIFIED.
- Native Android/iOS builds, physical mobile/foldable testing, FPS/GC/draw-call/Animator profiling and every action × direction × age combination are NOT VERIFIED for this phase.

Next production pass: finish the remaining directional/age-specific action art and per-pose wearable alignment, then perform manual timing/foot-sliding review and native device profiling. Existing economy, gacha, legal, housing, crowd AI and whole-world layout remain outside this pass.
