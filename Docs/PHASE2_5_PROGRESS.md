# Phase 2.5 진행

**상태: 2.5.1–2.5.8 구현 및 최신 어셈블리 검증 완료.** 핵심 40개 + 실제 Play 통합 46개 통과. 주식 진입: City → 증권거래소. 경제 관리: 상태 → 경제·세금·부채 관리.

## 2.5.1–2.5.3 구현
- 기존 EconomySystem 확장: 거래 유형·고유 ID·시각·관련 데이터·누적 수입/지출. 현금/부채 초기값 및 기존 enum 값 유지.
- 급여 영수증 경로를 PaySalary로 연결. 세금은 원천 공제 없이 소득별 세율로 누적 후 게임 날짜 7일마다 고지. 부분 납부/체납 비용 지원.
- 원금/미상환 이자/누적 이자 구분, 게임 날짜 watermark로 중복 이자 방지, 상환·연체와 Credit 연결.
- 수정: EconomySystem.cs, EconomyRules.cs, FinanceData.cs, EconomyFinance.cs, JobService.cs, Phase23Services.cs, GameManager.cs, SaveManager.cs, EventManager.cs.
- 저장 v4의 선택적 economy.finance schema=1 확장. 없는 구세이브는 현재 날짜에서 시작하여 과거 세금/이자를 소급하지 않음.
- 검증: Unity 컴파일 요청 완료, 오류 결과 확인 중. 기능 테스트는 아직 실행 전.
- 다음: 6종목 거래/시장/뉴스 구현 검증, 독립 StockMarket 화면과 경제창 제작, 저장/오프라인/미션 테스트.

## 시간 규칙
- 주가: UTC 실시간 300초, 보존된 watermark. 24시간(288회)까지 재생하고 초과 구간은 소비하여 재청구 방지.
- 세금/이자: 기존 게임 날짜. 자동 노동은 기존 행동이 진행하는 게임 날짜만 사용.
- 수치: 가상 게임 규칙이며 EconomyRules ScriptableObject에서 조절. 실제 세법/주가/화폐 미사용.

## 2.5.4–2.5.7 구현 및 핵심 검증
- 6종목, 평균 원가·평가/실현 손익, 원자적 매수/매도, 신용도 매수 제한, UTC 시장 갱신·차트 기록·뉴스 지속 효과 구현.
- City 진입 버튼 → StockMarket 독립 씬. 시장/포트폴리오/뉴스/거래내역 4탭, 기존 한글 픽셀폰트·Panel_64·PixelButton·UIShell 재사용.
- 각 장소 상태창에 경제 관리 진입 추가. 고지서 부분 납부·부채 상환·상환기록 제공. UI 거래는 자동 저장 실패 시 전체 모델 복구.
- 추가 파일: PixelStockChart.cs, FinancePresenter.cs, Editor/Phase25Builder.cs, Editor/Phase25CoreVerification.cs, Data/Phase25/*, Scenes/StockMarket.unity.
- 씬 수정: Home/School/Workplace/City/Battle(경제 상태창 진입), GameContext.prefab, SceneRoutes.asset, EditorBuildSettings.asset.
- 핵심 자동 검사 **40 통과**, Docs/Phase25/CORE_TEST_RESULTS.json. 실제 격리 세이브 파일 IO, 구 v4에서 finance 누락, 중복 거래/이자/뉴스/UTC, 부분 납부, 손익·평균 원가 포함.
- 발견/수정: Unity JsonUtility의 null 인라인 객체 기본 생성 → finance 부재를 명시 보존하고 최초 유효 watermark에서 초기화.
- Unity 수정 코드 컴파일 오류 0. 기존 Unity AI NoSubscription/UERP 로그는 수정 코드 오류와 구분.
- 다음: 실제 Play에서 City 진입·주문 UI·미션·오프라인 통합 검사, 화면 확인과 최종 점검.

## 통합 검사 진행
- 실제 City 버튼 → 독립 주식 씬, 실제 매수/매도 버튼, 주문 체크포인트, 손익 탭 미션 신호까지 통과.
- 발견/수정: 새 StockMarket도 SceneTransitionService의 게임 플레이 씬에 등록. 기존 idle Restore의 빈 JSON 보고서 객체가 화면을 덮는 경우 null/ID 정규화(한 줄) 추가.
- 픽셀 화면 확인 후 종목 가격 글자·수량 표기·헤더 줄바꿈 조정. Escape로 주식 화면을 닫으면 City로 복귀.
- 파일 IO 실패 검사는 자동 저장 파일명의 .autosave 접미사를 반영하여 수정. 검사 슬롯은 GUID로 격리하며 플레이 모델/슬롯/설정을 복원.
- 부채 미션은 이자를 제외한 실제 원금 상환만 누적. 거래에 Credit 전/후 기록 추가. 기존 60개 미션 데이터는 수정하지 않고 순자산 조건 평가 인터페이스만 확장.
- 남은 작업: 통합 검사 후반부·최종 화면·수정 범위 점검.

## 최종 화면 수정
- 실제 캡처에서 차트 CanvasRenderer 누락 발견. PixelStockChart에 RequireComponent 추가, 씬 빌더에서 렌더러를 추가하고 씬을 명시적으로 dirty 처리.
- Unity가 파일 수정 후 이전 어셈블리를 계속 사용한 것을 reflection/파일 시각으로 확인. 반복 Refresh 대신 변경 스크립트 명시 import + CompilationPipeline.RequestScriptCompilation으로 전환.
- 씬 단독 Play가 Bootstrap으로 복귀하는 동안 FinancePresenter/IdleActivityPresenter가 없는 GameContext를 참조하지 않도록 진입 가드 추가.
- 기존 미션 안내에 경제창·도시 증권거래소·손익 탭 위치를 표시.

## 최종 결과
- 사용자 Unity 재실행 후 최신 어셈블리 로딩 확인(차트 RequireComponent 포함). 핵심 **40개**, Play 통합 **46개** 모두 재통과. [핵심](Phase25/CORE_TEST_RESULTS.json), [Play](Phase25/PLAY_TEST_RESULTS.json).
- 실제 주문 저장 실패 시 복구, 오프라인 급여/세금 1회 정산, 기존 급여 영수증 재지급 거부, 저장 복원, M035/M036/M038/M043/M047/M048 및 보상 중복 방지 검증.
- 차트 실제 렌더링 확인(60개 이상 mesh vertices), 종목 버튼 터치 raycast 통과. 1920×1080 / 1280×720 / 2400×1080에서 주식 화면 텍스트 세로 넘침 0. [UI 기록](Phase25/UI_CHECKS.txt), [시장](Phase25/Screenshots/StockMarket.png), [경제창](Phase25/Screenshots/Economy.png).
- 최신 Runtime/Editor C# 컴파일 오류 **0**. Unity 생성 rsp와 참조 어셈블리로 별도 컴파일도 exit 0: [Runtime](Phase25/FINAL_RUNTIME_COMPILE.txt), [Editor](Phase25/FINAL_EDITOR_COMPILE.txt). 새 Unity 세션에서도 프로젝트 코드 오류 로그 0 확인.
- 기존 60개 미션 asset, 캐릭터/배경/폰트 파일 변경 없음. 기존 저장 v4와 economy.debt/otherAssets/processedIds 유지, finance schema=1 선택 확장. 테스트 모델·세이브 슬롯·자동 저장 설정 복원.
- 추가 연결 파일: MissionData.cs, MissionProgress.cs, MissionService.cs, MissionConditionEvaluator.cs, MissionGuidance.cs, SceneTransitionService.cs, IdleActivityPresenter.cs, IdleActivityService.cs. 나머지 실제 수정 파일은 위 작업 단위별 목록 참조(.meta 포함).
- 설정: Assets/_Game/Data/Phase25/EconomyRules.asset. 세율/직업별 세율, 고지·이자 주기, 금리·신용도, 6종목·뉴스 확률/조건 변경 가능. 뉴스 Duration은 시장 갱신 횟수 단위. 거래 수입/지출 통계는 주식 원금 이동과 부채 원금 상환을 소득·소비로 중복 집계하지 않음.
- 남은 기능 문제: 없음. Unity AI NoSubscription, 재귀 MissionCondition의 import depth 로그 및 기존 API 사용 중단 경고는 기록만 했으며 관련 없는 패키지/미션 직렬화 구조를 변경하지 않음.
- 미검증: 실제 Android/Windows 플레이어 빌드와 실기기 터치·성능. 이번 단계에서는 Editor Play와 실제 파일 IO를 검증했다.
- 다음 작업: 필요 시 실기기 QA. Phase 2.6 이후 기능은 추가하지 않았다.
