# 월드 재구성 통합 실행 결과

2026-11-26~27에 Unity 6000.6.8f1로 검증했다. Android 실기기, Unity Game View, macOS 실행 빌드, iOS 시뮬레이터에서 각각 독립된 시험 저장 슬롯을 썼다. 정상 게임에서 사용하는 장면, 스케줄러, 문, 좌석 점유, 수면, 저장, 승차·결제 경로를 실행했다. Android는 USB ADB로 연결한 삼성 폴더블(SM-F976N, Android 17)을 사용했다. 최종 Android Development 빌드의 오류는 0건이었고, 설치된 APK의 SHA-256을 로컬 빌드와 대조했다. [플랫폼별 빌드·실행 기록](WorldReconstructionQA/build-and-device.tsv)

## 실행 결과

| 검사 범위 | 상태 | 실행한 일과 증거 |
|---|---|---|
| 공식 성장 외형 | PASS | 네 환경에서 S01~S20을 차례로 적용해 원본과 실제 렌더를 비교했다. 20개 원본 슬라이스가 일치하며 S03~S08의 표시용 파일만 옷깃을 보정했다. [20단계 실기기 비교](WorldReconstructionQA/android-official-render.jpg) · [임포트 수치와 보정 범위](CHARACTER_PROPORTION_AUDIT.md) |
| 학교 보행 | PASS | Android에서 9세 이안의 정지·걷기를 601회 측정했다. 보행 표본 102개에서 X/Y 배율은 모두 0.8이었다. [연속 녹화](WorldReconstructionQA/android-school-walk.mp4) · [프레임별 기록](WorldReconstructionQA/android-school-walk.tsv.gz) |
| 공간과 학교 | PASS | Scene 파일 30개의 활성 스프라이트 1,067개를 목록화했다. 학교 서쪽 복도에 벤치·정수기를 두고 학생 둘이 개별 책상, 교사가 교탁을 사용하는 것을 확인했다. [전체 목록](WORLD_OBJECT_PLACEMENT_AUDIT.md) · [교실 전후](WorldReconstructionQA/school-before-after.jpg) · [복도 현장](WorldReconstructionQA/android-west-corridor.jpg) |
| 주거 배정·침대 | PASS | 주민 아파트 13세대의 23명과 부모님 두 명이 각자 지정된 집에서 자기 침대를 점유하고 이불을 덮었다. 야간 근무자는 별도 수면 시간에 확인했다. [25명 배정표](NPC_HOUSING_ASSIGNMENT.md) · [다음 날 실기기 검사](WorldReconstructionQA/android-world-day.tsv.gz) |
| 실제 귀가·외출 | PASS | 관찰 중인 집에 주민 25명이 일정에 맞춰 문으로 돌아와 첫걸음을 내디뎠다. 102호 학생의 저녁 귀가와 다음 날 아침 외출은 연속 녹화했다. [주민별 귀가](WorldReconstructionQA/android-world-returns.tsv.gz) · [저녁](WorldReconstructionQA/android-student-returns.mp4) · [아침](WorldReconstructionQA/android-student-leaves.mp4) |
| 실내 휴식·욕실 | PASS | Game View, Android, macOS에서 25명의 예정된 휴식·TV 시청 착석과 여섯 평면의 변기 또는 샤워 사용 후 조작 복귀를 확인했다. [실내 휴식](WorldReconstructionQA/android-home-rest.tsv) · [주택별 화면](WorldReconstructionQA/android-home-rest.jpg) · [욕실](WorldReconstructionQA/android-actions.tsv) |
| 주택·학교·공공시설 출입 | PASS | 세 환경에서 각각 20개 방향별 실제 문을 사용했다. 학교, 부모님 집, 아파트 주소 선택, 역 양쪽, 상점, 회사, 금융·부동산 시설을 포함한다. [Android 출입 기록](WorldReconstructionQA/android-public.tsv) |
| 지하철·버스·택시 | PASS | 세 수단의 목적지 도착·1회 결제·하차 후 이동을 네 환경에서 확인했다. Android·Game View·macOS는 세 노선 모두 역방향도 운행했다. Android에서는 탑승 취소와 건너뛰기·연속 탭을 추가 검사했다. [환경별 영상](TRANSIT_CUTSCENE_QA.md) · [Android 정방향](WorldReconstructionQA/android-fares.tsv) · [역방향](WorldReconstructionQA/android-reverse-fares.tsv) · [연속 탭과 취소](WorldReconstructionQA/android-transit-options.tsv) |
| 저장·이어하기 | PASS | 13세대별 시험 저장을 디스크에서 읽어 복원했다. Android와 macOS는 102호에서 프로세스를 종료하고 이어하기로 주소·날짜·재화·좌표와 주민 25명 배정이 복원되는 것을 확인했다. Android 기존 수동·자동 저장의 시험 전후 SHA-256도 각각 일치했다. [Android 재실행](WorldReconstructionQA/android-cold.tsv) · [macOS 재실행](WorldReconstructionQA/mac-cold.tsv) |
| Windows x64 실행 | NOT VERIFIED | Windows Development 실행 파일을 교차 빌드했다. 실제 Windows 실행 환경을 확보하지 못했다. |
| iPhone 실기기 | BLOCKED | 연결된 iPhone이 없었다. iPhone 17 시뮬레이터에서 iOS 27.2용 플레이어를 실제 실행했다. [iOS 성장 외형](WorldReconstructionQA/ios-official-render.jpg) · [iOS 실행 집계](WorldReconstructionQA/scenario-totals.tsv) |

네 환경에서 각각 공간·외형·주소 139개, 다음 날 취침·이동 85개, 귀가 75개 판정이 통과했다. 같은 주민이 여러 조건에 포함된다. 장소를 옮겨 가며 검사했고, 출입이나 승차 단일 시험은 QA 캐릭터를 시설 앞으로 배치한 다음 일반 상호작용을 호출했다. Android 승차 선택은 ADB로 화면을 터치했다. 시간표 검증은 일주일을 순회했으며, 사람이 시야 밖에 있는 동안의 이동은 기존 게임의 시간표 시뮬레이션을 사용한다. [4개 환경별 원 집계](WorldReconstructionQA/scenario-totals.tsv) · [7일 시간표 검사](WorldReconstructionQA/seven-day-schedule.txt)

## 실패를 재현하고 고친 항목

| 최초 상태 | 관찰한 현상 | 수정과 재시험 |
|---|---|---|
| FAIL | 102호 학생이 아침에 출구까지 가지 못했다. | 출입 통로에 걸린 가구를 옮겼다. [변경 전 이동 기록](WorldReconstructionQA/commute-before.tsv) · [수정 후 다음 날 외출](WorldReconstructionQA/android-commute-trace.tsv.gz) |
| FAIL | 세 사람이 사는 102·203호에서 지정 침대를 확보하고도 한 사람이 이불을 덮지 못했다. | 마주 보는 접근점 사이가 0.59·0.72로, NPC 간 최소 거리 0.78보다 좁았다. 침대와 발치 접근점을 조정해 두 간격을 모두 1.0 이상 확보하고 25명의 수면을 다시 실행했다. [수정 전](WorldReconstructionQA/android-before-world-results.tsv.gz) · [수정 후](WorldReconstructionQA/android-world-day.tsv.gz) |
| FAIL | 아버지가 현관에서 0.52 단위 걷고 멈췄다. | 거실 탁자와 겹친 청소 목적지를 옮겼다. 실기기에서 아버지는 현관 0.03 단위 거리에서 나타나 1.39 단위를 걸었다. [재시험 기록](WorldReconstructionQA/android-world-return-single.tsv.gz) · [연속 영상](WorldReconstructionQA/android-father-front-door.mp4) |
| FAIL | 승강장 탑승 목적지가 정차 열차의 고정 충돌체와 겹쳤다. | 충돌체 전면에서 접근점을 구해 열린 문으로 이어지게 했다. 실제 도심역에서 승차해 주거 골목역에 도착했다. [지하철 실기기 영상](WorldReconstructionQA/android-train.mp4) |
| FAIL | 택시 손잡이를 잡는 순간 발이 바퀴 높이로 뜨고, 도착 직전 승객이 지붕 위로 그려졌다. | 연석과 손잡이를 별개 접점으로 두고, 승객이 차량 안으로 넘어간 뒤에는 뒷문의 실제 윤곽 안에 그린다. 11월 27일 같은 휴대폰에서 다시 녹화해 승차 중 발이 연석에 닿고 도착 시 열린 뒷문으로 나오도록 수정된 것을 확인했다. [변경 전](WorldReconstructionQA/android-taxi-before.jpg) · [변경 후](WorldReconstructionQA/android-taxi-sequence.jpg) · [전체 녹화](WorldReconstructionQA/android-taxi.mp4) |

## 남은 검증 범위

- Android에서 기동 시 Play Asset Delivery의 `AssetPackManager` 클래스 조회 오류가 관찰됐다. 마지막 재시험의 게임 프로세스 로그 689줄에는 오류 1건, 치명적 예외·시그널 0건이 있었고 세 교통수단의 이동이 끝까지 진행됐다. Play 배포 AAB의 에셋 전달과 기기 Profiler 자동 연결은 `NOT VERIFIED`다. [첫 수집 집계](WorldReconstructionQA/android-log-summary.tsv) · [11월 27일 재시험 집계](WorldReconstructionQA/android-resume-log.tsv)
- 원본 성장 시트는 정면 그림 20장이다. 고유한 노년 원화, 모든 나이·방향에서의 손발 접촉, 모든 장식물의 개별 사용, 모든 NPC의 하루 전체 무편집 연속 추적은 `NOT VERIFIED`다. [성장 외형·애니메이션 범위](CHARACTER_PROPORTION_AUDIT.md)
- 운영체제가 승차 로딩 도중 프로세스를 종료하는 경우와 모든 연출 설정 조합에 대한 장애 주입은 `NOT VERIFIED`다. 실제로 수행한 환불·재결제 관련 시험 범위는 교통 문서에 기록했다. [승하차·결제 처리](TRANSPORT_INTERACTION_REWORK.md)

재현 시 프로젝트 메뉴 `Ian/World/Apply school, residences and transit stops`에서 제작 데이터를 갱신할 수 있다. Android 빌드는 `Tools/IanGrowing/Build Android QA APK`를 사용한다. 성장·주거 순회는 Editor 및 Development Build의 기존 검증 도구에 연결하며 별도 시험 Save를 쓴다. [개발용 월드 검증 코드](../Assets/_Game/Scripts/Development/Verification/IanGaitVerification.World.cs)
