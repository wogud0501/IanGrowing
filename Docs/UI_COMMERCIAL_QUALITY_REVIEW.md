# UI 상용 품질 검토

판정 기준은 플레이어가 먼저 읽어야 할 정보, 현재 선택, 실행 후 결과가 Game View에서 식별되는지다. 빈 저장과 아이템을 채운 저장을 따로 촬영했다. 렌더링된 한글과 실제 프리팹·버튼의 동작을 함께 검토했다. [개편 전후](../ArtSource/UI/Review/BeforeAfter.jpg) · [핵심 화면 다섯 개](../ArtSource/UI/Review/UIFamily.jpg) · [시안과 Game View](../ArtSource/UI/Review/MasterToGame.jpg)

## 상용 게임에서 확인한 설계 원칙

공식 배포 페이지의 게임 화면에서 읽을 순서와 배치를 비교했다. 아래의 적용 판단은 이 프로젝트의 디자인 결정이다. 스크린샷에서 전환 시간을 측정하지는 않았다.

| 참조 | 이안 키우기에 적용한 판단 |
|---|---|
| [Persona 5 Royal](https://store.steampowered.com/app/1687950/) | 제목 띠와 현재 선택을 먼저 읽게 하고, 동일 화면의 보조 버튼은 다음 순위에 둔다. |
| [Hades](https://www.supergiantgames.com/games/hades/) | 창의 테두리, 삽화, 글자 여백을 한 구성으로 보고 긴 설명은 별도 읽기 영역에 둔다. |
| [Dave the Diver](https://store.steampowered.com/app/1868140/) | 평소에는 픽셀 월드와 주변 행동이 보이고, 가방과 금융은 필요할 때 한 단계 더 들어간다. |
| [Cult of the Lamb](https://store.steampowered.com/app/1313140/) | 기능별 심벌을 창 제목·메뉴·기록에서 같은 잉크와 픽셀 선으로 연결한다. |
| [Stardew Valley](https://store.steampowered.com/app/413150/) | 물건 자체와 수량이 슬롯의 첫 정보다. 비교와 사용은 선택 상세에서 이어진다. |
| [VA-11 Hall-A](https://store.steampowered.com/app/447530/) | 금융 화면에서 종목, 현재가, 주문과 로그를 서로 다른 영역에 둔다. |
| [NEEDY STREAMER OVERLOAD](https://store.steampowered.com/app/1451940/) | 공통 창 안에서 목적별 화면을 나누고 한 번에 입력을 받는 최상위 창을 유지한다. |
| [Coffee Talk](https://store.steampowered.com/app/914800/) | 화자와 실제 선택지를 나란히 배치하고 월드의 색과 조명을 가리지 않는 대화 진입점을 둔다. |
| [Balatro](https://store.steampowered.com/app/2379780/) | 비용, 횟수와 결과가 한눈에 대응하도록 가챠 보장 수치와 결제를 배치한다. |

## 반복 검토

| Game View에서 발견한 문제 | 수정과 재검토 증거 |
|---|---|
| 금융의 현금·부채가 긴 장부 안에 묻힘. 첫 구현의 새 표면이 표시되지 않음 | 비활성 창에서도 ScrollRect를 찾도록 고치고, 현금과 순자산·부채를 실제 모델에 바인딩했다. [금융](../ArtSource/UI/After/Finance.png) |
| 세로 화면에서 미션 티켓이 오른쪽 위로 밀림. 모달 뒤에 HUD와 알림이 읽힘 | 안전영역과 창 소유자를 명시하고 HUD·알림의 표시를 스택과 연결했다. [해상도별 Game View](../ArtSource/UI/Review/ResolutionReview.jpg) |
| 상태 수치의 우선순위와 착용자의 구분이 약함 | 초상 옆에 능력치 여덟 개와 게이지를 두고 장비 화면은 실제 이안과 슬롯별 착용품을 표시한다. [상태](../ArtSource/UI/After/Status.png) · [장비](../ArtSource/UI/After/Equipment.png) |
| 가챠 보장 조건이 본문에 붙어 있고 결과만 봐서는 소모를 추적하기 어려움 | 풀에 설정된 규칙별 현재 횟수·기준·적용 횟수를 분리했다. Play Mode에서 1회 뽑기의 가챠권 차감과 결과 창을 확인했다. [가챠](../ArtSource/UI/After/Gacha.png) |
| 상점에서 결제한 뒤 플레이어가 닫기 전에는 영수증이 보이지 않음 | 현재 상호작용 오브젝트의 스프라이트와 최신 잔액, 구매 수량, 비용을 계산대에 함께 표시했다. [영수증](../ArtSource/UI/After/ShopReceipt.png) |
| 첫날의 주가 그래프가 빈 패널처럼 보임 | 실제 첫 시세 한 점과 기록 상태를 표시한다. 뒤이은 시세가 두 개부터 선을 그린다. [증권](../ArtSource/UI/After/Stock.png) |
| 학교와 회사의 공통 창에 Home 제목과 부적절한 행동이 남음. 첫 재배치에서는 레이아웃 그룹이 새 창 높이를 12로 줄임 | 현재 장소와 카탈로그를 읽는 전용 기록을 만들고 기존 명령으로 연결했다. 바깥 레이아웃 그룹의 크기 계산에서 기록 창을 제외했다. [학교](../ArtSource/UI/After/school-actions.png) · [회사](../ArtSource/UI/After/workplace-actions.png) |

## 판정 범위

내부 시각 판정에서 A는 해당 해상도의 주요 정보·행동·결과를 짧은 시간 안에 구별하고, 제목·선택·뒤로가기·글자·아이콘이 공통 체계를 따르는 상태다. 실제 판매 여부나 외부 평가단 점수를 뜻하지 않는다. 실제 기기 가독성, 엄지 도달 범위, 컨트롤러 조작성은 해당 기기에서 별도로 평가해야 한다.

| 항목 | 판정 근거 |
|---|---|
| HUD·미션 | 월드 위에 네 수치와 한 목표를 남겼고, 챕터·조건·보상·행동은 미션 창에서 확인한다. |
| 가방·장비·가챠 | 빈 상태, 소지 수량, 선택 상세, 착용 슬롯, 추첨 비용, 보장 횟수 및 결과가 같은 판형에서 구분된다. |
| 금융·상점 | 잔액, 부채, 구매 결과에 실제 거래 금액과 단위를 표시한다. |
| 설정·입력 | 설정 분류와 포커스를 유지하고 ESC·뒤로가기·선택·비활성 상태를 공통 컴포넌트에서 처리한다. |
| 폼 팩터 | Unity Game View의 1920×1080, 2560×1600, 2340×1080, 2048×1536, 2208×1768, 1080×1920 캡처를 비교했다. |

플레이 기록과 추가 레이아웃 검사 결과는 아래 실행 결과에 기록한다. 사람의 30분 사용성 인터뷰와 Android·iOS·콘솔 실기기 검증은 이번 Editor 세션의 측정 범위 밖이다.
