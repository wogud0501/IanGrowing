# Project file organization

Applies to IanGrowing-owned content in `Assets/_Game`. Folder organization does not change namespaces, class names, serialized fields, item/mission IDs, save keys or assembly boundaries. Follow [CODE_STYLE.md](CODE_STYLE.md) and the root `.editorconfig` for C#.

## Find code by responsibility

| Location under `Scripts/` | Contents |
|---|---|
| `Core/` | Bootstrap, game context, shared gameplay configuration and time |
| `Player/` | Motor, animation presentation, growth visuals, control locks and stats |
| `World/` | World controller/service, camera, rooms and environment presentation |
| `Interaction/` | Doors, shower, beds and activity anchors |
| `Life/` | Growth access, life stages, family and survival |
| `NPC/` | Schedules, navigation, personal space and room residents |
| `Mission/`, `Education/`, `Jobs/`, `Social/` | Existing progression services and their runtime data types |
| `Economy/`, `Housing/`, `Save/` | Economy, Ruby/purchase abstraction, residence and persistence |
| `Inventory/`, `Equipment/`, `Gacha/` | Item ownership/data, wearable presentation, draws and probability disclosure |
| `UI/` | `Core`, `HUD`, `Layout`, `Controls`, `Themes` and `Windows` presentation |
| `SceneManagement/` | Room routes, entry/exit points and `Transitions/` |
| `Presentation/`, `VFX/`, `Audio/` | Shared actor animation, feedback and world audio |
| `Idle/`, `Combat/` | Existing activity and combat services |
| `Editor/` | Authoring, build validation, QA menus and legacy builders; Editor assembly only |
| `Development/` | Existing development framework, scenarios and verification harness |
| `Release/` | Legal document service and shared release configuration |

`ItemService.cs` still owns the existing inventory/equipment service types. This cleanup does not split classes just to match folders. Partial classes remain together.

## Asset locations

- **Data:** `Data/Characters` (growth and young-adult actions), `Data/World` (shared world, rooms, interiors), `Data/SceneManagement`, `Data/Economy`, `Data/Housing`, `Data/Idle`, `Data/City`, `Data/LifeActivities`, `Data/Presentation`. Existing `Data/Missions` and `Data/Activities` remain. Phase-numbered generated bundles move as complete bundles so relative sprite/frame references and regeneration continue to work.
- **Art:** Ian's official reference stays at `Art/Characters/Ian/Reference`; growth art is in `Art/Characters/Ian/Growth`, young-adult actions in `Art/Characters/Ian/YoungAdult/Life`, locomotion texture in `Art/Characters/Ian/Locomotion`, and reconstructed bathroom art in `Art/Interiors/FamilyBathroom`. Existing well-defined art collections remain in place.
- **Animations:** legacy Animator clips/controllers are grouped in `Animations/Ian/Legacy`; four-direction clips/controller in `Animations/Ian/Locomotion`. The existing custom `ActorAnimationSet` sprite-sequence data remains with its related Data bundle.
- **Prefabs:** `Characters`, `System`, `NPC`, `UI` (including `UI/Pixel`) and existing `Facilities`.
- **UI assets:** `UI/Fonts`, `UI/Themes` and the existing `Pixel/Fonts`, `Collection`, `Indie` collections. UI prefabs remain under `Prefabs/UI`; UI scripts under `Scripts/UI`.
- **Audio:** existing world recordings in `Audio/World`. Materials stay in the existing `Materials` folder.
- **Scenes:** retain current `Scenes/` and `Scenes/Rooms/` paths, IDs and build-list order. Room loading and legacy authoring compose paths from this structure. Do not reorganize scenes merely for appearance.
- **Tests:** retain `Tests/EditorTools` and `Tests/PlayMode`, including their existing asmdefs and define constraints.

## Boundaries deliberately retained

`Scripts/Editor` remains in the Editor assembly. The existing `#if UNITY_EDITOR` verification harness under `Scripts/Development/Verification` remains in its original runtime assembly: its startup save-protection initializer and runtime framework share references. Moving it into an Editor assembly would change initialization or require a separate refactor. Its guards remain intact.

Root shared Data assets remain where existing generic builders write `Data + name`. Resources and all `Resources.Load` keys remain fixed. No Addressables migration, namespace change, new asmdef or gameplay architecture change accompanies this cleanup.

The existing item, equipment and gacha catalog stays at `Assets/Resources/IanCollection.asset`; release configuration and localized legal documents also retain their existing Resources locations. No empty `Data/Items` or `Data/Gacha` folders are created to imply a migration that did not happen.

## Rules for future files

- Use descriptive names matching the principal type for MonoBehaviour/ScriptableObject scripts. Keep partial class parts together. Avoid `temp`, `copy`, `final2`, `New Prefab` and numbered generic material names.
- Add only populated, useful folders, normally two to four levels deep. Use the nearest existing feature folder.
- Put Editor-only authoring beneath the existing Editor boundary. Keep tests in their existing test assemblies. Preserve deliberately guarded runtime/development adapters unless changing their architecture is separately authorized.
- Move Unity assets with `AssetDatabase.MoveAsset`, including the original `.meta`. Audit GUID, assembly, serialized references and path-based loaders before and after. Update explicit authoring paths when their asset moves.
- Never automatically delete apparent unused assets. Resources, string paths, reflection and custom loaders may still use them. Record uncertain candidates for review.
- Exclude Packages, plugins, SDKs, Unity templates, genuine generated source, Library, Temp, Logs, obj, Builds, UserSettings and recovery backups.

The cleanup manifest and verification results are in [ProjectCleanup](ProjectCleanup/RESULTS.md). Earlier Phase reports retain historical paths; use the manifest to locate moved assets.
