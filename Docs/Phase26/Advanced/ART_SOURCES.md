# Advanced Life original sprite sources

Created with the built-in image_gen tool on 2026-11-12. Original PNGs remain in the Codex generated_images directory; project copies are in Assets/_Game/Art/AdvancedLife. No commercial game's sprites were copied. Original official Ian reference and existing walk sheet were not overwritten.

Prompt set and intended layouts:
- IanActions.png — official Ian identity preserved; exact 8×8 atlas, each row contains two four-frame actions. Sleep/Wake Up, Sit Down/Stand Up, Eat/Drink, Wash Face/Shower (fully clothed symbolic game depiction), Work/Use Computer, Use Phone/Interact, Open Door/Pick Up, Put Down/Thinking. Transparent background, consistent full-body size/baseline, actual changed hand/body drawings.
- IanRunTurnStop.png — official Ian and existing pixel style; 4×8 atlas. Four directions of running, front-to-back and back-to-front turns, front and right-profile stopping. Four frames per row.
- IanEmotions.png — official Ian identity; 4×8 atlas. Happy, Sad, Tired, Angry, Stressed, Surprised, Thinking, Idle blink. Dark clothes/headphones/slippers unchanged; restrained emotion.
- CitizenDirections.png — reference four citizens retained in 2×2 character quadrants; each quadrant is four directions × four walk frames. Both profile rows actually face right in the generated result. Import metadata therefore uses the reviewed right-facing row with flipX for LEFT. Front/back rows are separate original frames. This is not described as a distinct hand-authored left profile.
- Doors.png — 6×3 atlas: oak left-hinged domestic, teal right-hinged bathroom, paired glass sliding. Fixed outer frames and six authored aperture stages; reverse playback closes each type. Open apertures are transparent.

Import: Point, no mipmaps, no compression, source aspect ratio, full cell rectangles, foot pivots; CPU-readable pixel data only during slicing. Near-transparent edge alpha (including 1/255 corners) is retained in the original PNG and rejected at rendering by the existing alpha-cut material. The source pixels are not edited in code.

Limitations: most non-locomotion player action clips are front-facing, with a right-facing Open Door clip; additional exact up/left/right action art is still needed where no exact directional clip exists. Runtime reports UsedDirectionFallback rather than claiming complete directional action coverage. Emotion sheets exist but final commercial art/animation quality remains subject to review at game scale.

Resident action production follow-up:
- CitizenActions.png is preserved as the rejected first action sheet. Its packed row heights did not match a uniform grid and it is not referenced by the final resident animation banks.
- CitizenActionsV2.png was generated with the same four citizen identities, explicitly requesting a square 8×8 atlas and constant anatomical scale: sit, eat, work at a persistent laptop, lie down/sleep. Generation still packed rows unevenly. The importer therefore uses reviewed row boundaries refined against transparent-row troughs, for this source and the reviewed CitizenDirections/IanActions sources. This changes Unity Sprite rectangles and pivots only; it does not alter PNG pixels.
- Original NPC and player source files remain intact. The selected V2 sprites are connected to all twelve residents. Final Game View captures verify complete bodies and separated dining seats; the earlier clipped-frame captures/results are retained as evidence of the correction.
- Most action directions beyond the supplied profile/front clips remain fallbacks. Correct four-direction locomotion is independently provided, including actual front/back frames and reviewed mirrored profiles.
