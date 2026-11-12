# Critical recovery — confirmed design and corrections

Sources read locally: Docs/IAN_DESIGN_BASELINE.md, Docs/MAIN_MISSIONS_60.md, Docs/Phase24/OFFICIAL_SOURCE.md, Phase23 activity/education/job assets, LifeProgression.cs. The original shared conversation was not fetched again; claims below refer to the preserved excerpts and current user instruction.

| Confirmed rule | Previous implementation conflict | Correction / validation |
|---|---|---|
| Stages 0–6 / 7–12 / 13–18 / 19–29 / 30–59 / 60+; age advances by mission, not calendar | Growth model correct; newer world routes ignored it | Keep model and all 60 missions; boundary tests 0/6/7/12/13/18/19/30 |
| Independent travel from Childhood (user confirmation in this session) | Door, Travel and debug scene routes allowed newborns | GrowthAccess used at execution, navigation, restore; home/bathroom remain accessible |
| Employment and finance from 19 | Employment service checked age; workplace entrance, stock Trade and property sale did not | Preserve job requirements; gate entrances and direct financial commands |
| Existing basics education 0–6 and free care | LifeWorld redirected care to unavailable adult furniture; basics activity17 only listed City/School | Existing bed context invokes care and basics at home, no free optional furniture |
| Existing saves retain progress | Newborn saved outside home could resume prohibited activities | Normalize only inaccessible location to home; retain holdings, debt, missions and inventory |
| Nine working NPC definitions | All used workplace and convenience job placeholder; work art faced right with monitor behind | Retain IDs/schedules; five office, two analyst branch, two brokerage administration roles; authored front workstation presentation |

## Door findings
- Baseline actual Play test: 213 checks, 16 doors, both approach sides, duplicate interaction rejection, destination and fixed transforms. Broad failure reported by user was not reproduced in this baseline; do not claim every reported symptom explained.
- Confirmed code defect: shop-exit inherited 07–23 entry restrictions, preventing late egress. Exit bypasses opening hours, explicit locked flag remains authoritative.
- Confirmed code defect: StepTo deadline used absolute unscaled time even during pause. Timeout now counts active movement time.
- Teardown MissingReference recorded for WorldPlayerMotor; door Draw/Occupied now check Unity object lifetime before accessing the player. Further Play teardown validation required.

## References
NYSE official Ramp article and Perkins Eastman trading-floor page were read; the latter photograph was actually viewed (NYSE.jpg). These describe an exchange floor, not evidence that the game's small customer investment room must become a full exchange. Existing retail investment interface is retained as a local securities branch. Ziptoss branch11 text was read; photo download failed, so no image analysis claimed. No commercial reference art copied into game assets.
