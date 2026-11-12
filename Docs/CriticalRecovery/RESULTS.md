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
- 물리 키보드 및 모든 장소의 실제 사람·NPC 동시 통과 조합을 전수 수동 검증했다고 주장하지 않는다.

## 회사·시설
- 기존 9명 근무 인물 ID 유지. 사무실 5석(주간 3명, 야간 2명), 증권 상담 2석, 부동산 상담 2석으로 업무 위치·직업·시간표 연결. 기존 주말/점심/귀가 기록을 유지했다.
- 좌석 예약은 기존 WorldActivitySlot/NPC 스케줄러 사용. 원래 편의점 직업으로 동일 workplace에 몰린 데이터의 역할을 office/analyst로 명시했다.
- 새 FrontWork/EmptyDesk 원화는 기존 NPC 디자인을 참조해 생성한 프로젝트 전용 에셋. 근무 상태에서 얼굴·노트북·손/서류 프레임을 함께 표시한다. 같은 자리의 빈 책상과 기존 옆모습 스프라이트를 중복 표시하지 않는다. 이동과 경로는 실제 NPC가 소유한다.
- 실제 퇴근 검사: 출입구에 선 플레이어를 밀지 않고 대기 → 플레이어가 비키면 걸어서 문 통과 → 다음 일정 진행.
- 두 금융시설은 기존 단말기/거래 UI/소유 데이터 유지. 직원 업무석, 고객 대기 공간, 상담 진입, 실제 시세·매물 안내를 연결했다. 안내판이 얼굴을 가리는 문제는 Game View 확인 후 수정했다.
- 추가 금융 고객 방문 일정, 완전한 정면 착석/기립 연결, 다양한 전화/상담 동작, 상업용 미술 최종 품질은 미완료다. 새 정면 업무 프레임만으로 모든 생활 애니메이션을 완료 처리하지 않는다.

## UI 회귀
- 기존 종이색 픽셀 UI/한글 폰트 유지. 반응형 매물 그리드가 정착 전 폭을 캐시해 일부 매물을 가리던 문제 수정.
- 헤더/탭/거래 버튼/피드백은 실제 높이로 배치하며, 스크롤 폭과 6개 매물 버튼 경계를 검사한다. 904×2316, 1840×2208, 1280×720, 1920×1080 확인.
- 미성년 거래 버튼은 잠김 표시 및 실행 함수 차단. 코드만 우회 호출해도 거래가 실행되지 않는다.

## 검증
- 성장·직접 실행·로드·돌봄: 90 PASS.
- 시설·정면 업무 재생·동선·반응형 경계·저장: 67 PASS, 텍스트 감사 결과는 원본 JSON 참조.
- 기존 경제 핵심: 40 PASS. 독립 검사 fixture에 실제 성인 성장 권한을 연결했으며 런타임 연령 제한을 우회하지 않았다.
- 전체 QA: 87 PASS / 0 FAIL / 13 NOT IMPLEMENTED / 4 MANUAL. 최초 실패 1건은 집 이동 없이 회복을 기대하던 검사였으며 실제 Home 이동을 추가하고 해당 실패만 재실행했다.
- Play 테스트는 격리 슬롯을 사용한다. 현재 원본 저장 3개는 시작 백업과 해시 일치. 이전 WorldRecovery 단계에서 발생했던 자동 저장 변동/복구 기록은 `../WorldRecovery/SAVE_PROTECTION.json`에 보존하며 없었던 일로 처리하지 않는다.
- 반복 문/모바일, 최종 빌드 및 체크포인트 결과는 아래 후속 기록에 추가한다.

## 파일·보존
주요 C#: GrowthAccess, LifeWorldController/Service, SceneTransitionService, GameplaySceneBindings, WorldDoor, EconomyFinance/Housing/Property, JobService, Phase23Services, WorldHUD, FinancePresenter, PropertyPresenter/ActionDock/UIFlow, WorldWorkstationPresentation, WorldFacilityInformation, CriticalFacilitySetup, WorldQualityRecoverySetup. 기존 개발 QA 레지스트리와 집중 검증 코드 확장.
실제 씬: LifeWorld 및 기존 UI 씬 City/Workplace/StockMarket/School/Home/MainMenu. standalone Door prefab/Animator는 새로 만들지 않았으며 문은 LifeWorld 직렬화 컴포넌트를 재사용했다.
데이터: LifeWorldRules 및 새 작업 원화/임포트 메타데이터. 기존 이안 원본 이미지, 경제 밸런스, 60개 미션 에셋은 변경하지 않았다.
시작 체크포인트 `bf6d5fb`, 이전 기반 `fb6d0ab`. 별도 변경 패치/시작 상태 및 로컬 백업 보존. 원격 origin은 있지만 이번 변경은 아직 push하지 않아 장비 손실에 대비한 원격 백업 완료가 아니다.

## 화면
- 이전 회사: `../WorldRecovery/Screenshots/01-Workspace-Working-Final.png`
- 수정 후: `Screenshots/workplace.png`, `Screenshots/finance.png`, `Screenshots/realestate.png`
- 반응형 매물: `Screenshots/Property-904.png`, `Property-1840.png`, `Property-1280.png`, `Property-1920.png`

## 참고 자료
[NYSE Ramp 공식 설명](https://www.nyse.com/taking-stock/the-unassuming-nerve-center-of-the-nyse-trading-floor), [NYSE 설계사 설명·사진](https://www.perkinseastman.com/projects/new-york-stock-exchange-next-generation-trading-floor/), [집토스 지점 설명](https://career.ziptoss.com/branches/11). NYSE 사진은 직접 확인했다. 집토스 사진 다운로드는 실패해 텍스트의 개별 업무석·상담 업무 설명만 참고했다. 상업용 자료는 게임 텍스처로 복제하지 않았다.
