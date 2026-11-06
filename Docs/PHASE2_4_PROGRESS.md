# Phase 2.4 — idle RPG progress

Implementation starts only after the mandatory Phase23 audit and restart gate is complete. Current work: UI birth introduction and readable font repair; Phase23 audit.

## Intended integration rules

- One selected automatic activity at a time. No parallel economy, stat model, or manual reward-claim wallet.
- Work/study repeat existing validated actions, preserving their existing game-day cost, school/job conditions, mission signals and work receipts. Starting is bound to the appropriate location, while a valid running activity can continue through scene transitions.
- Real cycle lengths are data, with work tied to WorkingTime and the shared real-seconds/game-day conversion. Completed cycles consume the existing action's game time exactly once. Incomplete cycles carry progress; no invented partial daily wages.
- Offline time uses UTC watermarks, capped at 8 hours by data; negative elapsed is zero, already-accounted intervals cannot repeat. Offline runs discrete bounded activities, not per-frame simulation.
- Survival resource/health/mental thresholds stop automation before a dangerous next action. Existing survival processing remains in TimeManager/SurvivalSystem.
- Rewards and state are durably saved before showing an offline report. Acknowledging the report does not grant rewards again. Save failure must preserve a retryable checkpoint instead of exposing uncommitted gains.
- Combat needs actual health/damage/turn resolution and recorded wins/losses. No existing combat implementation was found under Assets/_Game/Scripts. Future gear connects through a combat-stats interface; equipment/gacha systems are not added.
- Reuse the existing pixel frames, sprites, fonts and screen stack. Home stays a summary, detailed controls belong to School/Workplace/Battle and settings/status panels.

Final implementation, exact tunables, checks, platform limits and changed files will be recorded here after execution.
