# 월드 오브젝트 배치 감사

2026-11-26 · Unity 6000.6.8f1

일반 플레이의 장소 카탈로그에는 30개 방 ID, 18개 고유 Scene, 64개 방향별 이동 연결이 있다. 같은 주택 Scene을 공유하는 13개 주소는 입장 시 각자 지정된 평면과 거주자를 불러온다. 정적 조사에서는 프로젝트의 Scene 파일 30개를 각각 열어 활성 SpriteRenderer, 오브젝트 이름, 월드 좌표, 가장 가까운 상호작용 ID, 상위 고정 충돌체를 기록했다. 배우와 런타임 가구는 별도의 실제 게임 검사에 포함했다. [변경 전 목록](WorldReconstructionQA/world-objects-before.tsv.gz) · [변경 후 목록](WorldReconstructionQA/world-objects-after.tsv.gz)

| 실제 Scene | 변경 전 활성 스프라이트 | 변경 후 활성 스프라이트 |
|---|---:|---:|
| Battle | 10 | 10 |
| Bootstrap | 0 | 0 |
| City | 9 | 9 |
| City_Center | 76 | 77 |
| FamilyHome_Bathroom | 42 | 42 |
| FamilyHome_IanBedroom | 25 | 25 |
| FamilyHome_Kitchen | 25 | 25 |
| FamilyHome_LivingRoom | 45 | 45 |
| FamilyHome_ParentsBedroom | 25 | 25 |
| Home | 13 | 13 |
| HomeShowcase | 162 | 162 |
| IndependentHome_Bathroom | 22 | 22 |
| IndependentHome_Studio | 26 | 26 |
| LifeWorld | 324 | 324 |
| MainGame | 0 | 0 |
| MainGame2D | 2 | 2 |
| MainMenu | 0 | 0 |
| NeighborhoodShop | 36 | 36 |
| RealEstate_Office | 24 | 24 |
| Resident_Housing | 6 | 6 |
| ResidentialAlley | 36 | 38 |
| School | 13 | 13 |
| School_Classroom | 29 | 34 |
| Station_Alley | 14 | 11 |
| Station_City | 14 | 11 |
| StockMarket | 9 | 9 |
| StockMarket_Interior | 24 | 24 |
| Workplace | 6 | 6 |
| Workspace_Bathroom | 18 | 18 |
| Workspace_Office | 30 | 30 |
| 합계 | 1065 | 1067 |

## 장소별 조치

- 학교 교실에 보이던 가정용 소파를 서쪽 복도의 목재 대기 벤치로 교체했다. 교무 공간과 교실 사이에 충돌 벽과 통로를 만들었고 세면대 그림을 정수기로 바꿨다. [학교 검사](SCHOOL_ENVIRONMENT_REWORK.md)
- 주거 골목과 도심에 버스 승차 지점과 택시 승강장, 목적지 도착 위치를 연결했다. 양쪽 지하철역에서 옥색 욕실용 바닥·벽과 복제된 사무실 전실 표현을 걷어내고 정차 열차의 승차점과 대기 위치를 맞췄다. [교통 경로](TRANSPORT_INTERACTION_REWORK.md)
- 금융·부동산 상담 공간의 사무 수납, 상점 직원의 휴게용 테이블에 해당 용도의 스프라이트를 연결했다. 사무실 대기석처럼 사람이 사용하는 가구는 용도에 맞게 유지했다.
- 주민 주택은 동적 구성 결과까지 들어가 확인했다. 부모님 집과 독립 주택은 고유한 가구 규칙을 사용한다. [주택별 결과](NPC_HOME_INTERIOR_DESIGN.md)
- 이전 개발용 단일 `LifeWorld`에는 단체 침대 등의 레거시 배치가 남아 있다. 현재 새 게임과 이어하기는 장소 카탈로그 경로를 사용한다. 배경 전용 Scene과 레거시 Scene까지 포함한 모든 소품의 개별 사용·충돌 검사는 `NOT VERIFIED`다.

Scene 제작 진입점은 `WorldLayoutAuthoring.ApplyRealism`이다. 재생 중 실제 이동으로 확인한 문, 시설과 비용은 통합 결과에 기록했다. [통합 검증](ANDROID_PC_FINAL_QA.md)

## 이동을 막았던 가구

102호와 203호에서 침대 사이의 접근 지점이 너무 가까워 각자 침대가 예약된 상태에서도 동시에 잠들 수 없었다. 세 번째 침대를 놓은 두 평면의 침대 옆 통로를 넓히고 발을 디디는 위치를 앞뒤로 나눴다. 부모님 집에서는 청소 동선 첫 지점이 거실 탁자 안에 있었다. 청소 도구와 작업 지점을 탁자 남쪽으로 옮긴 뒤 아버지가 귀가해 움직이는 과정을 확인했다. [주택 검증](NPC_HOME_INTERIOR_DESIGN.md) · [가족 주택 귀가 영상](WorldReconstructionQA/android-father-front-door.mp4)
