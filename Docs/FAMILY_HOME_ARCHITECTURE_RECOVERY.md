# 부모님 집 공간 구조 복구 — 2026-11-14

## 원인과 수정 범위

제가 만든 첫 `FamilyLifeSetup.BuildFamilyLife()`는 넓은 Room 하나에 침대3개와 식탁3개를 배치했다. 실제 방/벽/출입구/가구별 동선을 정의하지 않은 것이 원인이다. 런타임에서 정상적인 방을 삭제한 문제가 아니었다. 첫 시안은 잘못된 공간 설계였으며 완성된 가정집으로 판정하지 않는다.

생성 코드와 저장된 LifeWorld 씬을 함께 수정했다. 기존 회사·학교·상점·금융시설은 편집 전후 구조 서명을 비교해 보호했다. 개인 독립 집을 가족 집으로 덮어쓰지 않았고, 건물 `WorldDoor`와 문짝 렌더 스크립트를 교체하지 않았다.

## 최종 평면

[설계 기준](FamilyLife/ARCHITECTURE_PLAN.md) · [실제 저장 평면 데이터](FamilyLife/FLOOR_PLAN.json)

- 북서쪽 **부모님 침실**: 전용 벽·문, 부모 잠자리2개, 옷장, 침실 생활용품. 부모와 이안의 침대는 같은 방이나 같은 전역 행에 놓지 않는다.
- 북쪽 중앙 **이안의 방**: 전용 벽·문, 0세 아기 침대/돌봄 표현, 성장 후 침대, 책과 생활용품 수납.
- 북동쪽 **독립 욕실**: 집 평면 안에 타일 바닥과 실제 벽·출입문·세면대·변기·샤워 시설. 이전 별도 family-bathroom 좌표는 이 실내 욕실 위치로 안전하게 매핑한다.
- 남서쪽 **거실**: 서로 향하는 소파와 TV, 낮은 생활 테이블. 주방과 침실을 잇는 통로를 확보했다.
- 남동쪽 **주방/식사**: 싱크–조리대–냉장고 작업 구역과 통로, **단일 가족 식탁 및 세 점유 좌석**. 기존 작은 식탁3개의 이미지와 Collider를 제거했다.

같은 LifeWorld Scene, 같은 가족 집 영역 안에서 물리 벽과 내부 문으로 나눈다. 방마다 Scene을 새로 만들지 않았다. 평소 카메라는 근처 공간을 따라가며, 전체 평면 캡처에만 QA에서 넓은 카메라를 사용한 뒤 복구한다.

## 문과 이동

기존 문 상태·경첩·충돌·개폐 연출을 사용한다. 같은 구역의 내부 문은 씬 전환을 실행하지 않고 실제 문턱을 걸어서 통과한다. 부모 NPC는 기존 A* 경로에서 닫힌 내부 문을 만나면 사용 권한을 얻고, 열린 뒤 통과하고, 지나간 뒤 반환한다. 이동 방향/보행 프레임은 기존 실제 속도 기준을 유지한다.

실행 검사에서 이안의 방문 아래에 거실/주방 칸막이가 바로 이어져 퇴로를 막는 문제를 발견했다. 문 앞의 대기·통과 공간을 확보하도록 칸막이를 짧게 조정한 뒤 세 내부 문의 양방향 통과와 실제 벽 충돌을 재검증했다.

새 부모의 저장 위치가 없을 때 공통 가족 집 spawn을 쓰면서 두 부모가 이안 방에 함께 나타나던 문제도 수정했다. 부모별 실제 침실 출발점을 정의했고, 기존 주민15명의 기본 동작은 변경하지 않았다.

## 생활 및 데이터

- 새 게임은 0세/0G/부채1,000,000G로 부모님 집에서 시작한다. 보행1세·독립 외출7세 규칙을 유지한다.
- 19세 이후에도 자동 이사하지 않는다. 기존 집 관리의 확인 창과500G 이사 비용으로 독립한다. 가족 가구는 개인 소유 목록으로 복사하지 않는다.
- 첫 독립 집은 기존 나무 장판·흰 벽지·옥색 고정 욕실·침대1개를 유지한다. 기존 소지금·개인 구매 가구·관계·미션·직업을 초기화하지 않는다.
- 부모는 전용 침실에서 실제 문을 열고 주방으로 걸어간다. 냉장고의 불투명한 문 프레임, 재료 꺼내기, 조리 프레임, 식사 준비, 좌석 사용을 기존 NPC scheduler와 시간표에 연결했다. 두 부모가 서로 다른 시간에 요리·가사·외출을 분담한다.
- 수면은 침대 접근→눕기/수면 프레임→기존 시간·회복 적용→기상→내려오기 순서다. 영아는 성인 수면/보행 대신 아기 침대와 돌봄 프레임을 사용한다.
- 냉장고/수면/식사는 기존 행동 처리기가 효과와 저장을 담당한다. `WorldLifeAction`은 위치·연출·점유를 보조하며 별도 보상 시스템을 만들지 않는다. 취소하면 점유와 입력을 정리하고 미완료 효과를 지급하지 않는다.
- 공동 식탁은 단일 물리 오브젝트다. 실제 점유된 가족만 표시하고, 세 사람의 동작 타이밍을 분리한다. PNG가 균일한8행이 아니어서 생긴 인접 프레임 조각은 알파 경계 측정에 따른 Sprite import 영역과 상체 애니메이션 영역으로 제거했다. 원본 PNG 픽셀을 코드로 덧그리지 않았다.
- 가족 자원과 관계는 기존 LifeWorldSnapshot의 선택 블록에 저장한다. 미성년 레거시 저장은 가족 거주로 이동하되 개인 자산을 보존한다. 기존 성인 개인 집은 독립 상태로 유지한다.

## 실제 Game View

| 요청 화면 | 실제 Unity 캡처 |
|---|---|
| 전체 평면 / 1280×720 | [전체 집](FamilyLife/Screenshots/Architecture-Overview-1280.png) |
| 부모님 침실 | [침실](FamilyLife/Screenshots/Room-parents.png) |
| 이안의 방 | [이안 방](FamilyLife/Screenshots/Room-ian.png) · [0세 아기 침대](FamilyLife/Screenshots/Birth-FamilyHome.png) |
| 거실 | [거실](FamilyLife/Screenshots/Room-living.png) |
| 주방·단일 가족 식탁 | [주방](FamilyLife/Screenshots/Room-kitchen.png) |
| 독립 화장실 | [욕실](FamilyLife/Screenshots/Room-bath.png) |
| 부모님의 실제 조리 | [엄마](FamilyLife/Screenshots/Architecture-Mother-Cooking.png) · [아빠](FamilyLife/Screenshots/Architecture-Father-Cooking.png) |
| 가족 공동 식사 | [가족 식사](FamilyLife/Screenshots/Architecture-Family-Meal.png) |
| 첫 독립 집 | [침대 하나](FamilyLife/Screenshots/Independent-BedOnly.png) |

움직임은 스크린샷만으로 판정하지 않았다. 실제 Rigidbody 이동/벽 차단, NPC 좌표 변화와 문 개방 이벤트, 좌석 점유, 애니메이션 프레임 변화와 식사 자원 차감을 실행 검사했다. 키보드 물리 입력을 주입한 테스트와 구분하며, 컨트롤러 호출을 WASD 실기기 검증으로 표시하지 않는다.

## 수정 파일

- 씬/데이터: `Assets/_Game/Scenes/LifeWorld.unity`, `Data/Phase26/LifeWorldRules.asset`, `Data/Phase26/FamilyHomeArchitecture.asset`.
- 생성 원인 수정: `Editor/FamilyLifeSetup.cs`, `Editor/FamilyArchitectureSetup.cs`, `FamilyHomeArchitecturePlan.cs`.
- 기존 행동/이동 확장: `LifeWorldController.cs`, `LifeWorldService.cs`, `WorldNPCScheduler.cs`, `WorldLifeAction.cs`.
- 가족/식탁: `FamilyLife.cs`, `FamilyHomeRuntime.cs`, `FamilySharedDiningPresentation.cs`.
- 연령·주거 UI/거래: `GrowthAccess.cs`, `WorldHUD.cs`, `HomeManagementPresenter.cs`, `EconomyHousing.cs`, `EconomyProperty.cs`.
- QA: `Development/Family*Verification.cs`, 기존 Editor bridge, `FamilyQAScenarios.cs`, `DebugCommandService.cs`, `TestScenarioRegistry.cs`. 일반 나이 조작은 이사를 일으키지 않는다. 기존 경제 검사의 성인 fixture만 명시적으로 독립 상태를 준비하며, 생애주기 preset은 가족 거주를 유지한다.
- 아트: `Art/FamilyLife/{IanLife,Appliances,EmptyAppliances,SharedDining}.png`. 내장 image_gen 원본 제작, 기존 이안 원본 보존. [프롬프트](FamilyLife/ART_PROMPTS.json) · [추가 프롬프트](FamilyLife/ART_PROMPTS_SUPPLEMENT.json).

## 검증/보존과 남은 항목

최종 실제 실행별 결과는 [RESULTS.md](FamilyLife/RESULTS.md), 저장 보존과 Editor 종료 상태는 같은 폴더의 JSON에 기록한다. 실패했던 검사도 삭제하지 않았다.

실제 Android/iOS/Fold 기기·저사양 FPS/발열은 **미검증**이다. Editor Safe Area/화면 변경/공통 터치 입력 검사와 구분한다. 이번 수정 후 네이티브 모바일 빌드를 새로 수행하지 않았다.

이번 긴급 복구의 완료 범위는 실제 방 구조·주요 가구·출입·가족 조리/공동 식사와 기존 시스템 회귀다. 원래 Family Life 전체 지시의 모든 세부 행동이 완성된 것은 아니다. 부모의 돌봄 요청을 즉시 동선에 끼워 넣는 기능, 침대 정리/가사/TV/서류 등 모든 동작의 전용 방향 아트, 세부 이유식 연령별 모션, 오프라인에서 지나간 모든 조리 단계를 연속 재구성하는 처리는 추가 작업/검수가 필요하다. 원본에 없는 세부 연령을 임의 확정하지 않았다. 상업용 최종 미술/자연스러움 평가는 사용자 육안 확인 대상으로 남긴다.

사용자의1세·100G 생산 저장을 백업하고 검사 동안 SHA-256으로 비교한다. 원격 저장소는 설정돼 있으나 이번 변경을 push하지 않으므로 로컬 체크포인트를 장비 손실 대비 원격 백업으로 보고하지 않는다.
