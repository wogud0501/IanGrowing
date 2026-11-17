# Retained review candidates

No assets or metadata were deleted. Apparent lack of a build-list/reference entry is not evidence that an asset is unused.

| Candidate | Why retained |
|---|---|
| `Assets/_Game/Art/2D/Ian_*` | Explicitly archived placeholder character art; legacy scenes/builders may still refer to it. Never make it the default again. |
| `Assets/_Game/Animations/Ian/Legacy` | Original prototype clips/controller, grouped without deleting dependencies. Audit legacy prefab and authoring use before retirement. |
| `Scenes/HomeShowcase.unity`, `MainGame.unity`, `MainGame2D.unity` | Existing showcase/legacy scenes outside the current enabled build list; editor generation paths remain. |
| `Assets/Scenes/SampleScene.unity` | Outside `_Game`, currently enabled in the original build list. Preserved exactly; separately establish its intended role before changing it. |
| Old empty `Scripts/Commercial` and `Scripts/Editor/Commercial` folders | Original folder GUIDs retained. Folder retirement can be reviewed separately. |
| Root shared Data assets and fixed Resources bundles | Referenced by generic authoring code and runtime string keys. Kept in place to preserve behavior. |

No suspicious `.bak`, `.old`, `_copy`, `_backup`, `_temp`, `unused`, `NewBehaviour`, `New Prefab`, numbered generic material or `final2` filenames were found among active project-owned asset files. Recovery snapshots and generated/external directories were excluded.

The baseline already has **32 unresolved `m_Sprite` references** across scenes/prefabs. The post-move audit reports the same objects/properties and no new missing references. Review them in a separate repair task; this cleanup deliberately preserves gameplay/assets rather than silently replacing their content. Details: [before](REFERENCES_BEFORE.json), [after](REFERENCES_AFTER.json).

Remaining broader organization work includes legacy art collections grouped by production pass. They were retained when splitting a generator-owned bundle would require a larger authoring refactor. Existing Phase 3.1 art acceptance limitations remain recorded in its own progress report.
