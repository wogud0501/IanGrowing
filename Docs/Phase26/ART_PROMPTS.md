# Phase 2.6 original artwork production

Tool: built-in image_gen (imagegen skill). Official identity reference: Assets/_Game/Art/Characters/Ian/Reference/Ian_OfficialDesign.png. Original reference is unmodified.

## WorldWalkAtlas.png
Destination: Assets/_Game/Art/Characters/Ian/Animations/WorldWalkAtlas.png
Generated draft: exec-2b8c0af0-bf3d-4d63-ae4a-b416575ffdf5.png (not selected: background alpha was 193).
Selected transparency edit: exec-5402ce35-4d63-4d3e-a75f-2f2356d327e7.png, 1536x1024. Inspected alpha at canvas corner and cell gutter = 0.

Production prompt specification: Original crisp pixel-art atlas retaining official Ian identity: tousled black hair, tired half-closed eyes, pale expressionless face, black headphones around neck, black short-sleeved shirt, loose black cargo trousers and black slide slippers. Six columns by four rows on 1536x1024 transparent canvas, 256x256 cells, one full-body sprite per cell. Rows Down/Up/Left/Right. Six successive walk poses with neutral passing, left contact, opposite leg passing, opposite passing, right contact, opposite leg passing; visible leg/slipper/arm movement. Same body scale and foot baseline, no labels, grid lines or props. No weapons, new clothes or replacement identity.

Final transparency edit prompt:
Edit this existing 6-column by 4-row Ian walk-cycle sprite atlas ONLY to remove the background. Preserve all 24 character poses, identity, pixel detail, exact scale, exact cell placement and 1536x1024 canvas dimensions. The current atlas incorrectly has a pale semiopaque white/gray background across the cells. Remove ALL white/gray background haze and ALL ground/cast shadows. Every blank area around and between the sprites, especially the corners and cell gutters, MUST be fully transparent alpha=0, not white, not translucent white, not a painted checkerboard. Keep character hair, skin, black clothes, neck headphones, cargo pants and slide slippers unchanged and fully opaque. Do not redraw or shift any character. This is an isolated game sprite atlas, not a printed sheet. Return a PNG cutout atlas with real clean transparent background.

Quality limits: direction-specific full-body frames have been generated; animation continuity and natural leg motion still require Game View review. This does not supply the remaining Sleep/Eat/Shower/Work action sprites.