# 성장 외형과 동작 검증

2026-11-26에 Unity 6000.6.8f1 Editor Game View, macOS 실행 빌드, Samsung SM-F976N Android 실행 빌드에서 같은 20단계 카탈로그를 검사했다.

| 검사 | Editor | macOS | Android | 관찰 방법 |
|---|---|---|---|---|
| 정면 원화와 나이 경계 | PASS | PASS | PASS | S01–S20 정면 출력, 19개 구간 경계 전후, 마지막 구간의 정수 최댓값 |
| 방향별 이동과 멈춤 | PASS | PASS | PASS | 단계마다 8개 방향으로 실제 이동 요청. 0세의 이동 거절 포함 |
| 방 이동과 저장 복원 | PASS | PASS | PASS | 성장 직전·직후 저장 및 복원, 30개 방 ID 재진입 검사 |
| 학교에서 독서와 자리 이탈 | PASS | PASS | PASS | 학교에 등록한 격리 프로필에서 실제 책상 행동과 독서 횟수 증가 확인 |
| 가구 접촉과 종료 | PASS | PASS | PASS | 플랫폼마다 진입 100건, 접촉 100건, 종료 100건 |
| 앱을 닫고 다시 이어하기 | PASS | PASS | PASS | 9세 교실에서 실제 재시작. Android·macOS에서 저장 전후 위치와 두 축 배율 수치도 비교 |

Windows x64 개발 빌드는 성공했다. Windows 네이티브 실행과 iOS 실기기 실행은 모두 `NOT VERIFIED`다.

**수치와 판정 범위.** 플랫폼마다 성장·저장·이동 검사 746개와 가구 검사 533개가 통과했다. 카탈로그 PlayMode 테스트는 6/6 통과했다. 보행 중 루트 X/Y는 1/1, 비주얼 X/Y는 0.8/0.8, PPU는 128이었다. 단계 안에서 방향을 돌리거나 걸음을 옮길 때 프레임 캔버스 높이가 바뀌는 사례를 발견하지 못했다. 사람의 발 미끄러짐을 모든 바닥 픽셀에서 측정한 결과는 없다. 아래 영상은 실제 입력과 프레임 전환을 판단할 수 있도록 원래 재생 시간으로 제공한다.

## 실제 화면과 기록

- Android 실기기에서 캡처한 [20단계 비교](Growth20QA/android-stages.jpg), [학교에서 조이스틱 이동](Growth20QA/android-school.mp4), [S01–S10](Growth20QA/android-stages-01-10.mp4), [S11–S20](Growth20QA/android-stages-11-20.mp4), [11세·55세 학교 재확인](Growth20QA/android-school-followup.mp4), [단계별 영상 시각](Growth20QA/android-video-index.tsv).
- macOS 플레이어의 [20단계 비교](Growth20QA/mac-stages.jpg), [학교 키보드 이동](Growth20QA/mac-school.mp4), [전체 단계 연속 재생](Growth20QA/mac-all-stages.mp4), [3세·5세 방 안 이동 재확인](Growth20QA/mac-toddler-followup.mp4).
- Editor의 [Game View 비교](Growth20QA/editor-stages.jpg)와 [실제 교실 이동](Growth20QA/editor-school.mp4). 첫 녹화는 창 포커스 때문에 위치가 계속 같았다. 다시 녹화한 영상에서 실제 이동과 242개의 보행 샘플을 확인했다.
- [60개 플랫폼·단계별 실측값](Growth20QA/stage-results.tsv), [Android 성장 검사 원본](Growth20QA/android-lifecycle.tsv.gz), [Android 가구 검사 원본](Growth20QA/android-furniture.tsv.gz). 같은 폴더에 Editor와 macOS 결과도 있다.

## 자세, 가구와 아트의 범위

정면 정지는 공급된 PNG의 실제 픽셀이다. 후면·측면·전후 45도, 착석·반쯤 기대기·수면은 해당 연령 원화를 기반으로 제작한 보조 아트다. 오른쪽은 같은 단계 왼쪽 방향을 반전한다. S01에는 걷기·달리기 클립이 없다. S02는 앉은 기본 원화와 서서 첫발을 떼는 동작을 따로 사용한다. 이동은 실제 이동 거리와 단계별 보폭으로 6개 보행 프레임을 재생한다. 달리기는 같은 단계 보행 그림에 더 긴 보폭을 사용한다. 자연 보행의 상체와 하지 협응을 비교할 때 참고한 자료는 다음과 같다. [University of Utah, Natural Gait](https://neurologicexam.med.utah.edu/adult/html/gait_normal.html)

침대 20건, 소파 19건, 식탁 의자 19건, 학교 책상 16건, 도시 벤치 16건, 회사 책상 10건을 각 플랫폼에서 검사했다. 이 횟수는 해당 단계의 이동 해금과 가구 접근 조건을 반영한다. 가구 어댑터 검사는 시험용 캐릭터를 접근 가능한 바닥에 놓은 뒤 게임의 실제 진입·동작·퇴장 코루틴을 사용한다. 수면 중 이불 덮임은 테스트 시점에 1.0, Android 좌석의 최대 골반 접점 오차는 0.000031 월드 단위 이하였다. [실기기의 침대·소파 연속 영상](Growth20QA/android-bed-sofa.mp4)

식사·손짓·표정 등 일부 보조 행동에는 같은 단계의 기본 자세 변형과 기존 소품 연출을 사용한다. 각 행동의 독립된 원화 세트, 좌우 비대칭 전용 원화, 별도의 노년 보행·노화 그림은 추가 제작 범위다. 이번에 촬영한 자세와 접촉 수치를 넘어 모든 생활 동작의 미적 완성도를 판정하지 않았다.
