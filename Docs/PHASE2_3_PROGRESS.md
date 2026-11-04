# Phase 2.3 진행 기록

## 기준 및 보존
- Unity MCP 연결: IanGrowing@16d876b2697e395d / Unity 6000.6.7f1.
- 기획 공유 링크 열람 완료. 이번 사용자 명세를 우선하며 기존 미션 ID와 행동 ID를 유지.
- 분석: 기존 교육은 ActivityData + ActionSystem + MissionService 카운터 방식. 독립 관계/시험/직업 상태 없음.
- 기존 공통 서비스는 GameContext prefab에서 유지. 새 서비스는 동일 루트에 한 개 추가.
- DevelopmentBackups/Phase23에 _Game, ProjectSettings, 실행 중 LiveSession.json 보존.
- 공식 캐릭터 이미지 편집 없음.

## 구현 중
- 교육 과정/활동, 시험/성적표, 졸업 저장.
- 명시적 친구 만들기, NPC 친밀도/대화 실패/관계 감쇠와 쿨다운.
- 7개 직업, 이력서/면접/근무/승진/퇴사/해고/재취업.
- ActionSystem에서 자원/시간을 단일 소비. EconomySystem 거래 ID로 급여 중복 방지.
- 기존 v4 저장에 선택적 phase23 블록 추가. 없는 경우 안전한 기본값.
- 스타터 장착은 기존 pending reward gateway 사용. 가챠 구현 없음.
- 기존 경제 부채 상환 연결. 세금/주식/인생 선택은 후속 단계 예정.

## 검증
- 서비스 추가 후 컴파일 오류 없음 (Unity Console). 중간 파일 도입 시 참조 누락은 후속 파일 추가로 해소.
- Unity AI 패키지의 NoSubscription 메시지 관찰. 게임 스크립트 컴파일 오류와 분리하여 기록.
- 통합 Play Mode 테스트: 대기.
