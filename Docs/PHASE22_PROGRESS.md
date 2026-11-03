# Phase 2.2 — implementation plan / design
Unity 6000.6.7f1, MainGame2D. Preserve current uncommitted save-fix changes and official artwork.
Checkpoint: C:\Users\ADMIN\Ian_Growing/DevelopmentBackups/Phase22-20261103-160503
Source precedence: user's current 60-mission specification; retain exact names M011–M020 from v1.0. Other names tagged UserProposal.
Architecture:
- MissionData/MissionCatalog assets, enum-based condition tree, explicit IMissionStateProvider.
- One MissionService component in existing Game Systems; no singleton or duplicate economy/time managers.
- Complete and claim are distinct. Completion applies monotonic age milestone once; reward claim is idempotent via economic/XP/wallet receipts.
- Age/stage events extend LifeProgression, level/XP separate. Restore/new life may load older age; normal progression never decreases.
- Existing successful action producer emits stable event IDs, persisted counter/seen-event records.
- Basic educational actions extend ActionSystem with data: enrollment, reading, friendship, homework, exam, report card, after-school and study.
- M001–M020 playable. M021–M060 registered with feature dependencies; no fake activity buttons or future system simulation in player builds.
- Future item/ticket rewards are pending entitlements, not inventory items.
- Reusable mission main panel + scrollable 60-row list, with reward/claim/next/notification UI. Keep existing save/load and center-aligned buttons.
- Save v4 stores mission/counter/unlock/receipt data and migrates v3 without resetting stats, money, debt or age. Separate manual/automatic channels remain intact.
- Tests: catalog/refs/cycles, actual UI actions M001–M020 with survival care, editor-only fake provider M021–M060, AND/OR, dedup, milestones, v3 migration, restart and save/load rollback, Phase2.1 regressions.
Balance choices not specified by user remain editable provisional data. No final art or complete job/stock/gacha/housing/ending services.
