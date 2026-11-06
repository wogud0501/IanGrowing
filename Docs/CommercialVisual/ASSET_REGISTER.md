# Asset register and production backlog

## Imported this session
| File under Assets/_Game/Art/Commercial | Purpose / layers | Status |
|---|---|---|
| Backgrounds/Home_Source.png | imagegen bedroom master, furniture/window/city/light baked | AI generated production candidate; original retained |
| Backgrounds/Home_640.png | 640×360, 100 PPU runtime background | integrated in Home |
| Backgrounds/School_Source.png | imagegen classroom master, desks/board/NPCs/light baked | AI generated production candidate; original retained |
| Backgrounds/School_640.png | 640×360, 100 PPU runtime background | integrated in School |
| UI/Panel_Source.png | imagegen navy/brass UI frame master | original retained |
| UI/Panel_64.png | 64×64 9-slice border 10px | integrated in HUD/buttons/reports/transition |
| Home_Detail.asset / School_Detail.asset | separate crop sprites referencing existing background texture | animated monitor tint / sunlight tint; no duplicate texture |
| CommercialUI.spriteatlasv2 | existing 16px icons + new panel texture, padding4, no rotation/tight packing, Point, RGBA32 | V2 enabled; 27 sprites actually packed for Android preview, built-in delivery and prebuild processor configured |

Masters are generated via the built-in imagegen tool, with no downloaded third-party art pack. This is provenance, not a legal guarantee of exclusivity. No identifiable artist or franchise style requested. Human commercial art review remains required. Original Ian source is user supplied; preserve its existing provenance and do not assert new licensing. Existing Nanum font/OFL license retained.

## Actual animation available
- Official Ian: original single pose, integer pixel position breathing. No new limb, blink or face drawings.
- RoomAtmosphere: 8Hz scheduler, independently phased glints/dust, separate monitor/sunlight tint, time-of-day tint; reduced-motion and effects-count controls. NPCs in masters are static.
- SpriteMotionLibrary / SpriteMotionPlayer: configurable duration/FPS/loop per state, fallback to authoritative sprite when frames are absent. Idle has one original frame. Other 12 states have **zero authored frames**.
- Transition: profile-driven unscaled shutter, departure positional cue and entry hooks, covered asynchronous scene load. No drawn door/elevator/walk cycle yet.
- AudioTransitionService: clip adapter present; profiles contain **no audio clips**. No new music or SFX delivered in Stage B.

## Required separate art production
| Filename proposal | Size / frame plan | Layers / use | Status |
|---|---|---|---|
| Ian_Idle.png | 256×384 per frame ×6, 100 PPU, 6fps, 1s loop | body/head/headphones, consistent feet pivot and 3.84-unit source height, matched to original actor scale | pending approved redraw against official reference |
| Ian_Blink.png | same ×3, 10fps, .3s | eyes only, preserve tired expression | pending |
| Ian_Sleep.png | same ×6, 4fps | sleep pose + breathing, bed alignment | pending |
| Ian_Eat.png / Ian_Drink.png | same ×8 each, 8fps | hands + bowl/cup | pending |
| Ian_Study.png / Ian_Work.png | same ×8 each, 6fps | seated body + hands + props | pending |
| Ian_Walk.png | same ×8 per direction, 8fps | feet-aligned side directions | pending |
| Ian_Happy/Sad/Tired/Stress/Interaction.png | same ×4–6 each, 6fps | face/body reactions, restrained expressions | pending |
| Home_Curtain.png | 80×144 ×6, 4fps | transparent curtain foreground | pending |
| Home_WindowWeather.png | 160×128 ×8, 8fps | transparent rain; independent sky behind | pending |
| School_Clock.png | 32×32 ×12 | hands separate from clock face | pending |
| School_NPCs.png | 48×80 ×6 per NPC | students/teacher idle, out of sync | pending (master contains static NPCs) |
| Transition_HomeDoor.png | 96×192 ×8, 10fps | opaque door cover + handle | pending |
| Transition_SchoolArrival.png | 640×360 ×6, 8fps | gate, students, entry foreground | pending |
| City_Back/Mid/Front.png | 640×360 each | skyline/shops/traffic separate parallax | Stage C pending |
| Office_Back/Props/NPC.png | 640×360 + modular props | job-specific variants, overtime lamps | Stage C pending |
| UI_Icons_32.png | 32×32 ×32 icons | consistent hand-authored high detail replacements | pending; existing 16px icons reused |

Furniture expansion socket is present under each Commercial Presentation root. Current furniture is baked into the master; purchasing/moving furniture is not implemented. Full environmental foreground separation needs authored transparent art. Do not call baked NPCs or master rain animated.
