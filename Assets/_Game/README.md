# 이안 키우기: BLACK COMEDY EDITION
"이안을 행복하게 만들어 주세요. 행복은 별도 구매입니다."

Unity 6000.6.7f1 기반 2D 육성 게임. 현재 범위는 로드맵 1단계 핵심 시스템입니다.
공식 이안 디자인은 CHARACTER_DESIGN.md를 따릅니다. 이번 작업에서 아트 및 애니메이션 고도화는 하지 않았습니다.

## 실행
Scenes/MainGame2D.unity를 열고 Play.
시작 시 기존 저장을 자동 복원하며, 없으면 초기 스탯과 1일차에서 시작합니다.
일하기/쉬기/놀기 한 번에 기본 하루가 진행되고, 매일 기본 40% 확률로 가중치 기반 인생 이벤트가 발생합니다.
일시정지/계속, 새 인생, 수동 저장/불러오기 버튼이 있습니다.
성공한 행동마다 자동 저장합니다. autoLoadOnStart/autoSaveAfterAction은 Game Systems > GameManager에서 조절합니다.

## 기본 밸런스
Data/DefaultBalance.asset에서 조절합니다.
초기 행복 50 / 스트레스 20 / 체력 70 / 돈 50.
일하기: 돈 +18, 스트레스 +15, 체력 -18.
쉬기: 무료, 체력 +28, 스트레스 -12.
놀기: 돈 -12, 행복 +20, 스트레스 -10, 체력 -8.
각 행동의 workDays/restDays/playDays로 소요일수를 조절합니다. 여러 날 행동은 효과/비용을 한 번 적용하고 매일 이벤트를 추첨합니다.
모든 유료 행동은 비용 부족 시 차단됩니다. 쉬기도 유료로 설정하면 동일합니다. 기본 무료 휴식은 돈/체력이 바닥나도 회복할 수 있게 합니다.
행복/체력/스트레스는 0~100, 돈은 0~int.MaxValue입니다. 강제 이벤트 손실은 잔액까지만 차감합니다.
기본 게임오버 조건은 스트레스 100 이상과 체력 0 이하를 동시에 만족하는 번아웃입니다. enableGameOver 및 임계값을 조절할 수 있습니다.
게임오버에는 행동이 차단되고 새 인생/불러오기로 재개합니다.

## 구조
- GameManager: 상태 전환, 모듈 연결, 진행/저장 정책.
- PlayerStats: 순수 스탯 데이터와 개별/통합 변경 이벤트. UI/저장 참조 없음.
- TimeManager: 날짜, 정지, 하루 진행 이벤트. 복원은 이벤트 추첨을 유발하지 않습니다.
- ActionSystem: 기존 GameBalance 필드의 행동 규칙/효과. 다른 행동 구현을 중복 추가하지 않았습니다.
- EconomySystem: 수입/지출/거래 이벤트. 향후 경제 콘텐츠 연결점.
- EventManager: 실행 코드. Data/LifeEvents.asset의 12개 샘플을 일별 확률/가중치로 선택합니다.
- SaveManager: JSON 읽기/쓰기/검증/손상본 보존.
- UIManager: 모델 구독/버튼 전달/표시. GameHUD 프리팹은 씬 모델 참조를 보유하지 않습니다.
- GameplaySceneBindings: HUD/캐릭터에 모델을 주입합니다.
- Character2DVisuals/CharacterIdentity: 공식 이미지와 향후 프레임/상태 이미지 데이터.
- Character2DPresentation: 기존 스프라이트 재생 및 행동 반응. 일시정지를 따릅니다.

## 저장
Application.persistentDataPath/ian-black-comedy-v1.json
파일명은 기존 호환성을 위해 유지하지만 새 JSON 형식은 version=2입니다. version=1도 읽습니다.
저장 교체 시 기존 파일은 .bak으로 보존합니다.
손상 데이터는 .corrupt-*로 복사하고 게임은 1일차/초기 스탯으로 안전하게 초기화합니다.
지원하지 않는 버전/읽기 실패는 현재 플레이 상태를 변경하지 않습니다. 이후 사용자가 새 저장을 수행하면 기존 파일은 .bak으로 보존됩니다.
일시정지는 재실행 시 해제되며, 게임오버 상태는 저장/복원됩니다.

## 검증 및 보존
Stage1Report.md에 변경 파일, 성공/실패 검증, 남은 사항이 있습니다.
Game View에서 Play 첫 화면을 확인한 다음 Ian > Verify Stage 1 Core (Play Mode)로 검증합니다.
자동 테스트는 별도 슬롯을 사용하고 기본 사용자 저장을 변경하지 않습니다.
현재 기본 씬은 MainGame2D입니다. 이전 MainGame 3D 씬과 SampleScene, 비활성 ARCHIVE 그룹은 보존합니다.
새 외부 패키지를 추가하지 않았습니다.
현재 OS 한국어 폰트의 배포용 교체, Standalone 빌드, 다양한 기기/해상도 검증은 남아 있습니다.
