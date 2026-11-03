# IAN DESIGN BASELINE — v1.0 / Phase 2.0–2.1
Source: https://chatgpt.com/share/6ae9f8e1-a144-83ee-99f0-06048d06d9c5
Firecrawl retrieved the shared conversation including its v1.0 design. Initial web reader returned no content; Firecrawl succeeded.
Authority: latest user request > v1.0 section > provisional implementation balance. Later conversation suggestions (90 missions, offline rewards, alternate starter) are not adopted.
Engine/platform: Unity 6000.6.7f1, 2D; platform-neutral runtime for Android/iOS/Windows. No build-target switch or platform build claimed.

## Scope
Mission-driven life from age 0 to old age; NOT a 30-day survival game.
Only Phase 2.0 and 2.1. No mission campaign, jobs, loans, stocks, combat, automatic work, offline rewards, IAP, new character art.
Official Ian original and applied sprite are immutable for this phase.

## Existing analysis
Phase 1 already has GameManager, PlayerStats, TimeManager, ActionSystem, EventManager, EconomySystem, SaveManager, independent GameHUD, weighted LifeEvents (12), version 1/2 saves.
Conflicts: initial money 50 vs newborn 0; no debt/age; only 3 core stats; burnout instant game over; day/action clock only; automatic overwrite of old save; work available immediately.
Reuse: retain classes, GUIDs, fields/API aliases, action ids 0/1/2, event data, scene/prefab references. Extend rather than create parallel managers.
Safety: git status --short clean before edits; checkpoint C:\Users\ADMIN\Ian_Growing/DevelopmentBackups/Phase2-20261103-151253. No existing saves reset.

## Newborn-only state
Age 0; cash 0G; debt 1000000G; social standing none; net worth total assets minus debt.
Initial configurable prototype defaults: hunger 100, thirst 100, knowledge 0, health 100, mental 100, social 0, credit 50, happiness 50.
Birth message appears on a genuinely new session, not when loading an existing save.
Existing Phase 1 balance defaults retained as compatibility preset; a separate life-settings asset supplies new-game values.

## Stats and migration
Eight values clamp 0..100. Health uses the existing energy storage/API. Stress = 100 - Mental; legacy stress converts to mental without losing its value.
Legacy old-money/energy/happiness/day and game-over remain unchanged. New fields use explicit migration defaults, not newborn financial reset.
Legacy files have no age: compatibility migration assigns adult age 19 so existing Work remains available; debt defaults 0, inferred fields documented in UI/report.
Age never advances with dates. LifeStage derives from age: 0–6,7–12,13–18,19–29,30–59,60+.
Age, level and experience are separate. Only mission-growth API can advance age; no missions authored in this phase.
Employment/finance require adult stages. Knowledge/social/credit eligibility APIs exist without job/loan execution.

## Simulation / provisional data
One simulation clock, 86400 simulation seconds/day. Real-time progression is configurable; action days use that same clock.
Prototype decay: hunger 8/day, thirst 12/day; zero-need danger grace 48 game-hours, not immediate death.
Free meal/drink/sleep/hospital ensure no paid-ticket dependency. Effects/costs/clock scale/grace/low-health efficiency are editable data.
Low health causes fictional fatigue status and reduced positive action yield. Low mental alters event chance and fictional stress status.
Statuses: separate SO trigger, duration, recovery threshold, effect values; no medical diagnosis or real-world treatment claims.
Offline: UTC timestamp and safe clamped elapsed calculation only; no time simulation/rewards on reconnect.

## Save policy
v3 records all eight stats, progress, debt/assets, transaction ids, danger timers/statuses, clock remainder, UTC stamp and daily settlement cursor.
v1/v2 are read-only migration sources; new writes go to a separate v3 path. Preserve source + backup. No forced reset on failed manual load.
Money remains nonnegative; explicit stable transaction ids are persisted for deduplication. Debt transactions never masquerade as consumption.
Desktop File.Replace has a non-destructive backup fallback for platform support; no platform-specific runtime dependencies.
