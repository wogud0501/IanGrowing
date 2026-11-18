# IanGrowing — Higgsfield art direction

Phase 3.3 · 2026-11-18

- Modern dense pixel art; elevated three-quarter top-down camera, architectural depth and walkable space. Slate/navy/brown city, quiet dark-clothed people, warm interiors, restrained amber/teal signs. No voxel, glossy mobile fantasy, extreme isometric camera or copied game art.
- The supplied master prompt is frozen in [MASTER_STYLE.txt](../ArtSource/Higgsfield/Prompts/MASTER_STYLE.txt). The shared style formula is [STYLE_FORMULA.txt](../ArtSource/Higgsfield/Prompts/STYLE_FORMULA.txt). Every generation includes both verbatim.
- [Master board](../ArtSource/Higgsfield/References/IAN_MASTER_BOARD.png): 30 selected original project references. Official Ian remains `Assets/_Game/Art/Characters/Ian/Reference/Ian_OfficialDesign.png`; no protagonist redesign was generated.
- Discover models/workflows first. This run found GPT Image 2.6, Soul and other image models. Popcorn/Canvas/Moodboard endpoints were not exposed. GPT Image 2.6 was used after inspecting its live schema; location variants explicitly reference the selected base image, preserving architecture.

## Production chain

`RAW → visual QA → Selected → separate objects → keyed alpha → nearest pixel resize → 96-color palette / binary edges → Point / 40 PPU / no compression → prefab / atlas → original room Scene → Game View`

- RAW, prompts, results and selected references live outside Unity under `ArtSource/Higgsfield`. Raw scene images are never used as new runtime backgrounds.
- [Manifest](HIGGSFIELD_ASSET_MANIFEST.md) links full generation IDs, timestamps, prompts, source references, SHA-256 and Unity outputs.
- `Tools/Higgsfield/generate_pack.py`: explicit candidate IDs, bounded workers, stored request/result files, no retry loop or resubmission of completed jobs. `convert_sources.py` requires an approved isolated source sheet. Pillow runs in the local build environment, not the Unity runtime.
- Modular apartment/storefront facades preserve original entrance GameObjects, door components, room IDs and collision boundaries. Footprint colliders on new props cover their bases rather than their entire tall silhouette.
- Selected NPC sheets are design references. Existing complete directional citizen animations are reused in the live population; a newly generated multi-direction NPC animation set is **not** claimed.

## Text and visual QA

No generated Korean, prices, companies or logos enter the game. World messages use separate Korean TMP labels: independence/bills, workplace wellness, long shifts, bus waiting. Humour comes from ordinary work and consumption; no horror/gore effects.

Reject voxel/rendered blocks, corrupted labels, changed architecture, clipped objects, inconsistent human scale or missing paths. Standalone store/NPC sheet failures received one focused prompt revision and two replacement candidates. Alternate candidates remain traceable; generation never retries automatically.

Selected references passed Codex composition review. Final human commercial art approval and physical device pixel-density review remain open.
