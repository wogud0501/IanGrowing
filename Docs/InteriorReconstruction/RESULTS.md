# PIXEL INTERIOR VISUAL RECONSTRUCTION

2026-11-14 · Unity 6000.6.8f1 arm64.

## 수정 내용

실제 Scene을 유지하고 가족 욕실의 공간·벽체·문·샤워·카메라를 함께 수정했다. 새 타일3개와 수납장/수건걸이/선반은6개의 개별 Sprite이며 각 GameObject에 배치된다. 콘셉트 전체를 배경으로 붙이지 않았다. 벽의 물리 경계는 후벽1.3/전면-1.3이며, 문턱은 전면 경계와 일치한다. 새 문설주는 기존 통로 안의 시각/충돌 간격을 맞춘다.

카메라는 가족 욕실 ortho2.5,720p 기준16픽셀 참조9배율이다. 방의 모양을 화면 폭에 맞춰 늘리지 않고 낮은 시점의 세로 바닥 길이를 줄였다. 실제 이동 가능한 통로, 기구 접근/퇴장점과 가족 NPC 목적지도 함께 수정했다. 다른13실은 가구 배치와 상태 데이터를 보존하고 공통 외곽/접지 표현을 적용했다. 개인 옥색 욕실은 카메라 여백만 추가했다.

문을 가리던 배경판은 템플릿의 과거 sorting899를 유지했지만 이동한 방의 문짝은630이어서 앞을 덮었다. `WorldDoorLeafVisual.Draw`가 현재 문짝 기준 -2에 배경판을 두도록 수정했다. WorldDoor 상태/충돌/출입·Scene 로딩 코드는 교체하지 않았다. 욕실의 크기 조정은 별도 스타일 에셋이므로 다른 문의 경첩/PPU를 변경하지 않는다.

샤워는 기존의 실제 진입→유리문→안쪽 배우→기존4프레임 모션→물→20분/위생 정산→퇴장을 재사용한다. 이번에는 유리의 세라믹 색 오염을 제거하고 안쪽 위치를 왼쪽으로0.22만큼 조정해 문살이 얼굴을 가리지 않게 했다. 이동/효과/취소 소유권은 기존 WorldLifeAction/LifeWorldController가 유지한다.

## 이미지와 근거

- [수정 전 실제 Game View](01_Current_Unity_Bathroom.png)
- [사용자 AI 아트 방향](02_AI_Bathroom_Art_Direction.png)
- [수정 후 실제 Game View](Screenshots/03_Unity_After.png)
- [샤워 진입](Screenshots/Shower-Entering-family-bathroom.png) / [샤워 중](Screenshots/Shower-Running-family-bathroom.png) / [퇴장](Screenshots/Shower-Exited-family-bathroom.png)
- [참고자료 확인 범위와 출처](REFERENCES.md)

새 아틀라스는 Point/무압축/mipmap off로 가져온다. 수납장의 유효 PPU는 약166, 기존 설비는160인 자료를 사용하되, 모든 원본 스프라이트의 PPU가 완전히 동일하거나 Unity Pixel Perfect 패키지를 새로 설치했다는 뜻은 아니다. 카메라 기준 정수 배율/위치 스냅을 유지하며 실제 그림의 선 굵기와 비율도 캡처로 비교한다.

## 검증 범위와 남은 사항

최종 결과 표는 실제 실행이 완료된 후 추가한다. 초기 작동 중 컴파일 이름 충돌과 Editor fake-null 컴포넌트 처리를 수정했으며 실패 상태를 PASS로 보고하지 않았다. 최초 반복 테스트 중 씬 참조가 사라진 경우는 중단 처리했다. 재현 시험은 게임을 잠시 Pause한 뒤 같은 실물 문 컴포넌트에30/60Hz dt를 주입하여 외부 입력/실시간 Update와 섞이지 않게 구성했다. 실제 플레이 출입은 별도30개 경로에서 실행한다. 이 결정적 검사와 기기 FPS 성능을 구분한다.

영아의 성인 샤워 차단을 유지하고 검사한다. 새로운 영유아 전용 목욕 아트나 부모가 아기를 안고 씻기는 연속 모션을 제작한 것은 아니다. 명확한 해금 나이가 없는 영유아 단계에 새 나이 제한을 임의로 추가하지 않았다. 해당 돌봄 연출은 별도 미완료 범위다.

실제 Android/iOS/폴더블 기기의 GPU 시간·메모리·발열, 물리 접힘 이벤트, iOS 네이티브 빌드, 모든 시설의 전체 행동 아트 완성도는 Editor 검사만으로 인증하지 않는다. 기존 각 방과 회사/학교/경제/가구/미션/가족 상태를 초기화하거나 삭제하지 않는다.

## 최종 Play Mode 결과

|시나리오|결과|통과 검사|
|---|---|---:|
|roomVisualArchitecture|PASS|20|
|roomVisualShower|PASS|29|
|roomVisualCheck|PASS|112|
|roomVisualDoors|PASS|158|
|roomVisualCycles|PASS|139|
|roomVisualMobile|PASS|54|
|sceneState|PASS|109|

총 621개 assertion 통과, 최종 UI 검사 오류0. 고유 기능 개수와 구분한다. 실제14실/1920p,28개 문과2개 거리 연결30경로,3종 문120회 결정적 개폐, 실제 샤워2개 및0세 차단을 검사했다. 회사5업무석 보존, 실제 QA 저장·불러오기와 가구/현금/부채/투자/미션/직업·관계 유지, 부모의 재료 꺼내기→조리→배식 및 부재 구역 중복 소비 방지도 재생했다.

모바일7차례 접힘/펼침/회전 크기 변경과 Safe Area/터치/백그라운드 복귀는 Editor 시뮬레이션으로 통과했다. 최초 held 값 검사가 간헐적으로 실패하여 실제 Input Action performed 이벤트 전달과 해제를 각각 검사하도록 테스트 관찰 방법을 바꿨다. 테스트 동안만 Input System의 focus reset을 격리하고 finally에서 원래 설정을 복원한다. 게임의 입력/기능 조건을 성공처럼 위조하지 않았다.

## 빌드와 미검증 구분

[Android BuildReport](ANDROID_BUILD.txt): ARM64 IL2CPP Development APK 생성 성공, 오류0/기존 경고1223,26개 Build Scene,43.6초. Editor는 StandaloneOSX로 복구했다. APK 경로·정확한 크기·해시는 ANDROID_APK.json에 기록했다. 이후 변경은 UNITY_EDITOR 전용 모바일 검증 관찰 코드이며 런타임 빌드 내용은 동일하다. 실제 APK 기기 설치/실행은 미검증이다.

iOS는 이번에도 CommandLineTools만 선택되어 있으며 iphoneos SDK 조회가 실패했다. iOS 네이티브 빌드는 BLOCKED, 실제 모바일·폴더블 기기 성능/발열은 미검증이다.

영유아 전용 보호자 목욕 연속 모션, 모든 가구의 모든 성장 단계별 정식 애니메이션 제작은 미완료로 유지한다. 이번 결과는 해당 아트 제작까지 완료했다는 뜻이 아니다. 실제 각 시설 전체 아트의 상업적 완성도는 자동 PASS 대상이 아니며 캡처와 직접 조작으로 계속 평가한다.

## 최종 저장 상태

운영3개 세이브는 시작 해시와 모두 일치했다(SAVE_FINAL_HASHES.json). 실제 운영 슬롯 쓰기 차단을 검사한 뒤, 마지막 Editor 명령으로 자동화 보호를 해제했다(EDITOR_RELEASED.json). 다음 수동 Play는 정상 저장할 수 있다. Editor는 macOS target, Play 중지/컴파일 완료, 이안 방 dirty=False로 남겼다(EDITOR_SAVED.json). 실제14실과 신규 스타일·Sprite/Material 에셋은 디스크에 저장했다.

로컬 복구 백업은 DevelopmentBackups/InteriorReconstruction-20261114이며 세이브 포함으로 Git 제외했다. 시작부터 존재하던 LIVE_KEY_TRACE.json 사용자 편집은 바이트 단위로 복원해 보존하고 커밋에서 제외한다. 기존 캡처/보고서는 덮어쓰지 않고 이번 근거를 이 폴더에 별도 보관한다. 기존 DoorTemplate/SceneResidents 프리팹 자체는 수정하지 않았고 씬 인스턴스의 스타일·Anchor·새 환경 오브젝트를 저장했다.

무관한 `.DS_Store` 변경과 기존 사용자 `LIVE_KEY_TRACE.json` 편집은 체크포인트에서 제외했다.
