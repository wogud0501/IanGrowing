# Family home: executed verification

Unity 6000.6.8f1 / OSXEditor / actual Play Mode. Assertion counts contain repeated startup checks; do not sum them as unique tests.

| Scope | Result | PASS assertions | UI issues | Evidence |
|---|---|---:|---:|---|
| FAMILYARCHITECTURE | Completed | 48 | 0 | [JSON](FAMILYARCHITECTURE_RESULTS.json) |
| FAMILYHOUSEHOLD | Completed | 30 | 0 | [JSON](FAMILYHOUSEHOLD_RESULTS.json) |
| FAMILYINTERACTION | Completed | 26 | 0 | [JSON](FAMILYINTERACTION_RESULTS.json) |
| FAMILYCHECK | Completed | 35 | 0 | [JSON](FAMILYCHECK_RESULTS.json) |
| FAMILYDOORS | Completed | 60 | 0 | [JSON](FAMILYDOORS_RESULTS.json) |
| CRITICALFACILITIES | Completed | 67 | 0 | [JSON](CRITICALFACILITIES_RESULTS.json) |
| CRITICALVISITORS | Completed | 13 | 0 | [JSON](CRITICALVISITORS_RESULTS.json) |
| FACILITYMOBILE | Completed | 48 | 1 | [JSON](FACILITYMOBILE_RESULTS.json) |
| FACILITYTERMINALS | Completed | 13 | 0 | [JSON](FACILITYTERMINALS_RESULTS.json) |
| PRE28FAILED | Completed | 19 | 0 | [JSON](PRE28FAILED_RESULTS.json) |

Final registry: 146 scenarios. {'PASS': 128, 'MANUAL': 5, 'NOT_IMPLEMENTED': 13}. See [individual results](REGISTRY_FINAL_146_RESULTS.json).

The full run initially failed only the new family architecture scenario because it requested travel before the previous internal transition settled. After adding the wait, only the failed scenario was rerun. Keep REGISTRY_BEFORE_RETEST.json, PRE28FULL_BEFORE_RETEST.json and PRE28FAILED_RESULTS.json together as evidence.

The shared dining source has nonuniform row heights; measured alpha components (ATLAS_ALPHA_COMPONENTS.json) informed the Unity sprite rectangles and upper-body regions. Source PNG pixels were not modified in code. The final real shared-meal animation was rerun after this correction.

The initial meal assertion ignored concurrent consumption by both parent NPCs. The corrected test starts its stock measurement after those two meals settle, then checks one portion for Ian, a released seat and restored control. Initial failure retained in HOUSEHOLD_CONCURRENT_MEAL_CHECK.json.

The three production save files match SAVE_BASELINE.json after every verification run. This preserves the user-confirmed age1/cash100 progress. Final save/guard release evidence: FINAL_EDITOR_STATE.json, SAVE_GUARD_RELEASED.json and FINAL_PRESERVATION.json.

Physical keyboard focus, Android/iOS/Fold devices, native builds, thermal/FPS behavior and every dedicated gesture/direction art are not certified by these Editor checks. Thirteen unimplemented and five manual cases remain honestly marked.
