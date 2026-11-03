# 멀티 씬 구조

Bootstrap을 열고 Play를 실행한다. 에디터 Play 시작 씬도 Bootstrap으로 지정했다.

| 씬 | 역할 | 현재 상태 |
|---|---|---|
| Bootstrap | GameContext 생성, 기존 게임 서비스와 로딩 UI 유지 | 구현 |
| MainMenu | 이어하기/새 인생 → Home | 구현 |
| Home | 공통 HUD, 생존/미션, School 이동 | 구현 |
| School | 같은 모델을 사용하는 교육/사회 행동, Home 복귀 | 구현 |
| Workplace / City / Battle / StockMarket / Gacha / Shop / Housing / Ending | 이후 단계 콘텐츠 | ID만 정의, 씬 미생성 |

GameContext.prefab 하나가 DontDestroyOnLoad로 유지된다. GameManager, SaveManager, PlayerStats, EconomySystem, MissionService 등은 그 루트에서만 소유한다. Home/School은 HUD와 장소 프레젠터만 생성하며 GameplaySceneBindings가 기존 서비스를 연결한다. 인벤토리 실체는 아직 없으므로 가짜 인벤토리를 생성하지 않고 pending reward를 동일 컨텍스트/세이브에 유지한다.

SceneTransitionService는 SceneId/SceneRoutes로 경로를 검증하고 async Single 로딩, 공통 로딩 오버레이, 중복 요청 차단, 실패 로그와 상태 복구를 담당한다. 로딩과 MainMenu에서는 생존 시계를 멈추고 장소 진입 시 정상 설정으로 복원한다. 중복 Bootstrap은 기존 컨텍스트를 교체하지 않는다. 현재는 Additive가 필요하지 않아 사용하지 않았다.

Build Profiles의 전역 Scene List는 Bootstrap, MainMenu, Home, School 순서이고 기존 SampleScene 항목을 보존했다. 현재 활성 커스텀 BuildProfile은 없으며 전역 목록을 사용한다. 기존 MainGame2D/3D MainGame은 삭제하지 않았다.

학교 행동은 School에서만 허용한다. Home에서도 학교에서 발생한 능력치/카운터/미션/보상이 즉시 표시된다. 프레젠터는 OnEnable/OnDisable에서 구독을 관리한다. 저장 파일에는 씬별 복제 모델을 만들지 않는다.

## 직접 확인

1. Bootstrap Play → MainMenu 이어하기 → Home.
2. 신규 게임 010 완료 후 7세에서 학교 버튼을 누른다.
3. School 교육/사회 메뉴에서 입학·독서·숙제 등 수행 후 Home으로 돌아간다.
4. 동일 날짜/재화/미션 진행도를 확인한다.
5. 저장 → 추가 행동 → 다른 씬 이동 → 불러오기: 명시적으로 저장한 시점으로 복원된다.
6. 종료/재실행은 자동 저장을 이어가고, 불러오기 버튼은 수동 체크포인트를 읽는다.

진행 중인 기존 세이브를 보존하려면 새 인생 버튼을 누르지 않는다. 새 인생은 사용자의 명시적 초기화 동작이다.
