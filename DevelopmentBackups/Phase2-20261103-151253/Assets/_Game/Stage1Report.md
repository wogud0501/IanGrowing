# 1단계: 2D 핵심 시스템 구축 결과
프로젝트: 이안 키우기: BLACK COMEDY EDITION
엔진: Unity 6000.6.7f1 / 실제 Unity MCP 에디터 조작
활성 씬: Assets/_Game/Scenes/MainGame2D.unity

## 작업 전 분석과 보존
기존: 공식 캐릭터, 2D 씬, 기본 세 행동, 스탯/날짜/UI/수동 JSON 저장 구현.
부족: 상태 전환, 개별 스탯 이벤트, 돈 100 초과, 행동/경제 분리, 가중치 이벤트, 이벤트 데이터 분리, 시작 자동 복원, 손상 초기화.
패키지 확인: URP 17.6.0, uGUI 2.6.0, Input System 1.20.1, Test Framework 1.8.0 등. 새 패키지 설치 없음.
프로젝트 루트 Git 저장소 없음. 변경 전 _Game 전체와 존재하는 기본 저장 파일을 다음 경로에 보존:
C:\Users\ADMIN\Ian_Growing\DevelopmentBackups\Stage1-20261103-145101

## 구현
- GameManager: Starting / Playing / Paused / GameOver / Resolving. Ready 별칭은 기존 API 호환용.
- PlayerStats: 행복/스트레스/체력 0~100, 돈 0~int.MaxValue, 개별 변경 이벤트 및 통합 Changed. 산술은 long 중간값으로 오버플로 방지.
- TimeManager: 날짜 복원 SetDay와 실제 진행 DayAdvanced 분리. 정지 시 진행 차단. 행동별 소요일수 조절.
- ActionSystem: 비용/체력/날짜 사전 검증, 행동 효과 및 시간 진행. GameManager의 상태 게이트를 통해 실행.
- EconomySystem: CanAfford/TrySpend/Earn/ApplyDelta. 거래 이벤트로 향후 상점/직업 등을 연결할 수 있음. UI 참조 없음.
- EventManager + LifeEventCatalog: 기존 5개 유지 + 7개 추가 = 12개. 일별 발생 확률 + 이벤트별 가중치. 가중치 0/음수/NaN/Infinity 제외.
- SaveManager: JSON v2, v1 읽기 호환, 기존 파일명 유지, 원자적 교체 및 .bak, 손상 파일 .corrupt-* 보존.
- GameManager: 시작 시 자동 로드, 성공한 행동마다 자동 저장. 손상 로드 시 초기 스탯/1일차로 안전하게 초기화. 버전 미지원은 현재 상태를 변경하지 않음.
- UIManager: 기존 프레젠테이션 유지, 상태 이벤트 표시, 버튼 가용성/가격/소요일수 표시, 일시정지 및 새 인생 추가.
- Character2DPresentation: 일시정지 때 프레젠테이션 업데이트를 정지. 아트/스프라이트 자체는 변경하지 않음.

## 작은 단위 검증
단위 1: 개별 스탯 이벤트, 오버플로 제한, 지출 차단, 돈 >100, 날짜 복원/진행 분리, 정지된 시간 — 6개 통과.
단위 2: v2 JSON, v1 호환, 손상 보존, 1:3 가중치, 0 가중치 제외, 확률 경계 — 6개 통과.
플레이 모드 통합 검증 — 40개 통과:
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
- PASS: Save/load buttons V2 round trip
- PASS: Game over persists
- PASS: V1 save compatibility
- PASS: Corrupt save copied and safely initialized
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

추가 재진입 검증:
- 별도 테스트 슬롯에 19일차, 행복 73, 스트레스 18, 체력 42, 돈 5432 저장.
- 일시정지 상태로 저장한 후 Play Mode 종료 및 재진입.
- Starting -> Playing, LastLoadStatus Loaded, 날짜/스탯/돈/UI 자동 복원 확인.
- 이는 실제 에디터 Play Mode 재진입 검증이며, Standalone 실행 파일 재실행 테스트는 아님.

## 실패 및 경고 (숨기지 않고 기록)
- 최초 Play 진입 명령 도중 MCP 플러그인이 컴파일/재연결 과정에서 끊어져 결과를 받지 못함. 에디터 상태 확인 후 재시도 성공.
- 최초 통합 검증에서 UI raycast 실패. Game View 미렌더링 상태(Graphic.depth=-1)와 동일 프레임 Canvas 비활성/활성화 순서를 확인. Game View 표시/포커스 및 raycast 검증 순서를 조정 후 전체 검증 통과.
- 동적 OS 한국어 폰트 IanKoreanSDF.asset에서 NativeFormatImporter inconsistent result 경고 1건 관찰. 화면 한국어 출력 확인됨. 이번 범위에서는 폰트/아트 고도화를 수행하지 않음.
- 최종 Console error 조회 0개. _Game 컴파일 오류 0개.
- 외부 Unity AI 구독/Discord/MCP 관련 메시지가 발생할 수 있으며 해당 외부 서비스 설정은 변경하지 않음.

## 보존 최종 확인
- 공식 원본 이미지와 게임용 투명 파생본 SHA-256: 변경 전후 완전 일치.
- 사용자 기본 저장 파일: 변경 전후 동일 (없었던 경우 새로 생성하지 않음).
- 테스트에는 stage1-suite-*, stage1-restart-*, unit2-* 전용 저장 파일 사용. 기본 슬롯은 복원됨.
- 기존 3D 씬과 비활성 보관 그룹 유지.
- 현재 편집 모드, MainGame2D 저장 완료, 미저장 변경 없음.

## 변경 파일
수정: Scripts/GameManager.cs, PlayerStats.cs, TimeManager.cs, GameBalance.cs, EventManager.cs, SaveManager.cs, UIManager.cs, Character2DPresentation.cs.
추가: Scripts/ActionSystem.cs, EconomySystem.cs, LifeEventCatalog.cs.
추가: Scripts/Editor/Stage1Verification.cs.
수정: Scripts/Editor/PrototypeVerification.cs (기존 검증 메뉴가 새 검증으로 연결).
데이터/씬/프리팹: Data/LifeEvents.asset 추가, Data/DefaultBalance.asset 휴식 기본 비용 0, Prefabs/GameHUD.prefab 상태/버튼 추가, Scenes/MainGame2D.unity 의존성 연결.
문서: README.md, Stage1Report.md.

## 미수행 및 수동 확인
Standalone 빌드, 실제 마우스 하드웨어 자동화, 타 플랫폼/다중 해상도, 상점/직업/인플레이션, 최종 아트/애니메이션은 구현/검증 범위 밖.
실제 마우스로 Play 후 3개 행동, 일시정지/계속, 저장/불러오기, 새 인생을 클릭해 확인할 수 있음.
UI 자동 검증은 Button의 IPointerClickHandler와 EventSystem raycast이며 물리 입력 장치를 구동한 것이 아님.
검증 재실행: Game View를 표시하고 Play 진입 후 첫 화면이 그려진 다음 Ian > Verify Stage 1 Core (Play Mode).
테스트는 별도 슬롯을 사용하며 사용자 저장을 덮어쓰지 않음.
