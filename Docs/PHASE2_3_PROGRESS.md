# PHASE 2.3 — 교육 · 관계 · 직업 · 멀티 씬

## 완료 범위

Unity 6000.6.7f1에서 Phase 2.3을 구현했다. 기획 공유 링크를 Firecrawl로 열람했으며, 이번 사용자 명세와 기존 프로젝트의 미션/나이 규칙을 우선했다.

- 교육: 기초 생활 학습, 초등교육, 중등·고등교육, 직업교육, 평생교육 데이터. 입학, 독서, 공부, 숙제, 시험, 성적표, 방과 후 학습, 진로, 졸업, 자기계발.
- 시험: 지식·준비도·멘탈·난수 기반 점수. 실패 기록과 재학습·재응시 가능. 성적표는 시험별 한 번 수령. 졸업은 해당 과정의 공부·숙제·합격 성적표 필요.
- 관계: 고유 NPC 3명, 명시적 친구 만들기, 대화 성공/실패, 친밀도 0~100, 관계 유지 비용, 방치 시 친밀도 감소, NPC별 일일 제한과 쿨다운. 사회성 증가나 일반 대화만으로 친구가 생기지 않는다.
- 직업: 무직 / 편의점 직원 1,000G / 사무직 3,000G / 개발자 7,000G / 투자 분석가 10,000G / 기업 대표 / 백수의 왕. 대표는 사업 연동용, 백수의 왕은 특수 해금용으로 일반 채용에서 제외.
- 취업: 19세 제한, 이력서, 지원 자격 검색과 직업 선택, 사회성을 반영하는 면접, 채용/탈락, 근무, 급여, 경력, 승진, 퇴사, 직업별 해고 조건, 재취업.
- 임시 uGUI: 씬별 활동 창, NPC/지원 직업/진로 변경, 성적·관계·현재 직업·지원 가능한 직업·활동 결과·실행 불가 사유·장소 이동. 기존 HUD와 미션 UI 유지.

## 구조와 수정 파일

전체 파일 목록: `Docs/PHASE2_3_CHANGED_FILES.txt` (작업 시작 백업과 해시 비교).

신규 코드:
- `EducationData.cs`, `SchoolActivityData.cs`, `EducationProgress.cs`, `EducationService.cs` (ExamService 포함).
- `RelationshipData.cs`, `SocialActivityData.cs`, `SocialService.cs` (관계 진행도와 사회활동 기록은 EducationProgress.cs의 직렬화 모델).
- `JobData.cs`, `JobService.cs` (EmploymentService, WorkActivityService 포함).
- `Phase23Catalog.cs`, `Phase23Services.cs`, `Phase23Presenter.cs`.
- `Editor/Phase23Builder.cs`: 누락 에셋 생성과 기존 씬에 UI 추가. 기존 씬을 덮어 새로 생성하지 않는다.
- `Development/Phase23Runner.cs`: UNITY_EDITOR 전용 격리 Play Mode 통합/회귀 검증.

기존 확장:
- `GameManager`: 공통 루트에서 모듈 구성, 새 인생 초기화, 새 서비스에서 처리한 행동의 미션 이중 집계 방지.
- `ActionSystem`: 기존 행동 ID 유지, 새 서비스 사전 조건 검사, 자원/시간 단일 소비. 여러 날 행동이 생존 실패로 중단되면 완료 이벤트와 급여를 지급하지 않는다.
- `MissionData`, `MissionService`, `MissionUI`: 고유 카운터와 기능 플래그, 실제 서비스 신호, 행동별 목적지, 예정/잠금 표시.
- `SaveManager`: 선택적 phase23 블록 저장·검증·복원.
- `SceneTransitionService`: City/Workplace에서도 기존 공유 시간 실행 정책 유지.
- `Development/Phase22Runner`: City/Workplace가 없어야 한다는 이전 단계 검사를 이번 단계의 씬 존재 검사로 갱신.

에셋:
- `Assets/_Game/Data/Phase23/`: 과정 5개, NPC 3개, 직업 7개, 활동 21개, 카탈로그 1개. 요구 능력치, 급여, 소비량, 승진/해고 조건, 반응 가중치와 문구를 Inspector에서 조절한다.
- `GameContext.prefab`에 Phase23Services 한 개 연결. 별도 GameManager/SaveManager 생성 없음.
- Home/School에는 씬 전용 UI만 추가. City/Workplace 신규 씬과 SceneRoutes, Build Settings Scene List 연결. 활성 커스텀 Build Profile은 없어 전역 Scene List 사용.
- 공식 이안 이미지와 기존 에셋 삭제 0건. 공식 PNG SHA-256은 작업 전 백업과 동일.

## 행동 · 저장 · 중복 방지

기존 TimeManager는 일 단위 행동을 사용한다. 현재 기본 활동/유효 근무는 게임 내 1일을 소비하며, ActionSystem 한 곳에서 차감한다. 8개 능력치는 기존 PlayerStats를 거쳐 0~100으로 제한한다. 교육/사회/근무 서비스가 시간을 다시 차감하지 않는다.

근무 완료에만 WorkRecord를 생성한다. 지급은 `EconomySystem.TryTransaction(workId + /salary)`를 사용한다. 동일 작업 완료 ID, 동일 급여 ID, 존재하지 않는 근무에 대한 청구를 거절한다. 근무 횟수, 경험치, 지급 여부와 경제 거래 영수증을 함께 저장한다.

기존 v4 슬롯과 v1/v2/v3 마이그레이션 정책을 유지하며, v4 JSON에 선택적 `phase23` 블록을 확장했다. 없는 경우 빈 교육/관계 기록과 무직 기본값을 생성하고 기존 나이·현금·부채·미션은 유지한다. 과거 학교 카운터를 근거로 새 친구·성적·졸업 기록을 소급 생성하지 않는다. 기존 세이브에서 새 과정에 입학해 진행할 수 있다. 새 데이터가 손상된 경우 기존 저장 보호 경로로 읽기를 거부한다.

스타터 장비는 M020 수령 후 명시적으로 장착할 때 한 번 지급하는 무료 튜토리얼 자격이다. 기존 IPendingMissionRewardGateway로 기존 대기 보상도 처리하며, 아이템/지급 ID/장착 상태를 저장한다. 장착에는 능력치 보너스를 붙이지 않았으며 공식 캐릭터 외형도 바꾸지 않는다. 가챠권은 기존 대기 보상으로 남는다.

## 미션 연결 및 남은 의존성

- M011~020: 실제 교육/친구 만들기 활동 연결. 기존 이름·보상·미션 기반 나이 성장 유지.
- M021~030: 실제 플레이 완료. M024/025 스타터 장착, M030은 기존 선행 미션에 더해 중등·고등 과정의 실제 졸업 이벤트 필요. 완료 시 19세.
- M031~034: 이력서, 면접 응시, 채용, 첫 급여 실제 플레이 완료.
- M035: 세금 기능 없음. 별도 Taxation 기능 플래그로 Planned, Phase 2.5 표시.
- M036: 기존 EconomySystem 원금 상환 연동. 현금/부채 100G가 함께 감소하고 실제 상환량을 집계. M035 미수령으로 순차 진행에서는 잠금.
- M037: 실제 근무 3회 조건을 격리 테스트에서 독립 검증. M036 선행 잠금을 유지하며 자동 수령하거나 건너뛰지 않는다.
- M038: 주식 매수 Phase 2.5 예정.
- M039: 기존 EventManager에 선택 기록 기능이 없어 Phase 2.8 예정.
- M040: 모든 선행 조건이 충족되기 전 완료 불가. 세금/주식/선택 의존성 때문에 이번 단계에서는 30세 성장까지 진행되지 않는다.

기업 대표 수익/사업, 백수의 왕의 실제 특수 해금 조건, 복잡한 NPC 대화·직업 이벤트 분기는 후속 단계용 데이터 경계만 준비했다. 미구현 경제/가챠/주거/자동 전투/오프라인 보상은 구현하지 않았다.

## 실제 검증 결과

Windows Unity Editor Play Mode / Unity MCP에서 최종 주 통합·회귀 693개 + 성인 교육 보강 44개 = 737개 검증 항목 PASS, 실패 0.
- 신규 통합 검증 439개: 실제 행동 가용성·실행·시간·돌봄 검사를 포함한다.
- Phase 1 회귀 40 PASS.
- Phase 2.1 회귀 45 PASS.
- Phase 2.2 회귀 169 PASS. 새 선택 모듈을 비활성화한 격리 환경에서 이전 계약을 검증하고, 새 계약은 별도 통합 경로에서 검증했다.

상세 결과: `Docs/PHASE2_3_TEST_RESULTS.json`, `Docs/PHASE2_3_ADULT_SMOKE_RESULTS.json`. 성인 교육 보강은 19/30/60세의 격리된 나이·자원 픽스처에서 실제 입학·학습·자기계발·시험·성적표·졸업을 실행하고 상태를 원복했다. 실행 코드는 `Docs/PHASE2_3_ADULT_SMOKE.cs`에 보관한다. 동일 평생교육 과정 재입학 거절을 확인한 초기 픽스처는 나이별로 교육 기록을 격리하여 수정했다.

확인한 주요 시나리오:
- 실제 행동으로 M001~034 진행, 초등 입학, 독서 3권, 숙제 5회, 공부/지식 증가, 청소년 교육과 졸업, 19세 성장.
- 낮은 준비도의 실제 시험 실패 → 성적표 → 졸업 거절 → 공부 → 재시험 합격 → 졸업 가능.
- 친구 생성, 대화만으로 친구가 생성되지 않음, 사회성 15/25, 친밀도 증가·감쇠, 반복 상호작용의 무비용 거절, 실제 대화 실패.
- 19세 미만 취업 차단, 이력서/면접/채용, 출근 3회, 급여/경험치/체력/멘탈, 중복/위조 급여 거절, 승진, 퇴사·해고·재취업, 중단된 3일 근무의 급여 미지급.
- 새 저장 데이터 전체 roundtrip, 모델 재복원, 저장 후 급여 재청구 거절, 실제 작업 전 v4 JSON의 무손실 기본값 마이그레이션, 기존 v1/v2/v3 회귀.
- MainMenu → Home → School → City → Workplace → Home에서 능력치·재화·나이·관계·직업 보존과 서비스 단일성.
- 미구현 미션의 예정 표시와 순차 잠금, 주식/선택 카운터 자동 생성 및 M040 자동 완료 방지.
- 장면 UI Button.onClick을 통한 대화/승진 실행. CanvasScaler와 기존 InputSystemUIInputModule 재사용.

최종 Play Mode 실행 후 Console error 조회 0건, C# 컴파일 오류 0건. 앞선 편집 중 Unity AI 패키지의 NoSubscription 메시지와 중간 파일 추가/에디터 어셈블리 참조 오류를 관찰했으며, 소스 참조와 테스트 리플렉션을 수정한 후 최종 검증을 통과했다. 백그라운드 프레임 정지는 런타임 runInBackground 및 Game View 포커스로 해결했다.

School/Workplace 화면 캡처: `Docs/Phase23Captures/school.png`, `Docs/Phase23Captures/workplace-1.png`. 새 씬의 카메라는 2D 단색 플레이스홀더를 사용한다. 런타임 동적 폰트 캐시가 에셋을 변경한 것은 작업 전 원본으로 복원했다.

검증은 별도 `phase23-suite-*`, 기존 회귀 전용 슬롯을 사용했다. 실행 중 원래 상태와 설정을 복원했다. 작업 시작 LiveSession은 `DevelopmentBackups/Phase23/LiveSession.json`에 별도 보존한다. 테스트 중 생성한 미래 조건 제공자·해고/대화 반응·시험 실패 픽스처는 격리 환경에 한정하고 원복했다. 실제 M001~034 진행은 가짜 제공자를 사용하지 않았다.

미검증:
- Android APK 및 Windows standalone 실제 빌드/기기 실행.
- 실제 터치 기기, 다양한 화면 비율, OS 종료/강제 종료·저장 쓰기 중 프로세스 종료, 장기간 플레이 밸런스.
- 사업/특수 직업 해금, 후속 경제·이벤트 기능의 실제 플레이.

## Phase 2.4 준비 상태

수동 근무 진입점 `GameManager.TryAction(0)`와 자격 검사, 공유 시간, 유효 근무 기록, 경제 지급 영수증, 저장 경계가 준비되어 있다. Phase 2.4는 이 경계를 재사용해 자동 노동의 예약·중단 정책과 자동 전투·오프라인 보상을 별도로 설계할 수 있다. 이번 작업에는 자동 실행 루프나 오프라인 지급을 넣지 않았다.

최종 씬 검사: 6개 씬 모두 Missing Script 0. Bootstrap에만 GameManager/SaveManager 각 1개, 나머지 씬 각 0개. 상세: Docs/PHASE2_3_SCENE_VALIDATION.txt. 에디터는 Play Mode 종료 후 Bootstrap에서 대기한다. git diff --check 종료 코드 0 (줄바꿈 안내만 존재).

