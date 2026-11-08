# 2026-11-09 — Phase 2.6 품질 복구 진행 중 (완료 아님)

## 연결/중단 복구
- Unity는 이미 실행 중이므로 재실행하지 않았다. 기존 로컬 MCP 8080에 남은 세션 ID가 404를 반환하여 세션 ID만 갱신해 통신 복구.
- MCP로 실제 프로젝트 IanGrow/IanGrowing, Bootstrap, Play 중지, 컴파일 대기 없음, 활성 씬 dirty=false 확인.
- Editor-prev.log 끝에 EditorApplicationQuit와 정상 종료 요청/종료 절차가 있다. 이 기록만으로 크래시 원인을 확정할 수 없음.
- git 미커밋 변경 및 IanSymbolsFallback.asset 변경을 보존. reset/clean/기존 세이브 삭제 없음.

## 현재 Stage: P0 입력 복구
- 완료(파일/참조): WorldControls.inputactions의 WASD/방향키 2DVector, 게임패드, E, I, M을 기존 LifeWorld 플레이어에 연결. Esc는 기존 UIShell 단일 소유, Tab은 UI 포커스 이동.
- 완료(코드): 가속/제동, 대각선 정규화, 창/메뉴/텍스트 입력 중 조작 차단. 포커스 상실 후 Update/FixedUpdate가 다시 이동시키지 않도록 포커스 게이트 추가.
- 진행 중: DeX + 블루투스 키보드 + 원격 앱 경로의 영문 키 전달. 사용자 보고: 방향키는 동작하나 WASD는 동작하지 않음. 키보드 고장으로 판단하지 않음.
- 실제 기록: 주입 검사기 종료 후 방향키 KeyState 및 위치 변화 수신. 같은 구간에서 실제 WASD KeyState 미수신. 00000412/00000409 레이아웃 모두 관찰. 원격 앱은 Microsoft임을 사용자에게 확인.
- 원인 확인: REMOTE_TEXT_INPUT_EVIDENCE.json에 실제 W/A/S/D가 KeyState 없이 TextEvent로만 수신됨. Microsoft 공식 문서의 Unicode 전달 방식과 일치. 클라이언트의 Use scancode input when available 설정 변경 후 사용자 정상 동작 확인. 실제 WASD KeyState와 위치 변화도 기록됨. 문자 이벤트에는 눌림/해제 정보가 없으므로 임의 타임아웃으로 이동을 흉내 내지 않음.
- 원인 확정 후 불필요한 Unity IME 강제 전환 코드를 제거. 텍스트 입력창/포커스 게이트는 유지.
- 진단: Phase26InputTrace는 게임 키의 상태 이벤트와 해당 키 문자(TextEvent) 전달을 구분해 기록하도록 확장. 임의 텍스트는 기록하지 않음.

- 안전 체크포인트: Docs/Phase26/Checkpoints/20261109-023252-input-recovery.zip (21개 변경 파일, ZIP 재열기 확인). 원본 파일과 세이브는 유지.

## 테스트 상태
- 이전 주입 입력 검사: 20개 체크 통과 후 DownArrow 해제/정지 체크에서 실패. 성공으로 덮어쓰지 않음. 당시 물리 틱이 지나기 전 정지 위치를 측정할 수 있는 시간 의존성 및 사용자 동시 입력 여부는 미확정. 검사에 제동 대기 0.12초 추가.
- Windows SendInput 검사: 전면 창 활성화 요청 거부, OS 키 눌림 확인 안 됨. 이는 자동 조작 검증 불가이며 게임 결함 증거가 아님. 실제 WASD/E/I/Esc와 원격 호환성은 미검증.
- 이전 Phase 2.6 자동 검사 기록은 기능 경로 검사이며 일반 키보드 조작/상용 그래픽 완료 근거가 아니다.

## 파일/씬
- 수정: WorldPlayerMotor.cs, LifeWorldController.cs, WorldHUD.cs, LifeWorldBuilder.cs, Phase26Verification.cs, Phase26EditorBridge.cs.
- 추가: WorldControls.inputactions, Phase26InputSetup.cs, Phase26InputTrace.cs, Phase26NativeInputProbe.cs.
- 저장된 씬: LifeWorld.unity 플레이어 controls 참조/가감속 값/조작 안내. 프리팹 재생성 없음.

## 남은 작업 / 다음 작업
1. 변경 입력 스크립트 컴파일 → 원격 문자/키 상태 구분 → 실제 WASD/E/I/Esc 검증. 입력 미해결 상태에서 아트 제작으로 우회하지 않음.
2. 진행 중: 공식 원본 기준 4방향 × 6프레임 시트의 배경 알파 0 확인, 24개 Sprite / 8개 Idle·Walk 클립 / Animator / WorldWalkSet 생성. LifeWorld 기존 플레이어에 연결하여 씬 저장 완료. 재임포트 후 크기 재조회와 Unity fake-null 판정을 수정. 실제 이동 거리 기반 재생과 발바닥 pivot 보정 적용. 사용자 실제 4방향 보행/정지 정상 확인.
3. 연결 완료/검증 중: HomeShell/CityGround/BathroomShell, 가구16종, 도시 건물4종·차량, 시민4외형×6프레임, 신호3상태, 실내 밤 배경. Phase26ArtSetup으로 기존 LifeWorld에 적용/저장. WorldPixelViewport 정수 확대, 변경된 벽 위치와 안전 로드 상한, 도로 위 벤치·단말기 이동 적용. Game View에서 집/도시 적용 화면 확인. 작은 월드 간판 글꼴/HUD 과밀은 수정 중.
4. 연결 완료/검증 중: 배경 시민10명 + 기존 친구2명, 전용2차선 차량4대, 시간대별 밀도/횡단보도 정지/차간 간격/플레이어 감지. Game View 및 A/B 관찰에서 시민10명 이동/프레임 변화 확인. 차량은 횡단 종료 조건 때문에 정지 지속됨을 발견(성공 아님). crossingActive로 시작한 횡단은 끝까지 마치도록 수정, 재검증 대기. 생활/경제/저장/UI 경로 재검증 남음.
5. 진행 중: 작은 HUD/동적 폭 E 안내, 메뉴 내부 조작 안내, 간판 확대/대비, 전용 차선 위치, 골목 진입점·친구 일정의 인도 배치, 원본 합성 발소리·문·환경음 연결. quality 검사는 정상 입력 정책을 변경하지 않고 컨트롤러/UI 경로로 실행하며 실제 키 검증과 구분. 
6. Phase 2.7로 넘어가지 않음. 완료 선언 금지.

---
# 이안 키우기 — Phase 2.6 진행 및 검증 기록

최종 갱신: 2026-11-08
프로젝트: `C:\Users\user\Desktop\IanGrow\IanGrowing` (Unity 6000.6.8f1)

## 현재 Stage 및 완료 판단

Stage A–F의 핵심 월드·생활·기존 시스템 연결을 구현하고 Unity Editor Play에서 검증했다.
**집에서 기상 → 샤워 → 음식 준비/식사 → 외출/도시/상점 → 출근/급여/NPC 대화 → 귀가 → 취침 → 다음 날 기상 → 저장/복원** 사이클이 실제로 통과했다.
정식 방향별/행동별 캐릭터 애니메이션, 환경 아트의 최종 제작과 실제 플레이어 빌드·실기기 검증은 남아 있다. 이를 상용 최종 품질까지 완료했다고 간주하지 않는다. Phase 2.7 시스템은 추가하지 않았다.

## 중단 상태 복구

- 기존 미커밋 변경과 Phase 2.5.6 작업을 유지했다. reset/clean/checkout으로 되돌리지 않았다.
- 중단 당시 월드 스크립트와 빌더는 작성되어 있었으나 실제 씬/프리팹에 연결되지 않았다.
- `SceneTransitionService.cs`의 잘못 삽입된 리터럴 개행 때문에 발생한 CS1056/CS1002를 수정했다.
- 현재 세션에서 Unity MCP 도구 목록이 사라져 기존 로컬 MCP 서버 연결 및 프로젝트 전용 Editor 명령 브리지를 복구했다.
- `LifeWorldBuilder.Build()` 실행으로 실제 씬, 규칙 에셋, GameContext 프리팹, SceneRoutes, BuildSettings를 저장했다.

## 구현 상태 분류

| 분류 | 현재 상태 |
|---|---|
| 완료됨 | 이동, 발밑 충돌, Y 깊이 정렬, 카메라 추적, 7개 연결 구역, 문 이동, 거리 제한 E 상호작용, 수면/기상/음식/위생/활동, 기존 급여·교육·사회성·미션·경제·주거·저장 연결 |
| 완료됨 | UI 전역 보정, 장소/시간/상황 분리 상태바, 간결한 HUD, 상세 창, 하이라이트, 메뉴/행동 중 입력 차단과 닫기/취소 복원 |
| 구현 중 | 핵심 실행 경로에 미연결 작업은 남아 있지 않음 |
| 오류 발생 | 확인한 게임 스크립트 컴파일 및 주요 Play 경로의 치명적 오류 없음. 초기 문법/조작 안내 높이/미션 Toast/최대 수치 폭 오류는 수정 후 재검사 |
| 미구현 | 정식 방향별 및 모든 생활 행동의 전용 스프라이트 프레임, 최종 환경 아트, 자유 드래그 가구 편집, 모바일 전용 이동 터치 UI |
| 검증 필요 | 실제 Windows/Android 플레이어 빌드, 물리 게임패드/터치/DPI, 실기기 성능과 장시간 플레이 |

## 기존 시스템 분석 및 선택한 구조

- 기존 `GameManager`, `TimeManager`, `ActionSystem`, `PlayerStats`, `EconomySystem`, `Phase23Services`, `MissionService`, `SaveManager`, `HousingCatalog`, 픽셀 UI를 재사용했다.
- 기존 행동은 날짜를 하루씩 진행시켰다. 기존 경로는 유지하고 월드 행동에만 시간 단위 실행 인자를 추가했다. 자동 시간 흐름과 행동 시간은 같은 TimeManager를 사용한다.
- 기존에는 이동 컨트롤러가 없고 배경/가구가 한 이미지에 구워져 있었다. 독립 SpriteRenderer와 실제 발밑 Rigidbody2D/Collider2D, Y 좌표 깊이 정렬을 추가했다.
- Orthographic 3/4 시점이다. 카메라는 감쇠 추적하며 실내/실외 크기는 규칙 데이터에서 조정한다. 방/건물/가구는 충돌 및 개별 상호작용을 갖는다.
- 기존 공식 이안 원본/복장/외형을 바꾸지 않았다. 기존 MotionLibrary의 프레임이 비어 있는 상태는 안전하게 기본 스프라이트로 대체한다.

## 실제 월드와 장소

`Assets/_Game/Scenes/LifeWorld.unity`의 작은 생활권:

- 집: 나무 장판, 흰색 벽, 침대/식탁/냉장고/수도/옷장/휴식 자리.
- 옥색 화장실: 옥색 타일·벽, 세면대/샤워/변기.
- 주거 골목: 집 현관, 도시 연결, 안내판/가로등.
- 도시 중심: 상점/Workspace/배움터, 도로/횡단보도/벤치, 증권 단말기.
- 상점: 실제 돈으로 음식 구매, 기존 가구 구매 창 연결.
- Workspace: 기존 이력서/직업 선택/면접/근무/승진/퇴사 조건을 사용하는 작업대.
- 배움터: 기존 입학/공부/과제/시험/졸업/진로 활동.

같은 씬 안의 구역을 문 접근과 짧은 페이드로 연결한다. MainMenu의 새 게임/이어하기는 실제 LifeWorld로 진입하며 기존 StockMarket은 도시 단말기를 통해 유지된다.

## 생활 행동 및 기존 시스템 연결

- WASD/방향키, 좌우 표현, 대기/걷기, 발밑 충돌, 깊이 정렬, 감쇠 카메라, E 거리 판정과 대상 강조.
- 수면 8시간, 식사 30분, 물 2분, 휴식 1시간, 샤워 20분, 세수 5분, 화장실 8분, 학습 1시간, 근무 8시간.
- 밥/빵 선택과 준비, 준비된 음식 소모, 기존 Hunger/Health/Mental 등 회복.
- 기존 식품 인벤토리가 없으므로 최소한의 쌀/빵/준비 음식 상태를 선택적 world 저장 필드에 추가했다. 기존 장비 인벤토리는 유지한다.
- 기존 Hunger/Thirst와 지갑을 복제하지 않았다. 피로/위생만 추가하며 시간/행동에 따라 천천히 변한다.
- 기본 시간 배율: 현실 1,200초에 게임 1일. WorldRules에서 조정한다. 기본 신규 게임은 DAY 1 08:00이며 기존 저장의 시간은 보존한다.
- 출근 시작 시간 09:00–10:59, 기존 취업 나이/자격 유지. 근무 완료 후 기존 WorkRecord/급여/경력/세금/미션 처리. 같은 날의 기존 지급 기록도 검사한다.
- NPC 2명은 기존 도시 걷기 프레임과 NPC 데이터를 사용한다. 시간표는 데이터이며 보이는 구역에서만 걷기/대기를 갱신한다. 대화/친구/관계 유지는 기존 SocialService와 연결한다.
- 교육은 실제 학교 구역의 책상에서 기존 과정·선행 미션·나이·경험치·지식·미션을 사용한다.
- 새 가구와 이사는 기존 가격/지갑/영수증/주거 저장을 사용한다. 재배치는 무료다. 구매 가구는 월드 SpriteRenderer/Collider2D/WorldInteractable로 나타난다. 개선 주거지는 실제 추가 수납/책상을 활성화한다.
- 기존 자동 활동은 월드 진입 시 안전 종료한다. 이미 정산된 결과를 삭제하거나 재지급하지 않는다. 월드에서는 작업대와 책상을 직접 사용한다.

## UI 및 그래픽 개선

- 기존 픽셀 프레임, Galmuri 한글 본문과 Fallback, UITypeScale/PixelTextFlow/UIShell을 재사용한다.
- 상단 상태바는 장소·날짜/시간·상황 메시지의 독립 TMP 요소와 HorizontalLayoutGroup으로 분리했다. Padding/Spacing과 각 요소의 최소/선호 폭을 지정했다.
- 항상 표시하던 큰 버튼 목록을 상세 메뉴로 옮겼다. 상단 상태/잔액, 하단 핵심 수치/조작 안내, 가까운 사물의 E 안내만 기본 표시한다.
- 상태 표시가 도시의 상점/학교 간판을 가리는 문제를 실제 캡처에서 찾아 하단으로 옮겼다. 학교 간판은 RectTransform의 anchoredPosition3D를 명시 저장하고 씬 재열기 후 위치 유지까지 확인했다.
- 조작 안내의 높이, 큰 글씨 미션 완료 Toast, 최대 날짜/소지금/상태의 폭을 수정했다. 글자를 축소해서 숨기지 않았다.
- 기존 City/Workplace/StockMarket의 혼합 문장 상태바도 장소/시간/상황으로 분리했다.
- 월드 미션 안내의 장소 판정은 LifeWorld 씬 ID 대신 실제 구역을 사용한다. 메뉴에서 실행해도 대상 가까이 있어야 하며 행동 시작 시 월드로 돌아온다.
- 낮/밤 색조, 밤의 창/가로등 발광, 문 열림/페이드, 수면 조도/기상, 물 사용 효과, 돈 증감/완료 피드백을 연결했다.

## 저장 및 실패 복구

- 저장 v4에 선택적 `world` schema=1을 추가했다. 위치는 구역 기준 좌표다.
- 장소/좌표/방향, 피로/위생, 음식, 준비 음식, 진행 중 행동, 마지막 근무일/하루 요약을 저장한다. 기존 성장/경제/주거/사회성/교육/미션 필드는 유지한다.
- world 필드가 없는 실제 v4 파일을 읽는 검사를 통과했다. 기존 돈과 진행은 유지하고 신규 생활 필드에 기본값을 적용한다.
- 벽/가구 안의 저장 좌표는 안전 위치로 옮긴다.
- 진행 중 행동은 로드 시 취소한다. 완료 시점 전에는 시간·물품·급여를 확정하지 않으므로 중복 보상을 만들지 않는다.
- 월드 경제/행동 변경 후 체크포인트 실패 시 전체 이전 모델로 복구한다.
- 테스트는 GUID별 저장 슬롯을 사용했고 원래 모델·슬롯·자동 저장 설정·입력 설정을 복원했다.

## 실제 테스트 결과

| 결과 파일 (`Docs/Phase26/`) | 검사 | 결과 |
|---|---|---|
| STAGEA_RESULTS.json | 실제 키보드 이동, 벽/가구 충돌, 거리 제한, 문/카메라, 출생 안내 | 16 통과 |
| CYCLE_RESULTS.json | 최종 코드의 온전한 하루, 급여/음식/주거/NPC/미션/세이브 | 75 통과 |
| EDGE_RESULTS.json | 취소/진행 중 저장과 로드/가구 내부 좌표/구 v4/저장 IO 실패/돈 부족/영업시간/낮밤/NPC 일정/시간 배율/지각/유료 이사 | 35 통과 |
| UI_RESULTS.json | 4개 해상도, 주요 메뉴, 닫기 후 입력, 최대 소지금·날짜·피로와 큰 글씨 | 11 통과, 넘침/글리프 오류 0 |
| INTEGRATION_RESULTS.json | 실제 초등교육/성장 제한/큰 글씨·중첩 탭/컨텍스트/출생/기존 증권 화면과 월드 복귀 | 26 통과, 넘침/글리프 오류 0 |
| CAPTURE_RESULTS.json | 실제 Game View와 월드 간판 글리프 | 별도 캡처 검사 기록 |
| COMPILE_RESULTS.json | Unity 생성 응답/참조 어셈블리를 사용한 Runtime 및 Editor 컴파일 | 두 exit code 0 |

UI 비율: 1280×720, 1920×1080, 2400×1080, 1080×1920. 큰 글씨 115%, 소지금 int 최대, DAY 999998, 핵심 상태 100도 별도로 표시해 확인했다.

`EDITOR_PERFORMANCE_SNAPSHOT.json`은 Editor의 한 시점 샘플이다: 전체 SpriteRenderer 159개 중 해당 카메라 내 15개, 카메라 1개, Rigidbody2D 1개, drawCalls 25. 전체 Editor 메모리는 플레이어 메모리 예산/실기기 성능 결과로 해석하지 않는다.

수정 코드의 주요 Play 오류는 발견하지 못했다. 도메인 재로딩 시 기존 Unity AI NoSubscription/UERP 패키지 로그가 발생할 수 있으며 게임 코드 오류와 구분했다.

## 주요 수정 파일

- 신규: LifeWorldRules.cs, LifeWorldService.cs, LifeWorldController.cs, WorldPlayerMotor.cs, WorldCameraController.cs, WorldInteractable.cs, WorldNPCScheduler.cs, WorldHUD.cs, CompactWorldStatusBar.cs.
- 기존: GameManager.cs, ActionSystem.cs, SaveManager.cs, Phase23Services.cs, SceneTransitionService.cs, SceneCatalog.cs, IdleActivityService.cs, BirthIntroductionPopup.cs, UIShell.cs, MissionUI.cs, PixelHomePresenter.cs.
- Editor: LifeWorldBuilder.cs, LifeWorldArtBuilder.cs, Phase26EditorBridge.cs.
- 검증: Development/Phase26Verification.cs.
- 실제 연결: Scenes/LifeWorld.unity, 기존 City/Workplace/StockMarket 상태바, Prefabs/GameContext.prefab, Data/Phase26/LifeWorldRules.asset, Data/SceneRoutes.asset, ProjectSettings/EditorBuildSettings.asset.
- 아트: Art/LifeWorld/*.png 및 메타데이터. 공식 캐릭터 원본 및 기존 완성된 배경/애니메이션은 교체하지 않았다.

## 실제 캡처

`Docs/Phase26/Screenshots/`: Home-World, Bathroom-World, Alley-World, City-World, City-World-Night, Workspace-World, Work-Context, Shop-Context, Housing-Furniture, World-Portrait, Birth-Large.
이전 화면 비교 자료는 기존 `Docs/Phase256/Screenshots/`에 보존했다.

## 남은 작업과 다음 재개 지점

1. 공식 이안의 정식 방향별/생활별 프레임 제작. 현재 기본 스프라이트·미러링·transform 동작/물 효과는 임시 연출이다. 기존 CharacterIdentity의 `idle-north/south/east/west`, `walk-north/south/east/west` 및 MotionLibrary의 행동 프레임을 연결할 수 있다.
2. 코드로 만든 개별 픽셀 가구/건물/바닥의 최종 아트 교체. 현재 기본 생활 기능과 충돌/상호작용이 연결된 대체 에셋이며 최종 상업용 아트 승인 상태는 아니다.
3. Windows 플레이어 빌드 → 실기기 입력/DPI/장시간 플레이 검증. 실제 키보드 Editor Play 외의 물리 게임패드·터치 조작은 미검증이다.
4. 필요 시 현재 preset 자리 변경을 자유 배치 UI로 확장하고 직업별 실내 표현을 보강한다. 이번 작업에서 Phase 2.7의 새 경제/부동산/NPC AI 시스템을 만들지는 않는다.

일반 실행: Bootstrap 씬에서 Play → 새 게임/이어하기 → E 상호작용, WASD 이동, Tab 상세 메뉴.
빌더 재생성이 필요할 때만 `Ian/Life World/Build Connected Life World`를 사용한다. Scene/Rules를 Inspector에서 조정한 뒤 빌더를 다시 실행하면 생성 기본값이 재적용되므로 변경을 먼저 보존한다.
검사를 재실행할 때는 Phase26EditorBridge의 stageA/cycle/ui/edge/integration/capture 명령을 선택한다. 원인 없이 전체 검사를 반복하지 않는다.
