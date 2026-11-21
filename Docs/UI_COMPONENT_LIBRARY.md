# 이안 키우기 UI 구성 요소

| 구성 요소 | 구현 | 사용 규칙 |
|---|---|---|
| 창·모달·입력 | UIShell, UINavigationService, RecordWindowBounds, UISafeLayout | 최상위 창이 입력을 받는다. Back 시 이전 포커스를 복구한다. 중앙 창은 폭을 제한하고 상황 창은 필요한 선택지만큼 자란다. |
| 픽셀 면과 삽화 | LifeRecordUI, IanLedger/Surfaces, Prints, SectionIcons | 공통 18종 9-slice 면을 캐시한다. 제목·본문·인셋은 깊이와 선 두께를 달리한다. 삽화는 입력을 가로채지 않는다. |
| 버튼·탭·포커스 | RecordButtonInk, Unity Selectable, PixelUXWidgets | 기본·호버·누름·현재 선택·비활성·포커스에 각각 면과 글자 대비를 지정한다. 현재 탭에 포커스가 오면 반전과 추가 테두리를 같이 쓴다. |
| 프리팹과 런타임 창 | RecordPage, LifeRecordUI | 기존 직렬화 리스너와 명령은 유지한다. 만들어진 창을 열 때 공통 버튼·본문·입력필드 잉크를 적용한다. |
| 읽기·툴팁 | Galmuri TMP, UITypeScale, PixelTooltip, ScrollRect | 스크롤 가능한 장문과 고정 행동을 나눈다. 키보드와 포인터는 툴팁, 터치는 선택된 항목의 상세를 이용한다. |
| HUD·알림 | WorldHUD, SurvivalMeters, NotificationService | 허기·갈증·체력·멘탈을 흰 막대, 아이콘, 수치로 표시한다. 낮은 값은 느낌표, 직전 증감은 부호와 화살표를 쓴다. 완료 알림은 짧은 선으로 강조한다. |
| 미션 | MissionUI, MissionRecordLayout | 실제 진행 서비스가 선택한 미션, 현재 챕터 달성 수·보상·다음 조건을 보여준다. 미션 명령은 기존 플레이 흐름에 연결한다. |
| 가방·장비·도감 | CollectionUI, InventorySpecimen, EquippedMark | 필터·정렬·빈 상태·선택 상세·비교를 공유한다. 장착 여부와 수량은 현재 장비·인벤토리에서 읽는다. 표본 재질은 UI 마스킹을 지원한다. |
| 가챠 | CollectionUI, PresentationFeedback | 1회·10회 결과는 같은 표본과 등급 프레임을 쓴다. 등급명과 NEW/DUPLICATE를 결과마다 붙인다. 풀과 보장 수치는 기존 서비스가 공급한다. |
| 금융·증권 | FinancePresenter, PixelStockChart | 현금과 부채·순자산을 분리한다. 흰 실선은 상승, 점선은 하락이다. 실제 가격 관측치만 그린다. |
| 취업·학교·대화·상점 | GameplayUXPresenter, LocationRecord, WorldHUD | 실제 시급, 교육 상태, 대화 선택지, 구매 가격·잔액을 해당 장소에서 표시한다. 유저 명령은 기존 게임 서비스에 전달한다. |
| 설정·계산 결과 | GameplayUXPresenter.Settings, IdleActivityPresenter | 설정값은 행 오른쪽에서 읽는다. 오프라인 결과는 실제 경과·Gold·EXP·활동 내역을 보여주며 확인은 기존 정산 확인 명령이다. |

[화면별 소유자·게임 캡처](UI_SCREEN_INVENTORY.md) · [상태별 입력 검사](../ArtSource/UI/Monochrome/Review/Gate.json) · [픽셀 자산과 재질](../Assets/_Game/UI/Pixel/Resources/IanLedger)
