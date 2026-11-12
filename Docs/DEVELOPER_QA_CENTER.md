# Developer QA Center

## 현재 상태 — Editor 실제 실행 검증 완료, Test Framework 실행 연결 최종 확인 중
- Editor / Development Build 전용. F10, Editor 메뉴 `Ian > Developer QA Center > Open (F10)`.
- 모든 씬에서 유지되는 별도 다크 픽셀 오버레이. 기존 한글 글꼴·PixelButton 재사용. 검색/분류/선택/스크롤/진단/결과 로그.
- 상태 조작: 현금 지급/설정/0, 부채/실제 상환, 나이/날짜, 8개 능력치, 체력/정신력(스트레스)/행복, 실제 시간 진행, 오프라인 1/24/168시간.
- 실제 서비스 시나리오: 기본 저장·출생·UI, 생존·성장, 60개 미션 조건 fixture→실제 평가/보상/중복 거부, 교육/직업/사회, 자동 행동/오프라인, 주식 손익/뉴스/세금/부채, 스타터 장착, 부동산.
- NOT IMPLEMENTED: 정식 가챠, 확장 장비 스탯/해제, 미지원 미션 서비스. MANUAL: 물리 이동/애니메이션 품질/기존 저장 버전별 확인.
- PASS는 해당 시나리오가 실행되고 실제 assertion을 하나 이상 통과했을 때만 부여. NOT RUN은 미실행. FAIL/NOT IMPLEMENTED/BLOCKED/MANUAL 별도.
- 전체/분류/선택/실패만 재실행, 진행률, 중지, 결과 복사/JSON 보고서. 보고서는 persistentDataPath/DeveloperQA, Editor에서는 Docs/DeveloperQA/LATEST_RESULTS.json에도 기록.

## 저장 안전
- 열기 전에 전체 모델/원래 채널·씬·설정·난수 상태·파일 해시를 보관하고 `qa-<GUID>.json` 전용 채널로 전환.
- 수동 Save/Load 및 자동 체크포인트 모두 QA 채널에만 기록. 원래 파일 삭제/교체 기능 없음.
- `원상복구·닫기`와 열린 화면의 F10은 실행 중 검사를 중지하고 원래 씬/현금/미션/직업/주식/부동산/인벤토리/시간을 복구. 관련 화면 미리보기에서는 QA 슬롯을 유지하고 상단 배너/F10으로 돌아옴.
- Play 종료/앱 종료에서는 QA 채널과 자동저장 비활성화를 유지해 종료 콜백 순서에 따른 원본 저장 덮어쓰기를 방지.
- OS 시계는 변경하지 않음. IQATimeSource를 통해 기존 IdleActivityService.SettleToUtc(now)와 EconomySystem.SettleMarket(now)에 시각 주입. 기존 오프라인 상한/워터마크 유지.

## 확장
`TestScenarioRegistry.Register(new QAScenario { id, category, title, description, success, run, related })`로 새 Phase 항목 등록. 실행은 `IEnumerator`와 `QAScenarioContext.Check/Require` 사용. 서비스 미구현은 QANotImplementedException, 환경 의존은 QABlockedException. 실제 서비스 호출과 assertion이 없는 항목은 PASS 불가.

## 미검증/남은 작업
- 실제 Play: F10 Input System 합성 키 이벤트가 실제 Toggle 경로를 열었고 입력 잠금/현금/나이/체력/스트레스 조작·저장·초기화·원상복구 검사를 통과.
- 첫 전체 실행: PASS 79 / FAIL 1 / NOT IMPLEMENTED 13 / MANUAL 3. 대화 검사의 잘못된 Home 실행을 정식 City 경로로 수정. 37개 UI 잘림은 동적 행 높이·툴바 유연 높이·진단문 높이로 수정.
- 실패 및 신규 2개만 재실행하여 최종 누적 PASS 81 / FAIL 0 / NOT IMPLEMENTED 13 / MANUAL 3 (97개 등록). 이전 PASS의 실행 시각/검사 수를 유지했고 재실행하지 않음.
- 통합 검사 19개, 1280×720/1920×1080/904×2316 UI 넘침 0. 실제 현금·미션·직업·주식·부동산·인벤토리·나이 및 진입 슬롯 복구, 진입 파일 SHA-256 동일.
- Release/Development StandaloneOSX 플레이어 스크립트 컴파일 각각 성공. Release DLL에서 DeveloperQACenter/DebugCommandService/TestScenarioRegistry/TestSaveSandbox/TestRollSource/EnterTestingChannel 심볼 없음, Development DLL에는 존재. 패키징된 실행 파일의 실행 검증을 의미하지 않음.
- Unity Test Framework PlayMode sandbox test 등록 완료, 실제 도구 실행 확인은 다음 단위.
- 모든 분류를 통과했다고 아직 주장하지 않음. 실제 Android/iOS 기기 및 물리 키보드 F10은 별도 수동 검증.
