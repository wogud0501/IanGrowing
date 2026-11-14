# ROOM VISUAL & CAMERA QUALITY RECOVERY

2026-11-14 · Unity 6000.6.8f1 arm64 · 기준 `71920d4`.

## 확인한 원인

|대상|실제 원인|수정|
|---|---|---|
|가족 욕실이 작게 떠 보임|바닥5.2×4.6에 고정 ortho4.6, 높은2.3 단위 후벽이 반복 타일링됨|바닥5.2×3.8, 후벽1.0, 기준720p ortho3.214286|
|실내 외곽 어두운 빈 화면|카메라 clear color만 표시|방 바닥과 충돌 크기는 유지하고 최하위 레이어에 저대비 주택 벽체 재질|
|문이 검은 입구처럼 보임|기존 apertureShadow에 거의 검은 틴트|밝은 안쪽 면·바닥 문턱 적용, 기존 solid leaf/경첩/WorldDoor/장소 전환 유지|
|샤워가 부스 앞에서 실행됨|WorldLifeAction이 AlignOnly이고 내부 이동/유리문 상태가 없음|접근·내부·퇴장 Anchor, 별도 가전 유리문, 실제 actor 이동·Shower 스프라이트·물 흐름|
|독립 욕실 바닥/벽 누락|이전 분리 씬에 disabled 렌더러와 약0.064배의 임시 바닥 Scale이 남음|실제 옥색 바닥과 벽 렌더러 복구|
|재적용 시 문턱 중복|Transform.Find가 이름의 `/`를 경로로 해석|직접 자식의 정확한 이름으로 탐색·기존 중복 제거|
|행동 진행 안내 가독성|밝은 바닥 위에 흰 글자만 표시|기존 픽셀 패널과 어두운 한글을 공통 HUD에 적용|

## 구현 범위

실제14개 실내 씬에 공통 `IndoorCameraSettings`를 저장했다. 작은 욕실과 큰 사무실에 서로 다른 정수 기준 배율을 사용하며, 좁은 화면에서는 실제 플레이어를 옮기지 않고 카메라만 추적한다. 카메라 위치도 렌더링 픽셀 간격에 맞춘다. 이것은 모든 원본 스프라이트의 PPU를 통일하거나 Unity Pixel Perfect 패키지를 설치했다는 의미가 아니다. 기존 소스는 PPU160, 문131.4074 등 서로 다르며 원본 캐릭터는 변경하지 않았다.

새 `ShowerBoothVisual`은 출입문 스크립트와 독립된 가전 표현이다. 기존 샤워 아트에서 프레임·뒤쪽 설비를 사용하고 유리문/물 층을 분리한다. 캐릭터를 숨기지 않으며 실제 안쪽 위치에서 기존4프레임 Shower 모션을 재생한다. 위생+65와20분 경과는 기존 Durable 효과 경로가1회 처리한다. 취소는 효과 없이 점유·충돌·입력을 복원한다. 회사 화장실에는 원래 샤워 설비가 없으므로 샤워 성공 대상으로 추가하지 않았다.

Workspace의5개 업무석, 회사/학교/상점의 기존 NPC 일정과 가구 배치는 보존했다. 원본 LifeWorld 씬/원본 Rules/공식 캐릭터 아트는 수정하지 않았다. 생성 도구를 다시 실행해도 이번 프로필이 사라지지 않도록 `SceneRoomExport.Build`의 마지막 단계에서 같은 시각 설정을 적용한다.

## 검증

최종 결과는 별도 아래 표에 확정한다. 실행 전/코드 존재만으로 PASS를 부여하지 않는다. `Results/INITIAL_DOOR_HARNESS_FAILURE.json`에는 실제3종 반복 개폐 통과 후 문 없는 거리 연결을 문으로 가정해 중단된 초기 하네스 기록이 남아 있다. 이 중단은 최종30경로 검사와 구분한다. 반복 개폐는30/60Hz dt를 주입한 상태·좌표 일관성 검사이며, 물리 모바일 기기의 실제30/60FPS 성능 측정이 아니다.

## 실제 Unity 캡처

- [수정 전 가족 욕실](Bathroom-Before.png): 이전 체크포인트의 실제 Unity Game View 캡처를 재사용했다. 이번 세션에서 새로 촬영한 수정 전 이미지라고 주장하지 않는다.
- [수정 후 가족 욕실](Screenshots/Family-Bathroom-After.png)
- [실제 부스 내부 샤워](Screenshots/Shower-Running-family-bathroom.png)
- [1920×1080 욕실](Screenshots/Family-Bathroom-1920.png)
- [가족 거실](Screenshots/FamilyHome_LivingRoom.png), [주방](Screenshots/FamilyHome_Kitchen.png), [부모님 침실](Screenshots/FamilyHome_ParentsBedroom.png), [이안 방](Screenshots/FamilyHome_IanBedroom.png)
- [개인 집](Screenshots/IndependentHome_Studio.png), [개인 옥색 욕실](Screenshots/IndependentHome_Bathroom.png)
- [보존한 회사](Screenshots/Workspace_Office.png), [학교](Screenshots/School_Classroom.png), [상점](Screenshots/NeighborhoodShop.png), [증권사](Screenshots/StockMarket_Interior.png), [부동산](Screenshots/RealEstate_Office.png)

## 범위 및 미검증

실제 Android/iOS 기기, 실제 폴더블 힌지/접힘 이벤트, GPU 프레임 시간/메모리/발열, iOS 네이티브 빌드와 앱 강제 종료 복구는 이 Editor 검사로 인증하지 않는다. 화면 크기 변경은 Editor의 입력·레이아웃·상태 검사와 구분한다. 기존 각 연령의 모든 생활 애니메이션 아트를 새로 제작한 작업도 아니다. 침실·주방 창문 상단 잘림은 마지막4실 점검에서 축소·정렬했다. 기존 일부 NPC 모션과 각 시설의 소품 아트 자체를 전면 재제작한 것은 아니며 추가 시각 폴리싱 범위로 남긴다.

로컬 복구 백업 `DevelopmentBackups/RoomVisual-20261114`는 운영 세이브를 포함하므로 Git에서 제외한다. 운영3파일은 작업 전후 해시로 확인하며, 테스트 데이터는 phase26 전용 슬롯에만 쓴다. 최종 사용자 작업 상태인 기존 LIVE_KEY_TRACE 편집 내용은 작업 시작 시점으로 복구한다.

## 최종 Play Mode 실행 결과

|실행 시나리오|결과|통과 검사항목|UI 문제|
|---|---|---:|---:|
|roomVisualDoors|Completed|130|0|
|roomVisualShower|Completed|28|0|
|roomVisualCheck|Completed|98|0|
|roomVisualPolish|Completed|38|0|
|roomVisualMobile|Completed|54|0|
|sceneState|Completed|109|0|

표의 숫자는6개 실행의 assertion 수로, 고유 기능 수가 아니다. 같은 준비·경계 검사 일부가 포함된다. 문은30개의 실제 장소 연결, 샤워는 실제2개 부스, 실내는14개 씬을 확인했다. 마지막4실 폴리싱과 욕실 모바일 검사는 해당 변경/화면에 한정했다.

sceneState에서는 실제 QA 슬롯 저장 후 복원, 현금/부채/투자/미션/직업·관계·가구 보존, 부모의 냉장고 재료 소비→요리→배식, 멀리 있는 가족 행동의 중복 소비 방지, 회사5개 업무석을 실행 검사했다. roomVisualShower는 안쪽 actor 위치와 서로 다른 프레임, 유리문 개방/폐쇄, 물 효과 프레임,1200초 정산, 취소 무보상과 점유 해제까지 검사했다.

## 빌드와 최종 저장

Android ARM64 IL2CPP Development APK를 이번 변경으로 실제 생성했다. [BuildReport](ANDROID_BUILD.txt): Succeeded, 오류0, 경고1223,26개 Build Scene,2분3초. APK는 `Builds/SceneArchitecture/Ian-Rooms-Development.apk`,168,472,896 bytes다. 설치·기기 실행 완료를 의미하지 않는다. 경고는 기존 API/패키지 경고를 포함하며 이번 범위에서 전체 정리하지 않았다. Editor target은 StandaloneOSX로 복원했다.

iOS는 `xcode-select -p`가 CommandLineTools만 가리키고 `xcrun --sdk iphoneos --show-sdk-path`가 SDK 없음으로 실패했다. iOS 빌드는 BLOCKED이며 성공으로 표시하지 않는다.

원본 세이브3개는 작업 시작 해시와 모두 일치했다([최종 해시](SAVE_FINAL_HASHES.json)). 보호 상태에서 실제 운영 슬롯 쓰기 차단도 재확인했다. 검증 후 보호 플래그를 해제해 다음 수동 Play에서는 정상 저장된다([해제 근거](EDITOR_RELEASED.json)). Editor는 Play 중지/컴파일 완료, FamilyHome_IanBedroom 씬 dirty=False로 남겼다([저장 상태](EDITOR_SAVED.json)). 모든14실 변경은 실제 `.unity` 파일에 저장했다.

기존 프리팹 자체 수정은 없다. DoorTemplate 및 기존 SceneResidents를 재사용했고 새 컴포넌트/Anchor/비주얼 참조는 각 실내 Scene에 저장했다. 정확한 파일 목록은 FILES_CHANGED.txt를 따른다.

## 복구 체크포인트

작업 도중 외부에서 생성된 `e76de30` 커밋에 구현과 캡처가 저장된 것을 확인했고, 해당 커밋을 되돌리지 않았다. 이번 최종 검증·세이브 해제 근거는 별도 문서 체크포인트로 저장한다. `LIVE_KEY_TRACE.json`은 작업 시작 시 백업과 바이트가 일치함을 확인했다. Build가 자동 갱신한 URP/atlas/네이티브 디버그 메타데이터도 외부 커밋에 포함되어 있으며, 게임 로직 변경으로 분류하지 않는다.
