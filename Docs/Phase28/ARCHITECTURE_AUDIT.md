# Phase 2.8 시작 분석 — 2026-11-16

실제 Unity MCP project/info로 /Users/hyunwol/IanGrowing, Unity 6000.6.8f1 확인. 시작 Scene FamilyHome_IanBedroom은 dirty=false, Play 정지. 시작 HEAD 711acd4. 사용자 변경 SceneScopedResidents.cs의 스폰 거리 0.42를 별도 백업하고 보존한다.

| 범위 | 기존 소유자 / 재사용 지점 |
|---|---|
| Phase 1~2.7 상태 | GameContext/GameManager, Docs/PHASE2_7_PROGRESS.md 및 기존 QA registry |
| 성장/연령 | LifeProgression, GrowthAccess, GrowthVisualCatalog. 보행1/외출7/성인19 유지 |
| 이동/충돌 | WorldPlayerMotor Rigidbody2D, WorldNPCScheduler 자체 Transform 이동. NPC에 별도 Rigidbody 이동기를 추가하지 않음 |
| 경로/점유 | WorldNPCNavigation 캐시된 .5 그리드, WorldActivitySlot 좌석 예약 |
| NPC 생성/시간 | SceneScopedResidents/RoomResidentState, 17명 전역 주민 + 게임 시간별 기존 일정 |
| 교통 | WorldStreetLife 횡단보도·차량 Rigidbody2D. residentSimulation으로 중복 보행자 제어 방지 |
| 장소/씬 | RoomSceneCatalog 16개 실제 Scene, SceneTransitionService/RoomSceneTransitions, 기존 문 코어 |
| 실제 도시 시설 | 주거지 진입·상점·회사·학교·증권사·부동산. 없는 카페/병원 씬을 새로 만들지 않음 |
| 경제/사회/교육 | EconomySystem/Phase23Services/MissionService의 실제 성공 이벤트, 지갑·보상 재구현 없음 |
| UI | 기존 WorldHUD/WorldMobileHUD/UIShell/PolishedNavigation, 픽셀 패널·아이콘·한글 TMP 재사용 |
| 저장 | SaveData.world의 선택적 추가 블록. 기존 version4와 기존 Capture/Restore/Durable 경계를 재사용 |
| 테스트 | Developer QA Center, 기존 격리 Phase26Verification + 신규 Phase28 시나리오. 원본 세이브 해시 비교 |

## 실제 P0 재현
8/12/18/23시 각각 8초 actual Play Mode 관찰. 저녁 overlap frame 3606, minGap .3094; 밤 overlap frame 8981, minGap .00017. 저녁 이동/정지 전환 891회. 수치는 기능 수가 아닌 관찰 frame/전환 횟수다.

원인: Move의 index>i 조건이 낮은 인덱스 NPC의 겹침 진입을 허용함. 같은 휴식/도시 목적 좌표를 여러 일정이 공유함. loading=true 스폰 및 저장 위치 재적용은 점유 검사를 생략함. 기존 시간표·길찾기는 재사용하고 해당 경로를 수정한다.

수정 방향: 로드 시 안전한 스폰, 도시 목적지 개인 점유, 공간 해시 이웃 검사, 최소 대기·고정 우회 지점·재시도 쿨다운. 매 프레임 Random이나 다른 NPC를 밀어내는 보정은 사용하지 않음.
