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

## Final audit note
Existing v2 source differed from the initial checkpoint (day21 vs day103). Cause not established; neither state was discarded. Original checkpoint and final-observed copy are both preserved. Current legacy source matches its read-only migration backup. See PHASE2_PROGRESS.md. Official artwork hashes match exactly.

## 2026-11-14 공식 보완 — 보행과 독립 외출
이번 사용자 지시가 기존의 미정 보행 해금 시점에 우선한다.
- 0세: 플레이어 보행/WASD·달리기 금지. 기존 침대 돌봄으로 성장 가능.
- 1세: 기본 보행 반드시 해금, 집 안 이동과 첫 걸음 안내1회. 안내·퀘스트 수령 여부는 보행 권한을 막지 않음.
- 2–6세: 일반 보행 가능, 독립 외출 불가. 보호자 동반 외출은 전용 시스템이 존재할 때만 허용하며 이번 QA에서 새로 구현하지 않음.
- 7세 이상: 기존 독립 외출 해금 유지.
- 별도 정책: `WalkingUnlocked = age >= 1`, `IndependentOutdoorUnlocked = age >= 7`.
현재 적용: LifeProgression 상수/속성, WorldPlayerMotor 실행부, GrowthAccess 외출 조건. 첫 걸음 표시는 저장하며 레거시 성인에게 재출력하지 않는다. 연령별 전용 캐릭터 아트는 별도 미완료 항목이다.
