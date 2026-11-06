# Phase 2.4 — idle RPG progress

구현 및 Play 검증 완료. [Phase 2.3 감사](PHASE2_3_AUDIT.md)의 실제 기능/재시작 검증을 통과한 뒤 구현을 시작했다. 기존 60개 미션, 나이 성장, 경제, 교육·관계·직업, 저장 v4와 상업용 픽셀아트 씬을 유지했다. 아래 플랫폼 빌드 결과는 별도 파일로 기록하며 실기기 검증과 구분한다.

## 통합 규칙

- One selected automatic activity at a time. No parallel economy, stat model, or manual reward-claim wallet.
- Work/study repeat existing validated actions, preserving their existing game-day cost, school/job conditions, mission signals and work receipts. Starting is bound to the appropriate location, while a valid running activity can continue through scene transitions.
- Real cycle lengths are data, with work tied to WorkingTime and the shared real-seconds/game-day conversion. Completed cycles consume the existing action's game time exactly once. Incomplete cycles carry progress; no invented partial daily wages.
- Offline time uses UTC watermarks, capped at 8 hours by data; negative elapsed is zero, already-accounted intervals cannot repeat. Offline runs discrete bounded activities, not per-frame simulation.
- Survival resource/health/mental thresholds stop automation before a dangerous next action. Existing survival processing remains in TimeManager/SurvivalSystem.
- Rewards and state are durably saved before showing an offline report. Acknowledging the report does not grant rewards again. Save failure must preserve a retryable checkpoint instead of exposing uncommitted gains.
- Combat needs actual health/damage/turn resolution and recorded wins/losses. No existing combat implementation was found under Assets/_Game/Scripts. Future gear connects through a combat-stats interface; equipment/gacha systems are not added.
- Reuse the existing pixel frames, sprites, fonts and screen stack. Home stays a summary, detailed controls belong to School/Workplace/Battle and settings/status panels.

## 실제 구현

| 시스템 | 구현 / 연결 |
|---|---|
| 자동 노동 | 유효한 일반 직업 필요. 기존 action0 → WorkActivityService → 실제 WorkRecord → EconomySystem 급여 영수증을 재사용한다. 일급·경력·체력/멘탈 비용·해고 규칙이 동일하다. 고용 변경/퇴사/해고 시 중단한다. |
| 자동 공부 | 현재 나이에 맞는 입학 과정 필요. 기존 action14 → EducationService의 지식·교육 EXP·준비도·공부 횟수·미션 신호를 재사용한다. 과정이 바뀌거나 졸업/나이 조건이 달라지면 중단한다. |
| 실제 전투 | 기존 전투 코드가 없어 최소 턴 전투를 추가했다. ICombatStatsProvider의 공격/방어, EnemyData의 HP/공격/방어/보상/턴 상한으로 플레이어와 적 피해를 계산한다. 적 HP=0이고 이안 HP>0인 실제 승리만 골드·성장 EXP·BattleWins 신호를 지급한다. 패배/체력 고갈/턴 상한은 보상 없이 중단한다. |
| 온라인 진행 | 장소당 UI가 타이머를 만들지 않는다. 공통 IdleActivityService의 0.25초 coroutine이 한 활동을 처리하며 씬 이동에도 유지된다. 활성 중 TimeManager의 일반 realtime 진행을 억제해 같은 활동 날짜를 두 번 차감하지 않는다. 완료 주기마다 기존 행동이 날짜를 한 번 진행한다. |
| 오프라인 진행 | IdleActivityService.Loaded/PrepareBackground/ResumeBackground/SettleToUtc에 OfflineProgressService 역할을 통합했다. OfflineRewardCalculator는 UTC 경과·역행·상한만 계산한다. 최대 512개 이산 주기 처리이며 프레임 시뮬레이션은 하지 않는다. |
| 저장/복구 | 보상·자원·미션·기록·UTC watermark를 묶어 자동 체크포인트에 저장한다. 실패하면 GameManager.CaptureState/RestoreState로 정산 전 전체 모델을 복구하고 재시도 전까지 멈춘다. 부분 온라인 주기는 전체 세이브를 매 tick 복제하지 않으며 30초 체크포인트를 사용한다. |
| 정산서 | OfflineProgressReport에 경과/상한 내 시간/실제 완료 활동 시간, 노동·공부·전투 결과, 골드/경험치/지식과 생존 변화, 중지 사유를 저장한다. 확인 버튼은 이미 저장된 결과를 확인할 뿐 추가 입금하지 않는다. 미확인 결과는 합산·보존한다. |

### 현재 데이터 값

`Assets/_Game/Data/Phase24/IdleSettings.asset`: 오프라인 최대 28,800초(8시간), 정산 최대 512회, 활동별 최대 512회. 기본 안전선: 체력20·멘탈15·허기20·목마름20. 다음 한 행동의 비용과 기존 일별 소모를 고려해 안전 정지한다. 기존 생존 실패/유예 처리는 변경하지 않았다.

노동 주기는 `realSecondsPerGameDay × WorkingTime / AutoWorkEfficiency / activity.efficiency`다. 기본 300초/게임1일이며 **부분 일급을 따로 만들지 않는다**. 공부 기본 60초, 전투 기본30초다. 이 수치는 원본 기획의 확정 수치라고 주장하지 않으며 Inspector에서 조절할 수 있다. 미완료 주기는 진행 시간만 저장하고 보상/자원/게임 날짜를 완료 시 정산한다. 자동 활동이 없는 구세이브에 새로운 오프라인 사망이나 수입을 소급 적용하지 않는다.

기본 적 `마감 알림`: HP30 / 공격7 / 방어1 / 승리80G·20EXP / 최대32턴 / 7세부터. 플레이어 기본 공격=6+지식/10+min(레벨,100), 방어=1+사회성/20. 미래 장비는 ICombatStatsProvider로 연결할 수 있으나 장비 가챠/헤드폰 스탯 확장은 구현하지 않았다.

### UI와 씬

Home은 자동 활동 요약/설정 진입만 추가했다. School에서 자동 공부, Workplace에서 자동 노동, Battle에서 자동 전투·1회 전투를 시작한다. 각 설정창에 현재 과정/직업, 주기, 안전 정지 정책과 실행 불가 사유를 표시한다. 설정에 오프라인 토글, 상태창에 최근 정산서를 연결했다. 기존 PixelButton/9-slice/픽셀 폰트/UIShell 스택/키보드 포커스/ScrollRect를 재사용했다.

실제 씬 이름 **Workplace**를 유지했다. Battle만 Phase2.4의 새 실제 씬으로 등록했다. 기존 City 배경과 애니메이션 간판을 `마감 알림` 적으로 재사용한다. **전용 몬스터·전투 배경을 새로 제작한 것은 아니다.** 전투 결과는 실제 계산된 턴 기록을 화면에서 재생한다. 공식 이안 원본은 그대로이며 중앙에서 볼 수 있다. 기존 전환 프로필12개에 Battle 왕복8개를 추가했다. 새 음원/환생/주식/가챠/부동산/엔딩은 구현하지 않았다.

### 저장 호환성과 중복 방지

기존 슬롯과 `version=4`를 유지하고 선택적 `idle` 블록(`schema=1`)을 확장했다. 없는 구세이브는 자동 활동 없음으로 읽는다. 활동 ID/세션 ID/선택 과정·직업/부분 진행/반복 수/UTC watermark/활동별 ledger/최근128개 전투 기록/미확인 보고서/오프라인 설정/중지 사유를 보존한다. 출생 플래그는 별도 선택적 확장이다.

같은 UTC 구간 재호출은 추가 보상을 만들지 않는다. 역행 시 watermark를 뒤로 옮기지 않는다. 상한 초과 시간은 다음 접속에 재청구할 수 없게 전체 구간의 끝으로 watermark를 옮긴다. 수동 과거 체크포인트 불러오기는 자동 활동을 멈추고 watermark를 현재/이전 정산 시각 이상으로 재설정하므로 저장 롤백으로 자동 보상을 재청구하지 않는다. 이 동작은 UI의 중지 사유로 설명한다.

급여는 기존 workId/salary, 전투는 고유 battle receipt로 EconomySystem에서 중복을 거부한다. 현금은 기존 int 한도, 계산 중간은 long으로 확인한다. 경력/교육 XP/승진 급여/정산 sequence의 한도와 정산서 합산의 checked overflow를 방어한다. 로컬 UTC 상한과 역행 검출은 기기 시간 조작을 완화하며, 서버 신뢰 시각 기반 부정행위 방지 시스템은 아니다.

## 검증 결과

| 검증 | 결과 / 증거 |
|---|---|
| Phase2.4 | **107 통과**, [IDLE_TEST_RESULTS.json](Phase24/IDLE_TEST_RESULTS.json) |
| 출생/폰트 | **48 통과**, [BIRTH_FONT_TESTS.json](Phase24/BIRTH_FONT_TESTS.json) |
| Phase2.3 통합 후 재검 | **506 통과**, [PHASE23_AUDIT_TESTS.json](Phase24/PHASE23_AUDIT_TESTS.json) |
| 기존 Phase2.1 / 2.2 | **45 / 169 통과**, [2.1](Phase24/PHASE21_REGRESSION.txt), [2.2](Phase24/PHASE22_REGRESSION.txt) |
| 새 컨텍스트 재시작 | 실제 Play stop/start 후 활동/부분 주기/급여/미확인 보고서 복원, [결과](Phase24/IDLE_FRESH_PLAY_RESULTS.txt) |
| 주요 창 | 글리프와 텍스트 컨테이너 높이 검사, [UI 결과](Phase24/UI_FINAL_INTEGRATION.txt), [잘림 목록](Phase24/UI_OVERFLOW_AUDIT.txt) |

정산 테스트는 10분/1시간/8시간/10시간, 같은 구간 재정산, 시각 역행, 오프라인 끄기, 무직/자원 부족/직업 변경/과정 변경, 씬 이동, 수동 로드 재정산 방지, 실제 파일 IO 실패 후 전체 복구/정확히1회 재시도, 승리/패배/골드·EXP·sequence 한도, 실제 touch raycast와 버튼 실행, 실제 pause/resume callback을 포함한다. 8시간의 경우 생존 자원이 먼저 부족하면 8시간 내내 보상을 지급하지 않고 실제 완료한 주기만 기록한다.

테스트 슬롯은 GUID로 격리하며 모델·슬롯·플래그를 복원한다. 기존 Phase2.1/2.2는 선택 모듈을 끈 원래 계약으로 실행한다. 출생 로그 기대는 새 팝업 계약으로, 미구현 장소 기대는 이제 구현된 Battle 대신 StockMarket으로 갱신했다. 실제 기존 미션 조건·경제·생존 결과 검사는 유지했다. 구형 Stage1 일렬 Work 버튼 화면 계약은 이 감사 범위 밖이며, 그 검사를 통과했다고 보고하지 않는다.

## 실제 화면

[출생 팝업](Phase24/Screenshots/Birth-1920x1080.png) · [빠른 행동 서체](Phase24/Screenshots/Quick-action-font.png) · [전투](Phase24/Screenshots/Battle-1920x1080.png) · [오프라인 정산서](Phase24/Screenshots/Offline-1920x1080.png)

[Home](Phase24/Screenshots/Home-1920x1080.png) · [School](Phase24/Screenshots/School-1920x1080.png) · [City](Phase24/Screenshots/City-1920x1080.png) · [Workplace](Phase24/Screenshots/Workplace-1920x1080.png)

Fixture 화면의 나이/직업/능력치/경과 시간은 검증용으로 설정했다. 보상은 해당 조건에서 실제 서비스를 실행한 결과다. Editor Game View에서 1280×720 / 1920×1080 / 2400×1080을 확인했다. Windows/Android 빌드 결과는 `Phase24/BUILD_*.json`에 별도 기록한다. 실제 Android 기기·APK 설치/터치·장시간 열/메모리 측정은 미검증이다.

## 도구와 파일

공식 Unity 플러그인의 unity-cli, ui/ui-ugui/ScrollView, optimize-text-mesh-pro, craft-game-floors/game-feel 문서와 로컬 game-ui-ux를 읽고 적용했다. CLI beta13은 설치됐지만 Editor descriptor 접근이 거부돼, 동일 프로젝트를 확인한 Unity MCP의 execute_code/refresh/play/stop/read_console 및 실제 Game View 캡처를 사용했다. 공식 기획은 Firecrawl로 열람했다. 한국어 줄바꿈은 설치된 TMP 소스의 useModernHangulLineBreakingRules 분기도 확인했다.

신규 핵심: IdleActivitySettings, IdleProgressData(OfflineRewardCalculator/OfflineProgressReport), IdleActivityService, EnemyData, CombatService/ICombatStatsProvider, IdleActivityPresenter, BirthIntroductionPopup, BirthPopupSafeArea, PixelTextFlow, Editor 제작/빌드 도구, 격리 검증 runner, Phase24 데이터와 Battle 씬.

주요 수정: GameManager, SaveManager, TimeManager, JobData/JobService, Phase23Services, MissionData/MissionService, SceneTransitionService, UIShell/UINavigationService, UITypeScale/PixelNotificationLayout, 기존 다섯 주요 UI 씬/prefab과 TMP 설정. 전체 경로는 [변경 파일 목록](Phase24/CHANGED_FILES.txt)에 기록한다.

임포트 시 기존 Unity AI 서비스의 NoSubscription 오류와 기존 obsolete API 경고가 별도로 발생할 수 있다. 이번 작업에 그 이미지 서비스를 사용하지 않았다. 최종 Play 검증의 Console 결과와 플랫폼 빌드 결과를 분리해서 기록한다.
