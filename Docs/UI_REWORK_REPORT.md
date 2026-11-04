# PIXEL UI REWORK v1.0

검증일: 2026-11-04 · Unity 6000.6.7f1 · 실제 Unity MCP 에디터 작업 및 Play Mode 검증

## 결과

기존 uGUI/TMP 기반 UI를 픽셀 프레임·아이콘·버튼과 목적별 화면으로 개편했다. 홈에는 공식 이안 이미지, 나이·날짜·골드, 생존 요약 4개, 현재 미션 1개, 최근 메시지, 빠른 행동 최대 3개, 하단 내비게이션 4개를 배치했다. 게임 서비스와 저장 스키마는 유지했다. 새로운 Phase 2.3 게임 시스템은 개발하지 않았다.

최종 격리 Play Mode 검사 **293개 통과, 실패 0개**. 최종 실행 후 Console 오류 0개. Android/Windows 실제 빌드 및 기기 검증은 수행하지 않았다.

## 변경 전 보호 및 조사

- 체크포인트: `checkpoint/pixel-ui-before-20261104`, 기준 커밋 `b1b2c65d9bfa69744abe979b260a6a4ce221cfb9`. 앞선 UI 작업 체크포인트도 보존했다.
- 기존 씬·프리팹·스크립트·UI 백업: `DevelopmentBackups/PixelUI/`.
- 기존 화면: [Before/home.png](PixelUI/Captures/Before/home.png).
- 기술: Built-in 파이프라인, uGUI, TMP, Screen Space Overlay, InputSystemUIInputModule. 프레임워크를 교체하지 않았다.
- Bootstrap의 공통 서비스와 기존 SceneTransitionService, Home/School/City/Workplace 씬을 유지했다. 씬을 삭제하거나 재생성하지 않았다.
- 기존 모바일 설정은 AutoRotation이며 가로·세로 허용을 유지했다.
- 버튼 호출 경로 및 새 배치: [PLAN_AND_INVENTORY.md](PixelUI/PLAN_AND_INVENTORY.md).
- 보호 게임 코드의 SHA256 기준과 비교: [GAME_LOGIC_BASELINE.json](PixelUI/GAME_LOGIC_BASELINE.json), **변경 0개**. 공식 Ian_OfficialDesign.png도 백업과 동일한 SHA256 `04DC5D03C74DF28EAF0CCCAB05CDD66AB325F3A71A70D7D72F660FAFB8D443D1`이다.

## 적용한 스킬

프로젝트의 `.agents/skills/game-ui-ux/SKILL.md` 및 layout-and-flow reference, 설치된 Unity 공식 `ui-ugui`와 ScrollView reference, `2d-pixel-perfect`와 pipeline/sprite reference를 읽고 적용했다. `create-game-assets`, `game-feel`은 조사한 설치 경로에 없었으며 사용했다고 주장하지 않는다. 참고 공유 대화도 확인했다.

## 새 화면 구조

| 진입점 | 내용 | 실제 호출 |
|---|---|---|
| 집 | 캐릭터·생존·미션·빠른 행동 | 기존 HUD 및 게임 이벤트 |
| 생활 | 식사·물·수면·휴식, 회복 하위 화면 | UIManager → GameManager.TryAction |
| 성장 | 능력·교육·사회·미션·성장 탭 | 기존 Stats/Growth/Mission 서비스 |
| 활동 | 집·학교·도시·직장 | SceneTransitionService.Navigate |
| 기타 | 인벤토리·설정·저장/불러오기·일시정지 | 기존 서비스 |
| 미션 상세 | 정확한 조건·현재 수치·다음 행동·보상 | 기존 MissionService |
| 학교 | 입학/진로·학습·시험/졸업·친구 탭 | 기존 Phase23Presenter |
| 도시 | 대화·구직 탭 | 기존 Phase23Presenter |
| 직장 | 구직·근무 탭 | 기존 Phase23Presenter |

미구현 상점·주식·가챠 등은 예정 문구로 표시한다. 새 게임을 시작할 때 확인 대화상자를 제공한다. 인벤토리는 기존 스타터 장착 기능만 연결했다.

`UINavigationService`는 UI 경로를 관리하고 실제 장소 변경은 기존 SceneTransitionService에 위임한다. `UIShell`은 중복 화면을 방지하는 스택, 이전 포커스 복원, 최상단 모달 입력, ESC/게임패드 뒤로 가기를 처리한다. 전환 중 추가 이동 요청을 무시한다. 긴 내용은 스크롤하며 PageUp/PageDown·오른쪽 스틱·포커스 자동 스크롤을 지원한다.

## 디자인 시스템과 에셋

- Theme: `Assets/_Game/UI/Pixel/PixelTheme.asset` 및 `PixelUITheme.cs`.
- 팔레트: 잉크 `#1C263A`, 크림 `#FFF1CF`, 골드 `#F0BC63`, 코럴 `#DC775B`, 민트 `#7FB696`, 블루 `#6A99AD`, 라일락 `#AA9DC5`.
- 자체 제작 32×32 픽셀 프레임: 이중 윤곽, 계단형 모서리, 하이라이트/그림자, 6px 9-Slice. 패널 3종, 버튼 Normal/Hover/Pressed/Disabled, 선택 탭, 게이지 Track/Fill.
- 자체 제작 16×16 아이콘 16종: 화폐·체력·물·식사·멘탈·책·사회·직업·집·생활·성장·활동·설정·미션·화살표·경고. 외부 게임 리소스를 복제하지 않았다.
- 새 도트 텍스처: Point, Single Sprite, 무압축, mipmap 없음, PPU100. 공식 HD 캐릭터의 원본·PPU·필터는 변경하지 않았다.
- Screen Space UI는 정수 Canvas 확대를 사용한다. 가로는 높이540, 세로는 너비480을 기준으로 가능한 정수 배율을 적용한다. 월드 카메라에 PixelPerfectCamera를 강제하지 않는다.
- 간격은 주로 4/6/8/12 논리 픽셀, 주요 버튼 높이44~48, 본문16~18, 제목20~24, 작은 보조14를 사용한다. 해상도가 작아지면 축 배치와 스크롤을 바꾼다. 초광폭에서는 안전 프레임 너비를 제한한다.
- 한글: Google Fonts Nanum Gothic, SIL OFL 1.1. 원본 TTF와 OFL.txt를 함께 보관하며 Theme에서 라이선스를 참조한다. 설정에서 라이선스를 열람할 수 있다. TMP Dynamic SDF, 조사한 실제 문자열 고유 문자372개 누락0개: [FONT_VALIDATION.txt](PixelUI/FONT_VALIDATION.txt).
- 인터랙션: 상태별 버튼 스프라이트와 픽셀 선택 커서, 모달 Fade 0.15초, 골드 숫자 0.2초, 게이지 약0.14초, 미션·보상·성장·위험 알림. 모션 감소 및 글자 확대 옵션을 제공한다.

## 변경한 씬·프리팹·스크립트

씬: `MainMenu`, `Home`, `School`, `City`, `Workplace`. Bootstrap과 기존 씬 목록을 보존했다.

공통 프리팹: `GameHUD.prefab`, `MissionRow.prefab`, 기존 MissionHUD의 폰트, `GameContext.prefab`의 전환 UI. 게임 서비스 설정은 보존했다.

새 재사용 프리팹(`Assets/_Game/Prefabs/PixelUI/`): PixelButton, PixelPanel, PixelTab, PixelProgressBar, StatSummaryWidget, MissionWidget, CurrencyWidget, PixelNotification, PixelModal, PixelTooltip, NavigationBar, SceneTransitionOverlay. 실제 위젯 프리팹과 조립용 시각 템플릿을 함께 제공한다. 게임 데이터 바인딩은 GameHUD의 프레젠터가 담당한다.

주요 새 스크립트: PixelUITheme, PixelButton, PixelTooltip, PixelTabGroup, PixelProgressBar, PixelCanvasScale, PixelHomePresenter, PixelMenuLayout, PixelFocusScroll, PixelNotificationLayout, UINavigationService, PixelNavigationButton.

기존 UI 수정: UIManager, MainMenuPresenter, MissionUI, MissionRowView, MissionGuidance, UIShell, Phase23Presenter. Phase23Presenter 수정은 화면 표시·포커스 처리에 한정한다. MissionUI 컨트롤러를 숨겨지는 패널 밖에 두어 비활성 화면에서도 이벤트 연결 수명을 유지했다. HUD는 데이터 변경 이벤트로 갱신하고, 매 프레임 전체 게임 스탯을 읽지 않는다. 이벤트 구독은 해제한다.

Editor 제작 도구: PixelAssetBuilder, PixelUIBuilder, PixelUIRefinements, PixelUIFontBuilder, PixelUIEditorTools. 픽셀 개편 전 백업 대비 루트 스크립트 목록: [CHANGED_SCRIPTS.json](PixelUI/CHANGED_SCRIPTS.json).

## 미션 안내 개선

60개 미션의 실제 조건 트리를 읽어 행동 횟수, 능력치, 관계·장비·이벤트 조건과 선행 미션을 표시한다. 예: `물 마시기 0/1회`와 `생활 행동 → ‘물 마시기’ 선택 후 실행하세요.`를 함께 보여준다. 장소 이동이 필요하면 해당 장소와 행동을 안내한다. 완료·잠금·구현 예정 상태를 구별하며 자동 완료나 가짜 보상을 추가하지 않았다. 보상 버튼은 기존 MissionService 검증을 사용한다.

## 실제 테스트

실행 도구: Unity MCP + 격리된 PixelUIRunner. 테스트는 고유 임시 저장 슬롯을 사용하고 종료 시 기존 모델과 저장 슬롯·설정·입력을 복원한다.

| 항목 | 결과 |
|---|---|
| 최종 전체 검사 | 293 통과, 실패0 |
| 기존 Phase2.1 검사 | 45 통과 |
| 기존 Phase2.2 검사 | 169 통과, 새 화면 경로를 여는 테스트 어댑터 사용 |
| 60개 미션/실제 행동/중복 보상 방지 | 통과 |
| 저장·불러오기, 날짜·재화·미션 복원 | 통과 |
| School/City/Workplace/Home 이동과 데이터·서비스 단일성 | 통과 |
| 스택 중복·포커스·ESC·게임패드 뒤로가기 | 통과, 가상 입력 장치 사용 |
| 라이선스 스크롤·새 인생 취소·전환 연타 | 통과 |
| 1920×1080 / 2560×1440 / 2560×1080 | 홈 경계·정수 스케일·한글 제목 검사와 캡처 통과 |
| 1080×2400 / 2400×1080 | 현재 허용된 모바일 방향의 Editor 검사 통과 |
| 노치 | 1080×2400에서 Rect(64,72,952,2232) 안전 영역 시뮬레이션 통과 |
| 터치 | 터치 위치 GraphicRaycaster 검사 통과; 실제 모바일 기기는 미검증 |
| 컴파일/실행 예외 | 컴파일 오류0, 최종 Console error0, NullReferenceException 없음 |
| 6개 실제 씬 스크립트 참조 | Missing Script 0개, [SCENE_VALIDATION.txt](PixelUI/SCENE_VALIDATION.txt) |

원시 결과: [TEST_RESULTS.json](PixelUI/TEST_RESULTS.json). 개발 중 Unity AI 패키지의 NoSubscription 메시지가 발생한 적이 있으나 최종 실행 확인에서는 오류가 없었다.

## 실행 캡처

| 화면 | 캡처 |
|---|---|
| 개편 전 | [Home 이전](PixelUI/Captures/Before/home.png) |
| 홈 PC | [1920×1080](PixelUI/Captures/Home-1920x1080.png), [2560×1440](PixelUI/Captures/Home-2560x1440.png), [2560×1080](PixelUI/Captures/Home-2560x1080.png) |
| 홈 모바일 비율 | [1080×2400](PixelUI/Captures/Home-1080x2400.png), [2400×1080](PixelUI/Captures/Home-2400x1080.png), [Safe Area](PixelUI/Captures/Home-Mobile-SafeArea.png) |
| 생활 | [PC](PixelUI/Captures/Life-1920x1080.png), [세로](PixelUI/Captures/Life-1080x2400.png) |
| 미션 조건 안내 | [PC](PixelUI/Captures/Mission-1920x1080.png), [세로](PixelUI/Captures/Mission-1080x2400.png) |
| 성장/수집 | [성장](PixelUI/Captures/Growth-1920x1080.png), [인벤토리](PixelUI/Captures/Inventory-1920x1080.png) |
| 학교 공통 HUD | [School](PixelUI/Captures/School-1920x1080.png) |
| 메인 메뉴 | [MainMenu](PixelUI/Captures/MainMenu-Final-1920x1080.png) |

자동 해상도 캡처는 해당 전체 해상도로 저장했다. 추가 MCP 패널 캡처는 이미지 확인용으로 일부 축소되어 있으며 파일명은 캡처 당시 GameView 해상도를 뜻한다.

## 남은 개선 및 미검증

- 실제 Android 기기·노치·터치 드래그, Windows 플레이어 빌드, 물리 게임패드는 미검증이다. Editor 시뮬레이션으로 기기 검증을 대체했다고 간주하지 않는다.
- 모든 60개 미션의 모든 장문 조합과 115% 글자 확대를 모든 화면 비율에서 시각 검수한 것은 아니다. 긴 상세 화면은 스크롤을 사용한다.
- 일부 세부 화면은 여백이 넓고 기존 서브패널의 밀도가 남아 있다. 후속 시각 작업에서 화면별 정렬·장식·빈 상태 표현을 더 다듬을 수 있다.
- 공식 캐릭터는 기존 고해상도 이미지 그대로다. 캐릭터 픽셀화·애니메이션과 고품질 배경은 이번 작업 범위에 포함하지 않았다.
- 자동 노동·전투·오프라인 보상·주식·가챠 등 새로운 게임 기능은 추가하지 않았다.
