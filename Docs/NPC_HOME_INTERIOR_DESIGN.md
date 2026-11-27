# 주민 아파트 실내

세대별 평면, 인원수, 침대, 휴식 좌석, 개별 소품을 주소 데이터에서 불러온다. 원룸은 생활·취침 구역을 공유하고 욕실에 문을 두었다. 소형·가족·노년 주택은 침실과 공용 생활 공간이 나뉜다. 부엌과 식탁에는 사용 위치를 따로 두고, 각 침대에는 접근·앉기·베개·기상 위치를 둔다.

| 주소 | 가구 | 실제 평면 Prefab | 주민 수 | 입주자 |
|---|---|---|---:|---|
| residence | household-101 | Family | 2 | 동네 선배 수연, 등교하는 학생 |
| apt-102 | household-102 | SharedChildren | 3 | 옆자리 민수, 지은, 버스를 기다리는 학생 |
| apt-103 | household-103 | Small | 2 | 정호, 출근하는 주민 |
| apt-201 | household-201 | Small | 2 | 도윤, 퇴근길 직장인 |
| apt-202 | household-202 | Small | 2 | 순자, 배달 대기 주민 |
| apt-203 | household-203 | SharedAdults | 3 | 지은, 같은 반 학생 준서, 장보는 주민 |
| apt-301 | household-301 | Older | 2 | 정호, 산책하는 주민 |
| apt-302 | household-302 | Small | 2 | 도윤, 서류를 든 주민 |
| apt-303 | household-303 | Studio | 1 | 순자 |
| apt-401 | household-401 | Studio | 1 | 지은 |
| apt-402 | household-402 | Studio | 1 | 정호 |
| apt-403 | household-403 | Small | 1 | 담임 선생님 서윤 |
| apt-501 | household-501 | Studio | 1 | 동네 상점 주인 영호 |

공유 아동 침실과 공유 성인 침실 두 평면에는 세 번째 침대, 식탁 자리, 거실 의자가 있다. 침대 수는 현재 입주자 수에 맞추므로 혼자 사는 세대에 남는 침대가 표시되지 않는다. 침실 문 앞에 통로를 남기고, 양측 침대 머리를 같은 벽에 붙였다. 여섯 평면의 변기와 샤워 칸에도 실제 사용 지점을 붙였다. [주택 Prefab](../Assets/_Game/Prefabs/World) · [실제 주소별 화면](WorldReconstructionQA/android-rooms.jpg)

부모님 집에는 거실, 주방, 침실, 욕실과 기존 가구가 있다. 이안의 독립 첫 집은 나무색 바닥, 흰 벽, 별도 옥색 욕실, 기본 침대 한 개를 유지한다. 별도 시험 세이브에서 독립한 뒤 선택 가구가 활성화되어 있지 않은 것을 검사했다. [실행 결과](WorldReconstructionQA/mac-world-results.tsv.gz)

## 침대 옆 공간과 다음 날 등교

세 명이 사는 두 집에서 침대 중심끼리는 충분히 떨어져 있었지만, 마주보는 접근점의 거리는 각각 0.59·0.72 월드 단위였다. NPC 두 사람이 동시에 설 때 필요한 최소 거리는 0.78이다. 그 결과 한 사람의 침대가 점유돼도 수면 자세로 들어가지 못했다. 침대를 양쪽 벽 가까이 배치하고 새 침대의 접근·기상 지점을 발치 쪽으로 옮겼다. 두 접근점 간격이 모두 1.0 단위를 넘는다. 일주일 시간표 검증은 실제 침대 접근점 사이의 최소 거리도 확인한다. [실패 원본](WorldReconstructionQA/android-before-world-results.tsv.gz) · [수정 후 실기기 결과](WorldReconstructionQA/android-world-results.tsv.gz)

Android에서 102호 학생이 자기 집에 돌아와 식사하고, 아침에 일어나 같은 현관으로 걸어 나가는 장면을 기록했다. 여섯 평면에서 실제 변기 또는 샤워를 사용한 뒤 플레이어 제어가 복구되는 것도 확인했다. [귀가 영상](WorldReconstructionQA/android-student-returns.mp4) · [등교 영상](WorldReconstructionQA/android-student-leaves.mp4) · [욕실 사용 기록](WorldReconstructionQA/android-actions.tsv)
