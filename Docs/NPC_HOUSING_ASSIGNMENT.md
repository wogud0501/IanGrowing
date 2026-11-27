# 주민 주소와 침대 배정

주거 생활형 NPC 25명 가운데 23명은 주민 아파트 13세대, 부모님 두 명은 가족 주택에서 생활한다. 주소 선택기와 기존 저장에 있던 집 ID를 유지했다. 기존 주민의 침대 ID와 공개 장소 시간표를 보존하고, 기존에 집은 있었으나 집에서 실체화할 수 없던 거리 주민 7명에게 침대와 집 안 행동을 연결했다. 학생·직장인 등의 역할은 실제 NPC 데이터에서 읽었다. 같은 주소만으로 가족관계를 새로 설정하지 않았다.

| NPC ID | 게임 이름 | Household ID | Home ID | Home Scene | Assigned Bed | 실제 검사 |
|---|---|---|---|---|---|---|
| colleague | 동네 선배 수연 | household-101 | residence | Resident_Housing | resident-bed-0 | 저장·복원, 다음 날 취침 |
| classmate | 옆자리 민수 | household-102 | apt-102 | Resident_Housing | resident-bed-1 | 저장·복원, 다음 날 취침 |
| resident-0 | 지은 | household-102 | apt-102 | Resident_Housing | resident-bed-2 | 저장·복원, 다음 날 취침 |
| resident-1 | 정호 | household-103 | apt-103 | Resident_Housing | resident-bed-3 | 저장·복원, 다음 날 취침 |
| resident-2 | 도윤 | household-201 | apt-201 | Resident_Housing | resident-bed-4 | 저장·복원, 다음 날 취침 |
| resident-3 | 순자 | household-202 | apt-202 | Resident_Housing | resident-bed-5 | 저장·복원, 다음 날 취침 |
| resident-4 | 지은 | household-203 | apt-203 | Resident_Housing | resident-bed-6 | 저장·복원, 다음 날 취침 |
| resident-5 | 정호 | household-301 | apt-301 | Resident_Housing | resident-bed-7 | 저장·복원, 다음 날 취침 |
| resident-6 | 도윤 | household-302 | apt-302 | Resident_Housing | resident-bed-8 | 저장·복원, 다음 날 취침 |
| resident-7 | 순자 | household-303 | apt-303 | Resident_Housing | resident-bed-9 | 저장·복원, 다음 날 취침 |
| resident-8 | 지은 | household-401 | apt-401 | Resident_Housing | resident-bed-10 | 저장·복원, 다음 날 취침 |
| resident-9 | 정호 | household-402 | apt-402 | Resident_Housing | resident-bed-11 | 저장·복원, 다음 날 취침 |
| school-teacher | 담임 선생님 서윤 | household-403 | apt-403 | Resident_Housing | school-teacher-bed | 저장·복원, 다음 날 취침 |
| school-pupil | 같은 반 학생 준서 | household-203 | apt-203 | Resident_Housing | school-pupil-bed | 저장·복원, 다음 날 취침 |
| shop-clerk | 동네 상점 주인 영호 | household-501 | apt-501 | Resident_Housing | shop-clerk-bed | 저장·복원, 다음 날 취침 |
| family-mother | 엄마 | family | family-parents-bedroom | FamilyHome_ParentsBedroom | family-mother-bed | 가족 침실 전환·다음 날 취침 |
| family-father | 아빠 | family | family-parents-bedroom | FamilyHome_ParentsBedroom | family-father-bed | 가족 침실 전환·다음 날 취침 |
| ambient-urban-0 | 등교하는 학생 | household-101 | residence | Resident_Housing | ambient-urban-0-bed | 저장·복원, 다음 날 취침 |
| ambient-urban-1 | 버스를 기다리는 학생 | household-102 | apt-102 | Resident_Housing | ambient-urban-1-bed | 저장·복원, 다음 날 취침 |
| ambient-urban-2 | 출근하는 주민 | household-103 | apt-103 | Resident_Housing | ambient-urban-2-bed | 저장·복원, 다음 날 취침 |
| ambient-urban-3 | 퇴근길 직장인 | household-201 | apt-201 | Resident_Housing | ambient-urban-3-bed | 저장·복원, 다음 날 취침 |
| ambient-urban-4 | 배달 대기 주민 | household-202 | apt-202 | Resident_Housing | ambient-urban-4-bed | 저장·복원, 다음 날 취침 |
| ambient-urban-5 | 장보는 주민 | household-203 | apt-203 | Resident_Housing | ambient-urban-5-bed | 저장·복원, 다음 날 취침 |
| ambient-urban-6 | 산책하는 주민 | household-301 | apt-301 | Resident_Housing | elder-301-bed | 저장·복원, 다음 날 취침 |
| ambient-urban-7 | 서류를 든 주민 | household-302 | apt-302 | Resident_Housing | ambient-urban-7-bed | 저장·복원, 다음 날 취침 |

Game View, macOS 실행 빌드, Android에서 각 주민 주소로 실제 진입해 각자의 수면 시간대에 배우·침대 점유자·이불 덮임을 조회했다. 야간 근무자는 동일한 밤 시각 대신 해당 시간표의 수면 구간을 사용했다. 주민 13세대는 저장 파일을 다시 읽어 소속 구성원의 `householdId`와 `residenceId`를 비교하고 게임에 복원했다. 부모님의 두 침대는 가족 전용 수면 시간표에서 읽었다. [Android 다음 날 검사](WorldReconstructionQA/android-world-day.tsv.gz) · [주소별 저장 결과](WorldReconstructionQA/android-world-results.tsv.gz)

소속, 가구, 인구 예산 및 장면 간 추적은 기존 `RoomWorldRules`, `RoomScenes`, `ResidentApartment`, `WorldNPCScheduler`, `LifeWorldService`를 사용한다. 보이지 않는 장소에서는 시간표와 주소를 저장하고, 관찰 중인 집으로 돌아오는 사람은 문에서 실체화한 뒤 걸어서 이동한다. 25명 전원의 문 밖 출발과 귀가를 개별 영상으로 추적한 범위는 통합 결과를 따른다. [통합 검증](ANDROID_PC_FINAL_QA.md)

## 주민 25명의 귀가 검사

각 주민의 평일 시간표에서 마지막 외부 활동과 최초 귀가 시각을 구해, 집에 없던 주민이 현관에 등장한 때부터 집 안으로 걷는 위치를 연속 기록했다. 처음에는 아버지만 현관에서 0.52 단위 이동한 뒤 멈췄다. 거실 탁자와 겹치던 청소 지점을 고친 후 Android에서 첫 관찰 위치는 문과 0.03 단위, 이어진 이동 거리는 1.39 단위였다. 최종 일괄 재검사에서 Android, macOS, Game View, iOS 시뮬레이터 각각 25명이 자기 집 현관을 통과했다. [주민별 실기기 결과](WorldReconstructionQA/android-world-returns.tsv.gz) · [아버지 재시험](WorldReconstructionQA/android-world-return-single.tsv.gz) · [현관 연속 영상](WorldReconstructionQA/android-father-front-door.mp4)

휴식·TV 시청은 거주자별 실제 시간표를 따라 관찰했다. 귀가 직후 도보 중인 주민은 착석 완료까지 기다렸다. 기록에는 당시 행동, 가구 ID와 실내 위치가 있다. 102호 학생은 저녁 귀가와 다음 날 아침 외출 구간의 영상도 남겼다. [25명의 실내 휴식](WorldReconstructionQA/android-home-rest.tsv) · [102호 아침 경로](WorldReconstructionQA/android-commute-trace.tsv.gz)
