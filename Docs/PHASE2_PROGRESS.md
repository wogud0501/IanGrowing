# PHASE 2 PROGRESS — 완료 보고
완료 범위: Phase 2.0 + Phase 2.1만.
기준: IAN_DESIGN_BASELINE.md / 공유 기획서 v1.0 / 최신 요청 우선.
Unity 6000.6.7f1, MainGame2D, Unity MCP 실제 조작.

## Phase 2.0 분석
- [x] 활성 씬, 모든 _Game 파일, 기존 클래스/프리팹/SO 및 패키지 확인.
- [x] v1.0 기획서 Firecrawl로 조회 성공 (기본 웹 리더는 본문 조회 실패).
- [x] 최초 Git 작업 트리 clean 확인. 셸 실행은 환경 오류였으나 Unity MCP의 읽기 전용 git 프로세스로 상태 확인.
- [x] 변경 전 체크포인트: C:\Users\ADMIN\Ian_Growing/DevelopmentBackups/Phase2-20261103-151253
- [x] 기존 구조를 보존하는 최소 변경안과 충돌 정책 기록.
- [x] Git 기록은 작업 도중에도 새 커밋이 생긴 것으로 관찰. 기존 커밋/다른 변경을 되돌리거나 히스토리를 수정하지 않음.

## Phase 2.1 구현
- [x] 신규 게임만 0세/0G/부채 1,000,000G/지위 없음, 출생 메시지.
- [x] 8개 능력치 및 개별/공통 이벤트. Health는 기존 Energy의 별칭, Stress=100-Mental.
- [x] Age/LifeStage/Experience/Level 분리. 날짜로 나이 증가 없음.
- [x] 여섯 성장 단계/성인 취업·금융 및 지식·사회성·신용 자격 API.
- [x] Phase 2.2용 MissionGrowthDefinition 데이터 계약과 중복 방지 나이 성장 API. 미션 인스턴스 0개.
- [x] 기존 TimeManager의 단일 초/일 시간축. 실시간과 행동 시간이 같은 생존 처리를 통과.
- [x] 허기/목마름 감소, 위험 지속시간 및 회복, 유예 후 생존 실패.
- [x] 낮은 체력의 효율 감소, 낮은 멘탈의 이벤트 확률 보정.
- [x] SO 조건/시간/회복/효과를 분리한 가상 상태이상 2종.
- [x] 식사/음료/수면/병원 버튼; 기본 비용 0G, 유료 치유권 필요 없음.
- [x] 현금/부채/총자산/순자산 구분. 명시적 부채 거래와 소비, 안정적 거래 ID 중복 방지 및 저장.
- [x] v3 JSON, v1/v2 메모리 마이그레이션 및 별도 쓰기 경로. 원본 덮어쓰기 금지.
- [x] 마지막 UTC 시각과 안전한 오프라인 경과시간 계산만 준비. 오프라인 시뮬레이션/보상 없음.
- [x] 8개 게이지/날짜/나이/현금/부채/순자산/위험/상태이상 UI 연결.
- [x] 공식 원본 이미지와 적용 스프라이트 SHA-256 변경 없음.

## 정책과 호환성
기존 ActionSystem의 0/1/2(일/휴식/놀이) API와 기본 수치를 유지. 식사3/음료4/수면5/병원6 확장.
새 게임은 미션 성장 전까지 일하기가 잠김. Phase 2.2 미션이 아직 없으므로 현재 UI에서 성장 미션을 진행할 수는 없음.
기존 v1/v2 파일에는 나이가 없으므로 기존 노동 기능 보존용 추정 나이 19세로 이관. 기존 현금/날짜/체력/스트레스/행복/게임오버는 보존. 부채는 구버전에 없으므로 0, 멘탈=100-기존 스트레스. 이 추정값은 신규 출생 설정과 별개.
Phase 1 번아웃 규칙은 호환 설정에서 유지. 신규 생존 모드는 허기/목마름 위험 유예로 실패를 판정.
손상 수동 로드는 현재 인생을 초기화하지 않음. 원본 복사/자동 쓰기 차단. 새 인생은 사용자가 명시한 경우만 시작.
형식은 v3지만 기본 구버전 파일명은 그대로 읽기 소스로 사용. 신규 기본 쓰기는 ian-black-comedy-v3.json.
커스텀 구버전 슬롯은 .phase2-v3.json 형제 파일로 이어서 저장. 대상 형제 파일도 구버전이면 덮어쓰지 않고 실패.
현금 저장은 기존 int API/오버플로 보호를 유지하고 경제 공개 Cash/자산/부채 계산은 long. 미래 대규모 재화 단계에서 현금 저장 폭을 확장할 수 있음.
재실행 시 저장된 소수 시간/위험/상태는 복원되지만, 실시간 모드라면 직후부터 정상 시간이 흐름. GameManager.realtimeLifeEnabled로 끌 수 있음.

## 실제 검증 결과
단위 기반 검증 7개 통과 (출생, 별칭, 중복 거래, 생존 감소/유예/회복/실패).
Phase 2.1 통합 검증 45개 통과:
- PASS: Newborn age/cash/debt/standing/net worth
- PASS: Birth system message
- PASS: Employment locked before adulthood
- PASS: Date alone never increases age
- PASS: All life-stage boundaries
- PASS: Shared time decays hunger/thirst
- PASS: Meal button and free recovery
- PASS: Drink button
- PASS: Sleep restores health and mental
- PASS: Fictional statuses triggered by data conditions
- PASS: Hospital recovery needs no paid ticket
- PASS: Zero need has grace period
- PASS: Recovery clears danger before deadline
- PASS: Sustained danger causes survival failure
- PASS: Low-health positive yield reduced
- PASS: Low mental changes event chance and fictional state
- PASS: Age, experience and level remain separate
- PASS: Duplicate mission-growth trigger rejected
- PASS: Knowledge/social requirements reject ineligible career
- PASS: Knowledge/social extension point
- PASS: Credit/age eligibility extension point
- PASS: Stable transaction id blocks duplicate grant
- PASS: Explicit consumption rejects negative cash
- PASS: Duplicate billing blocked
- PASS: Debt increase does not spend or grant cash
- PASS: Net worth assets minus debt
- PASS: V3 save readable
- PASS: Eight stats and fractional time round trip
- PASS: Debt/assets/transaction ledger round trip
- PASS: Danger timers/statuses/growth round trip
- PASS: Offline elapsed capped without rewards or simulation
- PASS: Clock rollback/unknown timestamp safe
- PASS: Corrupt load preserves current state and backup
- PASS: Corrupt source not automatically overwritten
- PASS: Legacy v1 preserves recorded stats/day and infers compatibility age
- PASS: Legacy v1 original never overwritten
- PASS: Legacy v2 preserves recorded stats/day and infers compatibility age
- PASS: Legacy v2 original never overwritten
- PASS: Duplicate day settlement rejected
- PASS: All eight ranges and stress relation
- PASS: Pause gates new actions and shared clock
- PASS: Temporary UI displays eight stats, age, cash and debt
- PASS: Care button binding does not duplicate
- PASS: Official Ian sprite unchanged
- PASS: No arbitrary mission campaign authored

Phase 1 호환 검증 40개 통과:
- PASS: Starting to Playing / new game
- PASS: Work button
- PASS: Rest button
- PASS: Play button
- PASS: Reactive HUD
- PASS: Money exceeds 100
- PASS: Per-stat events only when changed
- PASS: Clamps and integer overflow protection
- PASS: Unaffordable action is atomic
- PASS: Paid rest also requires funds
- PASS: Energy/funds guards
- PASS: Free default rest prevents soft lock
- PASS: Recovery leads back to work
- PASS: Pause gates UI
- PASS: Paused action and time blocked
- PASS: Resume button
- PASS: Reentrant actions blocked
- PASS: Configurable action duration and daily events
- PASS: Restoring date does not simulate days
- PASS: Weighted 1:3 selection and zero exclusion
- PASS: Event probability endpoints
- PASS: At least ten data-driven sample events
- PASS: Burnout game over and UI
- PASS: Game over blocks actions
- PASS: New life button
- PASS: Existing save/load button flow
- PASS: Game over persists
- PASS: V1 save compatibility
- PASS: Corrupt save preserved without resetting current life
- PASS: Corrupt original bytes preserved
- PASS: Unsupported future version preserved
- PASS: Automatic save after successful action
- PASS: Missing file preserves active session
- PASS: Work button reached by EventSystem raycast
- PASS: Repeated HUD binding does not duplicate actions
- PASS: HUD re-enable does not duplicate listeners
- PASS: Official character retained in all actions
- PASS: 2D orthographic camera
- PASS: Archived 3D remains inactive
- PASS: HUD prefab is independent
이 검증은 기존 초기 수치의 호환 프리셋으로 실행하며 실제 Phase 2 새 게임의 0세 취업 잠금을 우회해 서비스에 적용하는 기능은 아님.
기존 손상 파일 초기화 테스트는 최신 요구사항에 맞게 현재 상태 보존 검증으로 변경.

추가 검증:
- PASS missing transaction ledger rejected without reset
- PASS even legacy migration-target collision cannot overwrite old saves
- PASS status natural expiry separate from recovery threshold
- 실제 기존 사용자 v2 저장 읽기: 기록된 스탯/현금/날짜 보존, 별도 v3 경로 확인. 실제 원본에 새 저장하지 않음.
- 실제 Play 종료/재진입: 나이7/현금123G/부채100만G/거래ID 복원 확인.
- 시간 진행을 끈 실제 재진입: 8개 스탯/성장/경제/위험123.5초/상태2개/소수 시간 스냅샷 완전 일치.
- 최종 Console 오류 0개, _Game 컴파일 오류 0개, git diff --check 통과.

## 실패/관찰 사항
1. 작성 중 SurvivalSettings 중괄호 누락(CS1513) 발생. 수정 후 단위/통합 검증 통과.
2. Unity DLL 재빌드 중 MCP execute_code가 invalid metadata로 실패. 강제 refresh/compile 및 domain reload 후 도구 복구. 실패한 UI 쓰기는 복구 후 다시 실행.
3. 에디터 도구 호출 문자열에 잘못된 C# 문자 리터럴을 사용한 호출은 실행 실패. 파일 변경 없이 수정 후 성공.
4. 최초 웹 리더는 공유 본문을 읽지 못했으나 Firecrawl 조회는 성공. 피드백 요청은 만료 응답이었으며 기획서 조회 성공 여부와 무관.
5. 기존 v2 사용자 저장의 최종 해시가 체크포인트와 다름을 감지. 시작 당시 21일차에서 최종 103일차로 관찰, 파일 수정 UTC 2026-11-03T15:14:03.8213096Z. 원인은 단정하지 않음. 작업 도중 Git 커밋도 추가로 관찰되어 다른 활동과 동시 변경 가능성이 있음.
   - 체크포인트의 시작 파일을 보존.
   - 최종 관찰 파일도 SavesObservedAtFinalAudit에 별도 보존.
   - 현재 v2 파일은 실제 마이그레이션 시 생성한 .pre-phase2.bak과 바이트 단위 일치. 이 조회 이후에는 변하지 않음.
   - 최신 파일을 체크포인트로 되돌리지 않았음. 기존 저장 전체가 시작 시점과 불변이었다고 주장하지 않음.
   - 나머지 기존 JSON 저장은 시작 시점 해시와 일치.
6. TMP 동적 OS 폰트의 새 한글 glyph 캐시가 UI 테스트 과정에서 변경됨. 캐릭터 이미지 변경/새 아트 제작은 아님.

## 수정/추가 파일 (체크포인트 대비)
- Assets/_Game\Data\DefaultBalance.asset
- Assets/_Game\Data\InitialLife.asset
- Assets/_Game\Data\StatusLowHealth.asset
- Assets/_Game\Data\StatusMentalBuffer.asset
- Assets/_Game\Data\SurvivalBalance.asset
- Assets/_Game\Prefabs\GameHUD.prefab
- Assets/_Game\Scenes\MainGame2D.unity
- Assets/_Game\Scripts\ActionSystem.cs
- Assets/_Game\Scripts\Character2DPresentation.cs
- Assets/_Game\Scripts\EconomySystem.cs
- Assets/_Game\Scripts\GameBalance.cs
- Assets/_Game\Scripts\GameManager.cs
- Assets/_Game\Scripts\InitialLifeSettings.cs
- Assets/_Game\Scripts\LifeProgression.cs
- Assets/_Game\Scripts\MissionGrowthDefinition.cs
- Assets/_Game\Scripts\PlayerStats.cs
- Assets/_Game\Scripts\SaveManager.cs
- Assets/_Game\Scripts\StatusEffectDefinition.cs
- Assets/_Game\Scripts\StatusEffectSystem.cs
- Assets/_Game\Scripts\SurvivalSettings.cs
- Assets/_Game\Scripts\SurvivalSystem.cs
- Assets/_Game\Scripts\TimeManager.cs
- Assets/_Game\Scripts\UIManager.cs
- Assets/_Game\UI\IanKoreanSDF.asset
- Assets/_Game\Scripts\Editor\Phase2Verification.cs
- Assets/_Game\Scripts\Editor\Stage1Verification.cs
- Docs/IAN_DESIGN_BASELINE.md
- Docs/PHASE2_PROGRESS.md
- Assets/_Game/README.md
메타 파일은 Unity가 함께 생성/유지.

## 남은 문제와 미검증
- v2 저장의 작업 중 변경 원인은 확정하지 못함. 두 상태를 모두 보존했으므로 되돌릴 파일을 임의 선택하지 않음.
- Android/iOS/Windows 실제 빌드 및 실기기/다해상도 테스트 미실행. 런타임 코드는 Editor/Windows 전용 API를 사용하지 않음.
- 현재 한국어 폰트는 OS 의존 동적 폰트이므로 모바일 배포 전에 재배포 가능한 포함형 폰트로 교체 필요. 새 패키지/최종 폰트 아트 작업은 하지 않음.
- 자동 테스트는 Button 이벤트와 EventSystem 기반이며 물리적 터치/마우스 장치 입력 자동화는 아님.
- File.Replace 미지원 플랫폼에는 원본 백업을 유지하는 복사 fallback이 있으며, 해당 fallback의 기기 검증은 미실행.
- 신규 밸런스(300초/게임일, 허기8/일, 목마름12/일, 위험48시간)는 데이터로 바꿀 수 있는 기능 검증용 값.
- 미션 실행/60개 콘텐츠/보상, 직업/대출 실행, 세금/주식/부동산, 자동 노동/전투, 오프라인 보상, 최종 아트는 구현하지 않음.

## Phase 2.2 준비
MissionGrowthDefinition와 TryAdvanceAgeForMission(completionId,targetAge), 자격 조건 API, 거래ID, 성장/세이브 계약을 사용해 다음 단계 미션 엔진을 연결할 수 있음.
지식·사회성·신용도 및 Experience/Level은 저장되지만, XP 곡선/자동 레벨 보상/60개 미션은 다음 단계에서 확정.
현재 기본 슬롯 v3와 realtimeLifeEnabled=true를 복원했고 MainGame2D를 저장한 편집 모드로 종료.
검증 메뉴: Play 첫 화면 렌더 후 Ian > Verify Phase 2.1 (Play Mode), 또는 Verify Stage 1 Core (Play Mode).
