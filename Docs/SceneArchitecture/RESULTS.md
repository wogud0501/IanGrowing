# Scene/주거 개편 검증 결과

실제 Unity6000.6.8f1 arm64 Editor Play와 Android Development 빌드 결과다. 각 숫자는 실행된 assertion 수이며 서로 중복되므로 합산하여 고유 테스트 수로 발표하지 않는다. 원본 씬/세이브를 사용한 정상 플레이 데이터로 QA하지 않았다.

| 실행 단위 | 최종 상태 | assertion | UI 검출 | 근거 |
|---|---|---:|---:|---|
|16개 실제 씬 격리·공유 상태|Completed|160|0|[SCENECHECK](./SCENECHECK_RESULTS.json)|
|30개 문 실제 왕복·중복 입력|Completed|222|0|[SCENEDOORS](./SCENEDOORS_RESULTS.json)|
|가구·디스크 복원·부모 조리·터치|Completed|109|5|[SCENESTATE](./SCENESTATE_RESULTS.json)|
|축소 평면·독립 노출·가족 문·생활|Completed|157|5|[SCENEHOUSING](./SCENEHOUSING_RESULTS.json)|
|QA Center 실제11개 선택 실행|Completed|19|0|[SCENEREGISTRY](./SCENEREGISTRY_RESULTS.json)|
|최종16씬 UI 바인딩·수면·새 게임|Completed|69|0|[SCENEFINAL](./SCENEFINAL_RESULTS.json)|
|중복 안내 제거 후 가족5방 실제 캡처|Completed|20|0|[SCENECAPTURE](./SCENECAPTURE_RESULTS.json)|

분리된 주방 공동 식탁 추가 실행은 SCENEMEAL_RESULTS.json의10 assertion을 통과했다. 부모2명과 이안의3인 마스크/서로 다른 좌석/실제 식사 프레임/음식1회 소비/허기 회복/조작 복귀를 확인했다. [실제 가족 식사](Screenshots/Final-Family-Meal.png) 참조.

초기 상태/축소 주거 실행에서 발견된 UI5건은 미바인딩 레거시 버튼 문구와 장문 집 설명이었다. 공통 UI/미션 바인딩을 복구하고 설명을 정리한 뒤 최종16씬 실행에서0건을 확인했다. 최종 캡처는 선택한 해상도의 실제 Game View다. UI 자동 경계 검사가 모든 미술 품질을 인증하는 것은 아니다.

QA Center 누적 결과는150개(PASS132 / FAIL0 / NOT IMPLEMENTED13 / MANUAL5)다. 이번 마지막 선택 실행은 scene4개, save, family2개, property/trade, stocks/up, growth/world-access, education/study의11개다. 나머지는 기존 실행 이력이며 이번에150개를 전부 재실행했다고 주장하지 않는다.

## 발견·수정한 결함

| ID | 문제/원인 | 수정과 재검증 |
|---|---|---|
| SC-01 | 도시 교통이 제거된 기존 NPC Renderer 참조를 읽음 | 주민 시뮬레이션은 현재 Scheduler 인물로 횡단 판정. 이후 실제 도시 로딩 정상 |
| SC-02 | 도착 씬의 닫힌 문 충돌체가 진입점을 밀어냄 | 도착 문 Open 확인 후 최종 위치/물리 입력 복구.30문 및 축소 가족 문 왕복 통과 |
| SC-03 | 방 분리 Export가 Presentation Bindings까지 삭제 | UIManager·MissionUI 연결 루트 유지/복구.16씬 실제 Bind 확인, 수면/미션 버튼 실행 통과 |
| SC-04 | 벽 하단 Pivot을 중앙으로 취급하여 틈/측벽 단차 발생 | Pivot에 따른 렌더 bounds와 Collider 기준을 일치.5방 벽-바닥 접합 검사 및 화면 확인 |
| SC-05 | 높은 alpha cutoff가 빈 가족 식탁을 거의 제거 | 새 방 전용 DiningMaterial만 보정. 원본 아트/기존 공통 shader 보존 |
| SC-06 | 가족 거주 중 개인 집 문/표시판이 노출 | 기존 residency+연령에서 파생한 ResidenceAccess로 문/두 표지판 통일.7세/18세/19세/유료 독립 실제 검사 |
| SC-07 | 새 씬에서 기존 설치 가구의 RoomId/installed 누락 가능 | 구버전 기본값 마이그레이션, 소유권·구매품 유지. 실제 전용 세이브 디스크 복원과 레거시 fixture 통과 |
| SC-08 | 씬을 떠난 중단 조리가 완료 키만 있고 영수증이 없을 수 있음 | 실제 가족 영수증 기준으로 오프스크린 정산/중복 방지. scene/receipts 실행 PASS |
| SC-09 | 도시에서 새 게임 시 방 ID 미변환/이전 NPC 캡처 가능 | ResetLife에 canonical nursery 매핑, 이전 공간의 캡처 중지,0세 돌봄 지점 정렬. 실제 도시→새 게임 통과 |
| SC-10 | SC-09 검증 중 닫을 수 없는 출생 팝업을 닫는 while 무한 반복 | 전환 코드 및 UIShell 반복에 진행 여부 검사 추가. Editor 강제 종료 후 저장된 프로젝트로 복구, 최종 전체 시나리오 재실행69 assertion 통과 |

SC-10의 중단 결과는 SCENEFINAL_HANG.json에 보존했다. 당시 source/code/scene은 디스크에 저장돼 있었고 전용 QA 세이브를 사용했다. Editor 재시작 전후 생산3파일 해시는 유지됐다. 초기 fixture의 미독립 집 진입 기대값과 DontDestroy 씬 개수 기대값도 실제 Unity 동작에 맞게 보정했다.

## 플랫폼

- Android: 실제 IL2CPP arm64 Development APK 생성. 결과/경고/최종 크기 및 해시는 ANDROID_BUILD.txt와 APK_MANIFEST.json을 확인한다. 빌드 성공과 실제 기기 동작/발열/FPS는 구분한다.
- iOS: 모듈은 설치돼 있으나 `xcrun --sdk iphoneos --show-sdk-path` 실패. Xcode/iPhoneOS SDK가 없어 네이티브 빌드·서명·기기 검증 BLOCKED. IOS_ENVIRONMENT.txt 참조.
- 모바일 Editor: 실 입력 Action Map으로 가상 조이스틱/OnScreenButton, Safe Area, 접기/펼치기 화면비7회, 앱 정지·재개 상태를 검사했다. 실제 Galaxy Fold/iPhone을 테스트한 결과가 아니다.

## 최종 실제 Game View

- [거실](Screenshots/Final-FamilyHome_LivingRoom.png)
- [주방](Screenshots/Final-FamilyHome_Kitchen.png) / [실제 조리](Screenshots/Kitchen-Cooking.png)
- [부모님 침실](Screenshots/Final-FamilyHome_ParentsBedroom.png)
- [이안 방](Screenshots/Final-FamilyHome_IanBedroom.png) / [0세 잠자리](Screenshots/Final-Newborn-Nursery.png)
- [가족 욕실](Screenshots/Final-FamilyHome_Bathroom.png)
- [개인 스튜디오](Screenshots/Final-IndependentHome_Studio.png) / [개인 옥색 욕실](Screenshots/Final-IndependentHome_Bathroom.png)
- [회사](Screenshots/Final-Workspace_Office.png) / [학교](Screenshots/Final-School_Classroom.png)
- [증권사](Screenshots/Final-StockMarket_Interior.png) / [부동산](Screenshots/Final-RealEstate_Office.png)
- [도시에서 새 게임 후 출생 안내](Screenshots/Final-NewGame-From-City.png) / [1920×1080](Screenshots/Final-IanRoom-1920.png)

개편 전 단일 공간은 기존 Docs/FamilyLife/Screenshots/Architecture-Overview-1280.png에 남아 있다. 중간 분리안은 prefix 없는 FamilyHome_*.png, 최종 축소/정렬 화면은 Final-*다. 정지 화면만으로 이동·애니메이션을 PASS 처리하지 않았으며 실제 조리/수면/문/입력 시나리오를 실행했다.

## 남은 범위

로딩 도중 강제 I/O 실패 주입과 물리 기기 앱 종료 복구는 미검증이다. 실제 기기의 로딩/메모리/FPS/발열, 전체 생애 단계 전용 동작 아트, 돌봄 부모의 임의 호출 동선과 보호자 동반 외출, 모든 오프라인 조리 이력의 정확한 재생은 완료 인증하지 않았다. 현재 NPC는 시간표·현재 행동·영수증을 복원하며 먼 방은 데이터로 처리한다. 모든 생활 제스처가 독립적인 정식 애니메이션으로 완성됐다는 뜻은 아니다. 기존 미구현 가챠/확장 장비 등은 새 구조가 대신 구현하지 않는다.

## 변경 파일

실제 Scene16개와 NPC/문/활동 라벨 프리팹은 Assets/_Game/Scenes/Rooms와 Prefabs/SceneResidents에 저장했다. 주요 코드는 RoomSceneCatalog/Identity/EntryPoint, RoomSceneTransitions, SceneScopedResidents/RoomResidentState, LifeWorldService/Controller, 기존 Scheduler/교통, ResidenceAccess/ResidentialEntrancePresentation, Housing/EconomyHousing, UIShell, 기존 QA 및 새 RoomQAScenarios/Editor export다. 정확한 목록은 FILES_CHANGED.txt를 따른다. 원본 LifeWorld와 원본 Rules는 변경하지 않았다.

최종 APK 실제 파일 크기: 168,458,108 bytes (160.7MiB), ARM64 IL2CPP. BuildReport 전체 크기는 C++ 백업 등도 포함하므로 APK 설치 파일 크기와 구분한다. 빌드 오류0, 경고1223(기존 API/패키지 경고 정리 미수행). 원본3파일 및 원본 씬/규칙의 최종 해시는 SAVE_FINAL_HASHES.json.
