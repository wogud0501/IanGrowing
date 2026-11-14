# 9단계 성장 외형·생활 동작 검증 결과

작업: 2026-11-14~15 KST · Unity 6000.6.8f1 / Apple Silicon macOS.
중단된 변경 `8a95d70`에서 재개. 안정 기준선은 `0f08949`.
**판정: 핵심 외형 연결과 아래 실행 검증 통과. 전체 아트·전신 보호자 행동·실기기 검수는 미완료.**
Phase 2.8 개발이나 Phase 1~2.7 전체 QA 재완료를 선언하는 보고서가 아니다.

## 구현

| 외형 | 나이 | 권한과의 관계 |
|---|---|---|
| Newborn | 0 | 보행·직접 문 통과 금지, 수유/돌봄/수면 전용 표현 |
| Toddler | 1–2 | 집 안 보행, 독립 외출 금지, 보호자 목욕 |
| Preschool | 3–4 | 기존 행동 권한을 유지하며 별도 외형 적용 |
| EarlyChild | 5–7 | 6→7세에 외형은 같아도 독립 외출 권한만 해금 |
| SchoolChild | 8–10 | 별도 신체 비율·이동/생활 프레임 |
| Preteen | 11–13 | 별도 신체 비율·이동/생활 프레임 |
| Teen | 14–16 | 성인 직업·금융 권한 자동 해금 없음 |
| LateTeen | 17–18 | 청소년 외형, 독립 전 가족 거주 유지 |
| Adult | 19+ | 기존 성인 원화/뱅크 유지, 기존 비용·조건에 따른 선택적 독립 |

- `LifeProgression`의 기존 6단계 생애주기·60개 미션 조건은 변경하지 않았다. 9단계는 표현 프로필이다.
- 사용자 확정 규칙인 보행 1세 / 독립 외출 7세 / 선택적 독립 19세를 분리해 유지했다. 걷기에 퀘스트 완료를 추가 요구하지 않는다.
- 서로 다른 어린 연령 원화 8개와 기존 성인 원화를 사용한다. 기존 성인 원본·애니메이션 asset은 수정하지 않았다.
- 각 단계의 키는 원화 PPU와 별도 신체 비율로 맞추며 플레이어 Transform Scale은 모든 나이에서 기존 값으로 유지한다. 성인 키를 기준으로 점진적 비율을 적용했다.
- 실제 CircleCollider2D와 이동 속도·보폭을 갱신한다. Foot/Hand/Eye/좌석/욕실 프로필을 제공하되, 아래 미완료 범위와 구분한다.
- 영아용 침대/유아용 낮은 침대, 침대 방향에 맞춘 눕기·기상, 이불/침대 난간 Sorting을 연결했다.
- 어린 캐릭터의 실제 SpriteRenderer를 성인 수면·식사 합성 이미지가 덮던 경로를 분리했다.
- 신생아의 허기·갈증이 낮으면 전용 울음 프레임을 사용한다. 수유는 보호자 손/젖병, 걸을 수 있는 유아의 물 마시기는 해당 연령 Drink 프레임을 사용한다.
- 0~2세 단독 샤워를 실행 함수에서 차단하고 가족 침대의 보호자 목욕 돌봄으로 연결했다. 휴대 목욕/보호자 손을 표현하며 0세의 직접 보행을 사용하지 않는다.
- 3세 이상 샤워는 기존 허용을 유지한 것으로, 새로 '3세 해금' 규칙을 기획한 것이 아니다. 실제 부스 진입·물·퇴장 흐름과 위생 효과를 사용한다.
- 행동 중 나이 변경은 외형/Collider 적용을 안전한 종료까지 미룬다. 취소 시 점유·물·충돌·입력을 복구하며 보상은 지급하지 않는다.
- 성장 단계는 저장 나이에서 유도하므로 중복 저장된 Stage와 충돌하지 않는다. 기존 Scene/Save ID는 유지한다.
- 16개 실제 분리 씬에 직렬화했다. Workspace의 가구·NPC 배치와 기존 문/전환 코드는 교체하지 않았다. 프리팹 직접 변경은 없다.
- QA 센터에 성장 경계 검사, 수동 아트 검사, 데이터 기반 9개 외형 프리셋을 추가했다. 일반 새 게임에 비교 프리뷰를 노출하지 않는다.
- 생성/연결 메뉴는 `Ian/Growth`에 있으며 씬 재생성 후 성장 연결도 복구한다. 텍스처는 제작 후 CPU 읽기 복사본을 해제한다.

## 실제 실행 결과

아래 숫자는 **조건 검사(assertions)** 수이며, 서로 다른 게임 기능 수로 부풀려 계산하지 않는다. 실패한 초기 시도는 `Failures/`에 보존했고 최종 결과만 집계했다.

| 실행 모드 | 내용 | 최종 결과 |
|---|---|---|
| [roomVisualGrowth](Verification/ROOMVISUALGROWTH_RESULTS.json) | 17개 나이·4방향 보행·실제 성장 영수증·저장 복원 | PASS · 217 assertions |
| [roomVisualGrowthLife](Verification/ROOMVISUALGROWTHLIFE_RESULTS.json) | 수면·돌봄 목욕·샤워·취소/성장 지연·식사 | PASS · 124 assertions |
| [roomVisualGrowthMeals](Verification/ROOMVISUALGROWTHMEALS_RESULTS.json) | 식사 중 별도 캡처·실제 허기/점유 복구 | PASS · 27 assertions |
| [roomVisualGrowthDoors](Verification/ROOMVISUALGROWTHDOORS_RESULTS.json) | 0/1/6/7/18/19세 실제 문 왕복·외출 경계 | PASS · 73 assertions |
| [roomVisualGrowthQA](Verification/ROOMVISUALGROWTHQA_RESULTS.json) | 실제 QA 센터 성장 시나리오·스냅샷 복구 | PASS · 21 assertions |
| [roomVisualGrowthBed](Verification/ROOMVISUALGROWTHBED_RESULTS.json) | CPU 읽기 복사 해제 후 실제 수면/이불/기상 | PASS · 12 assertions |
| [roomVisualGrowthPreview](Verification/ROOMVISUALGROWTHPREVIEW_RESULTS.json) | 실제 Game View의 개발용 9단계 비교 화면 | PASS · 9 assertions |
| [roomVisualGrowthCare](Verification/ROOMVISUALGROWTHCARE_RESULTS.json) | 영아 수유·유아 물 마시기 전용 모션과 실제 수치 | PASS · 15 assertions |
| [roomVisualDoors](Verification/ROOMVISUALDOORS_RESULTS.json) | 모든 등록 출입구 양방향 회귀 | PASS · 158 assertions |
| [roomVisualCheck](Verification/ROOMVISUALCHECK_RESULTS.json) | 분리 실내 씬·픽셀 카메라·한글 HUD | PASS · 112 assertions |
| [sceneState](Verification/SCENESTATE_RESULTS.json) | 가족 생활·가구·경제·씬·디스크 저장 복원 | PASS · 109 assertions |
| [roomVisualMobile](Verification/ROOMVISUALMOBILE_RESULTS.json) | Editor 화면비·UI·터치 바인딩 회귀 | PASS · 54 assertions |

- 총 12개 Play Mode 시나리오, 931 assertions 통과. UI 감사 이슈 0개.
- Unity Test Framework의 PlayMode 테스트 2개 실제 통과: 나이 0~120의 단일 프로필 매핑, 서로 다른 9개 텍스처, 영아 보행 클립 부재와 연령별 프레임/방향 무결성. [XML](UNITY_FRAMEWORK_RESULTS.xml).
- 강제 나이 fixture와 기존 `TryAdvanceAgeForMission` 성장 영수증 경로를 구분했다. 후자는 실제 나이/외형 갱신과 중복 실행 거부를 확인했다. 60개 미션의 실제 행동을 전부 다시 실행한 것은 아니다.
- Android 빌드 후 macOS 복원·리소스 재임포트 뒤 성장 시나리오도 다시 실행해 개별 연령 표시와 저장 복원을 확인했다.
- 개발용 비교 화면의 공유 머티리얼/동일 정렬 순서에 따른 복사 렌더러 표시 오류는 개별 머티리얼·순서로 분리해 수정했다. 실제 플레이어의 성인 원화를 변경하지 않았다.

## 원인과 수정

| 항목 | 확인 원인 | 결과 |
|---|---|---|
| 성인 뱅크를 계속 쓰는 플레이어 | 중단 시 프로필/원화는 있었으나 실제 16개 씬에 연결되지 않음. 기존 0세는 침대 합성 가림 표현에 의존 | 실제 성장 프로필·Sprite·Collider 연결 |
| 어린 캐릭터가 성인 수면/식사로 바뀜 | 기존 WorldLifeAction/공유 식탁 합성 렌더가 원래 캐릭터를 숨김 | 어린 연령은 본인 프레임 유지, 부모/성인 합성 경로 보존 |
| 성장 후 키 급변 | 초안의 청소년 높이가 원래 성인 높이보다 지나치게 작음 | 원본 성인 높이에 대한 단계별 비율로 교정 |
| 침대 방향 불일치 | 새 수면 원화는 가로 자세인데 침대는 세로 방향 | 수면 프로필 방향과 이불 가림 연결 |
| 1~2세 단독 샤워 | 기존 가족 시설 예외가 0세 외의 연령을 통과시킴 | 0~2세 보호자 돌봄 분기, 실행 단계 제한 |
| 0세 직접 문 상호작용 | 보행은 막혀도 문 사용 요청은 가능했던 경로 | 직접 문 실행도 보행 권한 확인, 부모 NPC 문 사용은 변경 없음 |
| 수분 돌봄의 식사 모션 재사용 | 초기 연결이 3/4 행동을 동일 CareFeed로 취급 | 영아 젖병과 걸을 수 있는 유아 Drink 모션 분리 |
| 저장 비교 오판 | 로드 시 금융/부동산 marketUtcTicks가 재기준화됨 | 운영 시각 2개만 분리하고 자산/현금/소유권 동일성 검증 |
| 목욕 수치 비교 오판 | +65 회복만 기대하고 20분의 기존 위생 감소를 누락 | 기존 생존 공식까지 포함해 검증; 게임 공식은 변경하지 않음 |
| 수유 프레임 비교 오판 | Motion 변경 직후 아직 렌더되지 않은 이전 프레임을 집계 | 실제 해당 클립에 속한 렌더 프레임만 계수 |

## 화면 증거

| 0세 | 1–2세 | 3–4세 |
|---|---|---|
| [Game View](Screenshots/Age-00-Newborn.png) | [Game View](Screenshots/Age-01-Toddler.png) | [Game View](Screenshots/Age-03-Preschool.png) |

| 5–7세 | 8–10세 | 11–13세 |
|---|---|---|
| [Game View](Screenshots/Age-05-EarlyChild.png) | [Game View](Screenshots/Age-08-SchoolChild.png) | [Game View](Screenshots/Age-11-Preteen.png) |

| 14–16세 | 17–18세 | 19세+ |
|---|---|---|
| [Game View](Screenshots/Age-14-Teen.png) | [Game View](Screenshots/Age-17-LateTeen.png) | [Game View](Screenshots/Age-19-Adult.png) |

[9단계 개발용 비교](Screenshots/Nine-stage-live-preview.png) · [수면](Screenshots/Sleep-14.png) · [보호자 목욕](Screenshots/Care-Bath-1.png) · [실제 부스 내부 샤워](Screenshots/Shower-3.png) · [식사](Screenshots/Family-Meal-5.png).
이전 화면은 `Before/`, 기존 시설 회귀 캡처는 `RegressionScreenshots/`에 보존했다. 스크린샷만으로 애니메이션 통과를 판정하지 않고 실행 중 프레임·이동·효과·복구를 함께 검사했다.

## 플랫폼·저장

- Android: 실제 ARM64 IL2CPP Development APK 빌드. 최종 수치와 시각은 [빌드 로그](ANDROID_BUILD.txt) 참조. 기존 26개 활성 씬 포함, 경고는 로그대로 남긴다. 배포용 용량 최적화와 실제 단말 성능 검증은 별도다.
- iOS: iPhoneOS SDK 부재로 BLOCKED. [환경 확인](IOS_BUILD.txt). iOS 빌드 성공으로 보고하지 않는다.
- Android/iOS 실기기, 실제 폴더블 회전·접힘, 프레임 저하/발열/메모리 프로파일링은 미실행.
- Release의 QA 기능 제외 매크로 구조는 유지했다. 이번에 실제 Release 빌드를 별도로 실행하지는 않았다.
- 원본 플레이 세이브 3개는 재개 전후 SHA-256이 전부 동일하다. [보존 결과](SAVE_PRESERVATION.json). QA는 보호된 별도 슬롯을 사용했다.
- 이번 세션에는 Unity MCP 도구가 노출되지 않아 기존 프로젝트 로컬 Editor bridge로 실제 Unity를 제어했다.

## 미완료·수동 검수

1. 생성한 어린 연령 아트는 픽셀 정리·프레임 이음새·공식 디자인 일관성의 최종 인간 검수가 필요하다. 이를 출시용 최종 아트 승인으로 간주하지 않는다.
2. 이동은 4방향 프레임이 있지만 어린 연령의 생활 행동은 Down 방향 중심이다. 모든 행동의 Up/Left/Right 원화를 완성했다고 보고하지 않는다.
3. Turn/Stop은 Idle 재사용, 걸을 수 있는 어린 캐릭터의 CareFeed는 Eat 재사용이다. 일부 감정/미등록 행동은 해당 연령 Idle로 fallback한다. 미제작 행동을 전용 애니메이션으로 세지 않는다.
4. Hand/Eye 등의 프로필은 있으나 냉장고 손잡이·컴퓨터·세면대 등 모든 가구에 연령별 손 정렬/보조 설비가 연결된 것은 아니다. Run은 리소스 등록과 기존 권한을 유지했으며 각 연령의 실제 달리기를 별도로 모두 실행하지 않았다.
5. 보호자 손/안기기/목욕 연출과 실제 위생·식사 효과는 연결됐다. 부모 NPC 전신이 요청을 받고 다른 방에서 걸어와 아기를 안아 옮기는 전체 돌봄 연쇄는 미완료다.
6. 부모님 생활 일정·19세 유료 선택 독립·부모 가구 소유권 분리는 기존 시스템을 유지하고 관련 회귀를 확인했다. 모든 연령에서 모든 가구/금융/교육 시나리오를 다시 전수 검사한 것은 아니다.
7. UI 상세 프로필/초상화까지 전부 연령별 원화로 교체한 작업은 아니다. 실제 모바일 장치와 최종 아트 승인은 남아 있다.

## 저장·이어하기

변경 목록: [CHANGED_FILES.txt](CHANGED_FILES.txt), 원화 출처/프롬프트: [ART_PROVENANCE.json](ART_PROVENANCE.json).
실제 씬·데이터·원화·코드는 저장했으며 마지막 Editor 상태/세이브 보호 해제는 별도 체크포인트 파일로 남긴다.
다음 작업은 위 미완료 1~5의 아트/상호작용 검수부터 이어가고, 권한 연령이나 저장 ID를 임의로 바꾸지 않는다.

최종 Editor: FamilyHome_IanBedroom, Play 정지, 컴파일 정지, macOS 타깃. 씬 dirty=False 저장 확인 후 원본 슬롯 쓰기 차단을 실제 검사했고 마지막 명령으로 보호를 해제했다. [상태](FINAL_EDITOR_STATE.json) / [보호 검사](EDIT_SAVE_GUARD.json).
