# Commercial visual rebuild — Stage A / 2026-11-06

## Audit and checkpoint
- Restore commit: `125484c` (includes pre-existing user edits and existing captures). Git author was unset; a command-local `Codex <codex@local.invalid>` identity was used, without changing Git configuration.
- Editor verified live through MCP: Unity 6000.6.7f1, URP 17.6.0, Home open, not playing initially. CLI beta.13 discovery is blocked by OS access to Library/Pipeline descriptor; MCP works. CLI update check failed SSL; beta.17 notification recorded.
- Baseline actual Play Mode captures: `Before/Home-GameView.png`, `Before/School-GameView.png`. Existing-capture files predate this session.
- Main camera orthographic 5, no pixel perfect component; placeholder apartment bounds 8×5.2; official character bounds 2.38×3.84. An opaque teal UI hero panel covers the room. School uses the same presentation with placeholder tint.
- uGUI/TMP, Input System, shared GameHUD prefab, four destination navigation and modal stack already exist. Preserve existing presenter/button/service references.
- Existing warning: recursive MissionCondition serialization depth exceeds 10; observed before changes.

## Art bible
Dense Korean urban pixel interiors with worn objects and restrained dark comedy; dark navy outlines, warm light against blue shadow, the official tired Ian silhouette remains authoritative.
Shared roles: ink #1C263A, paper #FFF1CF, amber #F0BC63, danger #DC775B, jade #7FB696, blue #6A99AD. Home uses ochre wood/amber and blue rain; School uses chalk green/jade and cool sunlight; future Office uses gray/blue/red, City indigo/violet/neon.
Lighting comes from believable windows and fixtures, with solid clustered pixel highlights. Keep central floor readable and the room visible behind compact HUDs.

## Rendering decision
Stage B world: 640×360 reference, 100 PPU (retains project convention), orthographic size 1.8, URP PixelPerfectCamera, integer upscale RT with windowbox, Point, no mipmaps/compression/MSAA/HDR/postprocess. Existing official character remains its own source, sized to scene and snapped to .01 world units; it is NOT newly drawn pixel animation.
UI: separate ScreenSpaceOverlay canvas, existing integer scaling policy retained, anchored compact layout and safe area. Text stays native resolution. Below 640×360 world reference, pixel-perfect scaling cannot be guaranteed; target landscape Android >=720px height. Portrait requires separate production layout review.
AI generated background masters are retained as source assets, with 640×360 nearest-sampled runtime derivatives. AI output is a production candidate requiring human art review, not hand-authored pixel art. Avoid claiming all generated clusters share a perfect native pixel grid.

## Feature mapping / preservation
| Surface | Existing owner | Presentation approach |
|---|---|---|
| 8 stats / survival / time | PlayerStats, SurvivalSystem, TimeManager | reuse PixelHomePresenter + UIManager events |
| money / debt | EconomySystem | reuse HUD values / existing actions |
| 60 missions / age | MissionService, LifeProgression | retain mission UI, reward listeners, list and modal stack |
| save/load | SaveManager / existing buttons | unchanged data schema and slots |
| education / school exams | EducationService, Phase23Presenter | retain actions and eligibility; add result presentation |
| jobs / social / city | Phase23Services | no gameplay changes; future visual production |
| locations | SceneCatalog, SceneTransitionService | retain public Navigate/events; add profile/controller/overlay/load/entry/exit/audio adapters |

## Scope and status
Stage A audited. Stage B targets Home and School only. Stage C and D are NOT claimed complete. See VALIDATION.md for measured results and ASSET_REGISTER.md for missing animation art.
Skills read: unity-cli, craft-game-floors (art-direction, visual-scorecard), 2d-pixel-perfect (pipeline-detection, URP camera setup, API reference), ui, ui-ugui, imagegen; project-local game-ui-ux. create-game-assets and standalone game-feel not found in searched installed/local catalogs. Craft skill explicitly lacks a 2D art production floor; use supplied direction and imagegen.
