# 이안 키우기 QA 결함 기록

2026-11-19 · Unity 6000.6.8f1. 심각도는 발생한 동작을 기준으로 정했다. 아래 상태는 각 결함의 기대 동작에 대한 재시험 결과다.

## Fixed

| ID · 심각도 | 시스템·재현 | 원인·수정 파일 | 동일 조건 재검증·인접 회귀 | 상태 |
|---|---|---|---|---|
| BUG-001 · MEDIUM | 28개 씬과 91개 프리팹의 직렬화된 객체 참조에서 32개 UI 이미지가 끊어졌다. | SoftCorner GUID는 유효했지만 이전 스프라이트 서브에셋 ID를 참조했다. 기존 GUID를 보존하고, 24개 씬과 [공용 버튼](../Assets/_Game/Prefabs/UI/ButtonBase.prefab)의 대상 ID를 실제 임포트된 스프라이트에 맞췄다. | 같은 128,320개 컴포넌트 검사에서 Missing Reference 32→0, Missing Script 0. 상속 프리팹·실제 인벤토리·메뉴·가챠 화면 확인. [수정 전](QAFull/refs-before.json) · [수정 후](QAFull/refs-after.json) | PASS |
| BUG-002 · LOW | Windows 활성 빌드 목록에 카메라·라이트만 있는 Unity 템플릿 SampleScene이 포함됐다. | [EditorBuildSettings](../ProjectSettings/EditorBuildSettings.asset)에서 샘플의 빌드 포함만 해제했다. | 활성 25개 경로 재확인. Bootstrap·메뉴, 16개 방, 기존 7개 경로를 실제 로드했다. [목록](QAFull/build-scenes.json) · [구 경로](QAFull/housing-legacy.json) | PASS |
| BUG-003 · HIGH | M023까지 획득한 저장에서 빈 보상 정산 4회에 원장 9→13, 디스크 성공 쓰기 4회. 별도 순차 플레이에서 M024를 수행할 보장 장비가 제때 전달되지 않았다. | [ItemService](../Assets/_Game/Scripts/Inventory/ItemService.cs)가 M020을 이미 수령한 경우도 지급 대상으로 취급했다. 미지급 보장 장비·실제 전달 가능한 대기 보상만 검사하고, 실패한 정산만 재시도한다. 로드 시 미지급 권리를 다시 확인한다. | 동일 조건에서 원장 8→8, 쓰기 0회. 실제 M024/025 장착 후 M038까지 수령. 디스크 실패 시 지갑·아이템·천장 롤백, 재전송, 구버전 스타터 복원 포함 88개 검사 통과. [전](QAFull/pending-before.json) · [후](QAFull/pending-after.json) · [영구 회귀 검사](QAFull/items-regression.json) | PASS |
| BUG-004 · MEDIUM | 플레이어에서 재귀 미션 조건의 직렬화 깊이 제한 경고가 반복됐다. | [MissionData](../Assets/_Game/Scripts/Mission/MissionData.cs)의 재귀 children을 관리 참조로 직렬화하고, 자식이 있는 M009·010·030·040·050·053을 Unity에서 변환했다. | 60개 임포트 결과의 조건 트리 차이 0. 변경 후 Windows 플레이어와 Unity 빌드 로그에서 해당 메시지 0. 복합 조건과 연속 플레이 재검증. [트리 대조](QAFull/conditions-comparison.json) · [빌드·로그](QAFull/build-verification.json) | PASS |
| BUG-005 · MEDIUM | QA 도구가 정상 Home 경로·성장 단계·오프스크린 식사를 실패 처리했다. 자연 진행 검증기가 신생아 침대를 찾지 못했고, 인벤토리 검증기는 비동기 전환 직후의 잠금을 고착으로 보고했다. | [테스트 등록기](../Assets/_Game/Scripts/Development/Core/TestScenarioRegistry.cs), [성장](../Assets/_Game/Scripts/Development/Scenarios/GrowthQAScenarios.cs), [방](../Assets/_Game/Scripts/Development/Scenarios/RoomQAScenarios.cs), [인생](../Assets/_Game/Scripts/Development/Verification/PrePhase28Verification.cs), [인벤토리](../Assets/_Game/Scripts/Development/Verification/Phase29Verification.cs). 실제 방·11개 외형 프리셋·가족 침대와 접근 거리·전환 종료를 사용한다. | 등록기 초기 실패 3건 재시험. 실제 오프스크린 재료 1회 소비와 재정산, 잠금 복귀를 별도 검사했다. [초기](QAFull/registry.json) · [영수증](QAFull/receiptDiag.json) · [회귀](QAFull/registry-full.json) | PASS |
| BUG-007 · MEDIUM | 새벽 1시 도심에서 이안의 검은 옷·보도·횡단보도의 경계를 읽기 어려웠다. | [City 프로필](../Assets/_Game/Data/Presentation/City.asset)과 [재생성 설정](../Assets/_Game/Scripts/Editor/World/Phase30Setup.cs)의 야간색을 함께 조정했다. | 같은 카메라의 보행 영역 중앙값이 8비트 휘도 19→48. 01시·입력·낮 프로필 회귀 통과. 이미지에서 도로와 보행자를 확인했다. [전](QAFull/Screenshots/night-same-camera-before.png) · [후](QAFull/Screenshots/night-same-camera-after.png) · [측정](QAFull/night-contrast.json) | PASS |
| BUG-008 · LOW | 개발 QA 메뉴가 실제 구현된 장비 해제·스탯을 미구현으로 표시했다. | [수집 QA](../Assets/_Game/Scripts/Development/Scenarios/CollectionQAScenarios.cs)에 기존 ID equipment/stats로 실동작 시나리오 등록. | 다른 슬롯에 헤드폰을 장착한 상태에서 배지를 해제하고 급여 원복 및 헤드폰 유지 검사. [등록기 결과](QAFull/registry-full.json) | PASS |
| BUG-009 · CRITICAL | 허용된 65,000개 영수증을 저장하면 성공하지만 방금 기록한 10,701,222바이트 JSON을 로드가 거절했다. 큰 세이브가 마지막 정상 슬롯을 대체했다. | [SaveManager](../Assets/_Game/Scripts/Save/SaveManager.cs)의 쓰기에는 읽기의 8 MiB 제한이 없었다. 새 공통 상한 16 MiB를 UTF-8 바이트로 양쪽에 적용했다. 기존에 작성한 대형 JSON도 읽을 수 있다. 들여쓰기 때문에 한도를 넘을 때만 압축 없는 간결한 JSON으로 다시 직렬화하고, 그래도 크면 원본 교체 전 실패를 반환한다. 저장 스키마는 v4 그대로다. | 10 MiB 저장 재로드, 경계값 56,000개 영수증의 전체 수량, 한도 초과 후 이전 파일 SHA-256·기록 시각·읽기 가능 여부 검증. 개발 QA 등록기에 회귀 케이스를 추가했다. [재현](QAFull/size-before.json) · [수정 후](QAFull/size-after.json) · [상시 검사](../Assets/_Game/Scripts/Development/Scenarios/PrePhase28RiskScenarios.cs) | PASS |

## Remaining

**BUG-006 · BLOCKER · 미션·생애 진행 · FAIL.** 신규 인생에서 M038 첫 주식 매수까지 순서대로 완료한 뒤 해당 체크포인트를 복원했다. 27세, M038 보상 수령, M039는 Planned, 선택 횟수 0/3이며 보상 요청이 거절된다. 실제 MissionService의 등록 기능에 EventChoices가 없고, 기존 가중 생활 이벤트는 무작위 결과를 적용한다. M051–060에도 현재 공급되지 않는 기능 요구가 있다. M040–050의 독립 fixture 성공과 60세 외형 검증으로는 이 차단을 해소할 수 없다. 필요한 수정은 실제 선택·은퇴·생애 평가 콘텐츠와 완료 신호를 연결하고 같은 세이브에서 M060까지 다시 플레이하는 것이다. 기존 조건·보상·세이브는 보존했다. [재현 저장 상태](QAFull/gate.json) · [M039 데이터](../Assets/_Game/Data/Missions/M039.asset) · [미션별 판정](QA_FULL_PROJECT.md)

## Deferred / Not Reproduced / Not Tested

- **Deferred.** 학교·금융·회사 화면에서 책상 배치와 주변 여백이 비슷하다. 제작 품질 검토 대상으로 남겼다. 작동하는 방의 구조를 QA 중에 재설계하지 않았다. [캡처](QAFull/Screenshots/)
- **Not Reproduced.** 포커스 없는 Editor에 합성 키를 넣은 초기 검사에서 이동하지 않았다. 네이티브 macOS 개발 플레이어의 실제 입력 바인딩에서는 WASD·대각선·I·ESC·E와 입력 복귀가 통과했다. 상태창 검사도 Game View 크기를 고정 모니터 크기와 비교할 때 실패했으나, 실제 렌더 Canvas 좌표로 재검사한 6개 화면비는 모두 경계 안에 있었다. [입력](QAFull/input-player.json) · [레이아웃](QAFull/status-canvas.json)
- **Not Tested.** 네이티브 Windows PC에서의 조작·렌더링, Android/iOS 기기, OS가 실제 파일 쓰기 도중 종료되는 상황, 장시간 메모리·오디오 청취 검수는 잔여 범위다. [전체 케이스](QA_FULL_PROJECT.md)
