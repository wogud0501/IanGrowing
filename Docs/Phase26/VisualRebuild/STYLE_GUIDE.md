# Phase 2.6 Visual Rebuild — production direction, not visual approval

The previous Unity screenshots are failure examples, never style inputs. Keep original character identity, action IDs, input map and saves.

## Sources and interpretation
- Eastward official media: https://eastwardgame.com/media/ — environment craft, deliberate clutter, material contrast and hue-shift shadows; do not copy sprites/layouts.
- Fields of Mistria official press kit: https://www.fieldsofmistria.com/presskit — restrained furnishing proportions, coordinated interiors.
- Stardew Valley official media: https://www.stardewvalley.net/media/ — useful compact routes and legible interaction fronts.
- Little Witch in the Woods official developer store: https://store.steampowered.com/app/1594940/Little_Witch_in_the_Woods/ — local ambience and small contextual motion.
No third-party commercial asset is imported into the game.

## Common rendering contract
| Item | Target |
|---|---|
| View | Orthogonal 3/4; horizontal rear wall, shallow visible furniture tops. No mixing diagonal isometric furniture. |
| Ground tile | 32 logical pixels, 0.8 existing world units. |
| Pixel density | 40 logical pixels/world unit. Generated atlas is a candidate upscaled source: normalize importer PPU by source width / 384 × 40. Inspect actual clusters; generation alone does not prove grid compliance. |
| Camera | Existing orthographic camera, indoor 4.5 half-height. At 1280×720 this gives 80 screen pixels/world unit, intended 2× logical pixels. Direct Display 1 output, no offscreen camera-only presentation. |
| Texture | Point; no mipmaps, compression or non-power-of-two resampling. Full Rect sprite meshes for tiling. Native atlas size retained; no independent height normalization of each furniture sprite. |
| Actor | Existing official Ian walking frames/Animator retained. Environment pass uses common actor scale to fit a roughly 48 logical pixel tall person; official art is never overwritten. Character pixel cleanup/action animation remains a separate incomplete item until seen. |
| Dimensions | Person about 1.2 world units; door/fridge about 1.5, bed visual length about 1.5; use measured native sprite bounds before approving. |
| Palette | Ivory #e5d8b9, oak #a56a3f, shadow plum #373344, jade #568d82, slate #526977, brass #c99b51. Avoid grayscale shadow multiplication alone. |
| Light | Warm 2700K lamp feel / amber highlights; cool blue window at night. Small localized glow rather than full-screen yellow haze. |
| Shadows | Contact shadows at ground pivot; sprite order 1000 − round(groundY × 100). Structural wall base determines occlusion. Rugs/floors remain behind actors. |
| UI | Existing Galmuri/TMP fallback preserved. Body/controls 22–26 reference pixels; consistent padding; keep world center free. |

## House plan before furniture
World X/Y, north is up. Bedroom [-6,0] × [-0.75,2.5]; kitchen [0,6] × [-0.75,2.5]. A real north/south wall at X=0 separates them. Bedroom doorway gap X[-3,-1.5] at Y=-0.75 opens into living/hall [-3.5,6] × [-4,-0.75]. Front-left rectangle is outside the house: no floor and a solid boundary. This produces an L-shaped footprint instead of a huge rectangular empty floor.

Kitchen has a continuous native-scale worktop/fridge rail, dining below it. Bedroom has a bed/window wall, bedside lamp, desk/chair and wardrobe; cupboard/desk fronts remain accessible. Entry stays at X=0 near Y=-4, with shoes/coats beside the route. Existing separate jade bathroom remains behind the northern door, using its own tiles/walls and existing sink/toilet/shower actions.

The old white diagonal kitchen-divider is deactivated, not renamed as a finished architectural wall. New structure is made of authored wall modules with matching colliders and real doorway gaps.

## Validation gates
Before scene save: native scale/pivot and sprite direction review, static footprint overlaps, wall/door occupancy, actual approach points and flood-fill connectivity. Migrate only invalid furniture placements; retain ownership and put overflow in storage. Run actual scene routes and one day cycle only after applying the layout. Capture home/bedroom/kitchen/bathroom at 1280×720; city day/night follow only once home is visually acceptable.

Generated atlas review and Game View review are separate. No commercial-quality completion claim based only on an atlas, code or passing geometry checks.
