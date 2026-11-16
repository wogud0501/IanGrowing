# Phase 3.0 — 첫 제작 단위와 현재 레퍼런스 적용

2026-11-16~17 KST · Unity 6000.6.8f1. **전체 Phase 3.0은 진행 중입니다.** 우선 세 작업과 연결된 표현 계층을 실제 파일에 적용했으며, 모든 씬이 상업용 최종 품질에 도달했다고 판정하지 않습니다.

[최신 사용자 레퍼런스·적용 기준](ART_DIRECTION.md). 이 이미지가 이전 보라색 중심 시안보다 우선합니다.

## 실제 변경

- 픽셀 기반: 기존 실내 정수 배율 카메라를 재사용하고, 고정40 PPU로 재정렬하던 충돌을 실제 렌더 픽셀 기준 하나로 통일. 실외에는 작은 Dead Zone을 추가. 기존 카메라를 중복 생성하지 않음.
- 이안: 기존 0~18세8개 원화와 공식 성인 원화를 유지. 30~59세 중년, 60세 이상 노년의 별도8×8 원화·애니메이션 은행 추가. 총11개 외형 구간. 컨트롤러를 교체하거나 성장 표현을 Transform Scale만으로 처리하지 않음.
- 성장 전환: 기존 성장 이벤트에서 이전 실루엣과12개 재사용 스파클. Reduced Motion 준수. 나이·이동·행동 권한은 기존 시스템이 결정.
- Home: 새 독립 집의 연속된 나무 바닥·흰 벽·문틀 외곽을 별도 오브젝트로 제작. 침대와 문 위치, 가구 배치 데이터 보존. 첫 집은 침대 하나, 옥색 욕실은 별도 씬. 기존 가구가 있는 starterRevision0 세이브는 기존 건축을 유지.
- 다른 공간: 기존16개 씬의 비주얼 프로필과 연결을 저장. 가족 주택은 따뜻한 색감과 절제된 목재, 학교는 밝은 벽과 나무, 회사·금융은 차가운 벽과 어두운 목재, 상점은 타일로 구분. 배치·NPC 시간표·좌석·문 게임 로직은 재작성하지 않음.
- 낮밤·전환: 기존 가로등·차량 헤드라이트·창문·교통·업무 애니메이션 재사용. 프로필에서 색감·기존 전환 스타일·환경음 리소스·기상 표현 확장 지점을 제공. 실내 생활관이 도시 프로필을 덮어쓰던 작성 오류를 분리.
- 수집 화면: 차콜 종이 패널, 크림 사진 카드, 절제된 금색 주요 버튼, 개별 아이템 원화. 1회 결과는 중앙 카드,10회는 기존 그리드. 원화 자체를 등급색으로 덮지 않고 카드에 등급 힌트를 표시.
- 헤드폰:6종의 실제 착용 레이어를13세 이상 외형/4방향 이동 상태에 연결. 목 기준 앵커를 보정하고 원본 이안 스프라이트를 보존. 수면 등 미제작 부착 상태는 안전하게 숨김. UI 아이콘을 임시로 캐릭터에 붙인 방식이 아님.
- 피드백: HUD마다 최대32개 UI 스파클 풀. 미션·아이템·장착·희귀 공개·회복·돈 이벤트와 AudioRequested hook 연결. 기존 UI 스택·포커스·짧은 창 Fade 재사용, 버튼 라벨1픽셀 반응 추가. 새 음원은 생성하지 않음.

원화: `Assets/_Game/Art/Phase30/`. 카탈로그·프레임·프로필: `Assets/_Game/Data/Phase30/`. 아이템 정의는 기존 `Assets/Resources/IanCollection.asset`을 확장. 소유·확률·천장·비용은 변경하지 않았습니다. 신규 플레이 Scene/전역 Manager 없음.

## 실행 결과

- Core:106개 검사 PASS. 연령 경계0/1/7/15/18/19/29/30/59/60/65, 각 방향 원화, 실제 Rigidbody 이동, 중년/노년 수면·기상, 세 해상도, 실제 QA 파일 쓰기/읽기·외형 복원, 씬 왕복.
- Presentation:50개 검사 PASS, 텍스트 넘침0. 실제 장착, 연령/방향별 부착, 미제작 상태 fallback,1/10회 실제 비용, 공개·스킵·UI 포커스 소유·버튼 이벤트·세 화면비·풀 종료.
- Unity Framework:2개 PlayMode 테스트 PASS.0~120세 외형 구간 중복/누락 없음, 신생아 보행 클립 없음, 유효한 프레임과 방향 은행.
- 수정 전 비교: 실제 Play Mode Home/가족 거실/이안 방/도시 캡처14개 검사 완료.
- 최종 씬 순회:87개 검사 PASS, 텍스트 넘침0.13개 실제 공간 이동과 장소별 프로필, 낮밤, NPC 고유 ID·단일 플레이어·현금/시간 보존 확인.
- 기존 문 양방향 회귀:158개 검사 PASS. 문·입장·퇴장·재입력 방지와 실제 목적지/조작권 확인.
- 방 씬 직접 Play:초기화 전 NPC가 없는 전역 서비스를 참조하던 예외를 재현하고 Start/Update의 서비스 준비 가드만 추가. 직접 부모 침실에서 시작→Bootstrap→MainMenu→실제 새 게임→출생 확인→0세 이안 방을 재검증. 게임 소스 오류0, 신생아/현금0/보행 금지/단일 플레이어 확인. [실행 상태](DIRECT_START_RESULTS.json), [Console 구분](DIRECT_START_CONSOLE.json).
- 렌더 텍스처:기존16개 씬의 SpriteRenderer가 참조하는30개 소스 모두Point/밉맵 없음. [감사 자료](RENDER_TEXTURE_AUDIT.json).
- Android 최종 ARM64/IL2CPP 개발 빌드 성공, 오류0/경고42, 등록26개 씬. 시작 서비스 가드를 포함한 최종 소스입니다. 이전 전체 빌드 경고1231과 최종 증분 빌드는 캐시 조건이 다르며, 경고가 수정되었다는 뜻은 아닙니다. [최종 네이티브 보고서](ANDROID_NATIVE_REPORT.txt), [경고](ANDROID_WARNINGS.txt). 실제 기기 실행과 별도 판정합니다.
- 도시 Editor 성능 표본90프레임: 평균1.52ms, GC평균15,424B/프레임, Draw Call91. 실제 기기/Release 기준이 아니며 할당량 최적화는 후속 항목입니다. [원시 기록](EDITOR_PERFORMANCE.json).

테스트용 나이·현금·아이템·확률 주입은 QA 슬롯에만 사용했습니다. 정상 성장 전체를 처음부터 실시간 플레이한 결과는 아닙니다. 희귀 공개1회 캡처는 QA 확률 주입이며10회는 실제 샘플러입니다. 테스트 중 Domain Reload를 활성화해 비직렬화 서비스가 초기화된 세션에서 실행했고, 마감 후 기존 Editor 옵션을 복원했습니다.

## 실제 Game View

- [Home 수정 전](Before/home.png) → [수정 후](Screenshots/Home-Day.png)
- [6개 생애 구간 비교](Screenshots/Six-LifeStage-Lineup.png), [중년 수면](Screenshots/Sleep-35.png), [노년 수면](Screenshots/Sleep-65.png)
- [실제 0세 새 게임](Screenshots/Newborn-Family-Room.png)
- [헤드폰 실제 착용](Screenshots/Headphone-19.png)
- [가족 거실](Screenshots/Scene-family-living.png), [주방](Screenshots/Scene-family-kitchen.png), [이안 방](Screenshots/Scene-family-ian-bedroom.png), [욕실](Screenshots/Scene-family-bathroom.png)
- [도시 낮](Screenshots/Scene-city.png) / [도시 밤](Screenshots/Scene-city-Night.png), [학교](Screenshots/Scene-school.png), [회사](Screenshots/Scene-workplace.png), [금융](Screenshots/Scene-finance.png)
- [가챠](Screenshots/Gacha-Stationery.png), [1회 공개](Screenshots/Gacha-Rare-Presentation.png), [10회 결과](Screenshots/Gacha-Ten-Stationery.png), [세로 인벤토리](Screenshots/Inventory-Stationery-904.png)

## 미완성·미검증

- 전체 Scene/모든 생활 동작의 최종 아트 심사는 미완료. 이번에는 기존 배치와 고품질 에셋을 재사용하는1차 production pass입니다. 도시 전체의 새로운 외관, 추가 환경 모션, 학교/회사 전체 소품 재제작은 완료로 판정하지 않습니다.
- 신규 중년/노년의 이동은4방향, 생활 프레임은Down만 작성. Turn/Stop은Idle, Work는타이핑, PutDown은PickUp 공유. Sit·세부 손 정렬·감정/질병·모든 행동 방향은 추가 폴리싱 필요.
- 헤드폰은대기/이동 중심 부착. 수면·식사·작업 등 모든 모션용 착용 원화와 신규 코스튬 본체 애니메이션은 미제작입니다. 코스튬 그림은 기존 기본 복장의 인벤토리 아이콘입니다.
- 기상은Clear/Rain/Snow/Fog 데이터 hook이며 신규 강우/적설 게임플레이를 만들지 않았습니다. 새 음향 리소스·Mixer 제작은 미완료.
- 전체 시스템 밸런싱·엔딩·환생·경제 재작성은 수행하지 않음. 실제 Android/iOS/폴더블 성능·앱 백그라운드·물리 터치와 게임패드 전체 조작은 미검증.
- Console의 기존 Discord RPC 스레드 중단/Unity AI NoSubscription 오류는 패키지 오류로 남아 있습니다. 게임 소스 예외·컴파일 오류와 구분했고 패키지를 임의 삭제하지 않았습니다. iOS 네이티브 빌드는 현재 iPhoneOS SDK 부재로 BLOCKED입니다.
- Pixel Art 에셋의 원본 PPU는 서로 다릅니다. 이번 검사는 Point 필터·미압축·카메라 정수 배율·렌더 정렬·실제 화면을 확인하며, 모든 원본을 동일 PPU로 재제작했다고 주장하지 않습니다.

생성 프롬프트와 리소스 출처는 [ART_PROVENANCE](ART_PROVENANCE.md), ART_PROMPTS*.md에 기록했습니다.

## 저장·체크포인트

씬·에셋 저장 완료. FamilyHome_IanBedroom에서 Play를 종료하고 StandaloneOSX와 기존 fast-play 옵션(DisableDomainReload)을 복원했습니다. 테스트용 원본 슬롯 쓰기 차단도 해제했습니다. 기존 플레이 세이브3개는 최초와 최종 SHA-256이 동일합니다. [최종 상태](EDITOR_FINAL_STATE.json), [세이브 보존](SAVE_HASH_VERIFICATION.json). 시작8cf50fd와 중간 사용자 커밋5bf2b99를 보존하고 이번 후속 변경만 별도 체크포인트에 기록합니다.
