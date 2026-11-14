# 최종 실행 결과 — 2026-11-14

Unity 6000.6.8f1 OSXEditor 실제 Play Mode. 아래 assertion은 실행별 집계이며 중복 초기화 검사를 포함한다.

| 실행 범위 | 결과 | PASS assertion | UI issue | 원본 JSON |
|---|---|---:|---:|---|
| 학교·상점·좌석·거래·저장·10개 출입 경로·메뉴 | Completed | 114 | 0 | [FACILITYCHECK](FACILITYCHECK_RESULTS.json) |
| 보존한 회사/금융 업무석·퇴근·복원 | Completed | 67 | 0 | [CRITICALFACILITIES](CRITICALFACILITIES_RESULTS.json) |
| 금융 고객 상담·실제 도보 퇴장·다음 일정 | Completed | 13 | 0 | [CRITICALVISITORS](CRITICALVISITORS_RESULTS.json) |
| 기존 회사 실제 근무·급여·중복 지급 방지 | Completed | 10 | 0 | [FACILITYCOMPANYSHIFT](FACILITYCOMPANYSHIFT_RESULTS.json) |
| 점원 마감 후 실제 퇴장 | Completed | 10 | 0 | [FACILITYCLERKEXIT](FACILITYCLERKEXIT_RESULTS.json) |
| Editor 터치·가상 Safe Area·화면 변경·재개 | Completed | 48 | 0 | [FACILITYMOBILE](FACILITYMOBILE_RESULTS.json) |
| 실내 단말기→기존 주식/부동산 UI·복귀 | Completed | 13 | 0 | [FACILITYTERMINALS](FACILITYTERMINALS_RESULTS.json) |
| 자동 검증 생산 저장 보호 경계 | Completed | 11 | 0 | [PRE28SAVEGUARD](PRE28SAVEGUARD_RESULTS.json) |
| F10·QA 격리·전체 Registry 실행·복원 | Completed | 19 | 0 | [PRE28FULL](PRE28FULL_RESULTS.json) |

전체 Registry: **141개 / PASS 124 / FAIL 0 / NOT IMPLEMENTED 13 / MANUAL 4**. [개별 항목·이유](REGISTRY_141_RESULTS.json). 신규 시설 좌석/연령 회귀 2항목도 실제 실행했다.

`FACILITYFUNCTIONS_RESULTS.json`의 기능/UI 88 assertion도 통과했다. 이후 학교 실제 행동과 회사 출입을 포함한 `FACILITYCHECK` 114 assertion으로 최종 통합했다. `FACILITYACTORS_RESULTS.json`의 26개 통과 뒤 점원 퇴장 관찰 실패는 검사자 위치 문제였다. 해당 경로만 `FACILITYCLERKEXIT` 10개로 재검증했다. 상담 일정 실제 버그의 실패는 `VISITOR_SCHEDULE_BEFORE_FIX.json`, 수정 후 13개 통과는 `CRITICALVISITORS_RESULTS.json`에 남겼다.

애니메이션은 실제 좌표 변화·방향 내적·교체 프레임 수·좌석 정렬·문 개방을 검사했다. 정지 화면만으로 이동 성공을 판정하지 않았다. 종합 상업용 미술 품질과 물리 키보드·실제 Android/iOS/Fold 기기는 MANUAL/미검증이다. 이번 수정 후 모바일 네이티브 빌드는 실행하지 않았다.

사용자가 확인한 현재 1세·100G 생산 저장 3개는 매 실행 후 `SAVE_OBSERVED_BASELINE.json`의 SHA-256과 일치했다. 최종 보존/Editor 저장/보호 해제 기록은 `FINAL_PRESERVATION.json`, `FINAL_EDITOR_STATE.json`, `SAVE_GUARD_RELEASED.json`에 기록한다. 시작 0세 백업으로 되돌리지 않았다.

최종 저장: LifeWorld / Edit Mode / StandaloneOSX / dirty=False, 컴파일 대기 없음. 최종 Game View 재캡처11 assertion PASS. Edit Mode에서도 실제 SaveManager의 생산 슬롯 쓰기 거부 확인. SAVE_GUARD_RELEASED.json의 released 상태로 일반 플레이 저장 권한 복구. 기존 과거 스크린샷/임시 폰트 캐시의 생성 차이는 백업 후 원래대로 복원하고 이번 증거를 이 폴더에 분리했다.
