# 시설 품질 표준화 — 2026-11-14

Unity 6000.6.8f1 / Apple Silicon / LifeWorld. Workspace를 재구성하지 않고, 실제 존재하는 학교·동네 상점·이안 증권·이안 부동산을 개선했다. 별도 식당·카페 씬은 없으므로 기존 상점의 식료품·식사·가구 전시 기능을 구역으로 구분했다. Phase 2.8 기능 추가는 없다.

## 변경 내용

- **학교:** 표시 이름을 `학교`로 통일하고 내부 `school` ID를 유지했다. 교탁·칠판, 학생 책상/의자 4석, 교구장·게시판·시계·창문을 배치했다. 교사는 학생 방향, 학생은 교사 방향으로 활동한다. 칠판 설명·질문·쉬는 시간·복귀·하교를 기존 시간표에 연결했다.
- **동네 상점:** 정면 계산대 직원, 상품 진열·냉장 음료, 조리·음료 설비, 식탁 4석, 가구 전시를 분리했다. 기존 구매·소유·교육·식사 서비스를 호출한다. 상점 식탁에서 먹는 빵은 실제 보유 수량을 차감한다.
- **증권·부동산:** 기존 정면 업무석과 거래 서비스를 유지하고 시장 화면·지역 매물 자료·서류 보관을 보완했다. 상담 고객은 직원과 마주 서서 상담하며, 휴식에는 실제 커피 테이블/의자를 예약한다. 상담 후 20분에 다음 방문, 28분에 도시로 복귀하는 일정을 명시해 쇼핑 시간차 때문에 상담이 수 시간 연장되는 버그를 수정했다.
- **좌석:** 예약과 실제 점유를 분리했다. 선택적으로 `ApproachPoint → SeatAnchor → StandUpPoint`를 사용한다. 접근·착석·활동·기립·퇴장 후 점유를 해제한다. 이동은 실제 속도 방향을 유지하며, 업무 위치에서만 해당 가구 방향으로 전환한다.
- **활동 이미지:** 새 교사·학생·점원·식사/음료 프레임을 원본 제작했다. 정렬된 활동 중에만 인물+가구 합성 프레임을 사용하고 이동/앉기/일어나기에는 기존 배우를 복원한다. 단순 정지 이미지 이동을 전체 행동 애니메이션 완료로 판정하지 않았다.
- **NPC:** 기존 12명과 관계 ID를 유지하고 교사·학생·점원 3명을 추가했다. 기존 생활관에 이들의 잠자리를 연결했다. 점심 방문은 4석 수용 인원에 맞춰 분산하고 야간 근무자 2명의 기존 저녁/야간 일정을 보존했다.

## 보호한 시스템

- Workspace 및 초기 집 편집 전후 구조 서명이 일치한다. [보존 근거](FacilityStandardization/PROTECTED_LAYOUT.txt). 기존 9개 정면 업무석(회사 5 / 금융 2 / 부동산 2)을 유지했다.
- 문/씬 전환 구현을 교체하지 않았다. 기존 E·터치·연령 검사·현금·직업·교육·가구 소유 서비스를 재사용한다.
- 보행 1세, 독립 외출 7세, 기존 성인 금융/취업 조건을 유지한다. 초기 집의 침대 1개 및 고정 욕실 규칙을 변경하지 않았다.
- 사용자가 직접 플레이한 **1세·100G 저장을 보존**한다. 작업 시작 시 0세 사본은 백업으로만 남겼으며 복원하지 않았다. 자동 검증은 별도 슬롯을 사용한다. 기준과 확인 경위: [SAVE_PROVENANCE.json](FacilityStandardization/SAVE_PROVENANCE.json).
- 자동 검증 보호 플래그가 Play/Edit 전환 중 풀려도 마지막 저장 경계의 `automationHold`가 생산 슬롯 쓰기를 차단한다. 작업 종료 후 일반 플레이 저장 권한은 명시적으로 복구한다.

## 주요 수정 파일

- 씬/데이터: `Assets/_Game/Scenes/LifeWorld.unity`, `Data/Phase26/LifeWorldRules.asset`, 기존 12명 일정의 편집용 `Data/FacilityStandardizationBaseline.json`.
- 좌석/행동: `WorldActivitySlot.cs`, `WorldNPCScheduler.cs`, `WorldFacilityActivityPresentation.cs`, 재사용 `Prefabs/Facilities/FacilityActivitySeat.prefab`.
- 기능 연결: `WorldHUD.cs`, `LifeWorldController.cs`. 학교 표시 기본값: `Editor/LifeWorldBuilder.cs`, `Editor/Phase26ArtSetup.cs`.
- 적용 도구: `Editor/FacilityStandardizationSetup.cs`. `Ian/Life World/Standardize Existing Facilities` 메뉴로 대상 시설만 적용한다.
- 검증: `Development/Facility*Verification.cs`, `FacilityQualityScenarios.cs`, 기존 Registry·회귀 검사·Editor bridge·SaveManager 보호 경계.
- 아트: `Art/FacilityStandardization/{Activities,EmptyActivities,Furniture,Dining}.png`. Point / mipmap off / 동일 셀 규칙. [제작 프롬프트](FacilityStandardization/ART_PROMPTS.md), [아트 목록](FacilityStandardization/ART_MANIFEST.json).

## 실제 실행 근거

최종 실행별 집계는 [RESULTS.md](FacilityStandardization/RESULTS.md)에 기록한다. 각 JSON의 실제 `passed`, `failure`, `uiIssues`가 판정 근거다. 서로 중복되는 초기화/assertion이 있으므로 실행별 수치를 고유 기능 수로 합산하지 않는다.

검증은 기존 Unity Editor 파일 명령 bridge를 사용했다. 이 세션에서 Unity MCP 호출 가능 도구는 노출되지 않아 MCP로 검증했다고 주장하지 않는다. 운영체제 시간과 실제 플레이 저장을 변경하지 않고 별도 QA 슬롯 및 기존 시간 서비스를 사용했다.

수정 중 확인한 문제와 재검증:

1. 새 좌석 도착 오차 0.03보다 기존 경로 종료 오차 0.09가 커서 도착 직전에 멈춤 → 새 앵커를 사용하는 마지막 구간만 0.015로 조정했다.
2. 학교/상점 바닥의 이전 Transform scale이 새 Sliced 크기에도 남음 → 대상 바닥 scale만 정상화했다.
3. 교육 검사 fixture에 실제 선행 미션 M010이 없음 → 테스트 준비 조건을 보완했다. 게임의 입학 규칙을 변경하지 않았다.
4. 점원 퇴장 검사 관찰자가 식탁 충돌로 출입 통로에 보정됨 → 통로 밖에서 해당 퇴장만 재검사했다. NPC의 정상적인 플레이어 대기를 문 오류로 처리하지 않았다.
5. 시간차 쇼핑 일정이 금융 상담 뒤의 원래 일정을 지연 → 명시적 상담 종료/후속 일정으로 수정 후 실제 도보 퇴장·다음 일정 복귀를 재검증했다.

최초 실패 JSON도 증거 폴더에 보존했다. 스크립트 존재나 미실행 항목을 PASS로 표시하지 않았다.

## Game View 비교

| 시설 | 수정 전 | 수정 후 |
|---|---|---|
| 학교 | [Before](FacilityStandardization/Screenshots/Before-school.png) | [After](FacilityStandardization/Screenshots/After-school.png) |
| 동네 상점 | [Before](FacilityStandardization/Screenshots/Before-shop.png) | [After](FacilityStandardization/Screenshots/After-shop.png) |
| 이안 증권 | [Before](FacilityStandardization/Screenshots/Before-finance.png) | [After](FacilityStandardization/Screenshots/After-finance.png) |
| 이안 부동산 | [Before](FacilityStandardization/Screenshots/Before-realestate.png) | [After](FacilityStandardization/Screenshots/After-realestate.png) |
| 보호한 회사 | [Before](FacilityStandardization/Screenshots/Before-workplace.png) | [After](FacilityStandardization/Screenshots/After-workplace.png) |

활동 중 [교실](FacilityStandardization/Screenshots/School-Class.png), [질문](FacilityStandardization/Screenshots/School-Question.png), [식사](FacilityStandardization/Screenshots/Shop-Lunch.png), [증권 휴식](FacilityStandardization/Screenshots/Lounge-finance.png), [부동산 휴식](FacilityStandardization/Screenshots/Lounge-realestate.png)도 저장했다. 움직임 판정은 정지 화면이 아닌 Play에서 실제 이동/방향/프레임 변화를 관찰했다.

## 남은 검수 및 범위

- 실제 Android/iOS 기기·폴더블·저사양 발열/FPS는 **미검증**이다. 이번 Editor 창 크기/가상 Safe Area/공통 입력 검사는 실제 기기 호환 인증이 아니다. 이번 수정 후 네이티브 모바일 빌드를 새로 수행하지 않았다.
- 모든 상호작용의 네 방향 전용 아트가 존재하지는 않는다. 정확한 작업 방향 클립이 없으면 잘못된 측면 작업 대신 해당 방향 Idle을 사용한다. 이 부분과 상담/정리 등 풍부한 제스처는 추가 아트 검수가 필요하다.
- 교사/학생/점원과 식사/음료의 4프레임 활동은 구현했지만 모든 NPC 행동을 완전한 개별 애니메이션으로 제작한 것은 아니다. 사용자의 최종 상업용 미술/동작 품질 확인이 필요하다.
- 기존 미구현 가챠·장비 등은 이번 시설 수정의 완료 항목으로 포함하지 않는다. 전체 QA의 NOT IMPLEMENTED/MANUAL 상태를 유지한다.
- 공식 Eastward·Fields of Mistria·Stardew 자료 페이지를 확인했다. 확보한 Eastward/Mistria 이미지는 주로 야외 자료여서 실내 전체를 비교 검증했다고 주장하지 않는다. 실내 직접 기준은 승인된 Workspace다. 상용 게임 이미지를 Assets에 복제하지 않았다.
- 로컬 Git 및 백업을 보존한다. 원격 저장소 설정은 있으나 이번 변경을 push하지 않으므로 장비 손실에 대비한 원격 백업 완료로 판정하지 않는다.
