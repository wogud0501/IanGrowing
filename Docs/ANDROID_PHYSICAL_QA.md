# Android 실기기 QA

Unity의 **Tools → IanGrowing → Android QA Dashboard**에서 기기를 새로 고르고 **Device info**로 대상과 접힘 상태를 확인한다. **Build & run**은 Development APK를 만들고 데이터를 유지한 채 설치한 뒤 로그 수집과 실행을 시작한다. 이미 빌드했으면 **Install update → Start Logcat → Launch**를 쓴다. 로그 보기에서 **Pause view**와 **Clear view**는 수집을 계속한다. **End session**은 수집을 종료하고 보고서를 쓴다.

## 환경과 연결

2026-11-25에 M5 Max 36GB Mac과 USB로 승인된 삼성 SM-F976N, Android 17에서 확인했다. Unity 6000.6.8f1, Unity MCP 10.3.0, Unity Android Logcat 1.5.0, ADB 37.0.1, scrcpy 5.0.1, NDK r27c, Temurin 17.0.18을 사용했다. SDK에 API 34, 36, 37.0 플랫폼이 설치돼 있다. 기존 프로젝트 패키지는 `com.UnityTechnologies.com.unity.template.urpblank`이다. 개발 APK의 표기 버전은 0.1.0, 버전 코드는 1이다. 빌드와 필터는 프로젝트의 실제 Android 식별자를 읽는다.

USB 디버깅을 켜고 휴대폰에서 해당 Mac의 디버깅 연결을 승인한다. CLI의 `devices`에서 현재 transport ID를 확인한다. 기기가 한 대면 자동 선택한다. 여러 대면 모든 작업에 `--device ID`를 붙인다. USB 재연결 후에는 ID가 달라질 수 있다. 선택적 무선 디버깅은 신뢰하는 사설망에서 휴대폰의 무선 디버깅 페어링 화면을 연 뒤 `qa.py pair 주소:페어링포트`에 코드를 대화식으로 입력하고 `qa.py connect 주소:연결포트`를 실행한다. 코드와 장치 일련번호를 보고서에 기록하지 않는다.

## 반복 실행

프로젝트 루트에서 실행한다. Python 3, Unity에 포함된 Android SDK, 녹화와 미러링에는 scrcpy가 필요하다.

```sh
python3 Tools/AndroidQA/qa.py devices
python3 Tools/AndroidQA/pipeline.py --build
python3 Tools/AndroidQA/qa.py watch --preset Live
```

파이프라인은 기존 로컬 Unity MCP의 프로젝트와 Android 타깃, 컴파일 상태, 기기와 기존 설치 버전, 현재 슬롯 파일의 존재 여부를 확인한다. APK를 설치한 뒤 게임을 실행해 포그라운드, 원본 PNG, 성능 데이터를 기록한다. `--build`를 생략하면 기존 APK를 재사용하고 APK가 없으면 빌드한다. 소스가 바뀐 경우 `--build`로 다시 만든다. `--regression`은 강제 종료와 재실행도 수행한다. 기본 MCP 포트는 8080이며 `--port`로 로컬 포트를 지정할 수 있다. Editor를 별도로 띄우는 배치 빌드는 사용하지 않는다.

빌드 결과는 `Builds/AndroidQA/IanGrowing.apk` 한 곳에 있다. 기존 RoomPlatformBuild와 씬 및 Development 옵션을 공유하고 Profiler 자동 연결을 추가했다. 변경분만 배포하려면 Dashboard의 **Patch via Build Profiles…**에서 Android Development Build, 실제 Run Device, **Patch And Run**을 선택한다. Unity가 선택한 기기로 직접 배포하는 경로다. [Unity의 Android 패치 절차](https://docs.unity3d.com/6000.0/Documentation/Manual/android-AppPatching.html)

```sh
python3 Tools/AndroidQA/qa.py start
python3 Tools/AndroidQA/qa.py mirror
python3 Tools/AndroidQA/qa.py screenshot
python3 Tools/AndroidQA/qa.py record 10
python3 Tools/AndroidQA/qa.py screen
python3 Tools/AndroidQA/qa.py tap .5 .6
python3 Tools/AndroidQA/qa.py swipe .12 .94 .18 .94 600
python3 Tools/AndroidQA/qa.py long-press .5 .5 500
python3 Tools/AndroidQA/qa.py key BACK
python3 Tools/AndroidQA/qa.py background-resume
python3 Tools/AndroidQA/qa.py performance
python3 Tools/AndroidQA/qa.py end
```

좌표는 현재 기본 디스플레이 기준 0–1이다. 입력과 화면 캡처는 게임이 전면에 있어야 한다. PNG는 원본 해상도이며 scrcpy 녹화는 최대 1600픽셀, 30FPS로 제한한다. 미러의 오디오와 클립보드 자동 동기화는 꺼져 있다. 테스트 중 개인 알림은 휴대폰에서 숨기는 편이 좋다. 종료된 세션은 기기를 분리한 후에도 `--session 세션명`으로 읽을 수 있다.

## Unity MCP와 로그

기존 서버에 testing 그룹의 통합 도구 `ian_android_qa` 하나를 등록했다. 예를 들어 `{"action":"tail","preset":"Quest","device":"2"}`로 최신 관련 로그만 받는다. ID는 해당 연결의 예시다. MCP 클라이언트에 직접 도구가 보이지 않으면 기존 `execute_custom_tool`에 같은 이름과 매개변수를 전달한다. `build`는 예약 후 반환하므로 `build-status`의 결과를 확인한다. 대화형 미러와 무선 페어링은 Dashboard 또는 CLI에서 사용한다.

| 프리셋 | 실제 입력 |
|---|---|
| Live / Debug | 현재 Android 사용자의 게임 패키지 UID. Live는 Unity, AndroidRuntime 등 주요 태그의 Info 이상, Debug는 해당 UID의 모든 등급 |
| Warning / Error | 각각 Warning 이상, Error 이상 |
| Crash | 앱의 치명적 신호·Java 예외, 소속이 확인된 시스템 ANR 및 tombstone. 별도 crash.log |
| Quest / Save / UI / Legal | Development Player의 미션 완료·보상 ID, 저장 시작·성공·실패, 창 열림, 필수 문서 동의 이벤트 |
| Network / Performance | 실제 예외·프레임 지연 메시지 패턴. 미발생 시 빈 결과. Performance 버튼은 별도로 PSS와 게임 SurfaceView의 화면 표시 간격을 측정 |

같은 UID를 공유하는 다른 패키지가 있으면 수집을 거부한다. 앱이 다시 실행돼도 UID 스트림은 유지하고 새 PID는 process.log에 기록한다. 별도 시스템 스트림은 지정 태그를 기기에서 선별하고 패키지 소속을 확인한 행만 저장한다. 종료된 프로세스의 PID만으로 네이티브 스택을 찾지 않는다. 제조사별 로그 접근 제한, 버퍼 유실, 패키지 표식이 없는 시스템 오류는 누락될 수 있다. `errors.log`의 행 수에는 스택의 각 행과 의도적으로 발생시킨 개발용 예외가 포함된다.

```sh
python3 Tools/AndroidQA/qa.py errors --limit 8
python3 Tools/AndroidQA/qa.py stack LocalDiagnosticsProbe
python3 Tools/AndroidQA/qa.py crash
python3 Tools/AndroidQA/qa.py search --preset Quest Claimed
python3 Tools/AndroidQA/qa.py export --preset Save --limit 100
python3 Tools/AndroidQA/qa.py diagnostics list
python3 Tools/AndroidQA/test_filters.py
```

`search`와 `export`에는 기기 기준 Unix 초 `--since`, `--until`을 줄 수 있다. `diagnostics 보고서ID`는 기존 게임의 정제된 리포트 한 건을 복사한다. 네이티브 메모리 덤프와 전체 저장 슬롯은 복사 대상에서 제외한다. 화면 표시 간격은 Unity CPU·GPU 처리시간과 구별한다. 두 처리시간은 Unity Profiler에서 확인한다.

## 업데이트 후 보상 회귀

1. QA 세이브에서 실제 행동으로 목표를 달성해 보상을 미수령으로 남긴다. 미션 ID, 진행도, 잔액 화면을 캡처한다.
2. `start` 후 `install` 또는 `pipeline.py`로 덮어 설치한다. 서명·다운그레이드 오류가 나면 설치 결과를 확인한다. 이 도구에는 제거와 데이터 초기화 동작이 없다.
3. 이어하기로 같은 세이브를 열고 미션 창의 수령 버튼을 누른다. 미션 ID가 바뀌는 것과 잔액을 확인한다. 다시 입력한 뒤 `qa.py reward-check M001`처럼 직전 업데이트 이후의 해당 ID 수령 횟수를 검사한다.
4. 재실행 후 다음 미션과 잔액을 확인한다. 다른 ID가 이미 완료 상태였다면 뒤이은 입력이 다음 미션을 수령할 수 있다. 수령 버튼 클릭 횟수와 미션 ID별 지급 횟수를 구별한다.
5. 근거가 있는 항목만 `qa.py mark "체크 이름" PASS "근거"`로 기록한다. QA 목록과 결과는 `qa.py report`에 있다. 이전 베타와의 호환성은 해당 베타 APK에서 만든 테스트 세이브로 같은 절차를 실행한다.

## 보관과 운영

로컬 `QAArtifacts/Android/UTC날짜-세션ID/`에 로그, 오류, 크래시, PID 및 화면 상태 변화, 기기·배포·성능 JSON, PNG, MP4, 보고서를 저장한다. Git에서 제외한다. 로그별로 4MiB에서 회전하고 이전 한 세대를 남긴다. 새 세션을 시작할 때 종료된 세션을 14일 및 총 1GiB 기준으로 정리한다. 열려 있는 세션에서 누적한 이미지와 영상은 종료할 때까지 이 한도를 넘을 수 있다.

이 기능은 승인된 내부 테스트 Mac에서 실행된다. 외부 전송 엔드포인트는 추가하지 않았다. 프로덕션 게임의 기기 내 보고서, 공유 선택, 보존기간과 이번 Mac QA 폴더는 별개다. 기존 진단 화면·약관·개인정보처리방침의 처리 범위는 동일하다. Unity나 Android를 업그레이드할 때는 패키지 버전, MCP 등록, UID 격리, SurfaceFlinger의 레이어 이름과 접힘 상태 출력, PNG와 입력 좌표를 실기기에서 재확인한다.

## 2026-11-25 실측

- 정상 프로필에서 미설치 상태로 시작해 APK 설치, 약관 열람과 동의, 새 게임, 수면·물 마시기를 확인했다. 덮어 설치 후 수령 대기 M001의 1/1과 0 G가 유지됐다. 보상은 한 번 지급됐고 재실행 후 M002와 100 G를 확인했다.
- 다른 덮어 설치에서는 M002 수령 후 이미 조건을 채운 M003도 수령했다. 각각 한 건씩 기록됐고 M004와 200 G가 표시됐다. 테스트한 설치 간 버전 코드는 모두 1이었다.
- 기기 접힘 1080×2520, 펼침 2256×2504, 반접힘, 다시 접힘을 같은 세션에서 기록했다. 펼친 게임 HUD를 원본 PNG로 확인했다. 조이스틱 스와이프로 이동하고 방에서 거실로 들어갔다. macOS에서 미러 창 하나의 표시, MP4 생성, Unity Profiler 실제 연결을 확인했다.
- 게임 SurfaceView에서 약 40 FPS, 표시 간격 중앙값 및 95백분위 25 ms를 관찰했다. 장면과 시점에 따라 PSS가 달라진다. 이 값은 장시간 발열이나 전체 게임 성능의 기준값이 아니다.
- 기존 개발용 관리 예외를 실기기에서 발생시켰다. 로컬 보고서와 Logcat 모두 LocalDiagnosticsProbe.ReadLength, LocalDiagnosticsProbe.cs 36행을 가리켰다. 테스트용 이메일과 인증 문자열은 저장한 로그 검색에서 검출되지 않았다.
- 첫 빌드의 삭제된 주거 씬 렌더러 참조를 제거한 후 재빌드에서 동일 오류가 사라졌다. 최종 APK 빌드 결과는 Succeeded였고 빌드 중 Profiler 연결 관련 소켓 오류 3건이 집계됐다. 이후 실행에서 Profiler 연결 기록을 확인했다. Android 시작 시 AssetPackManager 클래스 조회 오류도 재현됐다. 일반 APK 실행·이어하기는 계속 진행됐다.
- 실물 기기 한 대에서 검사했다. 과거 Discord 배포본에서의 마이그레이션, 무선 연결, 다중 실물 기기, 실제 네이티브 충돌·ANR, Patch And Run 배포, NPC 행동·가구 애니메이션 전수 및 장시간 성능은 미검증이다. 시스템 크래시 상관관계와 타 앱 제외는 합성 입력으로 별도 검사했다.

## 구현 파일과 증거

| 위치 | 변경 |
|---|---|
| Tools/AndroidQA/device.py, logs.py, qa.py | 기기 선택, 앱 전용 입력·화면·성능, Logcat 수집·프리셋, 세션 관리 및 보고서 |
| Tools/AndroidQA/pipeline.py, test_filters.py | 기존 MCP를 이용한 빌드·설치·스모크 경로, 필터와 프라이버시 회귀 검사 |
| Assets/_Game/Scripts/Editor/AndroidQA/AndroidQABuild.cs, AndroidQATool.cs, AndroidQADashboard.cs | 빌드 예약·상태 조회, 통합 MCP 도구, Editor 전용 창 및 Unity 메타데이터 |
| Assets/_Game/Scripts/Editor/Build/RoomPlatformBuild.cs | 기존 Android Development 빌드 진입점을 QA 창과 공유 |
| Assets/_Game/Scripts/Diagnostics/AndroidQAEvent.cs, LocalDiagnostics.cs | 기존 breadcrumbs를 Development Android Logcat에 연결 |
| Assets/_Game/Scripts/Mission/MissionService.cs, Release/LegalDocumentService.cs, UI/Core/UIShell.cs | 보상 ID, 필수 동의, 창 열림 기록 |
| Assets/_Game/Scenes/Rooms/Resident_Housing.unity | 이미 삭제된 cutaway 렌더러를 참조하던 plaster 직렬화 필드 정리 |
| .gitignore | 호스트 QA 산출물 및 Python 캐시 제외 |

기존 패키지 manifest/lock에 있는 Android Logcat 1.5.0과 Unity MCP를 사용한다. 내부 QA용 런타임 SDK를 추가로 설치하지 않았다. 실기기 전체 테스트 세션은 `QAArtifacts/Android/20261125T101657Z-3474c76d`, 최종 수집기 재시작 및 UI 필터 재확인은 `QAArtifacts/Android/20261125T104058Z-38d56ba8`에 있다. 첫 세션의 `report.md`, `screen.log`, `process.log`, `performance.json`에서 결과를 확인할 수 있다.
