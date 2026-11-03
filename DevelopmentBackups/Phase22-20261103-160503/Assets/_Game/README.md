# 이안 키우기 — BLACK COMEDY EDITION
현재 구현: Phase 2.0 / Phase 2.1. Unity 6000.6.7f1, 2D.
기획 기준과 검증 결과는 프로젝트 루트 Docs/IAN_DESIGN_BASELINE.md 및 Docs/PHASE2_PROGRESS.md를 참조하세요.
이전 Phase 1 내용은 Stage1Report.md와 체크포인트에 보존되어 있습니다.

## 실행
Scenes/MainGame2D.unity를 열고 Play.
기존 v1/v2 저장이 있으면 원본을 읽어 현재 진행을 보존하며, 다음 저장은 별도 v3 파일입니다.
진짜 신규 게임은 0세/0G/부채 100만G/사회적 지위 없음으로 시작합니다. 날짜만으로 나이가 늘어나지 않습니다.
초기 생존 능력치는 Data/InitialLife.asset, 시간/생존/기본 회복 수치는 Data/SurvivalBalance.asset에서 변경합니다.
실시간 진행은 GameManager.realtimeLifeEnabled로 조절합니다. 기본 300초가 게임 내 하루입니다.
식사/음료/수면/병원은 기본 무료입니다. 허기/목마름이 0이면 기본 48게임시간의 위험 유예 후 실패합니다.
일하기는 19세부터 해금되므로 신규 0세에게 비활성입니다. 현재 미션 엔진은 아직 구현하지 않았습니다.
8개 능력치 범위 0~100. Stress=100-Mental, Health=기존 Energy.
공식 이안 디자인은 CHARACTER_DESIGN.md를 따르며 이미지는 변경하지 않았습니다.

## 모듈
기존 GameManager/PlayerStats/TimeManager/ActionSystem/EventManager/EconomySystem/SaveManager를 확장했습니다.
추가 LifeProgression은 나이/단계/XP/레벨, SurvivalSystem은 생존 위험, StatusEffectSystem은 SO 상태이상을 관리합니다.
UIManager는 이벤트 구독을 통해 표시/버튼 전달만 담당하며, 게임별 싱글톤을 추가하지 않았습니다.
MissionGrowthDefinition은 Phase 2.2 데이터 계약이며 미션 에셋은 0개입니다.
자동 노동/전투와 오프라인 보상은 없습니다. 오프라인 경과 UTC 계산만 준비되어 있습니다.

## 저장
Application.persistentDataPath/ian-black-comedy-v3.json
기존 ian-black-comedy-v1.json의 v1/v2는 읽기 소스로 보존합니다. .pre-phase2.bak 백업도 생성합니다.
커스텀 구버전 슬롯은 .phase2-v3.json 형제 파일을 사용하며 원본을 덮어쓰지 않습니다.
손상/미지원 저장은 현재 상태를 강제 초기화하지 않고 원본을 보호합니다. 새 인생은 명시적인 버튼 선택입니다.
기존 세이브에 없는 나이는 호환 정책상 19세, 부채는 0으로 추정합니다. 신규 출생과 다릅니다.

## 검증 / 남은 범위
Play 첫 화면 렌더 후 Ian > Verify Phase 2.1 (Play Mode).
Phase 1 검증 메뉴도 임시 호환 프리셋/별도 슬롯으로 실행한 뒤 현재 상태를 복원합니다.
이번 작업에서 관찰된 기존 v2 저장 파일 변경은 Docs/PHASE2_PROGRESS.md에 두 버전 보존 경로와 함께 기록했습니다.
Android/iOS/Windows 빌드/실기기, 포함형 한국어 폰트 교체, 최종 아트/애니메이션은 아직 검증/진행하지 않았습니다.


## 저장 동작 수정 (현재 기준)
저장/불러오기는 수동 체크포인트입니다. 행동·종료 자동 저장은 별도 .autosave.json 파일에 기록합니다. 재실행은 자동 저장으로 이어지고, 불러오기 버튼은 마지막 수동 저장으로 돌아갑니다. Docs/SAVE_CHECKPOINT_FIX.md 참조.
