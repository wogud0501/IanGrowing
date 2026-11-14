# PRE-PHASE 2.8 전체 게임플레이 QA

검사일: 2026-11-14 / Unity 6000.6.8f1, Apple Silicon, OSXEditor.
범위: 기존 Phase 1–2.7. Phase 2.8 신규 기능·미션·성장 나이는 추가하지 않았다.

## 진입 판정: CONDITIONAL

현재 구현된 성장·경제·저장 경로의 확인된 CRITICAL/HIGH 결함은 수정 후 재검사를 통과했다. 그러나 영아 전용 외형/행동 아트, 물리 입력·실기기 확인, 회사 화장실 조작권의 간헐 실패 사례가 남아 있어 완전 READY로 판정하지 않는다. Phase 2.8에 착수할 때 아래 잔여 조건을 추적해야 한다. 이번 작업에서는 Phase 2.8을 개발하지 않았다.

## 검사 방법과 전체 집계

Unity MCP는 이번 세션의 호출 가능한 도구에 노출되지 않았다. 기존 프로젝트의 Editor bridge로 **실제 Unity Play Mode**를 실행하고 기존 Developer QA Center/실제 서비스를 사용했다. 정적 코드 존재 확인을 PASS로 처리하지 않았다.

| QA Registry 최종 실행 상태 | 개수 |
|---|---:|
| 전체 등록 시나리오 | 139 |
| PASS | 122 |
| FAIL | 0 |
| PARTIAL | 0 |
| NOT IMPLEMENTED | 13 |
| BLOCKED | 0 |
| MANUAL | 4 |

원시 결과는 [REGISTRY_FINAL.json](PrePhase28/REGISTRY_FINAL.json)에 보존한다. PASS는 실행 시각과 assertion 수가 있다. 위 수는 시나리오 수이며 아래 별도 PlayMode 그룹의 assertion 수를 더하지 않는다.

기획 범위와 자연 성장 경로를 반영한 **동일 139항목의 보수적 판정**은 PASS 111 / FAIL 0 / PARTIAL 11 / NOT IMPLEMENTED 2 / BLOCKED 0 / MANUAL 4 / NOT APPLICABLE 11이다.

- M040–M050: 실제 평가·보상·저장 fixture 검사는 PASS이나, 순차 성장에서는 아직 Phase 2.8 M039를 통과할 수 없어 PARTIAL.
- M039 및 M051–M060: 보존된 기획의 후속 선택 이벤트·엔딩·업적 범위. 이번 단계에서는 NOT APPLICABLE. 원시 Runner의 NOT IMPLEMENTED 표시는 그대로 보존했다.
- 정식 가챠와 확장 장비 스탯/해제: NOT IMPLEMENTED. `PHASE2_6_FINAL_CHECK.md`가 확정한 연결 생활 월드 범위의 필수 기능으로 임의 추가하지 않았다.
- MANUAL 4: 물리 입력, 실제 구버전 UI 가져오기, 월드 시각 품질, 업무/착석 아트 품질. 별도 자동 모터·구버전 로더 검사는 실행했지만 수동 항목을 자동 PASS로 바꾸지 않았다.

전체 항목별 ID·결과·실행 시각·검사 수는 [FEATURE_MATRIX.csv](PrePhase28/FEATURE_MATRIX.csv), 60개 미션별 판정은 [MISSIONS_60.md](PrePhase28/MISSIONS_60.md)를 참조한다.

## Phase별 결과

| Phase | 실제 검증한 기능 | 판정 및 한계 |
|---|---|---|
| 1 | 새 게임, 이어하기, 실제 파일 저장/읽기, 출생 확인 버튼, 팝업 중복 방지, 씬/UI 왕복, 실제 모터 이동, v1–v4 복원 | 기능 검사 PASS. 물리 키보드·카메라 미술·전체 사운드 청취는 MANUAL |
| 2.1 | 8개 능력치 증감/극값, 0–100 clamp, 스트레스 연동, 체력 부족 차단, 무료 회복, 생존 유예/회복, 하루 진행 | PASS |
| 2.2 | 실제 행동 M001–M038 순차 성장, 60개 정의 전체 등록 검사, 조건/보상/중복/저장 fixture, 연령 경계·잠금, 기존 가중 생활 이벤트 | 현재 성장 경로 PASS. M040–M050 PARTIAL, 후속 11개는 현 범위 밖 |
| 2.3 | 기초/초등/중고등/직업교육/평생교육, 공부·졸업, 친구/사회관계, 이력서·면접·탈락·재응시·취업·급여·승진·퇴사·해고·재취업 | PASS. 성인/노년 교육은 데이터 fixture 후 실제 서비스 실행 |
| 2.4 | 자동 노동/공부/전투, 시작/중지, 실제 자원/보상, 1/24/168시간 오프라인, 상한/중복 방지, 1/7/30일 진행, 영아의 부적합 자동 활동 복원 차단 | PASS. 테스트 시간 소스 주입; OS 시계 변경 없음 |
| 2.5 | 급여/세금/체납, 부채·이자·상환·신용, 순자산 극값, 주식 매수/매도/부족/중복, 결정적 ±10% 손익, 경제 뉴스, 저장 | PASS. 신규 미성년 직접 상환/납세 차단도 재검사 |
| 2.6 | 기존 스타터 헤드폰/장비 1회 장착, 연결 생활 월드, 집/가구/문, NPC 업무석·생활, 기존 UI 메뉴 | 구현된 경로 PASS. 정식 가챠·확장 장비는 NI. 영아/방향별 생활 아트는 PARTIAL/MANUAL |
| 2.7 | 매물/매매/손익/중복, 전세 원금 반환, 월세·관리비/만료, 대출·상환, 시세/뉴스/오프라인, 주거 회복/순자산, 실제 거래 UI·확인창·저장 | PASS. 자연 미션 M045 이후는 M039 의존으로 PARTIAL |

## 출생 및 생애주기

근거: `IAN_DESIGN_BASELINE.md`, `MAIN_MISSIONS_60.md`, Phase 2.3/2.4/2.7 진행 문서, 실제 ScriptableObject/서비스. 보행 1세 규칙은 **이번 사용자의 공식 추가 지시**를 적용했다.

| 구간/해금 | 검증 및 결과 |
|---|---|
| 출생 | 실제 새 게임 0세/현금0G/부채1,000,000G, 초기 8개 능력치, 한글 DAY1 팝업·확인 버튼, 한 번만 표시, 기존 세이브에서 재표시 없음 |
| 0세 | 모터 Update/FixedUpdate/스크립트 통과 이동과 성인 이동 Pose 차단. 침대의 기존 무료 식사·수분·수면·회복·기초 학습으로 진행 가능 |
| 1세 | 실제 돌봄→M002 성장으로 보행 해금. 첫 걸음 안내1회, 집 안 실제 이동, 안내 중 조작 가능, 실제 저장/재로드 후 안내 재출력 없음 |
| 1–6세 | 보행 허용, 독립 외출 차단. 0/1/2/6/7세 양쪽 조건을 구분하여 검사. 임의의 보호자 외출 기능은 추가하지 않음 |
| 7세 | 실제 초기 미션 M010 완료로 성장, 독립 외출과 초등학교 해금. 강제 나이 fixture와 실제 성장 경로를 각각 검사 |
| 13세 | M020 성장 및 중고등 과정, 관계/시험/진로/졸업 실제 실행 |
| 19세 | M030 졸업 후 성인 진입. 18/19/20 경계, 취업/투자/금융시설/계약 직접 실행의 잠금 검사 |
| 30/60세 이후 | 직업교육/평생교육·단계 계산/저장 fixture 검사. 자연 생애는 후속 M039 때문에 아직 해당 나이에 도달하지 못함. 노년/엔딩 신규 구현 없음 |

`WalkingUnlocked = age >= 1`, `IndependentOutdoorUnlocked = age >= 7`을 `LifeProgression`에서 별도 관리한다. `GrowthAccess`와 실제 플레이어 모터가 각자 맞는 조건을 사용한다. 첫 걸음 안내 수령 여부는 보행 권한을 결정하지 않는다.

실제 성장 경로는 M001–M038 수령, 27세 / DAY297 / 현금5,421G / 신용20까지 도달했다. 무료 돌봄/회복, 실제 교육·사회활동·스타터 장착, 편의점→사무직→개발자 취업, 급여·납세·부채 상환·신용 회복·첫 매수를 수행했다. 나이/현금/능력치/미션 조건을 지급하지 않았다. 면접/사회성 난수만 결정적으로 고정하고 연출 시간을 단축했다. 초기 10개는 월드 돌봄, 이후는 기존 씬의 실제 행동 서비스를 호출했다. 전 구간 물리 키보드 플레이를 의미하지 않는다.

상세: [NATURAL_SUMMARY.json](PrePhase28/NATURAL_SUMMARY.json), 중단 복구용 [NATURAL_CHECKPOINT.json](PrePhase28/NATURAL_CHECKPOINT.json). 검사 도중 누락된 졸업 행동과 상위 직업/신용 회복 전략을 보완하여 동일하게 획득한 체크포인트에서 이어갔다. 이는 생산 세이브가 아니다.

## 발견 결함 및 수정

| ID / 심각도 | 재현·원인 | 수정 / 재검증 |
|---|---|---|
| QA28-001 HIGH | 0세 실제 모터 `BeginTraversal` 호출 시 약0.576 이동. 이동 실행부에 성장 조건 부재 | WorldPlayerMotor에 공식 WalkingUnlocked 검사. 입력/물리/스크립트 경로와 이동 Pose 차단. 출생 검사17개 및 공식 보행 검사44개 통과 |
| QA28-002 HIGH | 0세 보행만 차단하면 기존 현관 스폰에서 침대 돌봄에 닿지 않아 진행 불가 | LifeWorldController의 0세 Home 복원을 기존 침대 접근점에 연결. QA에서0세 설정 시 같은 경로 사용. 실제 돌봄→성장 진행 PASS |
| QA28-003 HIGH | 0세에 현금 fixture를 주고 부채 상환 서비스를 직접 호출하면 성공 | EconomySystem 최종 거래 경계에서 미성년 DebtRepayment 거부. EconomyFinance 직접 납세, FinancePresenter 실행/버튼에도 조건 적용. 8개 집중 검사 통과 |
| QA28-004 CRITICAL | v1/v2 파일은 처음 읽히지만 저장 후 다시 읽기 실패. JsonUtility의 null inline 금융/주거 블록이 기본 객체로 직렬화되어 검증 실패 | 선택 블록 존재 메타데이터 추가. Capture/Save가 실제 null 여부 기록, Loader가 기본 placeholder만 복원하고 실제 자산/표시 불일치는 거부. v1–v4 roundtrip 및 원본3개 복사본·손상/미래버전 검사 PASS |
| QA28-005 MEDIUM (QA 도구) | 학교 직전6세 preset이 M010까지 수령하여7세가 됨 | before preset은 현재 나이 미만 milestone만 수령. 실제 preset 검사 PASS |
| QA28-006 MEDIUM (새 안내) | 첫 걸음 흰 글씨가 바닥과 겹쳐 읽기 어려움; 배경 추가 후 터치 버튼과 일부 겹침 발견 | 기존 종이 패널/잉크 색으로 안내, 모바일에서는 조작 영역 위로 배치. 4해상도 글자/배경·터치 대상 비겹침 검사 PASS |
| QA28-007 MEDIUM (검사 기대값) | 이전 주민 검사가 공용 `workdesk`를 기대하여 현재 전용 직원 좌석에서 FAIL | 주민 데이터의 현재 시간표로 업무석 ID를 구해 실제 사용 기록 검사. 게임 NPC 데이터를 옛 공용 좌표로 되돌리지 않음 |
| QA28-008 MEDIUM / 관찰 중 | 주요 문 전체 검사에서 office-bath-front 통과 후 CanControl 확인1회 실패 | 같은 두 화장실 문·양쪽 접근 집중 재검사32개 PASS. 조작권 상태 진단 추가. 최초 실패 원인은 확정되지 않았으며 간헐 사례로 보존 |
| QA28-010 CRITICAL (테스트 세이브 보호) | 최종 시작 기준 대조에서 생산 자동 저장/백업2개 해시 차이 발견. 각 QA 진입/종료 파일 비교만으로는 테스트 사이의 변경을 잡지 못함. Editor bridge가 EnteredEditMode에서 보호를 즉시 해제해 재컴파일·늦은 생명주기 저장을 막지 못하는 경계 존재 | 테스트 예약 전 보호를 켜고, 종료/컴파일/Edit Mode에도 유지. 다음 명시적 일반 Play 진입에서만 정상 저장 권한 복구. 변경본 보존 후 시작 백업 복원 및 별도 Play/Edit 쓰기 경계 재검사. `SAVE_RECOVERY.json` 참조 |
| QA28-009 개발환경 제한 | Play 중 C# 재컴파일 시 비직렬화 서비스 참조가 소실되어 NRE/검사 정지 | 테스트를 cold Play로 실행. bridge는 import/reload 안정화와 EnteredPlayMode/서비스 준비 후 시작. 게임 전체 hot reload 지원을 완료했다고 주장하지 않음 |

수정 전 실패 JSON은 `PrePhase28/BeforeFix/`, `QA1_BEFORE_FIX.json`, `FINANCE_GATE_BEFORE_FIX.json`, `REGISTRY_FIRST_RUN.json`에 남겼다. 테스트 실패를 숨기거나 성공 결과로 대체하지 않았다.

## 실제 UI·월드 실행 증거

- 실제 메뉴 버튼/닫기·부동산6탭·구매 확인/매각·주식 매수/매도/탭·상환 버튼: 95 assertion PASS, UI issue0. 1280×720,1920×1080,904×2316,1840×2208.
- 공식 보행/첫 걸음·디스크 복원·7세 외출 및 안내 레이아웃: 44 assertion PASS, UI issue0.
- 주요 문16포털 검사: 최초169 assertion 이후 회사 화장실 조작권 확인 실패. 실패 지점 이후 두 문은32 assertion 집중 재검사 통과. 앞뒤 접근 중 벽으로 막힌 접근은 유효한 양쪽 포털로 검사하며 벽 안에서의 진입을 성공 처리하지 않는다.
- 회사/증권/부동산 실제 업무 프레임·점유·퇴근 이동·4해상도·NPC 저장 복원:67 assertion PASS, UI issue0.
- 실제 증권/부동산 고객 예약·상담 기록·걸어서 퇴장:13 assertion PASS.
- 생산 쓰기 보호:실제 Play10 assertion PASS 및 종료 후 Edit Mode에서 실제 SaveManager 호출 거부 확인. 변경되었던 생산 파일은 양쪽 상태를 보존하고 시작 백업으로 복구. [SAVE_RECOVERY.json](PrePhase28/SAVE_RECOVERY.json) / [Edit 검사](PrePhase28/SAVE_GUARD_EDIT_RESULTS.json).
- 최종 전체 재실행:139개 모두 다시 실행, PASS122 / FAIL0 / NI13 / MANUAL4. 이전 실패 항목만 재검사한 누적 결과와 별도로 [FINAL_INTEGRATION.json](PrePhase28/FINAL_INTEGRATION.json)에 보존.
- 주민/모바일/교통 추가 검사:83 assertion PASS, UI issue0. 실제 NPC 문 출입·업무·식사·수면·관계 방향·좌우/상하/대각선 속도 방향,7회 화면 재구성·Safe Area·터치 액션·백그라운드 복귀,27초 실제 차량/신호 주기와 야간 교통/전조등 확인. [WORLD_RESULTS.json](PrePhase28/WORLD_RESULTS.json).

관련 JSON: [UI_RESULTS](PrePhase28/UI_RESULTS.json), [WALKING_FINAL_RESULTS](PrePhase28/WALKING_FINAL_RESULTS.json), [FACILITIES_RESULTS](PrePhase28/FACILITIES_RESULTS.json), [VISITORS_RESULTS](PrePhase28/VISITORS_RESULTS.json), [문 집중 재검사](PrePhase28/DOORS_AFFECTED_RETEST.json).

실제 Game View: [출생](PrePhase28/Screenshots/Birth.png), [첫 걸음](PrePhase28/Screenshots/FirstSteps-1280.png), [세로 터치](PrePhase28/Screenshots/FirstSteps-904.png), [회사](PrePhase28/Screenshots/Facility-workplace.png), [부동산](PrePhase28/Screenshots/Facility-realestate.png), [증권](PrePhase28/Screenshots/Facility-finance.png), [투자 UI](PrePhase28/Screenshots/Stock.png). 움직임 검증은 프레임/속도/위치/상태를 실제 Play 중 관찰했으며 정지 캡처만으로 PASS 처리하지 않았다.

## 저장 안전·수정 파일

- 시작 시 생산 세이브3개와 Git 상태를 `DevelopmentBackups/PrePhase28-20261114`에 보존. 기준 SHA-256은 [SAVE_BASELINE.json](PrePhase28/SAVE_BASELINE.json).
- Play 진입 전 `phase26-*` 전용 슬롯, QA Center 내부 `qa-*` 슬롯, 전체 모델 snapshot과 원상복구 사용. 실제 원본 복사본만 QA 채널에서 읽었다.
- 원본 미커밋 `.DS_Store` 및 `Docs/PATCH_NOTES_2026-11-14.md`는 그대로 보존. 최종 전체 기준 해시 검사에서 자동 저장2개의 변경을 발견했다. 현금·미션·직업·runId는 시작 상태와 같고 시간·시세·월드 위치 등이 갱신되었다. 변경본을 별도 보관하고 아래 QA28-010의 보호 수정 후 시작 백업을 복원했다. 복원 후3개 파일 SHA-256이 시작 기준과 일치한다. 원본이 전 과정에서 불변이었다고 주장하지 않는다.
- 새 `walkingIntroductionSeen`은 저장되며 기존 2세 이상 파일은 완료 처리해 성인에게 안내가 재생되지 않는다. 기존1세 파일은 보행을 막지 않고 안내만1회 제공한다.
- 기존 선택 블록·현금·부채·미션·장비·가구·부동산·직업 데이터를 초기화하지 않는다. 60개 미션 정의와 공식 캐릭터 이미지 수정 없음.

실제 수정 런타임 파일: `WorldPlayerMotor`, `LifeWorldController`, `LifeProgression`, `GrowthAccess`, `WorldHUD`, `WorldMobileHUD`, `EconomySystem`, `EconomyFinance`, `FinancePresenter`, `SaveManager`, `GameManager`.

개발 전용 파일: `DebugCommandService`, `DeveloperQACenter`, `TestScenarioRegistry`, `PrePhase28Scenarios`, `PrePhase28RiskScenarios`, `Development/PrePhase28Verification`, `PrePhase28WalkingVerification`, `PrePhase28UIAudit`, `QACenterVerification`, `Phase26Verification`, `ResidentLifeVerification`, `CriticalRecoveryVerification`, `Editor/Phase26EditorBridge`.

씬/프리팹 직렬화 파일을 재생성하지 않았다. 기존 LifeWorld의 모터/HUD/controller와 저장·경제 서비스에 연결된 코드를 수정했으며 실제 씬에서 동작을 실행했다. 최종 Editor 씬 저장 상태 및 원본 해시 확인은 `FINAL_PRESERVATION.json`에 기록한다.

## 미검증·후속 조건

1. 영아·유아 전용 Idle/기어가기/보호자 동반 외출 리소스·시스템은 완성되지 않았다. 현재 공식 이안 외형과 기존 돌봄 모션을 사용한다. **0세 직접 보행은 차단**되어 있지만 연령별 미술을 완료로 판정하지 않는다.
2. M039와 후속 엔딩/선택/업적이 없어 전체60개를 자연 플레이로 완료할 수 없다. 현재 구현된38개 순차 진행과 나머지 fixture/후속 범위를 구분한다.
3. 직접 물리 F10/WASD/블루투스 키보드·게임패드, 모바일 터치 감각, 문/착석·기립 미술 품질, 전체 사운드는 수동 확인 필요. 합성 Input System과 실제 모터 경로의 성공이 실제 기기 검증을 대신하지 않는다.
4. Android adb 연결 기기0대, iOS native Xcode 없음(`/Library/Developer/CommandLineTools`). 이번 QA에서 새 Android/iOS 앱 패키지·기기 실행은 수행하지 않았다. 과거 빌드 결과와 이번 결과를 혼합하지 않는다. 폴더블은 Editor 창 크기/Safe Area 재구성 검사 범위이다.
5. 회사 화장실 조작권 간헐 실패의 원인은 미확정. Play 중 코드 hot reload 제한도 남는다. Console의 기존 Unity AI NoSubscription/UERP·라이선스 로그, 미션 재귀 직렬화·폐기 API 경고는 별도 기록 대상이며 무오류 Console을 주장하지 않는다.
6. 극초기 부채가 누적되면 편의점 급여만으로 신용 회복이 어렵다. 실제 상위 직업 전환/상환으로 M038까지 진행됨을 확인했으나 장기 경제 밸런스 조정은 하지 않았다.

원격 Git은 설정되어 있으나 이번 작업은 push하지 않았다. 로컬 커밋/백업은 장비 손실에 대한 원격 백업 완료와 구분한다. 마지막 복구 지점은 `git log -1`과 `PHASE_2_6_PROGRESS.md`를 참조한다.
