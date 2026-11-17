# Project cleanup results

2026-11-17 · Unity 6000.6.8f1 · Windows x64 Editor target

The completed Phase 3.1 vertical-slice implementation and prior readability changes were preserved in checkpoint **`8f0351d`** before organizing assets. Existing `.DS_Store` changes were left outside the cleanup. No gameplay features, balance, public/serialized names, namespaces, IDs or save schema were changed.

## Changes

- **331 project C# files** retain the completed readability pass: four-space indentation, Allman braces, separated statements/fields/methods and readable wrapping. `LifeWorldController.cs` is now in [`Scripts/World`](../../Assets/_Game/Scripts/World/LifeWorldController.cs).
- **1,423 files moved**, including **324 C# scripts**. All moves used Unity `AssetDatabase.MoveAsset`; 21 coherent asset folders moved as bundles. The manifest counts asset/source files, excluding companion metadata and folder entries.
- **57 populated functional folders** were added. Scripts now group by responsibility; Data bundles by function; characters, system/UI/NPC prefabs and Ian animation assets have clear locations. Existing scenes, Resources keys, test assemblies and Editor boundaries remain intact.
- **51 C# files / 117 asset-path references** updated so runtime/editor loaders and generators still locate moved assets. This includes the font-builder prefab path filter and the separate locomotion texture/clip output paths. No unrelated string contents or gameplay expressions changed.
- No files deleted. Existing review candidates and unresolved references are listed in [REVIEW_REQUIRED.md](REVIEW_REQUIRED.md).
- Existing `.editorconfig`, pinned CSharpier configuration and [CODE_STYLE.md](../CODE_STYLE.md) were retained; `_Game/AGENTS.md` now also links the file-organization policy.
- `.gitignore` explicitly includes the authored `Scripts/Editor/Build` tools so the generic generated `Build/` exclusion cannot hide their source or metadata after the move.

Use [PROJECT_STRUCTURE.md](../PROJECT_STRUCTURE.md) for locations and rules, [before tree](TREE_BEFORE.txt), [after tree](TREE_AFTER.txt), [new folders](NEW_FOLDERS.json), or the searchable [GUID move manifest](MOVE_MANIFEST.json).

## Verification

| Check | Result |
|---|---|
| Original asset/folder GUIDs | **1,807 preserved** |
| Original `.meta` contents | **1,807 byte-identical** to the checkpoint |
| Serialized non-C# asset contents | Unchanged; only the project instructions document was edited |
| C# assembly boundaries | Unchanged for every original script |
| Build Scene List | Same 26 entries, paths, order, enabled state and GUIDs |
| Scene/prefab audit | 28 scenes + 43 prefabs; **0 Missing Scripts**, **0 new missing references** |
| Existing unresolved references | Same 32 `m_Sprite` properties before/after |
| Path-based loading | 353 previously existing literal asset paths checked; **0 newly missing paths** |
| C# preservation | **331 files × 8 symbol combinations = 2,648 comparisons passed**, after applying only the explicit asset-path whitelist to the baseline |
| Formatting | CSharpier check passes all 331 own C# files; external/generated code excluded |
| Unity compile | 16 batch checks passed; final `scriptCompilationFailed = false` |
| Play Mode | **20 smoke assertions passed**, detailed below |
| Production saves | All 3 original save/backup files retain their original hashes |
| Editor restoration | Play stopped; clean `FamilyHome_IanBedroom`; original fast Play options restored; target unchanged |

The Play Mode smoke test used the existing isolated save framework: Bootstrap/MainMenu → newborn Home introduction → qualified 19-year-old independent Home fixture → Inventory and Mission windows → actual Rigidbody movement/stop → existing door interaction → alley scene → Save/read/restore. It confirmed retained age/money, restored control and one manager/player. Original legal acceptance and input settings were restored with the test state.

**Not verified:** physical WASD/E/Alt-Tab input and device builds. The smoke test invokes the production E handler directly and uses the existing motor's Rigidbody traversal. The host Editor could not reliably receive OS key focus; no input/gameplay guard was weakened. No native Windows/Android/iOS build was requested by this cleanup.

Unity's external AI Editor SDK emitted model-list `NoSubscription` diagnostics during the session. Those package diagnostics are outside the cleanup; no vendor SDK was modified. They are distinct from project compilation and the passing gameplay smoke test.

Evidence: [asset audit](ASSET_AUDIT.json), [metadata](META_AUDIT.json), [path audit](PATH_AUDIT.json), [path changes](PATH_UPDATES.json), [C# comparison](SEMANTIC_AUDIT.json), [compile batches](COMPILE_RESULTS.json), [smoke](SMOKE_RESULTS.json), [save preservation](SAVE_PRESERVATION.json).
