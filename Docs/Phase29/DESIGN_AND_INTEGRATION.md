# Phase 2.9 설계·기존 시스템 연결

사용자가 확정 수치가 없다고 확인하고 설계를 위임했으므로, 아래 값은 이번 Phase에서 정한 데이터입니다. 원본 기획에 있었다고 주장하지 않습니다.

## 확률·비용·천장

| 등급 | 기본 확률 | 분해 조각 (단위 가치1) |
|---|---:|---:|
| 일반 | 60% | 2 |
| 고급 | 25% | 5 |
| 희귀 | 10% | 12 |
| 영웅 | 4% | 30 |
| 전설 | 1% | 100 |

등급 내 PoolItem 가중치를 추가 적용합니다. 일반 등급 생수 캔은 가중치3, 다른 아이템은1입니다. UI는 GachaService.Probabilities가 반환한 실제 개별 확률을 표시하며, 다음 회차 천장 반영 확률도 같은 함수에서 계산합니다.

- 1회100G, 10회1000G. 보유 가챠권을 먼저 쓰고 부족분만 기존 EconomySystem으로 결제합니다.
- 분해 조각30개로1회 재추첨. 마지막 한 개 보호 기본값 true. 장착 중인 아이템은 중복분도 해제 전 분해하지 않습니다.
- 50회: 영웅 이상 /100회: 전설 /300회: 지정 전설 헤드폰 ‘세상 음소거’.
- 세 주기는 각각 누적·반복·리셋되며 자연 고등급 당첨으로 기본 주기를 초기화하지 않습니다.
- 300회에 세 구간이 겹치면 가장 강한 지정 결과 한 개만 지급하고 세 규칙 모두 처리 영수증을 기록합니다.
- Item/Rarity/Pool/PityRule 데이터는 Assets/Resources/IanCollection.asset에 저장합니다.

## 기존 구현 → 변경

| 원래 소유자 | 재사용 및 확장 |
|---|---|
| Phase23Services owned/equipped | 스타터 호환 정보 보존, 정식 ItemService로 보유/장착 조회·실행 위임 |
| MissionRewardService pendingRewards | 가챠권을 Inventory 가챠권으로 실제 전달, 기존 receipt/delivered 유지 |
| Mission024/025 | 실제 Equipment 슬롯 장착 성공 이벤트만 기록, 획득만으로 완료하지 않음 |
| Mission020 | 기존 보상 청구와 스타터 사용 권한을 실제 스타터 각1개로 연결 |
| EconomySystem | 원본 인게임 현금 비용 지출·거래 기록 그대로 사용 |
| PlayerStats | 저장되는 원본 수치 유지, 장착 효과를 별도 파생 값으로 계산 |
| ActionSystem / WorkActivityService | 데이터 공부 효율 / 급여 효율 사용 |
| IdleActivityService / CombatService | 자동 주기·전투 공격·방어·전투 보상 확장 연결 |
| CityLifeService | 수집 도전과제11종, 기존 Guide Seen에 최초 안내 기록 |
| UIShell / UINavigationService / WorldMobileHUD | 기존 입력 스택·키보드/터치·Safe Area·픽셀 패널·폰트 유지 |
| SaveManager version4 | 선택적 items 블록, 이전 필드가 없는 저장도 허용 |

## 하나의 상태와 거래

GameManager에 ItemService 모듈 하나를 연결합니다. InventoryService, EquipmentService, GachaService는 동일한 ItemProgressSnapshot을 조회하는 facade입니다. ItemDefinition은 하나의 ScriptableObject 카탈로그 안의 공유 정의이며, 보유 개수마다 정의를 복제하지 않습니다.

실행 단계 권한·비용 검사 → 비용 차감 → 서비스 추첨 → 수량/발견/천장/미션/업적 반영 → 기존 SaveManager 원자적 파일 저장 → 결과 이벤트/연출 순서입니다. 저장 실패 시 이전 GameManager 상태로 비용·아이템·천장·미션을 함께 롤백합니다. 결과 연출은 이미 확정된 결과만 표시하고, Skip/화면 종료는 재추첨하지 않습니다. 동일 transactionId는 저장 후에도 다시 처리하지 않습니다.

장비 효과는 아이템 슬롯으로 출처를 관리합니다. 해제·교체 시 해당 슬롯 효과만 재계산하고 기본 능력치를 덮어쓰지 않습니다. 저장에는 원본 능력치와 슬롯 ID가 들어가므로 반복 로드로 버프가 누적되지 않습니다.

## 저장 항목

- inventory: itemId, count, isNew, acquired, firstDay. count0이 되어도 발견 이력은 남습니다.
- equipment: Headphone / Equipment / Cosmetic 슬롯과 itemId.
- pools: poolId, total, 규칙별 progress/reached/applied.
- receipts: 결제·획득·분해·전달 등의 중복 처리 방지.
- unlocked 및 기존 world.cityLife: 도전과제·진행도·Guide Seen.
- 첫 획득 시각을 복구할 근거가 없는 구형 아이템은 ‘이전 플레이 기록’으로 표시합니다. 임의 과거 날짜를 생성하지 않습니다.

## 이벤트 및 후속 연결

ItemAcquired / NewItemDiscovered / ItemEquipped / ItemUnequipped / GachaPulled / GachaMultiPulled / DuplicateAcquired / ItemDismantled / PityReached / CollectionUpdated 및 AppearanceChanged / AudioRequested를 제공합니다.

ItemAppearanceController는 기존 성장 단계·방향·행동·프레임에 대응하는 ItemAppearanceFrame을 읽습니다. 원본 캐릭터 Sprite/Animator를 다시 제어하지 않고, 실제로 등록된 착용 Sprite만 레이어로 렌더링합니다. 현재 신규 착용 프레임은 비어 있으므로 각 희귀 헤드폰의 고유 외형이나 신규 의상을 제작 완료로 판정하지 않습니다. 기본 복장 선택은 공식 이안 디자인을 유지합니다. 신규 오디오 파일도 없으며 사운드는 hook까지만 제공합니다.

UI 전용 원본 픽셀 기호는 CollectionIconBuilder로 제작했습니다. 착용 아트를 UI 아이콘으로 대신하지 않습니다. 캐릭터·장비별 방향 원화와 사운드를 추가하면 기존 hook에 연결할 수 있습니다.

## 변경 범위 보호

Bootstrap과 기존16개 방 Scene에 모듈/화면 어댑터/외형 어댑터를 연결했습니다. Scene 삭제·통합·새 도시/NPC/미니맵 재작성·주택 재설계·실제 현금 결제를 구현하지 않았습니다. 폴더블/모바일 실제 기기 성능과 개별 착용 아트는 별도 검증 대상입니다.
