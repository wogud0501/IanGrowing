# Ruby & Purchase Foundation

2026-11-17. 실제 Store 구매·현금 결제·영수증 검증·환불 실행은 미연결입니다.

- 기존 EconomySystem이 RubyWallet을 하나 소유합니다. SaveData.economy.ruby는 선택 블록이며 Save version4 유지. 기존 필드/돈/아이템을 초기화하지 않습니다.
- freeRuby/paidRuby/totalRuby, walletVersion, 원장 및 Store 처리 ID를 저장합니다. 거래 ID·출처·UTC·관련 ID·무료/유료 실제 증감을 기록하고 중복 ID는 거절합니다. 만료/환전/타 앱 이전 없음.
- 기본 소비 FreeFirst, 설정에서 PaidFirst 가능. purchased/consumed/remaining 유료분 집계 제공. 상세 환불 회수·구매별 소진 FIFO·서버 복구는 향후 구현 대상입니다.
- 새 인생에서도 원장 보존: 마일스톤 ID는 runId와 분리하여 반복 새 게임으로 최초 보상을 파밍하지 못하게 합니다. Ruby 필드가 없는 구 Save는 초기 실행 시 0으로 시작하며 실행 중 보유한 원장을 지우지 않습니다.
- 기존 Gold 가챠 Pool의 비용·확률은 유지. Pool.costCurrency를 Ruby로 선택할 수 있으며 실제 ItemService 트랜잭션 안에서 비용→추첨→아이템→저장이 실행됩니다. 실패 시 기존 전체 상태 복원 경로를 이용합니다. 새로운 유료 가챠 배너를 임의 활성화하지 않았습니다.

## 무료 Ruby 데이터

`Assets/Resources/IanRelease.asset`에서 조정합니다. 일반 미션/반복 행동의 Ruby는0입니다.

| 실제 미션 ID | 현재 제목 | Ruby |
|---|---|---:|
| M010 | 유아기 졸업 | 2 |
| M020 | 12세 달성 (기존 targetAge13 유지) | 3 |
| M030 | 청소년기 졸업 | 3 |
| M040 | 청년기 졸업 | 3 |
| M050 | 중년기 졸업 | 3 |
| M060 | 생애 평가 완료 | 5 |

미션 총19. 도전과제 `move`(첫 이사)1, `item/collection10`(아이템10종)1. 현재 신규 전체 최초 보상 합계21. 기존 완료 미션에 소급 지급하지 않습니다. 향후 특별 이벤트/보상은 별도 승인 데이터 경로로 구현해야 하며 반복 노동/전투/날짜 경과에는 연결하지 않았습니다.

## 결제 구조

IPurchaseProvider: Initialize / QueryProducts / Purchase / Restore / IsAvailable / IsMock.
PurchaseResult: Success, Cancelled, Pending, Failed, NotSupported.
PurchaseReceipt→IPurchaseReceiptValidator→검증된 Store/상품/거래 ID 확인→중복 검사→Ruby 원장→저장 성공 후 확정. 저장 실패는 rollback, 공급자 영수증 재전달 시 reconciliation을 다시 요청할 수 있습니다. 기본 공급자는 Unavailable이며 fake success를 반환하지 않습니다.

MockPurchaseProvider 클래스는 UNITY_EDITOR/DEVELOPMENT_BUILD에서만 컴파일됩니다. UI에 DEV 지급 버튼은 만들지 않았고 기본 공급자로 자동 선택하지 않습니다. 실제 결제가 없으므로 상품 카드·가격·구매 버튼을 숨기고 '구매 기능 준비 중'과 정책을 표시합니다.

상품 준비 데이터: ruby_pack_small100 / medium300 / large1000. 수량은 초안 데이터, 모두 enabled=false, Apple/Google ID 공란. Store가 반환할 localizedPrice 필드만 준비하며 금액/할인 문자열은 코드에 넣지 않습니다. 첫 구매 보너스와 무료 보너스도 데이터로만 준비합니다.

## 후속 Store 연결

1. 실제 Bundle ID·Store Product ID와 상품량 확정. Apple/Google Provider 구현.
2. 서버 권위 지갑과 검증 API, Store transaction 검증·재전달·환불/취소 알림 구현.
3. pending/cancelled/failure/retry·동시 전달·저장 실패·앱 종료 경계 테스트.
4. 비소모품 복원과 Ruby 같은 소모성 재화의 서버 재동기화를 구분. Store Restore만으로 사용한 소모성 재화를 재지급하지 않음.
5. 실제 현지화 가격과 필수 구매 고지·미성년자 고지를 표시한 뒤 구매 버튼 활성화.
6. 정책·Privacy·Data Safety 및 실제 기기 sandbox 검증 갱신.

로컬 원장만으로 Save 파일의 전체 rollback이나 변조를 완전히 막을 수 없습니다. 서버 검증을 구현했다고 보고하지 않습니다. 실제 유료 판매 전 서버/복구 구조가 필수 후속 작업입니다.
