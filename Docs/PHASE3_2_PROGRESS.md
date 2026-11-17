# Phase 3.2 — Gameplay / UI integration

2026-11-17 · Unity 6000.6.8f1 · Baseline checkpoint `f85cf7e`

## Implemented

- **HUD:** age/day/time, Gold/Ruby, hunger/thirst/health/mental. Stat changes show signed feedback and a brief positive/negative tint. The optional recommendation and current mission use one compact card. Narrow layouts prioritize notifications over that card; wide layouts keep the notification away from the recommendation column.
- **Interaction:** the existing single target/selection feedback now considers facing and a small retention bias as well as distance. Fridge/bed/study/work prompts have action verbs. Age/access/action failures remain authoritative in existing services; open menus/context windows show their reason immediately.
- **Navigation:** `UIShell` remains the only window stack/input owner. The new Life / Item / Finance / System hub routes to existing inventory, mission, map, achievement, housing, finance, education/job and idle/offline views. The education/job Presenter is a separate scene root; its existing panel is explicitly bound on the new component. No duplicate gameplay service or player is created.
- **Status:** tabs for needs, life/education, employment, economy and equipment. Related-action buttons open the existing functional views. Employment shows wage, rank, experience and where to use it.
- **Notifications:** one session service on the existing GameManager; one visible card, priority/FIFO delivery, duplicate pending-message suppression, at most 24 pending and 60 history records. Mission completion works while its window is closed; actual reward, new-item, achievement, economy, warning and system feedback share the presentation path. Window-local availability explanations do not reappear later as stale queued errors.
- **Mission / achievement:** current title/progress, catalog chapter names/counts, full description/conditions and actual configured Ruby rewards. Completion and claim notices do not grant anything themselves. Existing achievement state/catalog is reused with five filters and an optional hidden-entry presentation flag.
- **Guide / unlocks:** existing `CityLifeService.Suggest()` supplies the age/state-appropriate recommendation. Help explains feature locations and can replay first-use guidance. Unlock notices derive from current authoritative access rules; existing mature saves are seeded quietly. NEW on unread employment and inventory discovery remains tied to actual state.
- **Inventory:** existing grid/filter/NEW/rarity/quantity/reveal/pity/probability views remain. Equipment details show signed additive/percentage differences. Dismantle captures the selected item and requires confirmation with the actual material return. Valid resignation actions also require confirmation; all commands still go through the original services.
- **Settings:** Game, Audio, Graphics, Controls, Accessibility and Legal entries. Master volume, original effects/transition preferences, text size, reduced motion, bounded integer UI scale, 2–8 second notification duration, guide replay and the existing legal screens. Existing UI/notification audio hooks are reused; no sound files or payment SDKs were fabricated.
- **Save feedback:** a guarded `WriteCompleted` event reports actual atomic write completion/failure, including protected-slot rejection. UI observers cannot convert a successful save into a failure by throwing. Failure text shown to the player is generic; technical details remain in development logs.

## Saved changes

**16 existing room scenes** bind `GameplayUXPresenter` and the original scene education/job Presenter. No scene is merged, deleted or added to the build list. Legacy `HomeShowcase` / `LifeWorld` authoring was restored to baseline rather than unnecessarily upgrading those views.

New code: `UI/Notifications/NotificationService`, `UI/Core/PixelUXWidgets`, `UI/Windows/GameplayUXPresenter` + `.Settings`, and `Editor/UI/Phase32Setup`. Existing HUD, shell, collection/mission/activity presenters, pixel scaler and the small event/guide adapters were extended. Exact paths are in [CHANGED_FILES.txt](Phase32/CHANGED_FILES.txt).

`SaveData` remains **version 4** with no new save block. Tutorial/read/unlock announcements append `ux/...` keys to the existing world city-life `guideSeen` list. Reset clears only the new tutorial keys; missions, discovery, achievements and unlock authority stay intact. UI preferences use PlayerPrefs. Notification history is transient session presentation, not a persistent life-event ledger.

## Validation

- Actual isolated Play Mode living flow: sleep/wake → low-hunger recommendation → selected fridge/food choice → preparation → table meal → hunger feedback / mission signals → restored control. Actual gacha → result/pity → inventory → cancelled/confirmed dismantle → equipment → door scene travel → Save/Load was exercised in the same fixture.
- A separate run consumed a real drink, completed/claimed original **M001**, checked the completion notification with the mission window closed, rejected an uncompleted next reward and restored the claim/guide state after Save/Load.
- Targeted menu checks cover nested back/cancel, original job/economy/idle windows, five achievement filters, indoor minimap suppression, immediate locked-gacha explanations, long Gold/Ruby display and simulated safe-area insets.
- **1920×1080, 2560×1600, 2340×1080, 2048×1536, 2208×1768 and 1080×1920**: actual Game View PNG dimensions match the requested size; the window stays within the rendered Canvas and long Korean content remains scrollable. In this Editor, `Screen.width/height` lagged Game View changes; validation uses the actual Canvas/picture dimensions and the new window layout tracks its parent RectTransform.
- UI/mission/save run: **45 assertions passed**. Final menu/access run: **30 assertions passed** (setup checks overlap; these are not 75 unique gameplay scenarios). The living-flow portion passed 53 checks before its separately corrected layout check.
- Final compilation has **0 errors**; all **336 own C# files** pass the formatter check. All 16 room bindings are complete. The audit of 28 scenes / 43 prefabs has 0 Missing Scripts and 0 new missing references.
- Protected-save rejection and exception failure paths both publish failure events without writing a file. Original **three save/backup files remain byte-identical**. Scene references, compilation and restoration results are in [VALIDATION.json](Phase32/VALIDATION.json). Build scene IDs/order are preserved. Standard C# formatting is retained.

Evidence: [living-flow assertions and the subsequently fixed layout failure](Phase32/ATTEMPT2_GAMEPLAY_PASS_LAYOUT_FAILURE.json), [UI/mission/save tests](Phase32/UI_ACCESS_RESULTS.json), [final access tests](Phase32/FINAL_ACCESS_RESULTS.json), [resolution measurements](Phase32/RESOLUTION_RESULTS.json), [save hashes](Phase32/SAVE_PRESERVATION.json), [latest menu](Phase32/Final/Hub.png), [long currency](Phase32/Final/Long-Currency.png).

Observed initialization/reference/layout failures are preserved in the attempt reports. They led to fixes for slash-containing button names, missing button labels, parent-Canvas resize detection, lookup under an inactive achievement panel, and the separately rooted education/job Presenter. Earlier debugging screenshots are kept apart from the latest captures.

## Not verified / follow-up

- Physical WASD/E/Alt-Tab, gamepad-only and native Android/iOS/foldable device testing. Automated movement uses the actual Rigidbody motor; interaction invokes the production handler directly.
- Native release builds, FPS/GC/UI rebuild profiling and every long item/mission/localization combination. Resolution tests are Editor render targets, not physical-device certification.
- The complete 60-mission life sequence was not replayed. Employment unlock presentation was tested with isolated 18/19-year-old authoritative growth fixtures; M001 completion/reward was tested independently.
- Optional daily summaries, persistent chronological life logs, input rebinding and a separate camera-shake control were not added. Existing reduced-motion support remains; no new camera shake was authored. Hidden achievement presentation is supported, but existing catalog entries were not arbitrarily made secret.
- The pre-existing 32 unresolved sprite references and external Unity AI `NoSubscription` diagnostics remain separate from this phase. Dedicated education/job UI artwork is still the existing shared activity view; gameplay and earlier character-art acceptance limitations were not reworked here.
