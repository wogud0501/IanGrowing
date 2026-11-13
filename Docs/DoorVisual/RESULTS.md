# Door visual rebuild — 2026-11-13

## 원인 및 수정
- 실제 프로젝트: /Users/hyunwol/IanGrowing, Unity 6000.6.8f1 arm64. 현재 세션에는 Unity MCP 도구가 노출되지 않아 기존 Phase26EditorBridge로 Editor/Play 검사를 실행했다.
- 기존 문짝은 존재했으나 WorldDoor.Draw가 문 뒤 플레이어에 대해 문짝/문틀 알파를 .32로 낮췄다.
- DoorGeometry 에셋의 Fixed frame에는 네 테두리를 의도한 코드와 달리 사각형 전체(4 vertices, 6 indices)가 저장돼 있었다. 열린 문 뒤에 닫힌 문 이미지가 함께 그려지는 것을 확대 Game View로 확인했다. Sprite.OverrideGeometry만 재호출하는 접근 대신 명시적인 UV를 가진 네 테두리 메시로 분리했다.
- 기존 상호작용 highlight도 옛 문 이미지를 덧그렸다. 문 선택은 실제 문틀 색상으로 표시하며 다른 오브젝트의 highlight는 유지한다.
- WorldDoorLeafVisual은 기존 Doors.png의 닫힌 문 텍스처를 사용한다. 모델 공간의 두께 .095인 여섯 면을 경첩 기준으로 회전시킨 뒤 2.5D 화면에 투영한다. Transform Scale로 너비를 줄이지 않는다. 양면/상단/측면과 바닥 그림자가 있다. 유리문은 두께 있는 두 패널이 고정 폭으로 이동한다.
- 현재 여닫이 문은 설치 방향에 따른 한 방향 개폐다. oak는 왼쪽, jade는 오른쪽 경첩. 접근하는 플레이어 쪽에 따라 경첩을 바꾸거나 FlipX하지 않는다. 별도의 양방향 스윙 문이나 새 스프라이트 프레임 제작 완료로 주장하지 않는다.

## 기존 시스템 연결
- WorldDoor가 개폐 상태·Aperture·임대·충돌·출입 허용을 계속 소유한다. 새 표현 컴포넌트에는 Update, Coroutine, 입력, 이동, 저장, Scene 전환 로직이 없다.
- WorldDoor가 Draw를 호출할 때만 메시를 갱신한다. 닫힌 상태에서는 캐시를 재사용한다. 생성 메시/문틀 리소스는 인스턴스 종료 시 해제한다.
- DoorRoot/기존 frame/hinge Transform은 이동하지 않는다. 기존 16개 문에 연결된 WorldDoor를 통해 런타임에 표시된다. 별도 씬/문 프리팹/Animator를 교체하거나 경제·NPC 데이터를 다시 생성하지 않았다.
- 기존 프레임 배열은 호환 정보로 유지된다. 실제 새 시각 표현의 재생 여부는 메시 꼭짓점 변화로 별도 검사한다.

## 변경 파일
- Runtime: WorldDoor.cs, WorldDoorLeafVisual.cs, WorldInteractable.cs, WorldPlayerMotor.cs, LifeWorldController.cs.
- Development/Editor: DoorVisualVerification.cs, Phase26Verification.cs, CriticalRecoveryVerification.cs(실패 진단 및 벽 속 가상 접근 제외), Phase26EditorBridge.cs.
- 원본 Doors.png/TextureImporter/Material/문 스타일·Collider/포털 도착 좌표·SceneTransitionService 변경 없음. 런타임 메시를 적용하므로 이번 변경의 직렬화 씬·프리팹 수정은 없다.

## 검증 기록
최종 결과는 DOORVISUAL_RESULTS.json 참조. 초기 시도는 유리문 두 번째 그림자를 불투명 면 검사에 포함한 fixture 오류, 잘린 문틀의 런타임 Sprite 재현 실패를 검출했다. 앞의 실패는 숨기지 않고 로컬 백업과 이전 로그로 보존했다. 여닫이 문에 Sprite.OverrideGeometry를 재사용하는 접근을 폐기하고 명시적인 메시로 바꿨다.
회사 정문 통과가 한 번 실패했으나 뒤 재실행은 통과했다. 원인을 임의로 NPC 충돌로 단정하지 않았으며, 위치/문 상태/겹친 Collider 진단과 회사 포함 다섯 연결의 10회 실제 왕복 검사를 추가했다.
통합 검사에서 성인 상태로 영아 급여 차단 fixture를 호출한 오류는 fixture의 연령 초기화를 보완했다. 게임의 연령 정책은 변경하지 않았다.

## 화면 및 한계
- Screenshots/{front-door,bath-door,work-exit}-{0,1,2}.png: 실제 Game View 닫힘/반열림/완전 열림. 문 확인용 확대 카메라를 검사 중에만 사용했다.
- DoorMotion.gif: 실제 Update로 열고 닫는 Game View의 연속 프레임. 스크린샷을 Swift ImageIO로 GIF 인코딩. 합성하거나 문짝 이미지를 편집한 영상이 아니다.
- 검사 fixture의 19세 성장 알림이 일부 캡처에 보인다.
- 물리 키보드/실제 터치·모바일 기기 FPS·모든 NPC 동시 진입 조합은 미검증. 이 변경으로 모든 상업용 미술 품질을 최종 승인한 것은 아니다.

## 저장 보존
테스트는 기존 Phase26 격리 슬롯/쓰기 보호를 사용한다. 기존 파일을 과거 세션 백업으로 덮어쓰지 않았다. 이전 세션 SAVE_BASELINE과 현재 v4 파일은 다르며, 현재 v4 수정 시각은 06:50 UTC로 첫 QA 실행 06:56 UTC보다 이르다. 이번 작업 시작 시점의 파일 해시를 소급해 검증했다고 주장하지 않는다. 마지막 관측 해시와 종료 해시는 SAVE_BASELINE_OBSERVED.json / SAVE_PROTECTION.json에 기록한다. 현재 파일 복사본은 DevelopmentBackups/DoorVisual-20261113에 보존한다.

## 추가 입장 정렬 수정
- 씬 감사 ALIGNMENT_BEFORE.txt: office-front root/frame Y4.00, Plane Y3.55. home bath-door도 root Y2.40, Plane Y1.90으로 어긋났다. 기존 통과 로직은 물리 문턱을 쓰지만 비주얼은 건물 root를 사용했다.
- 진입 방향(normal.y>0)의 메시 표시 기준만 Plane에 맞췄다. 기존 root/frame/leaf Transform 및 Collider 위치는 그대로이며, 기존 퇴장 방향 비주얼은 이동하지 않는다. 앞뒤 접근에 따라 FlipX/경첩을 교체하지 않는다.
- 기존 SideOf/Approach/Passage는 로컬 좌표계에서 문턱의 앞뒤를 판단하고 실제 Rigidbody 발 위치를 중앙 접근점에 정렬한다. Scene 전환은 기존 State.Open/통과 완료 뒤에 실행하며 portal.arrival은 그대로 쓴다. 깊이 정렬은 기존 Plane/플레이어 발 Y 기준을 유지한다.
- StepTo가 물리 이동과 달리 unscaledDeltaTime으로 타임아웃을 소진했다. WorldPlayerMotor가 실제 Playing 상태의 FixedUpdate에서 누적한 TraversalSeconds로 변경해 프레임 지연/일시정지와 일관되게 처리한다. 경로가 막히면 기존 시뮬레이션 시간 제한으로 취소한다.
- 욕실의 ‘동일 방 문 뒤쪽’은 벽 내부였다. 실제 반대편 출입은 bathroom->home의 bath-exit으로 검증한다. 테스트는 벽 속 가상 출발점을 성공 검사로 집계하지 않는다.
- 집 출입은 기존 alley<->home 구간이다. City에서 주거 골목으로 이어지는 기존 경로를 새 직통 포털로 바꾸지 않았다.

## Final verification
- DOORVISUAL_RESULTS.json: 1,291 PASS, no failure or UI issues. Includes ten round trips for each of five connected entrance/exit pairs (100 passages), duplicate input, original arrival positions, restored controls, 180 mechanism cycles at 30/60 FPS-equivalent and .11-second dt, shared player/NPC leases, actual customer departures and isolated disk save. Controlled dt is not a hardware FPS benchmark.
- Entrance backing now fits the aperture bounds and accounts for its own Sprite pivot. This last visual adjustment preserves exit backing, physical collision and travel. DOORENTRYCAPTURE_RESULTS.json covers final close-up geometry and actual office entry/exit after this adjustment.
- No new Android/iOS build or physical device test for this change. Earlier build reports do not validate these new changes.

Final capture verification: 63 PASS / no failure, after backing fit. Artifacts: office-front-Motion.gif (actual entering), work-exit-Motion.gif (actual exiting), DoorMotion.gif (actual mechanism open/close).
Editor finish acknowledgement: LifeWorld / dirty=False / StandaloneOSX / not playing or compiling. Save hashes match the current task's observed baseline; no older production save restored. Initial modified keyboard trace restored from its exact baseline Git blob + preserved starting patch; new QA traces retained in local backup. Dynamic font cache and overwritten historical customer screenshots backed up and restored.
Starting repository checkpoint for this task: b694d20 (preceding recovery commit 5884933 remains in history). Final checkpoint: Git latest commit.
