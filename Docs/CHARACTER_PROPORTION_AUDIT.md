# 이안 성장 스프라이트와 목선

공식 5×4 PNG의 SHA-256은 `9bd45a29f2090db99e1de519b70e750a76761f8308ceb33697e83b9ec916d18e`이다. 다운로드 원본, 보존본, S01~S20을 행 우선 순서로 자른 20개 파일을 RGBA 단위로 비교했다. 20개 모두 해당 원본 영역과 일치한다. [공식 원본](../Assets/_Game/Art/Characters/Ian/Reference/Ian_OfficialGrowth.png) · [단계별 크기와 픽셀 차이](WorldReconstructionQA/source-diff.tsv)

| 검사 | 결과 | 근거 |
|---|---|---|
| 전 단계 슬라이스와 지면점 | PASS | 앞 두 단계는 앉은 영아 지지점, 나머지는 발바닥 접점. 단계별 Rect와 피벗을 원본 좌표와 대조 |
| Editor·macOS·Android 정면 20단계 | PASS | 각 단계에서 실제 플레이어의 스프라이트, X/Y 배율, 바닥점, 직교 카메라 투영, 콜라이더를 기록하고 렌더 캡처 |
| 3~14세의 목선 | PASS | 정면 원본과 게임 화면을 비교해 S03~S08의 턱 아래와 옷깃 사이만 보정 |
| 모든 방향·생활 동작의 새 수작업 원화 | NOT VERIFIED | 성장 시트는 정면 외형 20점이며 명확한 노년 전용 원화도 추가 자료가 필요함 |

정면의 얇은 목선은 해당 여섯 장의 원본에도 있었다. 임포트는 128 PPU, Point, Full Rect, NPOT 원본 유지, mipmap 끔을 사용한다. 실기기 텍스처는 RGBA32였고 해당 시료에서 X/Y 투영과 Transform 배율은 일치했다. 이 증상에서 플랫폼별 비균등 확대가 원인이라는 증거는 발견되지 않았다. [Android 렌더 수치](WorldReconstructionQA/android-profiles.tsv.gz)

`Tools/Art/ian_growth.py`가 공식 파일을 보존하고 별도 표시용 그림 6장으로 옷깃을 정리한다. 프레임당 중앙 41×13 픽셀 범위 안에서 481~499개 RGBA 값이 바뀌었다. 얼굴, 헤어, 헤드폰과 전체 외곽의 키는 원본을 유지한다. 해당 단계의 정면 동작 시트와 `IanGaitImport`의 기본 표시 참조도 함께 갱신했다. 동일한 단계에서 학교만 다르게 그리는 보정은 사용하지 않는다. [표시용 그림](../Assets/_Game/Art/Characters/Ian/Growth/Presentation) · [기존 성장 단계와 방향별 애니메이션 검사](IAN_GROWTH_ANIMATION_QA.md)

각 칸에 보존 원본과 같은 단계의 실제 화면을 배치했다. [Android 비교](WorldReconstructionQA/android-official-render.jpg) · [Game View 비교](WorldReconstructionQA/editor-official-render.jpg) · [macOS 비교](WorldReconstructionQA/mac-official-render.jpg)

## 학교에서 움직이는 장면

Android에서 9세 S06으로 학교에 들어가 조이스틱으로 걷고 멈췄다. 601개의 위치·렌더 표본 중 102개가 보행 프레임이었다. 기록된 보행은 앞·뒤·오른쪽이었고 전 표본에서 X/Y 배율은 0.8000/0.8000이었다. 왼쪽·대각선을 포함한 방향별 검사는 별도의 성장 감사에서 수행했다. iOS 시뮬레이터에서도 정면 20단계를 다시 렌더했다. [실기기 보행 영상](WorldReconstructionQA/android-school-walk.mp4) · [프레임별 수치](WorldReconstructionQA/android-school-walk.tsv.gz) · [iOS 시뮬레이터 비교](WorldReconstructionQA/ios-official-render.jpg)
