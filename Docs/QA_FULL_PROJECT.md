# 이안 키우기 통합 QA · 2026-11-19

**출시 판정 보류.** 한 저장으로 출생부터 M038 첫 주식 매수까지 진행하고 복원했다. M039는 선행 보상을 모두 가진 상태에서도 선택 이벤트 공급원이 없어 진행할 수 없다. 아래에서 개별 조건 시험과 실제 순차 플레이를 구분한다.

Unity 6000.6.8f1, 기준 커밋 `dee61d1`, 시작 씬 Bootstrap, 활성 Windows 프로필, 저장 v4. 테스트 저장 경로를 분리했고, 기존 사용자 파일은 복사본으로만 읽었다. 시각·나이·채용 난수는 필요한 테스트에서만 입력했다. 주요 E2E는 미션 직접 완료 명령을 사용하지 않았다.

## 통합 케이스

ID는 요청서 절 번호이며 세부 재현 명령·assertion은 각 실행 기록에 있다. PASS는 해당 행의 실제 시험 범위에만 적용된다.

| ID | 시스템 | 시험 | 기대 | 실측 | 상태 | 심각도 | 재현·근거 | 수정 | 재시험 |
|---|---|---|---|---|---|---|---|---|---|
| 006–008 | 부팅 | 신규 인생, 부모님 집, 출생 안내 및 체크포인트 복원 | 0세, 초기 현금·허기·미션, 안내 1회와 조작 복귀 | 신규 자동 슬롯, 출생 확인 후 재로드, 가족 방 서비스 확인. 실제 메인메뉴 Continue 버튼에서 M038 저장과 조작 복귀 확인 | PASS | — | [Continue 버튼](QAFull/continue-final.json) · core/new-life · [실행 기록](QAFull/e2e-final.json) | — | — |
| 009, 144–146 | 입력 | WASD, 대각선, I, ESC, E, 포커스 상실 콜백 | 방향별 이동·멈춤, 창 열면 차단·닫으면 복귀 | macOS 독립 개발 플레이어 Input System 이벤트로 17개 확인. OS 자체 Alt-Tab은 별도 | PASS | — | 키 이벤트 → 실제 모터 · [실행 기록](QAFull/input-player.json) | — | — |
| 010 | 충돌 | 모든 벽·가구·NPC·경계 접근 | 장애물 비관통 | 문 충돌·침대 정렬과 제한 이동은 확인. 모든 장애물 양면 접촉 맵 미측정 | NOT TESTED | — | [실행 기록](QAFull/actions.json) | — | — |
| 011–016 | 성장·동작 | 연령 경계, 보행·외출·독립, 방향·동작 전후 | 0세 보행 차단, 7세 외출, 19세 계약 후 거주지 전환 | 0/1/7/13/19/30/60 외형·콜라이더, 선행 조건과 일치. 60+는 나이 fixture | PASS | — | growth/world-access · [실행 기록](QAFull/extended.json) · [실행 기록](QAFull/gear.json) | — | — |
| 017–021 | 생활 | 침대, 식탁, 냉장고, 문, 컴퓨터 접근 → 실행 → 종료 | 게임 시간·허기·가구 상태·입력 복귀 | 실제 정렬·조작 반환. 문 양쪽 리스와 복귀 및 침대·식사·의자·컴퓨터 화면 캡처 | PASS | — | world/door-lease · [실행 기록](QAFull/actions.json) · [실행 기록](QAFull/world.json) | — | — |
| 022–023 | 생존 | 8개 스탯과 정수 극값 변화 | 각각 최소/최대 보호, 스트레스 연동 | 8개 상하한, 극값·음수 변화 검사. 핵심 스탯 자체는 정수 | PASS | — | survival/stats · survival/clamp-delta · [실행 기록](QAFull/registry-full.json) | — | — |
| 024 | HUD | 네 생존 값에 서로 다른 값을 부여하고 재화와 함께 표시 문자열 대조 | 표시와 실제 값 일치 | 허기·수분·체력·멘탈과 현금·루비를 같은 프레임의 원본 모델과 대조 | PASS | — | [UI 대조](QAFull/hud-filters-save.json) | — | — |
| 025–026, 028–030 | 미션 | 목록·선행 조건·실제 장착·중복 보상 | 60개 ID, 조건 미달 수령 거부, 장착 후 024/025 수령 | M001–038 실제 순차 진행, M024/025는 획득만으로 미완료. 전체 목록·개별 커버리지는 아래 표 | PASS | — | [실행 기록](QAFull/e2e-final.json) · [실행 기록](QAFull/items-regression.json) | — | — |
| 027, 165–167 | 인생 진행 | M038 체크포인트 복원 후 M039 진행 | 생애 평가까지 도달할 수 있음 | 27세, M039 Planned, 선택 0/3. EventChoices 공급원 없음. 이후 자연 진행 중단 | FAIL | BLOCKER | 실제 M038 저장 → M039 확인 · [실행 기록](QAFull/gate.json) | BUG-006 | 동일 저장으로 재현 |
| 031–032 | 교육 | 과정별 입학·공부·자동 공부·복원 | 실제 학습이 스탯·경험에 반영됨 | 5개 과정의 실제 학습·디스크 복원. 자동 공부 시작·중단 및 오프라인 주기 검사 | PASS | — | education/course/* · idle/Study · [실행 기록](QAFull/registry-full.json) · [실행 기록](QAFull/extended.json) | — | — |
| 031b | 교육 | 숙제·시험·성적·친구 화면 개별 UX | 각 입력과 표시 정확 | 실제 순차 플레이의 저장에서 독서 3회·공부 11회·숙제 10회, 초등/중등 시험 44점/82점과 성적표 수령을 확인했다. 개별 화면의 모든 UI 표현은 미검증 | NOT TESTED | — | — | — | — |
| 033–034 | 사회 | NPC 대화·호감도 변화·화면 밖 행동 영수증 | 대화 종료 후 조작 복귀, 동일 영수증 재반영 방지 | 대화·호감도 변화, 부재 중 가족 식사 재정산 중복 방지 | PASS | — | social/talk · [실행 기록](QAFull/extended.json) · [실행 기록](QAFull/receiptDiag.json) | — | — |
| 035–039 | 직업 | 지원·불합격·재지원·채용·급여·승진·퇴사·해고 | 같은 급여 중복 거절, 해고 뒤 재취업 가능 | 기존 서비스의 면접 확률 입력을 고정해 양쪽 결과 실행. 승진 상한 및 보상 확인 | PASS | — | jobs/hire · audit/job-* · [실행 기록](QAFull/registry-full.json) | — | — |
| 040–042, 044–047 | 방치·시간 | 노동·공부·전투, 주입 UTC 1/24/168시간, 역행 | 완료 주기만 정산, 제한 시간·같은 시각 중복 방지 | 실제 정산 서비스·영아 활동 차단 및 비용 확인. 기기 시스템 시각 자체는 변경하지 않음 | PASS | — | idle/* · offline/* · [실행 기록](QAFull/registry-full.json) · [실행 기록](QAFull/extra.json) | — | — |
| 043a | 오프라인 | 현 설정으로 600초 정산 → 디스크 로드 → 같은 구간 재정산 | 완성 주기만 보상, 나머지 시간 보존 | 주기 1200초, 600초에는 급여 0·진행 600초. 이후 2회 근무의 실제 급여를 받은 후 같은 시각 재정산 시 증가 없음 | PASS | — | [시간·재무 회귀](QAFull/market-and-clock.json) | — | 동일 구간·지급 후 모두 재실행 |
| 043b, 047b | 기기 시간 | 10분 실제 기기 백그라운드·OS 시계 변경 | 복귀 후 단일 보상 | 가상 시각 테스트만 실행 | NOT TESTED | — | — | — | — |
| 048–052 | 경제 | 현금, 급여, 세금 납부·체납, 이자·상환·신용 | 실제 거래만 반영, 반복 이자·영수증 거부 | 세금·부채·부동산 오프라인 납부, 반복 정산, 기존 대출의 순차 상환과 투자 자격 회복 | PASS | — | finance/* · property/offline-rent · [실행 기록](QAFull/registry-full.json) · [실행 기록](QAFull/e2e-final.json) | — | — |
| 054–056 | 투자 | 매수·매도·잔액 부족·보유량 초과·가격 방향 | 상승·하락 시 손익 방향과 전량 매도 후 잔액 정확 | 실제 주문 중복, 매도 거부 및 손익 확인. 분할 매수 평단 별도 | PASS | — | stocks/up · stocks/down · audit/stock-negative · [실행 기록](QAFull/registry-full.json) | — | — |
| 053, 054b, 057–058 | 투자 | 6개 종목·분할 매수 평단·업데이트 경계·뉴스 재전송 | 설정한 간격 및 가중 평단, 뉴스 1회 | 주기 직전·경계 틱 대조, 2주+3주 매수/2주 매도 후 남은 원가·실현 손익 독립 계산 일치. 같은 뉴스 두 번째 거절 | PASS | — | [실행 결과](QAFull/market-and-clock.json) | — | 기존 상승/하락·자금 부족 회귀 |
| 059, 061–065 | 아이템·장비 | 스택·소모·교체·해제·장착 레이어·보관 | 한 번 소모, 이전 효과만 해제, Save 후 유지 | 실제 소모품, 급여 modifier, 헤드폰 6종 4방향, 코스튬 선택 및 저장 검증. 미보유 아트 전체는 미검수 | PASS | — | [실행 기록](QAFull/items-regression.json) · [실행 기록](QAFull/gear.json) | — | — |
| 060 | 인벤토리 | 17종 소유 fixture에서 실제 필터 버튼 6개 클릭 | 보이는 카드 ID가 해당 카테고리 소유 목록과 일치 | 전체·장비·헤드폰·음료·코스튬·재료/티켓 각각 집합 대조 | PASS | — | [클릭 결과](QAFull/hud-filters-save.json) | — | — |
| 066–076 | 가챠·도감 | 1·10회, 50/100/300, 원장 재전송, 중복·분해·NEW | 실제 비용·결과 개수, 천장·확률 고지·디스크 복원 일치 | 실제 연속 300회, 재화 부족·저장 실패 시 롤백, 마지막 1개 및 장착 보호 | PASS | — | items/pity · [실행 기록](QAFull/items-regression.json) · [실행 기록](QAFull/monetization.json) | — | — |
| 077–081 | 루비 | 미션·도전과제·모의 검증 영수증·부족 잔액·반복 로드 | 무료/유료 원장 보존, 중복 ID·지급 거절 | 실제 Wallet과 저장, 재화 가챠 차감, 무료/유료 소비 분리 및 중복 차단. 현금 결제 없음 | PASS | — | [실행 기록](QAFull/monetization.json) | — | — |
| 082–083 | 도전과제 | 26개 정의 임계값·동일 이벤트 ID 반복 | 해금 once 및 재적립 방지 | 기존 서비스 Record에 각 임계값 전달 후 디스크 저장. 26개 모두 개별 실제 플레이로 달성한 것은 아님 | PASS | — | [실행 기록](QAFull/extra.json) | — | — |
| 084–089 | 주거 | 부모 집, 독립 계약, 침대 1개·구매·배치·보관·복원 | 비용 1회, 유효 좌표만 허용, 소유권 유지 | 유효·NaN·외부 좌표, 저장 좌표, 계약 중복/회수 및 사유지 확인. 회전은 별도 | PASS | — | property/* · [실행 기록](QAFull/actions.json) · [실행 기록](QAFull/housing-legacy.json) | — | — |
| 087b–089b | 주거 | 회전·모든 이사 조합에서 가구 배치 자동 이전 | 해당 기능 있을 때 정책대로 복원 | 저장 좌표 및 보관 소유권 확인. 모든 새 거주지의 회전값·배치 이전 미검증 | NOT TESTED | — | — | — | — |
| 090–098 | 도시·교통 | 출퇴근·야간 인구, 학교·오피스·점포 출석, 버스·퇴장 | 시간·장소에 맞는 사람·차량, 버스 정차·재출발 | 근무 점원과 손님, 실물 학생·직원, 5종 차량, 밤 초과 차량 차선 끝에서 퇴장. 버스는 실제 FixedUpdate 통제 조건 | PASS | — | [실행 기록](QAFull/world.json) | — | — |
| 100–102 | 씬 | Bootstrap→MainMenu, 방 16개, 구 경로 7개, 왕복 | 로드 성공·기준 서비스 1개·방 플레이어 조작 복귀 | 활성 목록 25개. 구 씬 7개는 테스트 라우터로 로드·싱글턴 확인. 모든 레거시 UI 입력까지 통과했다는 의미는 아님 | PASS | — | [실행 기록](QAFull/build-scenes.json) · [실행 기록](QAFull/rooms.json) · [실행 기록](QAFull/route.json) · [실행 기록](QAFull/housing-legacy.json) | BUG-002 | 25개 구성·로드 대조 |
| 103–104 | 시계 | 1/7/30일 진행, 수면·오프라인·하루 비용 | 날짜 진행과 워터마크 중복 방지 | 날짜를 실제 서비스로 진행. 동일 정산 시각에서 주거비 재부과 없음 | PASS | — | time/* · property/offline-rent · [실행 기록](QAFull/registry-full.json) | — | — |
| 105–107, 117–119 | UI | 주요 창, 긴 한글, 화면비·안전영역, 입력 잠금 | 백스택 및 스크롤·텍스트 범위 유지 | 긴 받침·숫자·30행 문단, 가로/세로/태블릿/폴더블 Canvas 6개. 실기기 테스트는 별도 | PASS | — | [실행 기록](QAFull/UI_ACCESS_RESULTS.json) · [실행 기록](QAFull/layout-canvas.json) · [실행 기록](QAFull/input-player.json) | — | — |
| 108 | 클릭 | 전 메뉴의 버튼 최소 터치 영역·겹침 | 모든 버튼 사용 가능 | 중첩 분해 확인·취소 등 주요 버튼을 실행. 전체 실제 터치 영역 순회 미실시 | NOT TESTED | — | [실행 기록](QAFull/ux-retest.json) | — | — |
| 109–116 | HUD·창 | 상호작용, 혼잡 알림, 미션 신호, 장착·가챠·재화 | 주변 행동·비용·결과를 확인, 중복 알림 병합 | 냉장고→식사→미션 이벤트, 알림 상한 24·우선순위·FIFO, 잔액 변경 즉시 표시. 전 화면 숫자 전수 대조는 별도 | PASS | — | [실행 기록](QAFull/ux-retest.json) · [실행 기록](QAFull/monetization.json) · [실행 기록](QAFull/FINAL_ACCESS_RESULTS.json) | — | — |
| 120–121 | 픽셀 | Phase 3.3 플레이용 스프라이트 샘플러·크기 | WorldLife 에셋 Point 및 장면에서 식별 가능 | WorldLife 45개 모두 Point. 전체 Art 중 비-Point 21개는 용도별 승인 미완 | PASS | — | [실행 기록](QAFull/static-audit.json) · [이미지](QAFull/Screenshots/) | — | — |
| 122 | 정렬 | 나무·가구·문·식탁 전체 방향 가림 | 공간 깊이와 겹침 일치 | 침대·식사·문 동작은 촬영. 모든 옥외 오브젝트의 앞뒤 판정은 미측정 | NOT TESTED | — | [실행 기록](QAFull/actions.json) | — | — |
| 123 | 조명 | 낮→저녁→야간→비, 야간 횡단보도와 이안 | 야간에도 보행 지형을 읽을 수 있음 | 동일 카메라 01시 전후 캡처 비교. 낮과 따뜻한 저녁 색상 유지 | PASS | — | [실행 기록](QAFull/visibility.json) | BUG-007 | 실제 Game View 비교 |
| 124a | AI 아트 | 변환 매니페스트의 게임용 PNG를 실제 픽셀로 펼쳐 원근·문자·브랜드 확인 | 쓸 수 있는 투시·규격, 문자는 별도 제작 | 변환 28개 모두 접촉 인화 형식으로 검수. 식별 가능한 상표·깨진 글자는 발견하지 못했다. 간판·원룸·차량은 실제 씬 크기도 확인 | PASS | — | [28개 게임용 에셋](QAFull/Final/ConvertedWorldAssets.png) | — | [게임 뷰](QAFull/Screenshots/) |
| 124b | 원화 | 모든 원본 후보 및 파생 NPC 프레임 전수 확대 검수 | 부자연스러운 픽셀·원근 없음 | 런타임 변환본과 주요 장면 중심으로 확인 | NOT TESTED | — | [매니페스트](HIGGSFIELD_ASSET_MANIFEST.md) | — | — |
| 125–128, 130 | 저장 | 신규·중기·v1–4·백업·깨진/미래 저장·중복 로드 | 신규 슬롯, 원본 보존, 지원 범위만 복원 | 기존 v1/v4 실제 복사본과 fixture, 경제·장비·가챠·주거·루비 별도 저장 검증 | PASS | — | save/version/* · audit/corrupt-future-save · [실행 기록](QAFull/registry-full.json) · [실행 기록](QAFull/items-regression.json) · [실행 기록](QAFull/monetization.json) | — | — |
| 129 | 저장 중 종료 | 실제 디스크 쓰기 도중 프로세스 종료 | 재기동 후 일관된 체크포인트 | 트랜잭션 저장 실패 주입 후 원복과 손상 파일 거절은 통과. 부분 JSON이 남아도 직전 파일을 읽고 다음 저장으로 임시 파일을 대체하는 것을 확인. OS kill 중 재기동 미실행 | NOT TESTED | — | [실행 기록](QAFull/items-regression.json) | — | — |
| 125b·127b | 대형 세이브 | 유효한 원장 65,000개, 경계 크기와 UTF-8 한도 초과 | 성공한 쓰기는 다시 읽힘, 실패한 쓰기는 원본 그대로 | 이전에는 10,701,222바이트의 정상 쓰기를 같은 게임이 거절했다. 수정 후 같은 규모의 재로드, 공백을 줄인 경계 데이터의 56,000개 복원, 한도 초과 시 이전 파일 해시·성공 시각 보존 | PASS | CRITICAL | [재현](QAFull/size-before.json) | BUG-009 · SaveManager | [재시험](QAFull/size-after.json) |
| 099, 131–136 | 성능 | 도시·학교·회사·왕복 짧은 표본 CPU/GC/FPS와 업데이트 정적 검사 | 프레임·GC 표본 수집 및 이상 경로 식별 | 방문당 24프레임. 실기기 예산 판정은 별도. 가챠 창 별도 120프레임은 137a에 기록 | PASS | — | [실행 기록](QAFull/profile.json) · [실행 기록](QAFull/route-profiler.json) · [실행 기록](QAFull/static-audit.json) | — | — |
| 137–138a | 메모리·Canvas | 도시↔점포 6왕복 후 가챠 UI 120프레임 | 프레임·GC·씬 수·렌더 콜백 기록 | 각 방문의 메모리·캔버스·활성 앰비언스, 가챠 창 peak GC와 주 스레드 시간 측정 | PASS | — | [원자료](QAFull/memory-audio-samples.json) | — | 창 닫은 뒤 입력 복귀 |
| 137–138b | 성능 | 장시간 메모리 추세와 Canvas rebuild 원인 분해 | 장시간 안정성과 갱신 비용 입증 | 단기 표본의 총 메모리·렌더 콜백만으로 누수·실제 rebuild 비용을 판정하기 어려움 | NOT TESTED | — | [단기 표본](QAFull/memory-audio-samples.json) | — | — |
| 139 | 텍스처 | 월드 텍스처 수·최대 크기 확인 | 특이 규격 식별 | 전용 스캔으로 2172×724 EntryThreshold 확인. 용도·실기기 VRAM 별도 | PASS | — | [실행 기록](QAFull/static-audit.json) | — | — |
| 140 | 리스너 | 해지 후 전체 구독자·호출 누수 | 폐기된 Scene 객체 참조 제거 | 씬별 서비스 수와 중복 보상 확인. 모든 이벤트 구독자 장시간 추적은 미실행 | NOT TESTED | — | [실행 기록](QAFull/route.json) | — | — |
| 141 | 오디오 | 별도 Audio Mixer 그룹 | Mixer 사용 시 그룹별 독립 볼륨 | 프로젝트 자체 .mixer 없음. 개별 AudioSource와 마스터 음량 사용 | NOT APPLICABLE | — | Assets/_Game 검색 | — | — |
| 142 | 오디오 | 도시↔점포 6회 왕복, 홈으로 복귀 | 이전 방의 앰비언스 중첩 없음 | 방문별 활성 환경음 호스트 및 재생 소스 최대 1개 | PASS | — | [실제 소스 계측](QAFull/memory-audio-samples.json) | — | 같은 방 재방문 |
| 143 | 오디오 | 연타·음원·전환 실제 청취 | 음량 돌출·청감 중복 없음 | 음원 청취 미실행 | NOT TESTED | — | — | — | — |
| 147–149a | 설정 | 오디오·그래픽·접근성 버튼 변경 및 씬 재생성 | 값 저장, 다시 생성된 UI에 적용 | 볼륨·환경 효과·장소 전환·모션·텍스트·알림 지속·정수 배율 저장. 새 방에서 마스터 음량 복원 및 기존 사용자 설정 복구 | PASS | — | [설정](QAFull/settings.json) | — | 별도 방에서 재확인 |
| 147–149b | 설정 | 각 설정의 실제 OS 종료 후 재시작 | 재시작 후 동일한 설정 유지 | PlayerPrefs 저장 및 씬 재생성까지 검사. 실제 기기 재시작 후 모든 수치 재독 미실시 | NOT TESTED | — | [설정](QAFull/settings.json) | — | — |
| 150 | Windows | 기존 설정으로 x64 Development 빌드·부팅 | EXE 생성, 법적 정보 경고만, 엔진 초기화 | 0 build error, Wine 11 headless 시작 확인. 네이티브 Windows 그래픽/입력 미검증 | PASS | — | [실행 기록](QAFull/build-StandaloneWindows64.json) · [실행 기록](QAFull/windows-launch.json) | — | — |
| 151 | Android | Production AAB·실기기 | 서명된 AAB 실행 | Release 스크립트 75개 어셈블리 컴파일. 실제 서명 정보·정식 법무 승인 미확보 | BLOCKED | — | [실행 기록](QAFull/build-Android.json) | — | — |
| 152 | iOS | 기기 Release 컴파일·서명 | Xcode Archive와 기기 실행 | Unity 내부 검토용 Xcode 생성 성공. xcodebuild 종료 69, 호스트 Xcode 라이선스 미동의. 유효한 서명 신원 0개 | BLOCKED | — | [실행 기록](QAFull/build-iOS.json) · [실행 기록](QAFull/native-ios.json) | — | — |
| 153–154 | 릴리스 | Release 스크립트에 개발 콘솔·모의 구매·QA 도구 포함 여부 | 개발 전용 형식 제외, 샘플 씬 제외 | 비개발 Android 어셈블리 형식 검사 및 활성 씬 검토. 상점 제출 앱 실제 기기 탐색은 미검증 | PASS | — | [실행 기록](QAFull/release-types.json) · [실행 기록](QAFull/build-scenes.json) | — | — |
| 155–158 | 데이터 | 중복·누락 ID, Scene/Prefab 직렬화 참조, 필수 카탈로그 | 고유 ID, Missing Script와 깨진 대상 0 | 60 미션·26 도전과제·17 아이템·6 부동산·1 풀. 28씬/91프리팹/128,320컴포넌트 직렬화 검색. 모든 경로 기반 로더 전수 증명은 별도 | PASS | — | [실행 기록](QAFull/data.json) · [실행 기록](QAFull/refs-after.json) | BUG-001 | 동일 감사 32→0 |
| 159–162 | 코드 | 서식·폴더·주석 마커·매 프레임 로그 검색 | 자체 코드 범위에 형식 오류·릴리스 마커 없음 | 341개 자체 C# 검사. TODO/FIXME/HACK/TEMP 및 Update 직접 로그 매치 0. 서드파티 제외 | PASS | — | [실행 기록](QAFull/static-audit.json) · [실행 기록](QAFull/style-final.json) | — | — |
| 163–164 | 문구 | 전국면 문체·안내 전수 독해 | 정보 먼저, 건조한 일상적 유머 | 거래·저장·안내·법적 문구 샘플 검토. 전체 이벤트·문구 편집 검수 미실행 | NOT TESTED | — | [실행 기록](QAFull/monetization.json) | — | — |
| 168–174 | 회귀 | 수정 원인 재현→동일 조건→인접 서비스→E2E·화면 | 수정한 오류 회귀 없음, 차단점 기록 | 공통 QA 운영 절차. 10개 요청 화면 카테고리 캡처, 실제 M039 차단은 위 FAIL 유지 | PASS | — | [실행 기록](QAFull/e2e-final.json) · [수정 이력](BUG_FIX_REPORT.md) | — | — |
| 175a | 컴파일 | Unity 편집기와 플랫폼 컴파일 | C# 컴파일 오류 0 | Editor 및 Windows/iOS Unity 빌드 오류 0, Android 플레이어 스크립트 컴파일 완료 | PASS | — | [실행 기록](QAFull/editor-final.json) · [실행 기록](QAFull/build-Android.json) | — | — |
| 175b–176 | 로그 | 모든 플레이 경로 무경고·무예외 | 애플리케이션 예외 없고 반복 경고 설명 | 최종 콘솔 및 실제 빌드 경고는 별도 기록. 미도달 최종 구간에 대한 무예외 보증 불가 | NOT TESTED | — | [실행 기록](QAFull/console-final.json) | — | — |

## 미션별 검사

개별 시나리오는 입력 fixture를 사용하며 독립된 미션 명세를 검사한다. 자연 진행 열은 같은 저장에서 선행 조건을 순서대로 달성한 결과다. M040–050 개별 성공은 M039를 넘었다는 증거로 사용하지 않았다.

| ID | 이름 | 개별 시나리오 | 한 저장 순차 진행 |
|---|---|---|---|
| M001 | 물을 마셔보자 | PASS | PASS |
| M002 | 밥이라는 것을 먹어보자 | PASS | PASS |
| M003 | 잠을 자보자 | PASS | PASS |
| M004 | 배고픔을 해결해보자 | PASS | PASS |
| M005 | 갈증에서 벗어나자 | PASS | PASS |
| M006 | 휴식의 가치를 알아보자 | PASS | PASS |
| M007 | 건강한 인간이 되어보자 | PASS | PASS |
| M008 | 멘탈을 관리해보자 | PASS | PASS |
| M009 | 사흘 동안 살아남자 | PASS | PASS |
| M010 | 유아기 졸업 | PASS | PASS |
| M011 | 학교 입학 | PASS | PASS |
| M012 | 책 3권 읽기 | PASS | PASS |
| M013 | 지식 20 달성 | PASS | PASS |
| M014 | 친구 1명 만들기 | PASS | PASS |
| M015 | 사회성 15 달성 | PASS | PASS |
| M016 | 숙제 5회 | PASS | PASS |
| M017 | 시험 응시 | PASS | PASS |
| M018 | 첫 성적표 받기 | PASS | PASS |
| M019 | 방과 후 학습 | PASS | PASS |
| M020 | 12세 달성 | PASS | PASS |
| M021 | 중학교 입학 | PASS | PASS |
| M022 | 공부 10회 수행 | PASS | PASS |
| M023 | 지식 35 달성 | PASS | PASS |
| M024 | 헤드폰 첫 장착 | PASS | PASS |
| M025 | 장비 첫 장착 | PASS | PASS |
| M026 | 사회성 25 달성 | PASS | PASS |
| M027 | 시험 2회 응시 | PASS | PASS |
| M028 | 진로 선택 | PASS | PASS |
| M029 | 멘탈 40 이상 유지 | PASS | PASS |
| M030 | 청소년기 졸업 | PASS | PASS |
| M031 | 첫 이력서 작성 | PASS | PASS |
| M032 | 첫 면접 응시 | PASS | PASS |
| M033 | 첫 아르바이트 취업 | PASS | PASS |
| M034 | 첫 급여 수령 | PASS | PASS |
| M035 | 첫 세금 납부 | PASS | PASS |
| M036 | 부채 일부 상환 | PASS | PASS |
| M037 | 출근 3회 완료 | PASS | PASS |
| M038 | 주식 첫 매수 | PASS | PASS |
| M039 | 인생 이벤트 선택 3회 | BLOCKED | FAIL |
| M040 | 청년기 졸업 | PASS | BLOCKED |
| M041 | 승진에 도전하기 | PASS | BLOCKED |
| M042 | 저축 10,000G 달성 | PASS | BLOCKED |
| M043 | 신용도 60 달성 | PASS | BLOCKED |
| M044 | 첫 가구 구매 | PASS | BLOCKED |
| M045 | 월세 납부 | PASS | BLOCKED |
| M046 | 전세보증보험 알아보기 | PASS | BLOCKED |
| M047 | 주식 첫 매도 | PASS | BLOCKED |
| M048 | 투자 손익 확인 | PASS | BLOCKED |
| M049 | 부채 원금 추가 상환 | PASS | BLOCKED |
| M050 | 중년기 졸업 | PASS | BLOCKED |
| M051 | 노년기 진입 확인 | BLOCKED | BLOCKED |
| M052 | 은퇴 여부 결정 | BLOCKED | BLOCKED |
| M053 | 생존 능력치 관리 | BLOCKED | BLOCKED |
| M054 | 과거 인생 이벤트 회상 | BLOCKED | BLOCKED |
| M055 | 누적 소득 확인 | BLOCKED | BLOCKED |
| M056 | 사회성 50 달성 | BLOCKED | BLOCKED |
| M057 | 주요 업적 5개 달성 | BLOCKED | BLOCKED |
| M058 | 남은 부채 확인 | BLOCKED | BLOCKED |
| M059 | 최종 인생 평가 신청 | BLOCKED | BLOCKED |
| M060 | 생애 평가 완료 | BLOCKED | BLOCKED |

## 실행 기록 해석

- 개발 QA 등록 160건 중 142 PASS, 11개 시나리오 구현 없음, 6개 수동 전용, 1개 별도 수동 저장 슬롯 부재. 기존 등록기의 오래된 장비 미구현 표시도 실제 해제·급여 복원·다른 장착 슬롯 유지 시험으로 교체했다. [개별 검사](QAFull/registry-full.json) · [실제 장비 검증](QAFull/items-regression.json)
- 개발 입력 장치 포커스가 없는 에디터에서는 초기 합성 키 이동을 재현하지 못했다. macOS 네이티브 개발 플레이어에서 같은 키 입력과 실제 플레이어 컨트롤러를 연결해 재시험했다. Windows 부팅은 Wine null graphics 환경이다. [플레이어 결과](QAFull/input-player.json)
- 테스트 실패 증거는 수정 후 결과와 함께 보존했다. 실제 금전 결제, 배포 서명, 장시간 메모리 보증 및 기기 직접 조작은 실행 범위 밖이다. [출시 준비도](RELEASE_READINESS.md)

[실제 생애 저장의 교육 집계](QAFull/earned-school.json) · [임시 파일이 남은 체크포인트 검사](QAFull/hud-filters-save.json) · [에디터 로그 구간 검사](QAFull/editor-log-summary.json)

자체 ScriptableObject 205개에서 객체 참조 필드 3,285개를 추가 검사했다. 끊긴 참조 0개. [ScriptableObject 결과](QAFull/asset-refs.json)
