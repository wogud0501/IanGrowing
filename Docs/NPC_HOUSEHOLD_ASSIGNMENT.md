# 주민 가구와 침대 배정

주소와 세이브 키는 방 인스턴스의 식별자다. 주민의 배정 자리와 침대 ID는 주민 정의에 저장하며, 방문 시 해당 집의 프리팹 슬롯에 연결한다. 101호의 기존 저장 키는 `residence`다.

| 주소 / 입주자 | 방 ID | 가구 ID | 구조 | 주민 ID → 개인 침대 |
|---|---|---|---|---|
| 101호 · 수연 | `residence` | household-101 | 원룸 | `colleague` → `resident-bed-0` |
| 102호 · 지은 · 민수 | `apt-102` | household-102 | 소형 | `resident-0` → `resident-bed-2`<br>`classmate` → `resident-bed-1` |
| 103호 · 정호 | `apt-103` | household-103 | 소형 | `resident-1` → `resident-bed-3` |
| 201호 · 도윤 | `apt-201` | household-201 | 원룸 | `resident-2` → `resident-bed-4` |
| 202호 · 순자 | `apt-202` | household-202 | 원룸 | `resident-3` → `resident-bed-5` |
| 203호 · 지은 · 준서 | `apt-203` | household-203 | 가족형 | `resident-4` → `resident-bed-6`<br>`school-pupil` → `school-pupil-bed` |
| 301호 · 정호 · 어르신 | `apt-301` | household-301 | 노년 가구 | `resident-5` → `resident-bed-7`<br>`ambient-urban-6` → `elder-301-bed` |
| 302호 · 도윤 | `apt-302` | household-302 | 원룸 | `resident-6` → `resident-bed-8` |
| 303호 · 순자 | `apt-303` | household-303 | 원룸 | `resident-7` → `resident-bed-9` |
| 401호 · 지은 | `apt-401` | household-401 | 원룸 | `resident-8` → `resident-bed-10` |
| 402호 · 정호 | `apt-402` | household-402 | 원룸 | `resident-9` → `resident-bed-11` |
| 403호 · 서윤 | `apt-403` | household-403 | 소형 | `school-teacher` → `school-teacher-bed` |
| 501호 · 영호 | `apt-501` | household-501 | 원룸 | `shop-clerk` → `shop-clerk-bed` |

203호는 성인과 학생의 침실을 분리한다. 301호는 기존 배정 주소가 같았던 두 주민의 집으로 구성했다. 데이터에 없는 가족 관계는 새로 설정하지 않았다. 103·403호에는 한 사람만 입주해 두 번째 침대를 생성하지 않는다.

부모님 집과 이안의 독립 주택은 별도의 기존 씬을 사용한다. 도시의 일반 배경 보행자는 현재 지역의 인구 예산에 따라 표시한다. 집을 떠나도 지속 주민의 스케줄과 주소는 오프스크린에서 유지한다.

## 실거주 확인

macOS 실행 빌드에서 13개 주소를 차례로 로드했다. 각 집에 나타나는 침대 수는 명부와 일치했고, 주소를 바꿀 때마다 로드된 씬 수는 하나였다. 23:11에 집을 비운 103호·303호는 각각 02:35와 23:55에 다시 방문했다. 16명 모두 자기 주소에서 서로 다른 개인 침대 ID로 수면 동작에 도달했다. 방문한 집 전체에서 경로가 정확히 도달한 설비·출입 지점은 123개였다.
