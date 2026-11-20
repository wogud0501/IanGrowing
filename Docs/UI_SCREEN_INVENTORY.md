# 이안 키우기 UI 화면 목록

공통 HUD 프리팹을 쓰는 씬, 씬에 저장된 창, 런타임에 만드는 창을 실제 소유자 기준으로 표기했다. 입력창·툴팁·필터·스크롤과 아이템 비교는 해당 화면 안의 구성 요소다. 우선순위 0은 첫 스타일 확정에 사용한 흐름이다.

| 화면 | 프리팹·씬 또는 런타임 객체 | 담당 스크립트 | 용도 | 검토한 문제 | 우선순위 | Higgsfield 시안 | 상태·Game View |
|---|---|---|---|---|---:|---|---|
| 타이틀 | MainMenu 씬 | MainMenuPresenter | 이어하기·새 인생·설정 | 소개 문구가 버튼 행을 침범 | P1 | [MainMenu-A](../ArtSource/UI/Concepts/MainMenu-A.png) | [최종](../ArtSource/UI/After/MainMenu.png) · [개편 전](../ArtSource/UI/Before/MainMenu.png) |
| 월드 HUD | 각 Room의 WorldHUD | WorldHUD · SurvivalMeters | 시간·장소·생존·현재 목표 | 상세 수치와 미션이 월드를 가림 | P0 | [HUD-Edit](../ArtSource/UI/Concepts/HUD-Edit.png) | [최종](../ArtSource/UI/After/HUD.png) · [개편 전](../ArtSource/UI/Before/HUD.png) |
| 상호작용·냉장고 | World object activities | WorldHUD | 재료 확인과 준비 | 이름과 행동을 연결할 시각 단서 부족 | P0 | [Shop-Refine](../ArtSource/UI/Concepts/Shop-Refine.png) | [최종](../ArtSource/UI/After/Fridge.png) · [개편 전](../ArtSource/UI/Before/Fridge.png) |
| 생활 메뉴 | Room → UIShell | GameplayUXPresenter | 생활·아이템·금융·시스템 이동 | 같은 비중의 기능 버튼 | P0 | [MainMenu-A](../ArtSource/UI/Concepts/MainMenu-A.png) | [최종](../ArtSource/UI/After/Hub.png) · [개편 전](../ArtSource/UI/Before/Hub.png) |
| 상태 | Room → UIShell | GameplayUXPresenter | 공식 이안과 8개 능력치·인생 | 수치가 여러 줄로 붙음 | P1 | [Status-Refine](../ArtSource/UI/Concepts/Status-Refine.png) | [최종](../ArtSource/UI/After/Status.png) · [개편 전](../ArtSource/UI/Before/Status.png) |
| 미션 | GameHUD · MissionHUD | MissionUI · MissionRecordLayout | 현재 조건과 보상, 실행·수령 | 행동과 보상의 위계가 약함 | P0 | [Mission-2](../ArtSource/UI/Concepts/Mission-2.png) | [최종](../ArtSource/UI/After/Mission.png) · [개편 전](../ArtSource/UI/Before/Mission.png) |
| 60개 미션 보관함 | MissionHUD · MissionRow | MissionUI · MissionRowView | 진행·완료·잠금 목록 | 긴 목록에서 현재 목표 찾기 | P0 | [Mission-2](../ArtSource/UI/Concepts/Mission-2.png) | 전용 캡처 미확보 |
| 미션 달성 | WorldHUD 알림 / MissionHUD | MissionUI · NotificationService | 달성과 보상 안내 | 피드백이 오래 남음 | P1 | [Mission-2](../ArtSource/UI/Concepts/Mission-2.png) | 전용 캡처 미확보 |
| 인벤토리 | Room → Item Inventory | CollectionUI | 필터·수량·NEW·선택·비교·사용 | 빈 셀과 아이템이 비슷함 | P0 | [Inventory-2](../ArtSource/UI/Concepts/Inventory-2.png) | [최종](../ArtSource/UI/After/InventoryStocked.png) |
| 빈 가방 | Item Inventory | CollectionUI | 첫 획득 이전 안내 | 선택할 내용이 없는 영역이 큼 | P0 | [Inventory-2](../ArtSource/UI/Concepts/Inventory-2.png) | [최종](../ArtSource/UI/After/Inventory.png) · [개편 전](../ArtSource/UI/Before/Inventory.png) |
| 장비 | Item Equipment | CollectionUI | 슬롯·착용품·효과·비교 | 착용한 사람과 슬롯 관계가 약함 | P1 | [Equipment-Refine](../ArtSource/UI/Concepts/Equipment-Refine.png) | [최종](../ArtSource/UI/After/Equipment.png) · [개편 전](../ArtSource/UI/Before/Equipment.png) |
| 가챠 | Item Gacha | CollectionUI | 풀·비용·천장·1/10회 추첨 | 보장 규칙이 본문에 섞임 | P1 | [Gacha-A](../ArtSource/UI/Concepts/Gacha-A.png) | [최종](../ArtSource/UI/After/Gacha.png) · [개편 전](../ArtSource/UI/Before/Gacha.png) |
| 가챠 결과 | Item Results | CollectionUI | 등급·NEW·중복 공개 | 결과와 보관 여부의 구분 | P1 | [Gacha-A](../ArtSource/UI/Concepts/Gacha-A.png) | [최종](../ArtSource/UI/After/GachaResult.png) |
| 확률 공시 | Item Probabilities | CollectionUI | 실제 풀의 확률과 규칙 열람 | 긴 내역과 창 제목의 구분 | P1 | [Gacha-B](../ArtSource/UI/Concepts/Gacha-B.png) | [최종](../ArtSource/UI/After/Probability.png) · [개편 전](../ArtSource/UI/Before/Probability.png) |
| 도감 | Item Collection | CollectionUI | 분류·발견 상태·소장품 상세 | 가방과 탭의 선택 표시가 약함 | P1 | [Inventory-2](../ArtSource/UI/Concepts/Inventory-2.png) | [최종](../ArtSource/UI/After/Collection.png) · [개편 전](../ArtSource/UI/Before/Collection.png) |
| 가계 명세서 | Economy management | FinancePresenter | 잔액·자산·부채·세금·상환 | 현금과 부채가 내역에 묻힘 | P0 | [Finance-Edit](../ArtSource/UI/Concepts/Finance-Edit.png) | [최종](../ArtSource/UI/After/Finance.png) · [개편 전](../ArtSource/UI/Before/Finance.png) |
| 증권 거래소 | StockMarket 씬 | FinancePresenter · PixelStockChart | 시세·보유·뉴스·내역·주문 | 첫날 차트가 비어 보임 | P1 | [Stock-Refine](../ArtSource/UI/Concepts/Stock-Refine.png) | [최종](../ArtSource/UI/After/Stock.png) · [개편 전](../ArtSource/UI/Before/Stock.png) |
| 취업·직업 상태 | 이안의 상태 → 직업 | GameplayUXPresenter | 직업·급여·경력·근무 기록 | 상세 기능으로 이동할 표지가 약함 | P1 | [Job-B](../ArtSource/UI/Concepts/Job-B.png) | [최종](../ArtSource/UI/After/Job.png) · [개편 전](../ArtSource/UI/Before/Job.png) |
| 현장 근무 | Room → Life Window | Phase23Presenter · LocationRecord | 지원·면접·업무·퇴사 | 회사에 공통 Home 제목과 활동이 노출 | P1 | [Job-B](../ArtSource/UI/Concepts/Job-B.png) | [최종](../ArtSource/UI/After/workplace-actions.png) |
| 학교·사회 활동 | MissionHUD → Education Overlay | MissionUI | 공부·과제·시험·친구 탭 | 장소 제한 설명이 개발 용어 | P1 | [Education-Refine](../ArtSource/UI/Concepts/Education-Refine.png) | [최종](../ArtSource/UI/After/Education.png) · [개편 전](../ArtSource/UI/Before/Education.png) |
| 학교 기록 | Room → Life Window | Phase23Presenter · LocationRecord | 교육 단계·준비도·성적·활동 | 학교와 관계 없는 공통 경력 설명 | P1 | [Education-Refine](../ArtSource/UI/Concepts/Education-Refine.png) | [최종](../ArtSource/UI/After/school-actions.png) |
| 도전과제 | Achievements screen | CityLifeHUD | 분류·진척·발견한 업적 | 달성 수와 목록 분리 필요 | P1 | [Achievement-B](../ArtSource/UI/Concepts/Achievement-B.png) | [최종](../ArtSource/UI/After/Achievement.png) · [개편 전](../ArtSource/UI/Before/Achievement.png) |
| 지도 | City map screen | CityLifeHUD | 장소·가능한 시설 행동 | 선택·제목 규칙 통일 | P2 | [MainMenu-A](../ArtSource/UI/Concepts/MainMenu-A.png) | 전용 캡처 미확보 |
| 도시 안내 | Guide screen | CityLifeHUD | 발견한 장소와 안내 | 기타 창과 안내 판형 차이 | P2 | [MainMenu-A](../ArtSource/UI/Concepts/MainMenu-A.png) | 전용 캡처 미확보 |
| 도움말 | 도움말 · 선택하는 생활 | GameplayUXPresenter | 첫 사용 설명·안내 재설정 | 설명과 하단 행동 구분 | P2 | [Settings-Edit](../ArtSource/UI/Concepts/Settings-Edit.png) | [최종](../ArtSource/UI/After/Guide.png) · [개편 전](../ArtSource/UI/Before/Guide.png) |
| 가족·주민 대화 | World object activities | WorldHUD | 발화자·상대·대화 선택 | 장소 카드와 화자 구분 | P1 | [Shop-Refine](../ArtSource/UI/Concepts/Shop-Refine.png) | 전용 캡처 미확보 |
| 식료품 계산대 | World object activities | WorldHUD | 가격·잔액·구매 | 현재 잔액과 거래 결과가 떨어져 있음 | P1 | [Shop-Refine](../ArtSource/UI/Concepts/Shop-Refine.png) | [최종](../ArtSource/UI/After/Shop.png) · [개편 전](../ArtSource/UI/Before/Shop.png) |
| 구매 영수증 | World object activities | WorldHUD | 구매 직후 실제 차감과 수량 | 창 안에서 거래 결과 확인 | P1 | [Shop-Refine](../ArtSource/UI/Concepts/Shop-Refine.png) | 전용 캡처 미확보 |
| 부동산 | Real estate agency | PropertyPresenter | 매물·계약·대출·시세·거래 | 계약·탐색이 같은 시각적 비중 | P2 | [Housing-B](../ArtSource/UI/Concepts/Housing-B.png) | [최종](../ArtSource/UI/After/Housing.png) · [개편 전](../ArtSource/UI/Before/Housing.png) |
| 가구·집 관리 | Home management | HomeManagementPresenter | 구매·배치·이사 | 집 정보와 가구 행동 구분 | P2 | [Housing-B](../ArtSource/UI/Concepts/Housing-B.png) | 전용 캡처 미확보 |
| 자동 활동 | Automatic activity settings | IdleActivityPresenter | 근무·공부·전투 시작·중단 | 현재 활동과 제어 구분 | P2 | [Job-B](../ArtSource/UI/Concepts/Job-B.png) | [최종](../ArtSource/UI/After/Idle.png) · [개편 전](../ArtSource/UI/Before/Idle.png) |
| 오프라인 정산 | Offline progress report | IdleActivityPresenter | 부재 중 기록과 결과 확인 | 빈 정산과 일반 로그 구분 | P2 | [Finance-Edit](../ArtSource/UI/Concepts/Finance-Edit.png) | [최종](../ArtSource/UI/After/Offline.png) · [개편 전](../ArtSource/UI/Before/Offline.png) |
| 알림 | World notification | NotificationService · WorldHUD | 아이템·보상·주의·거래 결과 | 알림과 배경 대비가 낮음 | P1 | [HUD-Edit](../ArtSource/UI/Concepts/HUD-Edit.png) | 전용 캡처 미확보 |
| 기록 | 최근 기록 | GameplayUXPresenter | 지난 알림 다시 읽기 | HUD에 모든 문구를 유지하기 어려움 | P2 | [Mission-2](../ArtSource/UI/Concepts/Mission-2.png) | [최종](../ArtSource/UI/After/History.png) · [개편 전](../ArtSource/UI/Before/History.png) |
| 저장 피드백 | GameHUD 상태 표시 | GameplayUXPresenter | 체크포인트 상태 | 저장 결과가 플레이를 방해할 수 있음 | P2 | [HUD-Edit](../ArtSource/UI/Concepts/HUD-Edit.png) | 전용 캡처 미확보 |
| 일시 정지 | Window / Pause | UIShell | 복귀·설정·새 인생·메뉴 | 포커스와 뒤로가기 구분 | P2 | [Settings-Edit](../ArtSource/UI/Concepts/Settings-Edit.png) | [최종](../ArtSource/UI/After/Pause.png) · [개편 전](../ArtSource/UI/Before/Pause.png) |
| 환경 설정 | 편의 설정 | GameplayUXPresenter.Settings | 게임·오디오·화면·입력·편의·약관 | 옵션이 긴 목록에 섞임 | P0 | [Settings-Edit](../ArtSource/UI/Concepts/Settings-Edit.png) | [최종](../ArtSource/UI/After/Settings.png) · [개편 전](../ArtSource/UI/Before/Settings.png) |
| 루비 | ReleaseUI → ShopPanel | ReleaseUI | 잔고·유료서비스 안내 | 결제 안내와 일반 자산 구분 | P2 | [Finance-Edit](../ArtSource/UI/Concepts/Finance-Edit.png) | [최종](../ArtSource/UI/After/Ruby.png) · [개편 전](../ArtSource/UI/Before/Ruby.png) |
| 약관 목록 | ReleaseUI → Legal | ReleaseUI | 정책·약관 선택 | 긴 목록의 입력 상태 | P2 | [Settings-Edit](../ArtSource/UI/Concepts/Settings-Edit.png) | [최종](../ArtSource/UI/After/Legal.png) · [개편 전](../ArtSource/UI/Before/Legal.png) |
| 약관 원문 | ReleaseUI → DocumentPanel | ReleaseUI | 정책 문서 스크롤 | 장문 읽기와 뒤로가기 | P2 | [Settings-Edit](../ArtSource/UI/Concepts/Settings-Edit.png) | 전용 캡처 미확보 |
| 중요 행동 확인 | 실행 확인 | GameplayUXPresenter | 취소·확인·원래 창 복귀 | 명령과 취소의 계층 | P1 | [Settings-Edit](../ArtSource/UI/Concepts/Settings-Edit.png) | [최종](../ArtSource/UI/After/Confirm.png) · [개편 전](../ArtSource/UI/Before/Confirm.png) |
| 출생 신고서 | BirthIntroductionPopup | BirthIntroductionPopup | 새 인생 초기 상태와 시작 | 공식 첫 화면과 나머지 판형 차이 | P2 | [MainMenu-A](../ArtSource/UI/Concepts/MainMenu-A.png) | 전용 캡처 미확보 |

가구 배치 자체는 월드 조작이다. 구매·가격 확인과 가구 선택은 집 관리 창에서 진행한다. 장비 비교는 가방의 상세 설명과 착용 슬롯을 함께 쓴다. 미션·가챠·저장 알림의 이력은 최근 기록에서 확인할 수 있다.
