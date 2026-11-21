# UI 상용 품질 검토

## 2026-11-21 무채색 UI 검토

HUD, 미션, 가방, 금융, 가챠, 설정을 실제 Game View에서 나란히 비교하고 여섯 화면의 공통 스타일을 채택했다. 월드에서는 위치와 현재 목표가 먼저 읽히고, 창에서는 각 기능의 핵심 정보가 먼저 보이는지를 기준으로 했다. [여섯 화면](../ArtSource/UI/Monochrome/Review/UIFamily.jpg) · [직전 UI와 비교](../ArtSource/UI/Monochrome/Review/BeforeAfter.jpg) · [Higgsfield와 Unity 비교](../ArtSource/UI/Monochrome/Review/MasterToGame.jpg)

| 검토 대상 | 채택 또는 수정한 이유 | 확인 자료 |
|---|---|---|
| HUD | 이안과 방의 색을 유지하고 시간·재화·생존 지표를 가장자리로 보냈다. 막대·아이콘·숫자를 함께 사용해 각 상태를 구별한다. | [HUD](../ArtSource/UI/Monochrome/GameView/HUD.png) |
| 미션·알림 | 현재 챕터와 조건·다음 행동을 나눴다. 급수대의 실제 상호작용으로 미션 완료 알림을 촬영했다. | [미션 완료](../ArtSource/UI/Monochrome/GameView/MissionComplete.png) · [재현 결과](../ArtSource/UI/Monochrome/Review/Milestone.json) |
| 가방·장비 | 검은 헤드폰에 회색 받침을 넣고, 선택과 장착을 각각 반전 면과 모서리 기호로 나타냈다. | [장착 상태](../ArtSource/UI/Monochrome/GameView/InventoryEquipped.png) |
| 가챠 | 단일 결과는 큰 표본, 10회는 읽을 수 있는 10개 결과를 표시한다. 첫 10회 결과에서는 마지막 열이 잘렸다. 실제 뷰포트 너비로 재배치하도록 수정하고 여섯 해상도에서 열 개 카드의 양쪽 끝이 모두 보이는지 검사했다. | [단일 결과](../ArtSource/UI/Monochrome/GameView/GachaResult.png) · [10회](../ArtSource/UI/Monochrome/GameView/GachaTen.png) |
| 금융 | 현금·남은 부채·순자산을 큰 숫자로 분리했다. 빈 고지서와 실제 발행 상태를 구분하고 납부 정책은 아래로 옮겼다. | [수정 전 읽기 순서](../ArtSource/UI/Monochrome/Review/FinanceBeforeReadingPass.png) · [최종 고지서](../ArtSource/UI/Monochrome/GameView/FinanceBill.png) |
| 설정·약관 | 선택된 탭과 키보드 포커스를 구분하고 값을 오른쪽에 정렬했다. 큰 글자와 안전영역을 함께 적용했다. 약관 및 확률 공시의 마지막 글자를 실제 스크롤로 확인했다. | [설정](../ArtSource/UI/Monochrome/GameView/SettingsNotchLarge.png) · [읽기 검사](../ArtSource/UI/Monochrome/Review/Reading.json) |

### 이번 빌드에서 확인한 항목

| 검사 | 범위와 결과 |
|---|---|
| 입력과 표기 | 정상·호버·누름·선택·비활성·포커스, 설정값 왕복, 받침·원화·숫자·▲▼, 다섯 등급 프레임을 확인했다. 핵심 화면과 해상도 검사를 포함한 43개 확인 항목을 통과했다. [결과](../ArtSource/UI/Monochrome/Review/Gate.json) |
| 해상도와 긴 글 | 40개 화면 상태 × 6개 크기에서 TMP 잘림 표시 0건. 공통 창의 적용 배율을 포함한 너비와 높이도 안전영역 부모 안에 들어왔다. 별도 검사에서는 가로·세로 모두 약관·확률표의 마지막 글자와 시뮬레이션한 노치·큰 글자 설정을 확인했다. [해상도별 화면](../ArtSource/UI/Monochrome/Review/Responsive.jpg) · [240개 측정](../ArtSource/UI/Monochrome/Review/Responsive.json) |
| 무채색 범위 | 1920×1080 Game View의 실제 창 안쪽 픽셀에서 RGB 최댓값과 최솟값 차이가 4 이하인지 측정했다. 미션·가방·금융·가챠·설정은 각 100%, 주택 92.061%, 식료품점 94.057%였다. 월드 배경과 투명한 HUD 전체는 이 측정에서 제외했다. [영역 좌표와 결과](../ArtSource/UI/Monochrome/Review/NeutralPixels.json) |
| 실제 거래·정산 | 장착 후 모서리 표식, 정상 속도 10회 뽑기의 표기와 티켓 차감, 오프라인 전투 2주기의 실제 정산·잔액과 재확인 후 중복 지급 여부를 확인했다. [검사](../ArtSource/UI/Monochrome/Review/Extra.json) |
| 금융 읽기 | 정상 급여와 세금 고지 주기로 미납 고지서를 만들었다. 발행 날짜와 납부 기한을 표시하며 여섯 해상도에서 맨 아래 납부 안내까지 읽을 수 있었다. [검사](../ArtSource/UI/Monochrome/Review/Statement.json) |

증권 화면은 23회 시세 갱신 후 실제 가격 이력과 차트 값을 대조하고 보유·뉴스·거래 내역 탭으로 이동했다. [상승·하락 표시](../ArtSource/UI/Monochrome/GameView/StockTrend.png) · [검사](../ArtSource/UI/Monochrome/Review/Market.json)

### 연속 Play Mode

실시간 30분 5초 동안 한 테스트 세이브로 집·상점·학교·회사·주방·금융을 돌았다. 기본 UI 명령으로 식료품 구매 17회, 장착 17회, 가챠 1회 뽑기 17회, 창이 열린 동안 이동 잠금 18회, 키보드 Back 3회를 확인했다. 검증 실패 0건이며 종료 후 저장 파일 대조가 일치했다. [실행 결과](../ArtSource/UI/Monochrome/Review/PlaySession.json) · [시간순 조작 기록](../ArtSource/UI/Monochrome/Review/PlayRoute.txt)

연속 실행은 성인 나이와 소지품을 준비하고 방 이동과 입력을 자동화했다. 실제 이동 변위가 기록된 구간은 14개다. 다른 Editor가 동시에 열린 구간에는 이동 입력 변위가 0인 경우가 있었다. 최종 금융 명세와 가챠 그리드는 별도 Play Mode에서 수정한 코드로 재검사했다.

검증 환경은 macOS, Unity 6000.6.8f1 Editor Play Mode, 격리한 프로젝트 사본과 테스트 저장 슬롯이다. 해상도와 노치는 Editor에서 시뮬레이션했다. 스마트폰·콘솔 실물 조작감, 실제 디스플레이의 명암, 외부 플레이어 사용성은 별도 검증 범위다. 최종 변경 후 원본 Editor의 컴파일이 끝났고 콘솔에서 C# 및 셰이더 컴파일 오류는 0건이었다. 임시 프로젝트 시작 시 Unity 검색 인덱스가 자체 예외를 기록했다.

## 2026-11-20 이전 판형과 상용 레퍼런스 기록

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

## 회전 중 화면 재검토

활성 창을 연 채 40개 화면 상태를 6개 Game View 해상도로 전환했다. 고정 크기 창이 열릴 때만 측정되어 가로에서 세로로 회전하면 가챠·장비 창이 작게 남는 현상을 찾았다. 활성 창이 안전영역 부모의 크기 변화를 감지하도록 고친 뒤 240개 조합에서 실제 렌더 크기와 TMP 잘림을 다시 검사했다. 학교에서 같은 입학 조건이 반복되던 버튼은 공통 조건을 한 번 표시하도록 정리했다. [해상도별 실행 결과](../ArtSource/UI/Review/Responsive.json) · [세로 화면 모음](../ArtSource/UI/Review/PhoneScreens.jpg) · [최종 8개 화면의 48개 조합 재검사](../ArtSource/UI/Review/ResponsiveCorrections.json)

TMP에서 잘림을 보고하지 않아도 마지막 줄까지 스크롤할 수 있는지를 따로 측정했다. 가로에서 세로로 바꾼 뒤 약관은 필요한 2,012.51 단위에 1,512.51만, 천장 고지는 필요한 612.34에 572.34만 스크롤할 수 있었다. ScrollRect의 본문 높이를 실제 줄바꿈에 맞춰 다시 계산하도록 Unity 레이아웃에 연결했다. 같은 조건에서 각각 2,044.51과 644.34로 늘어 마지막 줄까지 도달했다. [재현](../ArtSource/UI/Review/ReadingBefore.json) · [수정 후 측정](../ArtSource/UI/Review/ReadingAfter.json) · [휠 입력 후 마지막 글자 위치](../ArtSource/UI/Review/ReadingEnd.json)

## 최종 플레이 검증

| 검증 | 실제 결과 |
|---|---|
| 연속 플레이 | Unity Play Mode에서 30분 9.3초 동안 103개 구간을 진행했다. 가족 거실·주방, 편의점, 학교, 회사, 금융을 오갔다. 빵 구매와 현금 차감 17회, 헤드폰 착용·슬롯 대조 17회, 가챠권 차감·결과 열기 17회, 메뉴 중 이동 잠금 18회를 확인했다. [세션 결과](../ArtSource/UI/Review/PlaySession.json) · [구간별 기록](../ArtSource/UI/Review/PlayRoute.tsv) |
| 입력과 스크롤 | 39개 화면 상태에서 활성 버튼 254개에 포인터 진입·이탈 이벤트를 전달했다. 넘치는 세로 목록 16개 모두 휠 이벤트로 실제 위치가 바뀌었다. 버튼 누름·해제, Submit, 중첩 금융 창, Back 시 포커스 복귀와 월드 입력 복구를 따로 확인했다. [컨트롤 검사](../ArtSource/UI/Review/ControlCoverage.json) · [포커스와 눌림](../ArtSource/UI/Review/Navigation.json) |
| 미션·거래 | 회사 급수대에서 실제 음용 행동으로 미션이 완료되고 완료 알림이 보였다. 상점은 영수증에 비용과 잔액을 함께 표시한다. 증권의 세 보조 탭과 도시 복귀가 동작했다. [미션 완료](../ArtSource/UI/Review/Milestone.json) · [장소별 재검증](../ArtSource/UI/Review/WorldControls.json) |
| 오프라인 정산서 | 자동 전투를 정상 시작하고 오프라인 두 회차를 정산했다. 2승, +160G, +40 EXP가 서비스 기록과 일치했다. 결과 확인 후 잔액도 같았다. 자동 노동 상태를 직접 준비한 별도 시나리오에서는 근무가 중단되어 급여 지급까지 검증되지 않았다. [정산 화면](../ArtSource/UI/Review/OfflineSettled.png) · [재지급 검사](../ArtSource/UI/Review/OfflineUI.json) |
| 가챠 고지 | 긴 고지를 기본 확률·다음 1회·천장과 고지로 나눴다. 현재 연령과 보장 횟수의 실데이터, 15개 다음 추첨 확률 표시, 세 탭 스크롤 초기화, 원래 추첨 창 복귀를 확인했다. [확률 검사](../ArtSource/UI/Review/Probability.json) |
| 텍스트와 안전영역 | 1920×1080의 40개 화면 측정에서 TMP 잘림 플래그와 큰 텍스트 경계 초과가 모두 0이었다. 한글 받침, 원화, 음수, % 표본에 누락 글리프가 없었다. 78픽셀 좌우 인셋과 큰 글자 설정이 실제 레이아웃에 적용됐다. [화면별 측정](../ArtSource/UI/Review/ScreenMetrics.json) · [글리프](../ArtSource/UI/Review/KoreanGlyphs.json) · [안전영역](../ArtSource/UI/Review/Focused.json) |
| 저장과 컴파일 | 실제 사용자 저장과 분리한 슬롯에서 19세와 시작 금액·아이템만 준비했다. 영업시간을 고정하고 기존 구매·장착·추첨·체크포인트 명령을 사용했다. 장시간 실행과 최종 보완 검증 모두 사용자 저장 파일 일치, 최종 Editor 컴파일 오류 0. [최종 화면 검증](../ArtSource/UI/Review/FinalCoverage.json) |

내부 Game View 판정은 첫 다섯 화면(HUD·미션·인벤토리·금융·설정)과 가챠가 A다. 같은 종이 면과 제목 띠를 쓰면서 월드에서는 현재 목표가, 가방에서는 아이템이, 금융에서는 잔액이 가장 먼저 읽힌다. 상태·장비·상점·학교·회사·대화·증권은 집중 수정 후 화면별 동작을 다시 검증했다. 특히 가족 대화는 선택지 수에 맞춰 화면 아래에서 열리도록 줄였고, 증권 주문 화면에서는 입력을 받지 않는 하단 메뉴를 숨겼다. [최종 화면 모음](../ArtSource/UI/Review/Windows.jpg)

장시간 세션은 자동 입력을 사용했다. Editor에 키보드 포커스가 돌아온 구간에서 실제 이동 4회와 Escape 복귀 1회를 측정했다. 나머지 구간의 입력 잠금과 UI 명령은 활성 GameObject와 실제 서비스 상태로 확인했다. 가장 최근 대화·증권·가챠 고지 수정에는 별도 짧은 회귀 테스트를 적용했다.

긴 약관과 자동 진행 상세의 읽기 경험은 내부 B+로 남겼다. 마지막 줄까지의 스크롤과 실제 정산 수치·확인 동작은 검증했다. 작은 화면에서 장문을 읽는 속도와 피로도는 추가 사용자 관찰이 필요하다. Android·iOS·콘솔 실기기의 조작감과 사람이 직접 연속 플레이할 때의 피로도는 미검증이다. 별도 실행한 비교용 Editor의 에셋 검색 인덱스에서 시작 중 예외가 기록된 실행이 있었다. 해당 실행의 화면 검증과 저장 대조는 정상 종료했다.
