# Phase 2.9 — 가챠·인벤토리·장비·헤드폰·도감 결과

2026-11-16 · Unity 6000.6.8f1 · /Users/hyunwol/IanGrowing

판정: **핵심 수집 시스템과 실제 Play Mode 루프 구현·검증 완료. 개별 신규 착용 아트, 신규 음향, 실제 모바일 기기 검증은 미완료.** 이를 포함해 모든 아트·출시 품질이 완료됐다고 판정하지 않습니다.

## 실제 구현

- 가챠 → 기존 현금/가챠권 차감 → 실제 확률 추첨 → 수량/발견/NEW → 슬롯 장착 → 파생 능력치 → 미션024/025 및 도전과제 → 중복 분해 → 조각 재뽑기 → 독립 천장 → 저장을 연결했습니다.
- ItemService 하나가 상태와 거래를 소유하며 InventoryService/EquipmentService/GachaService는 같은 모델의 facade입니다. 별도 지갑, 중복 전역 Manager, 임시 완료 bool을 추가하지 않았습니다.
- ScriptableObject 카탈로그의 수집15종(헤드폰6·장비5·음료3·기본복장1), 가챠권·조각2종. 슬롯은 Headphone/Equipment/Cosmetic입니다.
- 1회/10회, 비용 견적, 가챠권 우선, 부족 시 추첨 전 거절, 실제 개별 확률 및 다음 천장 적용 확률 표시.
- 50 영웅 이상 /100 전설 /300 지정 전설 헤드폰. 세 규칙의 주기와 지급 이력이 독립적으로 저장되며 겹친 보장은 결과 하나에 적용합니다.
- 중복 수량, 장착 중 분해 금지, 마지막 한 개 보호, 등급별 조각 지급, 발견 이력 유지, 처음 확인 시 NEW 해제.
- 음료는 실제 상태 회복 소비형입니다. 일시 버프 음료는 이번 데이터에서 사용하지 않았습니다.
- 장비의 기본 능력치, 공부/노동 효율, 자동 주기, 전투 공격·방어·전투 보상 확장 지점을 연결했습니다. 원본 능력치를 저장하고 슬롯 효과를 별도 계산해 해제·반복 로드에 누적되지 않게 했습니다.
- 기존 CityLife 도전과제11종과 Guide Seen을 재사용했습니다. 강제 튜토리얼·퀘스트 잠금·추가 보상 지갑은 없습니다.
- 원자적 저장이 실패하면 비용·아이템·천장·미션을 복구합니다. 가챠 화면에 취소 안내를 유지하고 조작권을 정상 반환합니다.

확률60/25/10/4/1%, 현금100G/조각30, 천장 보장은 사용자가 설계를 위임해 이번에 정한 값입니다. 원본 기획에 있었다고 주장하지 않습니다. 자세한 규칙은 [설계·연결 문서](DESIGN_AND_INTEGRATION.md)에 정리했습니다.

## 기존 임시 기능과 저장 호환

기존 Phase23 스타터 보유·장착 정보와 완료 미션을 삭제하지 않았습니다. 기존 Mission020 사용 권한은 실제 스타터 각1개와 연결해 미션024/025가 확률에 막히지 않게 했습니다. 미션 보상 대기열의 가챠권은 전달 영수증을 유지하며 실제 Inventory 수량으로 이전합니다.

장착은 실제 소유 아이템을 슬롯에 배치한 성공 이벤트에서만 미션 Counter를 증가시킵니다. 획득만으로 완료하지 않습니다. 기존 24/25 행동 진입도 같은 장비 경로를 호출합니다. 기존 장비 해금 기준13세를 유지하며, 걷기1세/독립 외출7세/성인·주거 조건을 변경하지 않았습니다.

SaveData version4에 선택적 items 블록을 추가했습니다. 보유 수량·NEW·최초 획득·발견·슬롯·Pool 누적·각 천장 progress/reached/applied·거래 영수증·해금이 저장됩니다. 업적·가이드는 기존 world.cityLife에 저장됩니다. 필드가 없는 저장은 스타터 정보만 안전하게 이전하며, 원래 획득 시각을 모르는 경우 ‘이전 플레이 기록’으로 표시합니다.

## 실제 실행 검증

| 단위 | 결과와 근거 |
|---|---|
| 핵심 A~G | PASS · [91개 검사](ROOMVISUALITEMSCORE_RESULTS.json). 실제 비용을 지불하며 누적300회까지 실행 |
| 천장 | PASS · 50주기6회/100주기3회/300주기1회, 겹친 회차 아이템1개 · [실행 상태](PITY_EXECUTION.json) |
| UI 실제 버튼·연출 | PASS · [27개 검사](ROOMVISUALITEMSUI_RESULTS.json), 글자 넘침0 |
| 실제 UI Raycast·스크롤·부족 잔액 가격 | PASS · [9개 검사](UI_SCROLL_RESULTS.json) |
| 저장 실패 화면·복구 | PASS · [8개 검사](UI_SAVE_FAILURE_RESULTS.json) |
| Developer QA Center | PASS · [6개 시나리오/34개 내부 검사](QA_REGISTRY_RESULTS.json), [래퍼15개](ROOMVISUALITEMSREGISTRY_RESULTS.json) |
| Unity PlayMode Framework | PASS · [3개 테스트](UNITY_FRAMEWORK.json): 실제 난수 가중 샘플링, 장비 반복 효과, 중복 슬롯 저장 거절 |
| 기존 문·Scene 연결 | PASS · [158개 검사](ROOMVISUALDOORS_RESULTS.json) |
| 생활·가구·디스크 복원·가족·모바일 레이아웃 | PASS · [110개 검사](SCENESTATE_RESULTS.json) |
| 기존 회사 근무 | PASS · [10개 검사](FACILITYCOMPANYSHIFT_RESULTS.json) |
| 원본 플레이 세이브 | PASS · [시작 SHA-256와 동일](SAVE_HASH_VERIFICATION.json), 3개 파일 |
| Android 개발 빌드 | PASS · ARM64/IL2CPP, 오류0/경고41 · [최종 BuildReport](ANDROID_BUILD.txt). 실제 기기 실행/성능과 구분 |
| iOS 네이티브 빌드 | BLOCKED · iPhoneOS SDK 없음 |
| 실제 Android/iOS·폴더블 기기 | MANUAL · 미검증 |

새 UI는1280×720/1920×1080/904×2316 모바일 시뮬레이션과 큰 글씨115%에서 검사했습니다. 기존 모바일 회귀에는 회전·접힘/펼침 화면 크기 및 Safe Area 시뮬레이션, 가상 버튼/조이스틱 입력 경로가 포함됩니다. 실제 기기, 물리 키보드/게임패드 전체 조작, 앱 백그라운드 전환과 메모리·프레임 성능은 미검증입니다.

연령·선행 미션은 테스트용 fixture로 준비했고, 장착·결제·미션 완료·보상·저장은 실제 서비스로 실행했습니다. 미션001~023을 정상 속도로 모두 플레이한 결과라고 주장하지 않습니다. 일부 뽑기는 NEW/중복/천장 재현용 QA 난수 주입을 사용했으며, 주입은 QA 슬롯에서만 허용되고 Release 코드에서는 제거됩니다. 별도 Framework 검사는 실제 서비스 난수 샘플러를10,000회 실행했습니다. 테스트를 위해 모든 아이템의 확률을 동일하게 바꾸지 않았습니다.

## 발견하고 해결한 항목

- QA fixture가 이미 자동 완료된 M020을 다시 삽입해 저장 검증기가 거절함: fixture 수정, 정상 보상 경로 재검증.
- 확률 스크롤 텍스트 높이·10회 DUPLICATE 배지 넘침: 활성 화면 기준 높이 계산 및 카드 줄 구조 수정.
- 긴 확률 화면의 빈 영역이 포인터 스크롤 입력을 받지 못함: 투명 viewport Raycast 대상 추가, 실제 GraphicRaycaster→ScrollRect 이벤트 경로 검증.
- 저장 실패로 화면이 닫힌 뒤 취소 이유가 사라질 수 있음: 가챠 화면에 오류 안내를 유지하며 미확정 결과를 표시하지 않게 함.
- 잔액 부족 시 가격이 사라지던 UI: 실제 견적은 유지하고 실행 버튼만 비활성화.
- UI 조회 때 전체 가챠 이력까지 JSON 복제하던 경로: 필요한 Inventory/Equipment/Pool 정보만 복사하도록 축소.
- 기존 모바일 회귀의 조이스틱 준비 시점 예외: 상태를 강제로 바꾸지 않는 최대2초 준비 대기와 명시적 검사 추가. 단독50개와 전체 회귀110개가 통과했으며 생산용 입력 코드는 재작성하지 않음.

최초 Android 빌드에는 경고1229건, 최종 증분 빌드에는41건이 보고됐습니다. 캐시/빌드 단계 차이이므로 프로젝트 전체 경고를 해결했다고 주장하지 않습니다. [최종 경고](ANDROID_WARNINGS.txt)를 별도 보관했습니다.

실패 근거는 [Failures](Failures)에 보존했습니다. FAILED_ITEM_TRANSACTION.json은 저장 실패 직전 QA 상태의 진단 자료이며, 실제 플레이 세이브가 아닙니다.

## 실제 화면

- [가챠](Screenshots/Gacha.png) / [1회 NEW](Screenshots/Gacha-One.png) / [10회 중복 검사](Screenshots/Gacha-Ten.png)
- [인벤토리](Screenshots/inventory-1280.png) / [장비](Screenshots/equipment-1280.png) / [도감](Screenshots/collection-1280.png)
- [세로 화면](Screenshots/inventory-904.png) / [큰 글씨](Screenshots/Inventory-Large.png)
- [확률](Screenshots/Probabilities.png) / [실제 스크롤 후](Screenshots/Probabilities-Scrolled.png) / [저장 실패 복구](Screenshots/Save-Failure-Recovered.png)

10회 스크린샷은 중복 판정을 반복 검증하기 위한 고정 QA 입력 결과입니다. 실제 Pool이 그 아이템만 지급한다는 의미가 아닙니다.

## 변경·저장 범위

Bootstrap의 기존 GameManager와 기존16개 방 Scene에 모듈/화면/외형 어댑터를 저장했습니다. 신규 플레이 Scene·Prefab은 없으며 기존 문, NPC 이동, 도시 미니맵, 주거 배치를 재작성하지 않았습니다. [변경 경로](CHANGED_FILES.txt)와 [진행 기록](../PHASE2_9_PROGRESS.md)을 참고하세요.

원본 캐릭터/성장 Sprite와 Animator는 보존했습니다. Headphone/Costume appearanceId 및 성장·방향·동작별 레이어 적용 어댑터는 존재하나, 신규 착용 원화 프레임은 아직 비어 있습니다. 현재 개별 헤드폰의 고유 착용 외형이나 새로운 코스튬 아트를 구현 완료로 판정하지 않습니다. 공식 기본 복장은 기존 이안 디자인을 유지합니다. 원본 픽셀 UI 기호는 별도로 작성했으며 착용 원화로 대신하지 않습니다. 신규 음향 파일은 없고 AudioRequested/RareReveal hook만 제공합니다.

Android 결과물은 Builds/SceneArchitecture/Ian-Rooms-Development.apk입니다. 실기기 성능 및 상업 배포 검증과 구분합니다. Editor 최종 Scene은 FamilyHome_IanBedroom, 타깃 StandaloneOSX로 복원했습니다. 씬 저장과 dirty=False를 확인했고 Play/컴파일은 종료 상태입니다. 정상 수동 Play 저장 보호도 해제했으며 [최종 Editor 상태](EDITOR_FINAL_STATE.json)에 기록했습니다.

시작 소스69adda3. 작업 중 기존 Update 체크포인트c0ee76a가 생성됐고 이를 유지해 이어서 작업했습니다. 최종 코드·에셋·실행 근거 체크포인트는 `97cc01f57b04f34c06ef1c8a6b2fb5422db25d3f`입니다. 이후 문서만 갱신했으며 원격 push는 하지 않았습니다.

후속 항목: 개별 착용 아트/음향 리소스, 실제 기기 성능·접힘/백그라운드 검증, 카탈로그 확장·장기 밸런스 검토. 실제 현금 결제, 엔딩/환생, 전체 시스템 재작성은 이번에 추가하지 않았습니다.
