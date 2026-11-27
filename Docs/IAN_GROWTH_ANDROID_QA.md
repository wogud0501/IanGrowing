# Android 실기기 성장 검증

**PASS.** Samsung SM-F976N, Android 17, 1080 × 2520, 480dpi에서 USB ADB로 검사했다. Unity MCP를 통해 실제 프로젝트에 20개 라이브러리를 임포트하고 Android IL2CPP Development APK를 생성했다. 빌드 결과는 오류 0개였다. 패키지는 `com.UnityTechnologies.com.unity.template.urpblank`이고 결과물은 `Builds/AndroidQA/IanGrowing.apk`다.

1. 기존 앱 위에 `adb install -r`로 설치했다. 자동화 중 ADB 연결이 한 번 끊겨 USB 기기를 다시 확인하고 설치했다. 수집 중의 일시적 연결 단절에서는 폰에서 계속 실행 중인 세션에 재연결해 같은 결과를 회수했다.
2. `persistentDataPath/IanGaitQA/start`에 `audit`가 있을 때만 개발 검증기가 시작된다. `qa-ian-gait.json` 격리 채널에서 성장 경계·가구·저장 검사를 실행했다. 기존 수동 저장과 자동 저장의 SHA-256은 전후 일치했다.
3. S01–S20을 실제 카메라로 촬영했다. 성장 시각 자료는 정지 원화 RGBA32, 동작 아틀라스 ASTC 4×4였다. 같은 나이의 프레임 높이, 피벗, PPU와 Collider가 Editor·macOS 기록과 일치했다. [단계별 비교 기록](Growth20QA/stage-results.tsv)
4. ADB 터치로 학교에서 정면·측면·후면·대각선 보행을 녹화했다. 전 단계 촬영 후 11세와 55세는 빈 교실 바닥에서 다시 이동시켜 실제 보행 샘플을 확보했다. 최종 입력 기록에는 보행이 가능한 19개 단계 모두에 Walk 상태가 있고, 0세 8방향 이동은 거절됐다. [학교 녹화](Growth20QA/android-school.mp4) · [보충 녹화](Growth20QA/android-school-followup.mp4)
5. 9세로 학교에서 저장한 뒤 앱 프로세스를 종료하고 다시 실행했다. 같은 S06 원화, 학교, XY 좌표와 0.8/0.8 배율이 복원됐다. 격리 세션의 앱 PID로 필터한 Logcat을 수집했다. 원시 로그는 로컬 `Library/Growth20/device-full-logcat.txt`에 두었다.

이 기기의 실제 검증 화면은 세로 1080 × 2520이다. 다른 종횡비, 다른 GPU, iOS, Windows 네이티브 실행은 별도 검증 대상이다.
