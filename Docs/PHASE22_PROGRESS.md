# Phase 2.2 완료 기록

- 엔진: Unity 6000.6.7f1 (Unity 6.6), 2D.
- 범위: 메인 미션 60개 데이터, 001~020 실제 플레이, 성장/보상/저장, Bootstrap/MainMenu/Home/School.
- 기준: 사용자 Phase 2.2 명세와 멀티 씬 추가 지침. M011~M020은 명세상 원본 확인 명칭으로 표시하며, 나머지는 UserProposal로 구분한다.
- 체크포인트: DevelopmentBackups/Phase22-20261103-160503. 기존 미커밋 변경을 보존한 파일 백업이며 별도 Git 커밋은 만들지 않았다.

## 기존 프로젝트 분석

기존 MainGame2D에는 GameManager, PlayerStats, TimeManager, ActionSystem, EventManager, EconomySystem, SaveManager, 생존/상태이상/LifeProgression 및 Canvas HUD가 있었다. 공식 캐릭터와 JSON v3 수동/자동 저장을 재사용했다. 기존 3D MainGame 및 2D 원본 씬은 삭제하지 않았다. 단일 씬에 결합된 서비스와 UI만 새 공통 컨텍스트/장소별 프레젠터로 점진적으로 분리했다. 기존 패키지를 이용하며 외부 패키지는 추가하지 않았다.

## 구현 파일

- 데이터: Scripts/MissionData.cs, MissionCatalog.cs, MissionProgress.cs, MissionConditionEvaluator.cs, MissionCatalogValidator.cs; Data/Missions/M001~M060.asset, MainMissions.asset.
- 실행: MissionService.cs, MissionRewardService.cs; 기존 GameManager, LifeProgression, EconomySystem, ActionSystem 확장; LifeStageService/AgeProgressionService.
- 학교 최소 행동: ActivityData.cs, ActivityCatalog.cs, Data/Activities 8개, BasicActivities.asset.
- UI: MissionUI.cs, MissionRowView.cs, Prefabs/MissionHUD.prefab, MissionRow.prefab 및 기존 GameHUD 연결. 신규 버튼과 기존 버튼 중앙 정렬 유지.
- 씬: GameContext.cs, SceneTransitionService.cs, SceneCatalog.cs, BootstrapEntry.cs, MainMenuPresenter.cs, LocationPresenter.cs, GameplaySceneBindings.cs; GameContext.prefab, SceneRoutes.asset, 4개 씬.
- 저장: SaveManager v4. 활동 ID/카운터, 진행도, 수령/성장/해금/보상 대기 기록을 함께 저장.
- 에디터: MissionDataBuilder, MissionDataInspector, MissionUIBuilder, MultiSceneBuilder.
- 검증: Scripts/Development/Phase22Verification.cs 및 Phase22Runner.cs는 전체 UNITY_EDITOR 조건부 컴파일. 이전 회귀 검사 3종 갱신.

## 플레이 범위와 규칙

001~020은 실제 UI 버튼으로 순서대로 진행할 수 있다. 유아기에는 무료 식사/음료/수면/휴식을 이용하고, 010 완료로 7세가 된 후 학교로 이동한다. 학교 입학, 독서, 친구 만들기, 숙제, 시험, 성적표, 방과 후 학습, 공부는 데이터 기반 행동이다.

021~060은 데이터와 조건만 등록했다. 실제 서비스가 없는 조건은 Planned로 표시되며 자동 완료하거나 가짜 직업/주식/아이템을 만들지 않는다. 미래 조건 검증에는 에디터 전용 제공자만 사용했다.

날짜는 나이를 올리지 않는다. 미션 완료의 성장 마일스톤만 적용한다. 010→7세, 019→12세, 020→13세이며 020은 019의 나이 12 조건을 읽어 순환하지 않는다. 레벨은 XP와 별도 계산한다. 아이템/가챠권은 서비스 미구현 시 pending entitlement로 보관한다.

## 실제 검증 결과

- Phase 2.2 Play Mode 비동기 검증: 169 PASS, 실패 0.
- 기존 Phase 2.1 회귀: 45 PASS.
- 기존 Phase 1 회귀: 40 PASS.
- 수동 저장 체크포인트 회귀: 4 PASS.
- 실제 001~020 UI 진행 및 수령 완료: 13세, XP 400, 레벨 5, 1,050G, 성장 포인트 20. 해당 수치는 격리 테스트 상태다.
- 숙제 2회 저장 → 진행/보상 수령 → 불러오기: 이전 진행도/나이/영수증 복원.
- MainMenu→Home→School→Home 및 반복 전환: 동일 컨텍스트/서비스 유지.
- 미래 021~060: 에디터 이벤트 제공자로 조건/보상/19·30·60세 마일스톤 검증. 플레이 가능 판정과 구분.
- 중복 수령/활동 ID, AND/OR, 아이템 소유 조회, 선택 결과, 지연 보상 재진입 방어 검증.
- Play Mode 종료 후 재시작: 별도 proof 슬롯에서 M021 활성, 20개 수령, 13세, 1,050G, 카운터 11종/활동 ID 36개/해금 3개/대기 보상 1개 복원. 중복 수령 거부.
- 신규 v4 및 구버전 v1/v2/v3 마이그레이션/보호 검사 통과.
- 원래 존재하던 저장 55개는 체크포인트와 줄바꿈 정규화 비교 시 내용 차이 0. 백업 생성 과정의 LF/CRLF 차이 때문에 바이트 동일 주장에는 사용하지 않았다.
- 공식 원본/컷아웃 PNG SHA-256 모두 작업 전과 동일.
- 4개 씬 및 GameContext 프리팹 Missing Script 0. 장소/메뉴에는 GameManager/SaveManager 0개, Bootstrap에 각각 1개.
- 마지막 Unity Console error 조회 0개. git diff --check 종료 코드 0 (Git 줄바꿈 안내만 존재).

자세한 실행 항목은 PHASE22_TEST_RESULTS.json을 참고한다. 초기 시도 중 발견한 카탈로그 MonoScript 참조 누락과 에디터 폴더 테스트 컴포넌트 부착 문제는 파일 분리/에디터 전용 Development 러너로 수정 후 재검증했다. 초기 테스트가 새 v4 기본 슬롯을 사용한 문제도 수정해 테스트 슬롯을 자체 격리하며, 해당 테스트 v4 파일은 체크포인트 TestEvidence에 보관했다.

## 사용자 상태 보존

v3 수동 체크포인트를 v4 수동 파일로, 작업 시작 당시 진행 중이던 LiveSession을 v4 자동 파일로 각각 검증 후 옮겼다. 사용자 원본 v1/v3 파일을 덮어쓰지 않았으며 새 게임을 강제 시작하지 않았다. 새 v4 자동 복원도 7일차/0세/0G/부채 1,000,000G/허기51/목마름26/미션 M001로 확인했다. 테스트의 20개 완료 상태를 실제 사용자 슬롯에 남기지 않았다. 에디터는 Bootstrap에서 Play 준비 상태이며 실시간/생명주기 저장 설정을 정상값으로 복원했다.

## 남은 범위 / Phase 2.3 준비

- 장비/직업/자동화/금융/주거/연쇄 이벤트/생애 평가 정식 시스템은 이후 단계다. 미래 8개 씬은 아직 생성하지 않았다.
- IMissionFeatureSource로 실제 서비스 가용성/활동 이벤트/아이템 조회를 연결하고, 안정적인 이벤트 ID를 재사용해야 한다.
- IPendingMissionRewardGateway는 동일 보상 ID에 대해 외부 서비스도 멱등성을 보장해야 한다.
- Phase 3 아트·애니메이션 미진행, 공식 캐릭터 유지. 유아기 아트도 아직 별도 제작하지 않았다.
- Android/iOS/Windows 실제 플레이어 빌드·기기·터치·모바일 폰트 패키징 검사는 실행하지 않았다. 현재 검증은 Windows Unity Editor Play Mode다.
