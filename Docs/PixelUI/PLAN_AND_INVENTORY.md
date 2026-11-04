# PIXEL UI — 조사 및 화면 배치

체크포인트: checkpoint/uiux-1.0-before-20261104 (이전 UI 작업 시작), checkpoint/pixel-ui-before-20261104 (b1b2c65, 픽셀 개편 전).
이전 캡처: Docs/UIUX1/Captures/home-refined.png, Docs/PixelUI/Captures/Before/home.png.
기술: uGUI, TMP, Screen Space Overlay, InputSystemUIInputModule. Built-in 파이프라인. 기존 2D placeholder PPU 100/Bilinear. 기존 공식 HD 캐릭터의 필터·PPU·원본은 변경하지 않는다.
모바일 방향: AutoRotation, 가로/세로 모두 허용. 변경하지 않는다.

| 기능 | 기존 호출 | 새 배치 |
|---|---|---|
| 식사/물/수면/휴식 | UIManager → GameManager.TryAction(3/4/5/1) | 생활 1단계, 필요 시 홈 빠른 행동 |
| 놀기/병원/상태 | UIManager → 기존 ActionSystem / PlayerStats | 생활 → 회복 2단계 |
| 능력치 8개/부채/경력 | 기존 Stats/Economy/LifeProgression 이벤트 | 성장 → 능력치, StatusPanel |
| 현재 미션/조건/보상 | MissionUI → MissionService / Claim | 홈 요약 1개 → MissionPanel |
| 60개 미션 목록 | MissionUI.OpenList / MissionRowView | 성장 → 미션 → 목록 |
| 학교 입학/학습/시험/졸업 | 기존 교육 행동 ID 7~17 | 성장 교육 또는 활동 → School → 카테고리 |
| NPC/친구/친밀도 | 기존 Phase23Presenter / SocialService | 성장 사회 또는 City → 대화 탭 |
| 이력서/면접/근무/퇴사 | 기존 Phase23Presenter / JobService | Workplace → 구직/근무 탭 |
| 스타터 장착 | 기존 행동 24/25 | 기타 → 인벤토리 (기존 기능만) |
| 장소 이동 | SceneTransitionService.Navigate | 활동 → 집/학교/도시/직장 |
| 저장/불러오기 | UIManager → SaveManager | 기타 메뉴 |
| 설정/일시정지 | UIShell 화면 스택 | 우측 기타 버튼 → 설정 / ESC |
| 미구현 상점/주식/가챠 | 호출 없음 | 예정 문구, 작동 버튼 없음 |

홈 와이어프레임: 상단 [게임명 / 나이·날짜 / 골드 / 기타] → 중앙 [공식 이안] + 측면 [생존 요약 4개 / 현재 미션 1개 / 최근 메시지] → 빠른 행동 1~3개 → [집][생활][성장][활동].
모바일 세로: 캐릭터와 요약을 세로로 재배치. 상세 화면은 Safe Area 안의 큰 패널/스크롤로 제공. 하단 4개 항목 유지.
픽셀 정책: 새 UI 텍스처만 Point/무압축/PPU100. Screen Space UI는 정수 스케일을 우선하는 별도 Canvas 정책. 기존 HD 월드/캐릭터에는 PixelPerfectCamera를 추가하지 않는다.
스킬: game-ui-ux+reference, Unity ui-ugui+scrollview reference, Unity 2d-pixel-perfect+진단 reference 적용. create-game-assets/game-feel은 확인한 설치 경로에 없음.
참고: https://chatgpt.com/share/6aeb169d-0564-83ec-a6d0-f3296e66a099
