# Phase 2.8 — 구현 및 검증 결과

2026-11-16 · Unity 6000.6.8f1 · 실제 프로젝트 `/Users/hyunwol/IanGrowing`

판정: **도시 UX 핵심 구현 및 macOS Editor 회귀 검사 통과. 실제 모바일 기기 및 출시 품질 검증은 미완료.** 기존 Phase 1~2.7 전체 재감사나 60개 미션의 Android 실행 완료를 의미하지 않습니다.

## 실제 수정

### NPC P0
기존 `WorldNPCScheduler.Move`의 `index > i` 조건은 일부 NPC만 충돌을 피하게 했고, 도시 휴식 목적지와 로드된 위치에는 독립 점유가 없었습니다. 실제 Play Mode에서 저녁 최소 간격 .3094, 밤 .00017로 재현했습니다.

- 기존 이동기 하나를 유지하면서 .48 월드 단위 개인 공간과 공간 해시를 추가했습니다. 이동할 때 전체 주민을 다시 순회하지 않습니다.
- 도시 스폰/로드 복원과 개인 목적지에 점유 검사를 추가했습니다. 회사와 가족 가구의 기존 SeatAnchor는 재배치하지 않았습니다.
- 막힌 NPC는 대기 → 1.1초 이상 간격의 점유 반영 경로 재탐색 → 유지되는 우회 지점 순으로 처리합니다. 매 프레임 Random, 위치 밀어내기, ID 문자열 우선권을 사용하지 않습니다.
- 기존 정적 경로 캐시/신호등/차량을 재사용하고, 횡단보도에 방향별 경로 차선을 적용했습니다.
- 초기 수정에서는 겹침을 막았지만 야간 병목이 남았습니다. 동적 점유 경로 재탐색을 적용한 뒤 아래 결과를 확인했습니다.

| 실제 시각 표본 | 관찰 시간 | 최대 → 종료 인원 | 겹침 frame | 빠른 왕복 반전 | 최소 간격 |
|---|---:|---:|---:|---:|---:|
| 08:00 | 8초 | 1 → 1 | 0 | 0 | 단독 |
| 12:00 | 8초 | 2 → 1 | 0 | 0 | 9.2509 |
| 18:00 | 8초 | 11 → 11 | 0 | 0 | .48005 |
| 23:40 | 60초 | 11 → 4 | 0 | 0 | .48001 |

빠른 반전은 같은 NPC의 반대 방향 전환 간격이 .5초 미만인 경우입니다. 일반 경로 회전과 대기/출발 횟수는 원본 JSON에 별도 보존했습니다. 시험은 QA 시간 변경으로 혼잡을 재현했으며 정상 속도로 하루 전체를 연속 관찰한 결과가 아닙니다. 밤에 모든 NPC가 귀가했다고 판정하지 않습니다. 기존 17명 데이터와 각자의 시간표를 유지합니다.

### 지도·생활 UX
- 도시의 실제 출입구 6곳: 주거지 진입, 회사, 학교, 상점, 증권사, 부동산. 없는 카페/병원/은행을 새로 만들지 않았습니다.
- 실외 미니맵, 플레이어 위치/방향, 미발견 시설 표시, 실제 시설 좌표와 연결한 전체 지도.
- Hover/키보드 focus/선택 시 시설별 가능한 행동과 기존 연령 제한 설명. 목적지 선택은 다음 문을 안내하며 순간이동이나 퀘스트 필수 조건을 만들지 않습니다.
- 최초 실제 방문 시 장소 발견 및 안내 이력 저장, 재방문 시 간결한 상호작용 안내.
- 15개 기본 도전과제. 실제 이동·식사·학교 행동·급여·매수·가구 구매/배치·유료 이사·임대/소유 주택 입주 등의 기존 성공 경로를 연결했습니다. 자체 금전/아이템 보상은 없습니다.
- 연령·허기/갈증·피로·현금·발견 상태에 따른 선택적 추천. 가이드 화면에서 숨길 수 있습니다.
- 보행 1세 / 독립 외출 7세 / 기존 성인·독립 조건 유지. 외형 단계로 행동 권한을 새로 해금하지 않습니다.

### 기존 구조 보호
16개 기존 방 Scene에 `CityLifeHUD` 참조만 추가했습니다. 회사 공간, 문 코어, Scene ID, 전역 Manager와 지갑을 새로 만들지 않았습니다. 신규 Prefab은 없습니다. 기존 UIShell/모바일 HUD/Safe Area/픽셀 패널/한글 TMP/아이콘을 재사용했습니다. 원본 PNG를 바꾸지 않고 기존 아이콘의 투명 여백을 별도 Sprite 메타데이터로 잘랐습니다.

`SaveData` version4의 world에 선택적 `cityLife`를 추가했습니다. 발견 장소·진행률·완료 도전과제·영수증·Guide Seen·목적지·추천 숨김을 기존 Capture/Restore/Durable 경계로 저장합니다. 없는 블록은 지연 초기화하며 기존 슬롯을 삭제하지 않습니다.

사용자가 작업 전에 변경한 `SceneScopedResidents.cs`의 스폰 거리 `.42`는 별도 백업하고 보존했습니다. [변경 경로](CHANGED_FILES.txt)와 [초기 구조 분석](ARCHITECTURE_AUDIT.md)을 참고하세요.

## 실제 실행 근거

| 검사 | 결과 | 근거 |
|---|---|---|
| 도시 UX 실행 | PASS · 40개 검사, UI overflow 0 | [UX 결과](ROOMVISUALPHASE28UX_RESULTS.json) |
| NPC 시간대/혼잡/귀가 | PASS · 10개 검사 | [표본](roomVisualPhase28After.json), [실행 결과](ROOMVISUALPHASE28AFTER_RESULTS.json) |
| 도시·부동산 QA Center 시나리오 6개 | PASS · 내부 46개 검사, 외부 래퍼 16개 검사 | [등록 시나리오 결과](QA_REGISTRY_RESULTS.json), [실행 결과](ROOMVISUALPHASE28REGISTRY_RESULTS.json) |
| 실제 문·양방향 Scene 이동/중복 입력 | PASS · 158개 검사 | [문 결과](ROOMVISUALDOORS_RESULTS.json) |
| 가구·디스크 복원·가족 요리·회사 보존 | PASS · 109개 검사 | [상태 결과](SCENESTATE_RESULTS.json) |
| 기존 회사 근무 | PASS · 10개 검사 | [회사 결과](FACILITYCOMPANYSHIFT_RESULTS.json) |
| Unity PlayMode Framework | PASS · 3개 테스트 | [Framework 결과](UNITY_FRAMEWORK.json) |
| 원본 플레이 세이브 3개 | PASS · 시작 SHA-256와 동일 | [해시 비교](SAVE_HASH_VERIFICATION.json) |
| Android 개발 APK | PASS · ARM64 / IL2CPP / 26개 등록 Scene, 오류 0 | [BuildReport](ANDROID_BUILD.txt) |
| iOS 네이티브 빌드 | BLOCKED · iPhoneOS SDK 없음 | xcrun --sdk iphoneos --show-sdk-path 실패 |
| 실기기 Android/iOS·폴더블 성능 | MANUAL · 미검증 | Editor 화면 시뮬레이션과 구분 |

1280×720, 1920×1080, 904×2316 모바일 시뮬레이션, 큰 글씨 설정에서 새 HUD/지도/가이드/도전과제의 텍스트 넘침을 검사했습니다. 실제 터치, 홈 버튼 Safe Area, 접힘/펼침 중 상태 보존, 모바일 앱 백그라운드 전환·메모리 성능은 미검증입니다.

처음 UX 검사에서 물음표 중복과 버튼 글자 넘침 21건을 발견해 수정했습니다. 첫 NPC 검사에는 일반 회전까지 떨림으로 세던 측정 오류가 있어 빠른 왕복 지표로 분리했습니다. 실패 기록은 [Failures](Failures)에 남겼습니다. 기존 QA Center F10 입력 주입 검사는 토글 단계에서 실패했습니다. 등록 시나리오는 실제 QA Open/격리 슬롯/실제 실행으로 별도 통과했으며, 물리 F10 키 검사는 MANUAL입니다.

컴파일 후 Unity AI `NoSubscription`, Discord RPC UERP `ThreadAbortException` 외부 Editor 오류가 관찰됐습니다. 게임 코드 오류와 분리한 [원본 기록](EDITOR_EXTERNAL_ERRORS.json)을 보존했습니다. Android 빌드는 성공했으나 경고 1,225건이 남습니다. MissionCondition 직렬화 깊이 경고와 IAP 설정 경고 등을 [경고 기록](ANDROID_WARNINGS.txt)에 남겼으며 출시 문제 해결 완료로 판정하지 않습니다.

## 실제 Game View

- [도시 HUD](Screenshots/City-HUD.png) / [전체 지도](Screenshots/Map.png) / [세로 화면](Screenshots/Map-904.png)
- [도전과제](Screenshots/Achievements.png) / [가이드](Screenshots/Guide.png) / [기존 메뉴 연결](Screenshots/Menu.png)
- [개선 전 저녁 혼잡](Screenshots/roomVisualPhase28Before-18.png) / [개선 후 저녁](Screenshots/roomVisualPhase28After-18.png) / [야간 귀가 표본](Screenshots/roomVisualPhase28After-23.png)
- [회사 보존](RegressionScreenshots/SceneArchitecture/Workspace-Preserved.png) / [가족 요리](RegressionScreenshots/SceneArchitecture/Kitchen-Cooking.png)

정지 화면으로 NPC 동작 PASS를 판정하지 않았습니다. 위 NPC 검사는 Play Mode 위치/이동 벡터를 연속 표본으로 관찰한 결과입니다.

## 저장 체크포인트

코드·Sprite 메타데이터·16개 Scene을 실제 파일로 저장했습니다. APK는 `Builds/SceneArchitecture/Ian-Rooms-Development.apk`입니다. 빌드 후 Editor 타깃은 StandaloneOSX로 복원했습니다. 자동 검증 보호를 종료하고 정상 수동 Play 저장 접근을 복원한 최종 상태는 `EDITOR_FINAL_STATE.json`에 별도로 남깁니다. 이전 QA의 기준 스크린샷과 보고서는 복원하고, 이번 실행의 새 근거는 이 폴더에 분리했습니다.

시작 HEAD는 711acd4이며 최종 Git 체크포인트는 진행 문서와 최종 응답에 기록합니다. 실제 기기 검증, 장시간 전체 인생 플레이, 모든 미션의 Android 검증과 빌드 경고 정리는 후속 항목입니다.
