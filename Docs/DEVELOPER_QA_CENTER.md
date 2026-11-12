# Developer QA Center

## 현재 상태 — 구현 및 핵심 실제 실행 검증 완료
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
- Unity Test Framework PlayMode 샌드박스 검사 실행 PASS 1 / FAIL 0. 직접 Test Runner 실행에서도 Play 진입 전에 보호 훅을 자동 적용. 원본 파일 보존, 미리보기에서 실제 행동 허용, QA 재개 시 시간 정지 확인.
- 전체 런타임 Registry를 호출하는 Explicit PlayMode 브리지도 등록했다. 기존 전체 검사가 통과했으므로 이 브리지로 전체를 중복 실행하지 않았다.
- 선택 미션 카운터의 임의 값 설정 → 실제 RecordSignal/완료 판정과 읽기 전용 조건 관찰 검증을 추가했다. 상태 관찰 버튼은 기존 PASS/FAIL 판정을 덮어쓰지 않는다.
- 실제 자동 노동의 오프라인 정산이 DAY 40 월세/관리비와 M045 이벤트로 연결되고 동일 시각에 중복되지 않음을 추가 검증했다.
- 큰 기존 부채+미납의 순자산 부호 역전 방지와 미납 합계 오버플로 저장 거부를 추가 검증했다.
- 최종 누적: 100개 등록, PASS 84 / FAIL 0 / NOT IMPLEMENTED 13 / MANUAL 3. 각 추가 검사는 새 항목만 실행했다. 최종 REGISTERED_SCENARIOS_RESULTS.json과 INTEGRATION_RESULTS.json 참조.
- 저장 채널의 경로/차단 상태뿐 아니라 원본 로드/마이그레이션/출처/복구 경로 메타데이터도 복원한다. 독립 Unity 객체에서 메타데이터 round trip PASS.
- 최종 플레이어 스크립트 Release/Development 양쪽 컴파일 및 DLL 심볼 증거 확인 완료. 소스/에셋은 저장되었으며 네이티브 앱 패키지 실행 검증은 아님.
- 모든 분류를 통과했다고 아직 주장하지 않음. 실제 Android/iOS 기기 및 물리 키보드 F10은 별도 수동 검증.

## 바로 사용
1. Bootstrap Play → `F10` 또는 Editor 메뉴로 열기. macOS 미디어 키 설정에 따라 `Fn+F10`이 필요할 수 있다.
2. `상태 조작`에서 숫자 입력 후 현금/나이/능력치/날짜 설정. 오프라인 보상은 먼저 해당 자동 활동과 기존 조건을 준비한다.
3. 분류/검색으로 항목 선택 → 테스트 실행 / 관련 화면 / 실제 현재 조건 관찰 / 테스트 데이터 초기화.
4. 자동 행동의 관련 화면은 기존 별도 School/Workplace/Battle 씬으로 열린다. 생활 월드의 직접 상호작용 정책은 바꾸지 않았다.
5. 미리보기 중에는 QA 채널과 상단 배너가 유지된다. F10으로 돌아오고 원상복구·닫기로 실제 진입 상태를 복원한다.
6. 전체/분류 실행, 실패한 항목만 재실행, 중지, JSON 저장/결과 복사를 사용한다. 스크롤 목록의 포커스 선택 항목을 따라 화면을 이동한다.

## 남은 수동 범위
물리 F10/외부 키보드/게임패드 및 Android/iOS 기기 동작, 시각적 애니메이션 품질은 직접 검증해야 한다. F10의 합성 Input System 입력 경로는 실제 실행으로 검증했다. 초기 화면 생성 및 씬 간 유지/자금/시간/저장/복구는 자동 검증했다. 스크립트 재컴파일 시 QA 채널 유지/화면 재생성 방어는 구현했지만 실제 hot reload 경로 자체는 별도 미검증이다.

주거 대출은 기존 LifeProgression.MeetsLoanRequirements를 사용해 19세 성장 해금과 신용 기준을 함께 검사한다. 이 변경은 대출 항목만 재실행하여 검증했다.

최종 코드/에셋 저장: Bootstrap / Edit Mode / StandaloneOSX / 씬 dirty=false. 현재 C# 컴파일 오류 0. 기존 Unity AI 패키지 NoSubscription/UERP 로그와 미션 재귀 직렬화 경고는 별도이며 이번 기능 오류로 혼동하지 않는다. 원격 push는 수행하지 않았다.
