# 이안 키우기 — BLACK COMEDY EDITION
최종 개발 방향: Unity 6000.6.7f1 기반 2D 육성 게임.
슬로건: 이안을 행복하게 만들어 주세요. 행복은 별도 구매입니다.

## 실행
Assets/_Game/Scenes/MainGame2D.unity를 열고 Play.
일하기/쉬기/놀기 한 번에 하루가 진행됩니다. 행동 후 기본 40% 확률로 이벤트가 발생합니다.
저장/불러오기 버튼은 JSON 수동 저장입니다. 자동 불러오기는 하지 않습니다.
돈과 체력이 모두 없어도 쉬기 버튼으로 회복할 수 있습니다. 휴식 비용은 잔액 내에서만 차감합니다.
행복/스트레스/체력/돈은 모두 0~100입니다.
밸런스: Data/DefaultBalance.asset. 이벤트 목록: Game Systems > EventManager Inspector.
저장 경로: Application.persistentDataPath/ian-black-comedy-v1.json.
검증은 별도 ian-verification-*.json 슬롯을 사용하며 사용자 저장을 덮어쓰지 않습니다.

## 구조
Scripts/GameManager, PlayerStats, TimeManager, EventManager, SaveManager: 기존 게임 로직.
Scripts/UIManager: HUD 표시, 구독 관리, 버튼 전달. Bind로 모델을 주입합니다.
Scripts/Character2DPresentation: SpriteRenderer 프레임 재생 및 행동 반응.
Scripts/Character2DVisuals: 행동별 Sprite 배열과 재생 속도.
Scripts/GameplaySceneBindings: 씬의 모델과 프레젠테이션 연결만 담당합니다.
Prefabs/GameHUD.prefab: Canvas/Scaler/TMP/게이지/버튼. 에셋은 씬 GameManager를 참조하지 않습니다.
Prefabs/Ian2DCharacter.prefab: SpriteRenderer + 독립 프레젠테이션.
Data/Ian2DVisuals.asset: Idle/Work/Rest/Play 교체용 프레임 목록.
Art/2D: 기능 검증용 절차적 PNG 캐릭터 8장 및 방 1장.
UI/IanKoreanSDF.asset: Windows Malgun Gothic 기반 동적 OS 폰트. 배포 전 라이선스 확보된 한국어 폰트로 교체해야 합니다.

## 보존
Assets/Scenes/SampleScene.unity: 초기 씬을 변경하지 않았습니다.
Scenes/MainGame.unity: 이전 3D 프로토타입 보존.
Scenes/MainGame2D.unity: 최종 활성 2D 프로토타입.
MainGame2D의 ARCHIVE - 3D Elements (Inactive): 원근 카메라, 라이트, 방, 리깅 모델, 볼륨을 삭제하지 않고 비활성 보관.
기존 3D 프리팹, 재질, Animator, 클립, 에디터 빌더도 보존합니다.
원본 3D 씬의 기존 Inspector 버튼 연결도 중복 호출하지 않도록 UIManager가 보호합니다.
Git 저장소는 프로젝트 루트에서 발견되지 않았습니다.

## 최종 2D 비주얼 제작 방향
현재 그래픽은 기능 검증용 플레이스홀더이며 최종 디자인이 아닙니다.
목표는 상업용 인디게임 수준의 강한 개성과 화려한 2D 연출입니다.
굵은 실루엣, 독특한 표정, 산성 라임과 살구색, 거친 인쇄 질감, 블랙코미디 행정 서류 콘셉트를 아트 탐색의 출발점으로 삼습니다. 최종 스타일은 별도 확정합니다.
캐릭터는 Idle/Work/Rest/Play 고해상도 스프라이트 시퀀스로 교체합니다. SpriteRenderer와 데이터만 교체하고 게임 로직은 수정하지 않습니다.
현재 기준은 중앙 피벗, 100 PPU, 동일 프레임 크기입니다. 다른 규격이면 캐릭터 프리팹에서 위치와 크기를 조정합니다.
최종 배경은 레이어/패럴랙스, 상태별 표정, VFX, 전환 및 사운드를 추가할 수 있습니다. 이들은 아직 구현하지 않았습니다.
Sprite Atlas, 대상 기기별 해상도/메모리 최적화, 정식 폰트, UI 디자인과 접근성 검증은 제작 단계에서 진행합니다.
현재 URP 렌더러 설정은 기존 3D 씬 호환을 위해 유지했습니다. 2D SpriteRenderer는 조명에 의존하지 않는 기본 스프라이트 렌더링입니다. 2D Light/Normal-map 렌더러는 아직 추가하지 않았습니다.

## 검증 재실행
MainGame2D에서 Play 후 메뉴 Ian > Verify Active 2D Play Mode.
기능 테스트는 실제 버튼 onClick, 상태 변경, UI raycast를 검사합니다. 실제 마우스 장치 입력의 자동화 테스트는 아닙니다.
프리팹 연결은 편집 모드에서 확인합니다. Unity는 플레이 모드에 들어갈 때 해당 런타임 인스턴스의 프리팹 연결 메타데이터를 유지하지 않습니다.
Standalone 빌드/타 기기 테스트는 아직 수행하지 않았습니다.


## 공식 이안 디자인 적용
캐릭터 기준은 CHARACTER_DESIGN.md 및 Art/Characters/Ian/Reference/Ian_OfficialDesign.png입니다. 기존 절차적 캐릭터는 보존용이며, 현재 기본 캐릭터는 IanOfficialIdentity.asset의 baseSprite로 교체되었습니다. 기존 UI/배경 임시 디자인 표시는 유지합니다.
