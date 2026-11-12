# 원본 기획·회귀 복구 — 2026-11-13

신규 확장은 보류하고 기존 LifeWorld, 성장·경제·저장·NPC 서비스를 수정했다. 실제 프로젝트는 Unity 6000.6.8f1 / macOS arm64. Workspace는 LifeWorld 안의 workplace 구역이며 별도 씬을 새로 만들지 않았다.

## 기획 근거와 0세 오류
- 보존 기획 `Docs/IAN_DESIGN_BASELINE.md`: 0–6 / 7–12 / 13–18 / 19–29 / 30–59 / 60+; 취업·금융 19세; 날짜로 자동 나이 증가 없음. 원본 공유 대화는 이번에 재접속해 읽었다고 주장하지 않는다.
- 사용자 추가 확인: 독립 외출은 아동기부터. 기존 아동기 시작 7세를 그대로 사용한다.
- 실제 원인: 기존 EmploymentService에는 연령 검사가 있었으나 LifeWorld의 Use/Travel, 문 요청, SceneTransitionService 및 주식 Trade 실행에는 빠져 있었다. 부동산 매각도 매수의 minAge 검사와 달리 제한이 없었다.
- GrowthAccess로 공통 조건을 연결했다. 일반 이동/테스트 씬 경로/문 요청/상호작용/직접 매매/급여가 실행 단계에서 검사한다. raw legacy 씬 진입도 GameplaySceneBindings가 허용 장소로 복귀시킨다.
- 로드 시 연령에 맞지 않는 장소만 집으로 정규화한다. 현금·보유 주식·부채·부동산·인벤토리·미션은 유지한다. 유아기의 침대에 기존 돌봄 행동과 기초 학습을 연결했다. 추가 무료 가구 없음.
- 교육은 기존 minAge/maxAge/requiredStage/선행 미션을 유지한다. 주거 계약은 기존 PropertyCatalog의 minAge 19를 사용한다. MissionService의 Housing 표시 해금은 30으로 남아 있어 계약 최소 연령과 표시 해금의 차이는 기획 정합성 후속 확인 대상이다. 임의로 60개 미션을 변경하지 않았다.

## 문
- 수정 전 16개 문 양면 출입 213개 검사는 통과했다. 사용자 보고의 광범위한 문 고장을 전부 재현한 것은 아니다.
- 확인한 결함: 상점 출구에 영업시간 제한이 적용돼 폐점 후 퇴장 불가, 일시정지 중 접근 타임아웃 소진, 파괴된 플레이어의 문 렌더 접근.
- 최소 수정: 출구 영업시간 예외(명시적 잠금은 유지), 실제 이동 시간으로 타임아웃 계산, Unity 객체 수명 검사, 열린 뒤 걷기 포즈 복귀. 기존 문틀/경첩/문짝 분리·개폐 상태·충돌·임대·씬 전환은 유지.
- 최종 주요 문 검사 215 PASS: 최초 집/욕실/회사/증권/부동산/학교/상점/회사 화장실, 앞뒤 접근, 중복 입력, 고정 Transform, 정확한 도착 구역, 제어 복귀, 일시정지/재개, 폐점 후 퇴장.
- 추가 재현 결함: 증권 상담 고객이 퇴장할 때 (349.79, -3.99)에서 멈췄다. 경로의 마지막 격자점이 실제 문 접근점보다 문짝에 가깝게 반올림되고, 정적 경로망은 닫힌 문을 제외하는 반면 실제 이동은 문 충돌을 검사해 발생했다. WorldNPCNavigation에서 닫힌 문을 포함한 원형 sweep으로 마지막 접근 구간을 확인해 정확한 목적점으로 연결한다. 순간이동·문 충돌 해제·임계 거리 확대를 사용하지 않았다. 증권/부동산 고객 퇴장 모두 수정 후 통과.
- 물리 키보드 및 모든 장소의 실제 사람·NPC 동시 통과 조합을 전수 수동 검증했다고 주장하지 않는다.

## 회사·시설
- 기존 9명 근무 인물 ID 유지. 사무실 5석(주간 3명, 야간 2명), 증권 상담 2석, 부동산 상담 2석으로 업무 위치·직업·시간표 연결. 기존 주말/점심/귀가 기록을 유지했다.
- 좌석 예약은 기존 WorldActivitySlot/NPC 스케줄러 사용. 원래 편의점 직업으로 동일 workplace에 몰린 데이터의 역할을 office/analyst로 명시했다.
- 새 FrontWork/EmptyDesk 원화는 기존 NPC 디자인을 참조해 생성한 프로젝트 전용 에셋. 근무 상태에서 얼굴·노트북·손/서류 프레임을 함께 표시한다. 같은 자리의 빈 책상과 기존 옆모습 스프라이트를 중복 표시하지 않는다. 이동과 경로는 실제 NPC가 소유한다.
- 실제 퇴근 검사: 출입구에 선 플레이어를 밀지 않고 대기 → 플레이어가 비키면 걸어서 문 통과 → 다음 일정 진행.
- 두 금융시설은 기존 단말기/거래 UI/소유 데이터 유지. 직원 업무석, 고객 대기 공간, 상담 진입, 실제 시세·매물 안내를 연결했다. 안내판이 얼굴을 가리는 문제는 Game View 확인 후 수정했다.
- 기존 주민 resident-2/7에 서로 다른 평일 14:20 증권·15:20 부동산 상담 일정을 연결했다. 전용 상담 위치 예약, 기존 직원과 대화 방향, 시설 사용 기록, 16시 이후 원래 쇼핑 일정으로 실제 문을 통과해 퇴장하는 흐름을 확인했다. 화면 밖 방문 상태의 표시를 눈앞의 출입 애니메이션 검증으로 계산하지 않았다.
- 완전한 정면 착석/기립 연결, 다양한 전화/상담 동작, 상업용 미술 최종 품질은 미완료다. 새 정면 업무 프레임만으로 모든 생활 애니메이션을 완료 처리하지 않는다.

## UI 회귀
- 기존 종이색 픽셀 UI/한글 폰트 유지. 반응형 매물 그리드가 정착 전 폭을 캐시해 일부 매물을 가리던 문제 수정.
- 헤더/탭/거래 버튼/피드백은 실제 높이로 배치하며, 스크롤 폭과 6개 매물 버튼 경계를 검사한다. 904×2316, 1840×2208, 1280×720, 1920×1080 확인.
- 미성년 거래 버튼은 잠김 표시 및 실행 함수 차단. 코드만 우회 호출해도 거래가 실행되지 않는다.

## 검증
- 성장·직접 실행·로드·돌봄: 90 PASS.
- 금융 고객 상담·문 퇴장: 13 PASS, UI issue 0. 처음 두 시도에서 같은 문 접근 정지 오류를 재현한 뒤 경로 마지막 구간을 수정해 통과했다.
- 시설·정면 업무 재생·동선·반응형 경계·저장: 67 PASS, 텍스트 감사 결과는 원본 JSON 참조.
- 기존 경제 핵심: 40 PASS. 독립 검사 fixture에 실제 성인 성장 권한을 연결했으며 런타임 연령 제한을 우회하지 않았다.
- 전체 QA: 87 PASS / 0 FAIL / 13 NOT IMPLEMENTED / 4 MANUAL. 최초 실패 1건은 집 이동 없이 회복을 기대하던 검사였으며 실제 Home 이동을 추가하고 해당 실패만 재실행했다.
- Play 테스트는 격리 슬롯을 사용한다. 현재 원본 저장 3개는 시작 백업과 해시 일치. 이전 WorldRecovery 단계에서 발생했던 자동 저장 변동/복구 기록은 `../WorldRecovery/SAVE_PROTECTION.json`에 보존하며 없었던 일로 처리하지 않는다.
- 반복 문/모바일 590 PASS: oak/jade/glass 각 30·60FPS 상당 dt 및 0.11초 지연 dt에서 20회씩(총180회), 두 사용자 임대, 정확한 닫힘·고정 위치, 모바일 4화면·입력·복귀. 실제 기기 FPS 벤치마크가 아니다.

## 파일·보존
주요 C#: GrowthAccess, LifeWorldController/Service, SceneTransitionService, GameplaySceneBindings, WorldDoor, EconomyFinance/Housing/Property, JobService, Phase23Services, WorldHUD, FinancePresenter, PropertyPresenter/ActionDock/UIFlow, WorldWorkstationPresentation, WorldFacilityInformation, WorldNPCNavigation, CriticalFacilitySetup, WorldQualityRecoverySetup. 기존 개발 QA 레지스트리와 집중 검증 코드 확장.
실제 씬: LifeWorld 및 기존 UI 씬 City/Workplace/StockMarket/School/Home/MainMenu. standalone Door prefab/Animator는 새로 만들지 않았으며 문은 LifeWorld 직렬화 컴포넌트를 재사용했다.
데이터: LifeWorldRules 및 새 작업 원화/임포트 메타데이터. 기존 이안 원본 이미지, 경제 밸런스, 60개 미션 에셋은 변경하지 않았다.
시작 체크포인트 `bf6d5fb`, 이전 기반 `fb6d0ab`, 수정·검증 체크포인트 `5f4bea9`. 별도 변경 패치/시작 상태 및 로컬 백업 보존. 원격 origin은 있지만 이번 변경은 아직 push하지 않아 장비 손실에 대비한 원격 백업 완료가 아니다.

## 화면
- 이전 회사: `../WorldRecovery/Screenshots/01-Workspace-Working-Final.png`
- 수정 후: `Screenshots/workplace.png`, `Screenshots/finance.png`, `Screenshots/realestate.png`
- 실제 고객 상담: `Screenshots/finance-customer.png`, `Screenshots/realestate-customer.png`
- 반응형 매물: `Screenshots/Property-904.png`, `Property-1840.png`, `Property-1280.png`, `Property-1920.png`

## 참고 자료
[NYSE Ramp 공식 설명](https://www.nyse.com/taking-stock/the-unassuming-nerve-center-of-the-nyse-trading-floor), [NYSE 설계사 설명·사진](https://www.perkinseastman.com/projects/new-york-stock-exchange-next-generation-trading-floor/), [집토스 지점 설명](https://career.ziptoss.com/branches/11). NYSE 사진은 직접 확인했다. 집토스 사진 다운로드는 실패해 텍스트의 개별 업무석·상담 업무 설명만 참고했다. 상업용 자료는 게임 텍스처로 복제하지 않았다.

## 최종 빌드·마지막 저장
- 기본 복구 체크포인트: `5f4bea9`. 이후 금융 고객 일정·문 접근 경로 수정 및 집중 회귀 결과까지 디스크에 저장했다. 최종 체크포인트는 Git 최신 커밋 참조. Android 릴리스 APK 성공(오류0, 경고1214), iOS 릴리스 Xcode 프로젝트 내보내기 성공(오류0, 경고628). 이전 빌드와 같은 MissionCondition 직렬화 깊이 경고가 남아 있으며 이번 범위에서 숨기거나 대규모 변경하지 않았다. 상세 메시지 파일 보존.
- APK: `Builds/WorldRecovery/Android/IanGrowing.apk`; iOS 프로젝트: `Builds/WorldRecovery/iOS/`. iOS 네이티브 빌드는 Xcode/iPhoneOS SDK 부재로 미실행. 실기 Android/iOS/Fold 테스트와 장기 성능/발열은 미검증.
- 최종 고객/경로 수정 포함 빌드: Android 00:01:09.935 / iOS export 00:00:29.281, 양쪽 오류0. 빌드 종료 후 StandaloneOSX로 복귀, Edit Mode LifeWorld dirty=false 확인. 생성된 렌더 설정 목록/아틀라스 정렬/동적 폰트 캐시/컴파일 명령은 별도 백업 후 이전 디스크 값으로 정리. 실제 구현 변경은 보존했다.
- 최종 Console에는 Unity AI 서비스 NoSubscription 오류가 남아 있다. 게임 C# 컴파일/빌드 실패와 구분하며 구독 설정은 변경하지 않았다.
- 다음 작업: 사용자 보고 문 고장이 재발하는 구체 동선/입력 상태 재현, 정면 착석·기립 연결, 전화·다양한 상담 행동, 주거 표시 해금30/계약 최소19의 기획 정합성 확인, 실제 기기 회귀. 현재 전체 마스터 요구사항 완료 상태는 아니다.
