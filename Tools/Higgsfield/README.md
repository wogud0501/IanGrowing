# Higgsfield → Unity production assets

Run from the Unity project root. Requires the authenticated `higgsfield` CLI and an explicitly selected workspace; never store access tokens in this repository. Create a Python virtual environment outside Assets and install `requirements.txt` for conversion/contact sheets.

1. Inspect `higgsfield workspace list`, `higgsfield model list --json`, and the selected model's live schema. The generator repeats model/schema discovery and selects an available compatible image model; `--model` allows an explicit discovered model.
2. Start from the frozen `ArtSource/Higgsfield/Prompts/MASTER_STYLE.txt` and `STYLE_FORMULA.txt`. Add one named `.txt` prompt per candidate, normally `_a` and `_b`. Optional sibling `.refs.json` lists project-relative approved location images.
3. `python3 Tools/Higgsfield/generate_pack.py asset_a asset_b --workers 2`. This spends generation credits. Only explicitly named candidates run, max 32 per invocation. Request/result files prevent silent resubmission. Failed/interrupted requests require manual inspection; no automatic retry loop.
4. Review pixel edges, perspective, human scale, clear paths, branding/text and location consistency. Copy the chosen source to `Selected/<asset>.png` and record the candidate, status and specific QA reason in `selection.json`. Never mark an unchecked result approved.
5. For the supported isolated prop/vehicle/store sheets and modular facades, run `convert_sources.py` with the Pillow environment. It removes the magenta key, separates connected silhouettes, uses nearest resizing, limits colors, creates binary alpha and emits individual 40 PPU sprites. It also derives sparse warm/teal night-window masks. Inspect output edges before Unity import. New sheet layouts require updating the converter intentionally.
6. In stopped Unity, use **IanGrowing → Phase 3.3 → Apply living city slice**. It uses existing rooms/controllers, creates GUID-backed prefabs, configures Point/uncompressed imports, and packs the V2 atlas. Existing room IDs, entrance roots, colliders and gameplay services are retained.
7. Use **Verify population data and spatial grid** after atlas packing, then the isolated Play Mode checks. Inspect actual Game View screenshots.
8. `python3 Tools/Higgsfield/update_manifest.py` updates full provenance and the Markdown manifest without submitting jobs.

Raw candidate rasters remain local outside Assets/Git; result URLs, request metadata, prompts and selected sources remain versioned. No giant generated scene background is introduced. The two `VFX_` textures are authored pixel effects, not Higgsfield outputs. New NPC design sheets are references; live NPCs reuse the project's existing complete citizen animation banks.

## Production72 revision

The later user-authored 72-prompt pack is separate from the original Phase 3.3 master and results. See `ArtSource/Higgsfield/PromptPacks/Production72/README.md` and `INDEX.md`.

- `production72.py prepare <IDs>` resolves selected location dependencies, attaches official Ian when required, freezes prompt/reference hashes, and prepares exactly two named candidates. It does not call the generation API. `generate_pack.py` submits only explicit candidates; downloads become visible atomically after completion.
- `production72.py select <ID> a|b --note '<specific visual QA>'` locks a reviewed reference. It cannot replace a location already used by submitted dependent variants. Previous rejected revisions retain their original prompts and references.
- `production72.py report` records actual returned IDs separately from attempted requests and API failures. `audit` verifies all 72 source bodies, prepared master/style blocks, reference hashes, Ian attachments, dependency locks and selected-source hashes.
- `convert_production72.py` uses the reviewed 71 survival sheet and three additional 67 street props, emits separate 96-color/binary-alpha/40 PPU pixel sprites and conversion provenance. The urban sheet has uneven rows, so it selects reviewed connected silhouettes rather than clipping tall adjacent props with a fixed grid.
- Run `import_production72.cs.txt` through Unity MCP `execute_code` in a stopped clean editor. It creates GUID-backed prefabs, applies Point/uncompressed imports, reuses the existing atlas builder and adds only decorative bills/boxes beside the existing Home entrance. Runtime services, door colliders, IDs and save keys are retained.
- Production72 results live in the pack's `manifest.json`; `update_manifest.py` intentionally describes the original Phase 3.3 run only. Neither command silently generates images.
