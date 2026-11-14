# ADVANCED OVERHAUL 추가 필수 작업 — 2026-11-12

## 현재 Priority 6 이번 구현·검증·보존 완료 — 전체 필수 조건 완료 아님
- 기존 HEAD 1766e16 / 실제 프로젝트 `/Users/hyunwol/IanGrowing`, Unity 6000.6.8f1 ARM64. 기존 작업/사용자 변경 보존.
- 수정 전 로컬 복구 ZIP: DevelopmentBackups/Advanced-Before-20261112.zip, 무결성 확인.
- 필수 범위: 고급 행동 애니메이션, 실제 문/전환, Android+iOS/접힘 화면, NPC 방향 동기화/일정/저장/오프스크린.
- 확인: Android/iOS Editor 모듈 지원 true. xcode-select는 CommandLineTools, xcodebuild 미설치. 실제 기기 검증과 Editor/빌드 검증을 구분.
- 원인 후보: WorldStreetLife는 단일 우향 프레임을 상하 이동에도 사용, 세로 이동 FlipX가 경로 인덱스 기반. WorldNPCScheduler는 시간표가 구역을 바꾸면 즉시 위치를 바꾸며 실제 출입 과정 없음.
- 다음 순서: 실제 방향 프레임/속도 기반 공통 재생 → 문 상태/충돌/출입 연출 → 터치/화면 재구성 → NPC 개인 일정·포털 경로·저장 → Play/빌드 검증.
- Priority 1 체크포인트 1871f5f: 이안 40개 방향·행동 클립, 4시민 방향 뱅크, 실제 이동 거리 기반 발걸음, 달리기/감속/정지. ADVANCEDANIMATION_RESULTS.json 실제 Play 105개 통과. 작은 프레임 이동을 정지로 보던 새 임계값 문제 수정, 최초 실패 기록 보존.
- Priority 2: 10개 실제 문, 여닫이 2종/미닫이 1종 원본 6프레임, Closed/Opening/Open/Closing/Locked, 닫힘 충돌/열림 통과, 소유자별 사용/해제, NPC 호출 공통 경로. 실제 플레이어 Rigidbody로 접근→개방→통과→장소 확정→닫힘. 실내 전면 문은 캐릭터 가림 완화.
- ADVANCEDDOORS_RESULTS.json 실제 Play 106개 통과: 10개 연결 경로, 중복 거부, 장소별 PixelFade/ShortSlide/LightingReveal, 소지금/입력 복원, NPC 문 열기·닫기, 잠금, 로드 중단 복구. 최초 실패는 재진입 문이 안전 감지로 이미 열린 상태인 것을 검사기가 무조건 Opening으로 요구한 문제, 조건 보정 후 통과.
- 행동별 원본이 없는 방향은 UsedDirectionFallback으로 구분. 모든 행동의 4방향 정식 아트 완성으로 간주하지 않음.
- Priority 3: OnScreenStick/OnScreenButton을 동일 World 액션에 연결. 휴대폰 세로/가로/확장 화면 3개 배치, Safe Area, 입력 해제/재개, 30/60FPS·환경 효과 옵션, Android ARM64/IL2CPP 및 iOS 설정 저장.
- ADVANCEDMOBILE_RESULTS.json Editor Play 48개 통과/UI issue 0. 904×2316↔1840×2208 접힘/펼침 반복, 2340×1080 회전, 1920×1440 확장, 상태/위치 보존, Safe Area 내 터치 타깃, 115% 주요 메뉴, 실제 복제 InputActionMap까지 OnScreenButton/가상 조이스틱 디바이스 이벤트 전달과 해제 확인. 초기 뒤로 버튼 5개 넘침은 버튼 폭을 104로 확대해 해결, 최초 기록 보존.
- 실제 터치스크린/블루투스 키보드 혼용·Android/iOS 기기·접힘 하드웨어 검증은 아님. adb 연결 기기 없음, 전체 Xcode 앱 없음. 빌드 시도와 실기기 결과를 분리할 것.
- Priority 4/5: 기존 단일 NPC scheduler 확장. 12명 개인 일정/주말/야간 근무, 공통 시간·사회 ID, 실제 직장/상점/생활관 오브젝트, 문 사용, 이동 벡터 방향, 캐시 경로, 오프스크린 데이터 진행과 저장. NPC 임금은 플레이어 경제와 분리.
- ADVANCEDRESIDENTS_RESULTS.json 34개 통과, ADVANCEDRESIDENTVISUAL_RESULTS.json 10개 통과, MASTER_RESULTS.json 기존 경제·구매·배치·하루 회귀 90개 통과/UI issue 0. 기존 강제 정오 보행/일괄 야간 인구 검사를 개인 일정 기준으로 수정. 최초 실패 결과 보존.
- GameContext Play 재컴파일 후 정적 Instance만 초기화되는 오류를 실제 확인, OnEnable 재연결 추가. 부트 재실행 후 귀가/수면 검사 통과.
- Android ARM64 IL2CPP 개발 APK 생성 성공: Builds/Advanced/Android/IanGrowing.apk (139.2 MiB), 오류 0, 경고 1207, 최종 클린 빌드 92초. 원본 빌드 경고/해시/라이브러리 목록은 Docs/Phase26/Advanced. 경고에는 기존 MissionCondition.children 재귀 직렬화 한계가 포함됨. 실기기 실행 아님.
- 저장 감사: 시작 해시 대비 기존 v4 자동 저장/백업 2개가 21:56경 변경됨. v1은 동일. 현재 두 파일을 DevelopmentBackups/Advanced-SaveAudit-20261112에 별도 보존했으며 원래 경로는 덮어쓰지 않음. 통계/성장/경제는 현재 주/백업 간 동일하나 시작 버전의 전체 바이트/진행 보존은 입증 불가. 테스트 MainMenu 진입 이전 자동 로딩도 격리하도록 BeforeSceneLoad+sceneLoaded 테스트 시작 경로 보강. 모든 기존 데이터 무변경이라는 주장은 하지 않음.
- 최종 문/전환 회귀 110개 통과. 닫힘 완료에 기존 문 음원 재사용(별도 피치/음량) 연결. 최종 UI 11개 통과/넘침 0. 보강한 초기 슬롯 격리 이후 기존 3개 파일 SHA-256 추가 변화 없음(SAVE_ISOLATION_VERIFIED.json). 시작 기준 2개 변경 사실은 별도 SAVE_AUDIT.json에 유지.
- iOS 최종 Xcode 프로젝트 내보내기 성공: Builds/Advanced/iOS/Unity-iPhone.xcodeproj, 오류 0 / 경고 616, 32초. Xcode 앱이 없어 네이티브 컴파일/서명/설치 미검증. Android 기기 없음. 현재 Editor target macOS / Edit Mode / Bootstrap 씬 dirty=false, SaveOpenScenes/SaveAssets 실행.
- 클린 Android APK는 139.2 MiB. 증분 패키지에서 확인된 과거 항목 사이 빈 공간(약 90 MiB)을 클린 빌드로 제거. 빌드 캐시(.utmp) 및 재정렬/동적 폰트 초기화만 발생한 에셋은 이번 작업 이전 버전으로 복원하여 기존 소스 상태 보존.
- 미완료/미검증: 시작 저장 원본 2개 전체 무변경 입증(현재 주/백업 별도 보존), 실기기 터치/접힘/성능, 모든 행동의 정확한 4방향 아트, 수면 스프라이트와 침대 포즈/비율 정합 및 상업용 시각 품질 최종 승인, 기존 미션 재귀 직렬화 경고와 모바일 불필요 리소스 포함 문제.
- 다음 순서: 저장 원본 사본이 있으면 시작 해시 대조(현재 파일 덮어쓰기 금지) → MissionCondition 에셋 안전 마이그레이션 및 회귀 → 수면/방향별 원본 아트 정합 → 실기기 Android/iOS/접힘·성능 검증. 테스트는 BeforeSceneLoad 슬롯 격리를 유지할 것.
- 사용자가 제공한 시작 원본 사본은 아직 없음. Time Machine latestbackup 대상 마운트 실패, 로컬 snapshot 목록 비어 있음. 로컬 ZIP/Git이 장비 손실 대비 원격 백업을 대체하지 않음. 원격 push 실행 안 함.

- 마지막 코드/검증 체크포인트: e568505. 최종 소스 복구 ZIP DevelopmentBackups/Advanced-Final-20261112.zip 무결성 통과. 파일 수/크기/SHA-256은 Docs/Phase26/Advanced/BACKUP_MANIFEST.json. ZIP은 로컬 보존, 빌드 출력은 Builds/Advanced, 원격 백업 아님.

---
# MASTER DIRECTIVE — macOS 재개 (2026-11-12)

## 현재 Stage: 9 이번 변경 보존 완료 — 전체 상업용 완성 아님
- 실제 경로 `/Users/hyunwol/IanGrowing`, Unity 6000.6.8f1, Apple M5 Max, MCP 연결 정상, Edit Mode. MainGame2D에서 시작; LifeWorld 확인 중.
- 기존 Git HEAD 1f14318, origin GitHub 등록. 기존 변경 `Docs/Phase26/LIVE_KEY_TRACE.json` 보존. 원격 등록만 확인했으며 이번 변경의 원격 백업 완료를 의미하지 않음.
- 수정 전 복구 ZIP: `DevelopmentBackups/Master-20261112-194924.zip` (791파일, 무결성 확인). 로컬 백업은 장비 손실에 대비한 원격 백업이 아님.
- 기존 생활/경제/저장/주거/미션/입력/걷기/도시를 재사용. 과거 Windows 검증 결과는 이번 macOS 통과로 취급하지 않음.
- 발견한 요구 충돌: 기본집에 무료 식탁/책상/냉장고/소파/장식이 있음. 신규 게임과 기존 저장의 초기 주거 상태를 분리해야 함.
- 미검증: 현재 모든 Play 경로, 720p/1080p UI, 실제 물리 키보드, 아트 최종 품질.
- Stage 1: 실제 4개 해상도(1280×720/1920×1080/2400×1080/1080×1920) 메뉴/HUD 및 115% 글씨·최대 소지금/날짜 검사. 수정 전 넘침 2건 → 수정 후 0건, 11개 체크 통과. WorldHUDLayout을 LifeWorld 씬에 연결하고 저장. HUD는 상태 이벤트로 갱신.
- 다음 순서: 실제 UI 감사 → 신규 침대 전용 초기 상태/기존 저장 호환 → 주거 구매·배치 → 생활/도시 실행 → 캡처/체크포인트.

## 2026-11-12 현재 구현 및 검사
- Stage 2 핵심 구현: 신규 주거 starterRevision=1은 침대만 보유. starterRevision=0인 기존 세이브는 무료 가구/소유권 보존. HomeShowcase.unity에 기존 완성형 씬 보존(빌드 경로 추가 없음).
- 신규 일반 가구/장식/싱크·주방 무료 제공 제거. 나무 바닥/흰 벽지, 독립 옥색 욕실의 변기/세면대/샤워만 건물 설비 유지. 기존 캐릭터와 아트 원본 유지.
- 기존 주거 지갑/영수증/저장 사용, 가구 카탈로그 11종으로 확장. 냉장고·조리대·식탁·책상·소파·옷장·협탁 구매 가능. 구매 후 직접 그리드 배치/초록·빨강 미리보기/충돌·문앞·동선 검사/회수·취소·무료 재배치. 이사 비용 차감, 소유권 유지, 보관함 이동.
- 상점 식사(빵 소모)와 정리 도움(30분/30G)을 연결해 무료 가구/초기 잔액 없이 생활과 수입 가능. 기존 직업/교육 조건은 유지.
- MASTER_RESULTS.json: Unity macOS Play 90개 통과, UI issue 0. 신규/기존 저장, 출생 1회, 욕실/음식/수입, 구매 UI, 유효/무효 배치, 유료 이사, 실제 디스크 저장·로드, 기존 근무/급여/중복 방지, 관계, 다음날 기상, 도시 27초 이동·3상 신호·걷기 프레임·야간 밀도 확인.
- 실패 기록 보존: MASTER_FIRST_ATTEMPT.json은 미보유 가구를 소유권 검사 API로 사전 조회한 검사기 오류. MASTER_PLACEMENT_FAILURE.json은 소파 추천점이 현관 예약 영역과 겹침. 추천점을 통로 밖으로 수정하고 옷장도 벽 안쪽으로 이동 후 90개 통과.
- ARM64 Editor 확인, 실행 중 한 시점 할당 메모리 약 939MB(플레이어 빌드 성능 측정 아님).
- 공식 Eastward Octopia 저녁 실내, Mistria 가구배치 GIF, Stardew PennyHouse, Little Witch 숲 스크린샷을 실제 열어 검토. 자료는 References/sources.json. 에셋으로 복제하지 않음. 현재 환경의 선명한 픽셀 클러스터/캐릭터와 배경 밀도/행동별 애니메이션은 최종 품질 미달 또는 미검증으로 유지.
- 최종 UI: 카탈로그 확장 후 4개 해상도 전체 메뉴/HUD/115% 및 최대 숫자 재검사 11개 통과, UI issue 0.
- 자유 배치 좁은 검사: 유효/무효 표시, 추천 슬롯 이외 실제 그리드 좌표 확정, 재배치 취소, 주거 패널이 닫힌 안정 화면 확인. MASTERCAPTURE_RESULTS.json에 별도 기록. 16개 실제 Game View PNG는 Docs/Phase26/MasterScreenshots/.
- 입력 자동검사: INPUT_RESULTS.json은 W 이동 실패. 최초 7개 연결 체크는 통과, 이동 delta=(0,0), 누르는 동안 `automation lost application/Game View focus` 관찰. osascript로 Unity 활성화 후에도 발생. 자동검사 포커스 전제 미충족이며 실제 WASD 정상/고장을 확정하지 않음. 모터의 포커스 보호 정책을 우회하지 않았고 추가 반복 중단. 기존 Windows 원격 원인과 동일시하지 않음.
- DAY 1 메시지 및 인생 시작 버튼 유지, 실제 저장 뒤 로드 시 재등장 없음 확인. 초기 침대 전용 상태를 씬에도 저장하고 전체 LifeWorldBuilder의 마지막 단계에 적용을 연결.
- 전체 미완료: 이사 후 집 평면/넓이·고급 인테리어의 시각적 발전, 전용 수면/식사/위생 동작 스프라이트, 최종 픽셀 밀도·조명·도시 아트 폴리싱, 물리 키보드/게임패드, macOS 플레이어 빌드·다른 플랫폼, 장시간 성능. 기존 eight-state 걷기 Animator 자산 연결은 확인했으나 이번 환경의 실제 키보드 보행/걷기 프레임 재생 검증은 완료하지 못함.
- Console: Play 종료 전 게임 오류 0건 조회. 도메인 리로드 때 Unity AI(UERP) NoSubscription/ThreadAbort가 반복되며 게임 컴파일 오류와 구분. 패키지 임의 삭제 없음.
- 검사기는 고유 임시 슬롯에서 실행하고 원래 모델/슬롯/자동저장/입력 설정을 finally에서 복원. 기존 사용자 저장 삭제/리셋/덮어쓰기 없음.

### 마지막 저장 지점 (2026-11-12)
- LifeWorld 씬을 침대 전용 초기 표현으로 디스크 저장, Edit Mode, scene dirty=false, 기존 Display 1 카메라 직접 출력/중복 카메라 없음. HomeShowcase 및 기존 프리팹/원본 아트 유지.
- 최종 좁은 검사 MASTERCAPTURE_RESULTS.json 15개 통과/UI issue 0. scene 초기 상태에서 legacyRevision=0 복원/초기 상태 복귀까지 확인.
- 구 JSON에 starterRevision 필드가 없어도 0으로 읽히고 기존 보유 3항목이 남는 실제 Unity JsonUtility 역직렬화 확인.
- MASTER_MAC_SUMMARY.json에 이번 결과/제약 요약. 코드 공백 검사 통과, Play 종료 후 Console error 0 조회.
- 최종 로컬 복구 ZIP: DevelopmentBackups/Master-Final-20261112.zip (무결성 확인), 수정 전 ZIP도 보존. Git 첫 체크포인트 613956e 및 후속 주거/검증 체크포인트. origin 등록은 확인했으나 이번 세션에서 push하지 않았으므로 원격 보존 완료 아님.
- 기존 LIVE_KEY_TRACE.json과 Unity가 갱신한 Readme/Walnut 등 작업 외 변경을 제거하거나 체크포인트에 섞지 않음. 기존 결과 파일은 Git 이력과 BEFORE/실패 JSON으로 보존.
- 남은 모든 항목은 위 '전체 미완료' 목록과 재개 순서 참고. 이번 결과를 모든 Stage 최종 완료로 간주하지 않음.

### 재개 및 사용자 직접 검증
1. Bootstrap 씬 → Play → 새 게임. 출생 안내에서 인생 시작하기. 침대 1개, 욕실 필수 설비만 확인.
2. Unity 창을 직접 클릭하고 Game 탭 내부 클릭 → WASD/방향키 각각 이동·방향별 걷기/정지 확인. I 인벤토리 중 이동 차단, Esc 닫기·복귀. 창 밖 클릭 후 이동 정지 확인. 자동 포커스 문제를 해결하려고 모터 게이트를 제거하지 말 것.
3. E로 현관→골목→도시→상점. 정리 돕기로 수입, 빵 구매·바로 식사. 구매 전 소지금 확인. 가구 구매 후 집 관리에서 배치(집에서), 마우스/방향키로 초록·빨강 미리보기, Enter 확정/Esc 취소.
4. 집 관리에서 회수/무료 재배치, 저장/불러오기. 30세·10,000G 이상 등 기존 조건으로 유료 이사 확인. 현재 더 큰 집의 실제 평면 확대는 미완료.
5. 다음 개발 순서: 물리 입력 확인 → 생활 동작 원본 제작/연결 → 이사 후 실제 주거 공간 차별화 → 레퍼런스 대비 아트 폴리싱 → macOS 빌드/지속 플레이. Phase 2.7로 임의 확장하지 않음.
6. 재생성이 필요할 때만 `Ian/Life World/Apply Bed Only Starter Home` 사용. Inspector 변경 보존 후 실행. 개발용 HomeShowcase는 새 게임 진입 경로가 아님.

---
아래는 이전 환경의 이력이며 현재 환경 검증 결과와 구분한다.

# 2026-11-09 — Phase 2.6 품질 복구 진행 중 (완료 아님)

## 현재 상태 — 집 긴급 수정 최종 확인 (2026-11-09)
- 실제 저장된 LifeWorld 집: 13.5×9, 카메라 4.8 orthographic. 바닥/벽은 종횡비를 유지해 균일 축소. 좌측 침실/협탁/책상/옷장, 우측 연속 주방 작업대/싱크/냉장고/식탁, 뒤쪽 문으로 옥색 화장실 연결.
- HOME_COMPACT_CYCLE_RESULTS.json: 최종 배치에서 119개 통과. 침대 수면/기상, 샤워, 냉장고 준비/식사, 외출/도시/근무/급여/대화/귀가, 다음 날, 가구 구매/배치, 저장/복원 확인.
- HOME_ROUTE_RESULTS.json: 고정 가구 점유영역 겹침, 실제 플레이어 Rigidbody2D의 10개 집 동선, 외벽 충돌, 마이그레이션, 낮밤/HUD 포함 108개 통과, UI issue 0. 모터를 검증 중에만 끄고 몸체를 직접 구동하므로 물리 키보드/걷기 애니메이션 검증으로 간주하지 않음.
- HOME_SINK_RESULTS.json: 실제 세수 상호작용 수락, 위생 20→34.9479, 정확히 300초 경과, 검사 후 기존 상태 복원.
- HOME_HOUSING_RESULTS.json: 책장 600G 구매(5,000→4,400G), 중복 구매 거부, 현관 배치 거부/차감 없음, 유효 배치 무료, 실제 저장 파일 읽기/로드 후 소유권·배치·잔액 유지 모두 통과.
- HOME_CAMERA_AUDIT.json: Game View targetDisplay 0과 카메라 display 0 일치, 실제 1280×720, Main Camera 2D 활성/URP Base/MainCamera/cullingMask -1/targetTexture null, 활성 Display 카메라 1개. 월드는 카메라로, HUD만 ScreenSpaceOverlay로 출력. 중복 카메라 생성 없음.
- 최종 Game View 스크린샷: Urgent-Home-1280x720.png, Urgent-Home-Night-1280x720.png, Urgent-Bathroom-1280x720.png. 기존 Recovery-Home-Art.png, 중간 Urgent-Home-Layout-v1.png 보존.
- 주요 추가 파일: HomeTraversalVerification.cs. 수정: Phase26HomeSetup.cs / HomeLayoutPlan.cs / Phase26Verification.cs / LifeWorld 씬·규칙·HomeLayoutPlan.asset. 검사 실패 시 모터 복원 처리도 보완.
- Play 검증 종료 직후 Console 조회 오류 0건. 이후 카메라 Edit Mode 초기값 저장을 위한 재컴파일 시 Unity AI(UERP) ThreadAbort 로그가 다시 발생함. 게임 컴파일/생활 검증 실패와 구분한다. 검사 종료 후 Play 정지, 씬·에셋 저장. 기존 사용자 저장 슬롯과 플레이 상태 복원. 미커밋 작업 보존.
- 미검증/남음: 새 배치의 사용자 물리 WASD/E 체감, 최종 상업용 미술 품질 검토, 기존 Phase 2.6의 미연결 일상 애니메이션. 전체 Phase 완료 선언 없음.

## 이전 작업 이력
## 연결/중단 복구
- Unity는 이미 실행 중이므로 재실행하지 않았다. 기존 로컬 MCP 8080에 남은 세션 ID가 404를 반환하여 세션 ID만 갱신해 통신 복구.
- MCP로 실제 프로젝트 IanGrow/IanGrowing, Bootstrap, Play 중지, 컴파일 대기 없음, 활성 씬 dirty=false 확인.
- Editor-prev.log 끝에 EditorApplicationQuit와 정상 종료 요청/종료 절차가 있다. 이 기록만으로 크래시 원인을 확정할 수 없음.
- git 미커밋 변경 및 IanSymbolsFallback.asset 변경을 보존. reset/clean/기존 세이브 삭제 없음.

## 현재 Stage: P0 입력 복구
- 완료(파일/참조): WorldControls.inputactions의 WASD/방향키 2DVector, 게임패드, E, I, M을 기존 LifeWorld 플레이어에 연결. Esc는 기존 UIShell 단일 소유, Tab은 UI 포커스 이동.
- 완료(코드): 가속/제동, 대각선 정규화, 창/메뉴/텍스트 입력 중 조작 차단. 포커스 상실 후 Update/FixedUpdate가 다시 이동시키지 않도록 포커스 게이트 추가.
- 진행 중: DeX + 블루투스 키보드 + 원격 앱 경로의 영문 키 전달. 사용자 보고: 방향키는 동작하나 WASD는 동작하지 않음. 키보드 고장으로 판단하지 않음.
- 실제 기록: 주입 검사기 종료 후 방향키 KeyState 및 위치 변화 수신. 같은 구간에서 실제 WASD KeyState 미수신. 00000412/00000409 레이아웃 모두 관찰. 원격 앱은 Microsoft임을 사용자에게 확인.
- 원인 확인: REMOTE_TEXT_INPUT_EVIDENCE.json에 실제 W/A/S/D가 KeyState 없이 TextEvent로만 수신됨. Microsoft 공식 문서의 Unicode 전달 방식과 일치. 클라이언트의 Use scancode input when available 설정 변경 후 사용자 정상 동작 확인. 실제 WASD KeyState와 위치 변화도 기록됨. 문자 이벤트에는 눌림/해제 정보가 없으므로 임의 타임아웃으로 이동을 흉내 내지 않음.
- 원인 확정 후 불필요한 Unity IME 강제 전환 코드를 제거. 텍스트 입력창/포커스 게이트는 유지.
- 진단: Phase26InputTrace는 게임 키의 상태 이벤트와 해당 키 문자(TextEvent) 전달을 구분해 기록하도록 확장. 임의 텍스트는 기록하지 않음.

- 안전 체크포인트: Docs/Phase26/Checkpoints/20261109-023252-input-recovery.zip (21개 변경 파일, ZIP 재열기 확인). 원본 파일과 세이브는 유지.

## 테스트 상태
- 이전 주입 입력 검사: 20개 체크 통과 후 DownArrow 해제/정지 체크에서 실패. 성공으로 덮어쓰지 않음. 당시 물리 틱이 지나기 전 정지 위치를 측정할 수 있는 시간 의존성 및 사용자 동시 입력 여부는 미확정. 검사에 제동 대기 0.12초 추가.
- Windows SendInput 검사: 전면 창 활성화 요청 거부, OS 키 눌림 확인 안 됨. 이는 자동 조작 검증 불가이며 게임 결함 증거가 아님. 실제 WASD/E/I/Esc와 원격 호환성은 미검증.
- 이전 Phase 2.6 자동 검사 기록은 기능 경로 검사이며 일반 키보드 조작/상용 그래픽 완료 근거가 아니다.

## 파일/씬
- 수정: WorldPlayerMotor.cs, LifeWorldController.cs, WorldHUD.cs, LifeWorldBuilder.cs, Phase26Verification.cs, Phase26EditorBridge.cs.
- 추가: WorldControls.inputactions, Phase26InputSetup.cs, Phase26InputTrace.cs, Phase26NativeInputProbe.cs.
- 저장된 씬: LifeWorld.unity 플레이어 controls 참조/가감속 값/조작 안내. 프리팹 재생성 없음.

## 남은 작업 / 다음 작업
1. 변경 입력 스크립트 컴파일 → 원격 문자/키 상태 구분 → 실제 WASD/E/I/Esc 검증. 입력 미해결 상태에서 아트 제작으로 우회하지 않음.
2. 진행 중: 공식 원본 기준 4방향 × 6프레임 시트의 배경 알파 0 확인, 24개 Sprite / 8개 Idle·Walk 클립 / Animator / WorldWalkSet 생성. LifeWorld 기존 플레이어에 연결하여 씬 저장 완료. 재임포트 후 크기 재조회와 Unity fake-null 판정을 수정. 실제 이동 거리 기반 재생과 발바닥 pivot 보정 적용. 사용자 실제 4방향 보행/정지 정상 확인.
3. 연결 완료/검증 중: HomeShell/CityGround/BathroomShell, 가구16종, 도시 건물4종·차량, 시민4외형×6프레임, 신호3상태, 실내 밤 배경. Phase26ArtSetup으로 기존 LifeWorld에 적용/저장. WorldPixelViewport 정수 확대, 변경된 벽 위치와 안전 로드 상한, 도로 위 벤치·단말기 이동 적용. Game View에서 집/도시 적용 화면 확인. 작은 월드 간판 글꼴/HUD 과밀은 수정 중.
4. 연결 완료/검증 중: 배경 시민10명 + 기존 친구2명, 전용2차선 차량4대, 시간대별 밀도/횡단보도 정지/차간 간격/플레이어 감지. Game View 및 A/B 관찰에서 시민10명 이동/프레임 변화 확인. 차량은 횡단 종료 조건 때문에 정지 지속됨을 발견(성공 아님). crossingActive로 시작한 횡단은 끝까지 마치도록 수정, 재검증 대기. 생활/경제/저장/UI 경로 재검증 남음.
5. 진행 중: 작은 HUD/동적 폭 E 안내, 메뉴 내부 조작 안내, 간판 확대/대비, 전용 차선 위치, 골목 진입점·친구 일정의 인도 배치, 원본 합성 발소리·문·환경음 연결. quality 검사는 정상 입력 정책을 변경하지 않고 컨트롤러/UI 경로로 실행하며 실제 키 검증과 구분. 
6. Phase 2.7로 넘어가지 않음. 완료 선언 금지.

---
# 이안 키우기 — Phase 2.6 진행 및 검증 기록

최종 갱신: 2026-11-08
프로젝트: `C:\Users\user\Desktop\IanGrow\IanGrowing` (Unity 6000.6.8f1)

## 현재 Stage 및 완료 판단

Stage A–F의 핵심 월드·생활·기존 시스템 연결을 구현하고 Unity Editor Play에서 검증했다.
**집에서 기상 → 샤워 → 음식 준비/식사 → 외출/도시/상점 → 출근/급여/NPC 대화 → 귀가 → 취침 → 다음 날 기상 → 저장/복원** 사이클이 실제로 통과했다.
정식 방향별/행동별 캐릭터 애니메이션, 환경 아트의 최종 제작과 실제 플레이어 빌드·실기기 검증은 남아 있다. 이를 상용 최종 품질까지 완료했다고 간주하지 않는다. Phase 2.7 시스템은 추가하지 않았다.

## 중단 상태 복구

- 기존 미커밋 변경과 Phase 2.5.6 작업을 유지했다. reset/clean/checkout으로 되돌리지 않았다.
- 중단 당시 월드 스크립트와 빌더는 작성되어 있었으나 실제 씬/프리팹에 연결되지 않았다.
- `SceneTransitionService.cs`의 잘못 삽입된 리터럴 개행 때문에 발생한 CS1056/CS1002를 수정했다.
- 현재 세션에서 Unity MCP 도구 목록이 사라져 기존 로컬 MCP 서버 연결 및 프로젝트 전용 Editor 명령 브리지를 복구했다.
- `LifeWorldBuilder.Build()` 실행으로 실제 씬, 규칙 에셋, GameContext 프리팹, SceneRoutes, BuildSettings를 저장했다.

## 구현 상태 분류

| 분류 | 현재 상태 |
|---|---|
| 완료됨 | 이동, 발밑 충돌, Y 깊이 정렬, 카메라 추적, 7개 연결 구역, 문 이동, 거리 제한 E 상호작용, 수면/기상/음식/위생/활동, 기존 급여·교육·사회성·미션·경제·주거·저장 연결 |
| 완료됨 | UI 전역 보정, 장소/시간/상황 분리 상태바, 간결한 HUD, 상세 창, 하이라이트, 메뉴/행동 중 입력 차단과 닫기/취소 복원 |
| 구현 중 | 핵심 실행 경로에 미연결 작업은 남아 있지 않음 |
| 오류 발생 | 확인한 게임 스크립트 컴파일 및 주요 Play 경로의 치명적 오류 없음. 초기 문법/조작 안내 높이/미션 Toast/최대 수치 폭 오류는 수정 후 재검사 |
| 미구현 | 정식 방향별 및 모든 생활 행동의 전용 스프라이트 프레임, 최종 환경 아트, 자유 드래그 가구 편집, 모바일 전용 이동 터치 UI |
| 검증 필요 | 실제 Windows/Android 플레이어 빌드, 물리 게임패드/터치/DPI, 실기기 성능과 장시간 플레이 |

## 기존 시스템 분석 및 선택한 구조

- 기존 `GameManager`, `TimeManager`, `ActionSystem`, `PlayerStats`, `EconomySystem`, `Phase23Services`, `MissionService`, `SaveManager`, `HousingCatalog`, 픽셀 UI를 재사용했다.
- 기존 행동은 날짜를 하루씩 진행시켰다. 기존 경로는 유지하고 월드 행동에만 시간 단위 실행 인자를 추가했다. 자동 시간 흐름과 행동 시간은 같은 TimeManager를 사용한다.
- 기존에는 이동 컨트롤러가 없고 배경/가구가 한 이미지에 구워져 있었다. 독립 SpriteRenderer와 실제 발밑 Rigidbody2D/Collider2D, Y 좌표 깊이 정렬을 추가했다.
- Orthographic 3/4 시점이다. 카메라는 감쇠 추적하며 실내/실외 크기는 규칙 데이터에서 조정한다. 방/건물/가구는 충돌 및 개별 상호작용을 갖는다.
- 기존 공식 이안 원본/복장/외형을 바꾸지 않았다. 기존 MotionLibrary의 프레임이 비어 있는 상태는 안전하게 기본 스프라이트로 대체한다.

## 실제 월드와 장소

`Assets/_Game/Scenes/LifeWorld.unity`의 작은 생활권:

- 집: 나무 장판, 흰색 벽, 침대/식탁/냉장고/수도/옷장/휴식 자리.
- 옥색 화장실: 옥색 타일·벽, 세면대/샤워/변기.
- 주거 골목: 집 현관, 도시 연결, 안내판/가로등.
- 도시 중심: 상점/Workspace/배움터, 도로/횡단보도/벤치, 증권 단말기.
- 상점: 실제 돈으로 음식 구매, 기존 가구 구매 창 연결.
- Workspace: 기존 이력서/직업 선택/면접/근무/승진/퇴사 조건을 사용하는 작업대.
- 배움터: 기존 입학/공부/과제/시험/졸업/진로 활동.

같은 씬 안의 구역을 문 접근과 짧은 페이드로 연결한다. MainMenu의 새 게임/이어하기는 실제 LifeWorld로 진입하며 기존 StockMarket은 도시 단말기를 통해 유지된다.

## 생활 행동 및 기존 시스템 연결

- WASD/방향키, 좌우 표현, 대기/걷기, 발밑 충돌, 깊이 정렬, 감쇠 카메라, E 거리 판정과 대상 강조.
- 수면 8시간, 식사 30분, 물 2분, 휴식 1시간, 샤워 20분, 세수 5분, 화장실 8분, 학습 1시간, 근무 8시간.
- 밥/빵 선택과 준비, 준비된 음식 소모, 기존 Hunger/Health/Mental 등 회복.
- 기존 식품 인벤토리가 없으므로 최소한의 쌀/빵/준비 음식 상태를 선택적 world 저장 필드에 추가했다. 기존 장비 인벤토리는 유지한다.
- 기존 Hunger/Thirst와 지갑을 복제하지 않았다. 피로/위생만 추가하며 시간/행동에 따라 천천히 변한다.
- 기본 시간 배율: 현실 1,200초에 게임 1일. WorldRules에서 조정한다. 기본 신규 게임은 DAY 1 08:00이며 기존 저장의 시간은 보존한다.
- 출근 시작 시간 09:00–10:59, 기존 취업 나이/자격 유지. 근무 완료 후 기존 WorkRecord/급여/경력/세금/미션 처리. 같은 날의 기존 지급 기록도 검사한다.
- NPC 2명은 기존 도시 걷기 프레임과 NPC 데이터를 사용한다. 시간표는 데이터이며 보이는 구역에서만 걷기/대기를 갱신한다. 대화/친구/관계 유지는 기존 SocialService와 연결한다.
- 교육은 실제 학교 구역의 책상에서 기존 과정·선행 미션·나이·경험치·지식·미션을 사용한다.
- 새 가구와 이사는 기존 가격/지갑/영수증/주거 저장을 사용한다. 재배치는 무료다. 구매 가구는 월드 SpriteRenderer/Collider2D/WorldInteractable로 나타난다. 개선 주거지는 실제 추가 수납/책상을 활성화한다.
- 기존 자동 활동은 월드 진입 시 안전 종료한다. 이미 정산된 결과를 삭제하거나 재지급하지 않는다. 월드에서는 작업대와 책상을 직접 사용한다.

## UI 및 그래픽 개선

- 기존 픽셀 프레임, Galmuri 한글 본문과 Fallback, UITypeScale/PixelTextFlow/UIShell을 재사용한다.
- 상단 상태바는 장소·날짜/시간·상황 메시지의 독립 TMP 요소와 HorizontalLayoutGroup으로 분리했다. Padding/Spacing과 각 요소의 최소/선호 폭을 지정했다.
- 항상 표시하던 큰 버튼 목록을 상세 메뉴로 옮겼다. 상단 상태/잔액, 하단 핵심 수치/조작 안내, 가까운 사물의 E 안내만 기본 표시한다.
- 상태 표시가 도시의 상점/학교 간판을 가리는 문제를 실제 캡처에서 찾아 하단으로 옮겼다. 학교 간판은 RectTransform의 anchoredPosition3D를 명시 저장하고 씬 재열기 후 위치 유지까지 확인했다.
- 조작 안내의 높이, 큰 글씨 미션 완료 Toast, 최대 날짜/소지금/상태의 폭을 수정했다. 글자를 축소해서 숨기지 않았다.
- 기존 City/Workplace/StockMarket의 혼합 문장 상태바도 장소/시간/상황으로 분리했다.
- 월드 미션 안내의 장소 판정은 LifeWorld 씬 ID 대신 실제 구역을 사용한다. 메뉴에서 실행해도 대상 가까이 있어야 하며 행동 시작 시 월드로 돌아온다.
- 낮/밤 색조, 밤의 창/가로등 발광, 문 열림/페이드, 수면 조도/기상, 물 사용 효과, 돈 증감/완료 피드백을 연결했다.

## 저장 및 실패 복구

- 저장 v4에 선택적 `world` schema=1을 추가했다. 위치는 구역 기준 좌표다.
- 장소/좌표/방향, 피로/위생, 음식, 준비 음식, 진행 중 행동, 마지막 근무일/하루 요약을 저장한다. 기존 성장/경제/주거/사회성/교육/미션 필드는 유지한다.
- world 필드가 없는 실제 v4 파일을 읽는 검사를 통과했다. 기존 돈과 진행은 유지하고 신규 생활 필드에 기본값을 적용한다.
- 벽/가구 안의 저장 좌표는 안전 위치로 옮긴다.
- 진행 중 행동은 로드 시 취소한다. 완료 시점 전에는 시간·물품·급여를 확정하지 않으므로 중복 보상을 만들지 않는다.
- 월드 경제/행동 변경 후 체크포인트 실패 시 전체 이전 모델로 복구한다.
- 테스트는 GUID별 저장 슬롯을 사용했고 원래 모델·슬롯·자동 저장 설정·입력 설정을 복원했다.

## 실제 테스트 결과

| 결과 파일 (`Docs/Phase26/`) | 검사 | 결과 |
|---|---|---|
| STAGEA_RESULTS.json | 실제 키보드 이동, 벽/가구 충돌, 거리 제한, 문/카메라, 출생 안내 | 16 통과 |
| CYCLE_RESULTS.json | 최종 코드의 온전한 하루, 급여/음식/주거/NPC/미션/세이브 | 75 통과 |
| EDGE_RESULTS.json | 취소/진행 중 저장과 로드/가구 내부 좌표/구 v4/저장 IO 실패/돈 부족/영업시간/낮밤/NPC 일정/시간 배율/지각/유료 이사 | 35 통과 |
| UI_RESULTS.json | 4개 해상도, 주요 메뉴, 닫기 후 입력, 최대 소지금·날짜·피로와 큰 글씨 | 11 통과, 넘침/글리프 오류 0 |
| INTEGRATION_RESULTS.json | 실제 초등교육/성장 제한/큰 글씨·중첩 탭/컨텍스트/출생/기존 증권 화면과 월드 복귀 | 26 통과, 넘침/글리프 오류 0 |
| CAPTURE_RESULTS.json | 실제 Game View와 월드 간판 글리프 | 별도 캡처 검사 기록 |
| COMPILE_RESULTS.json | Unity 생성 응답/참조 어셈블리를 사용한 Runtime 및 Editor 컴파일 | 두 exit code 0 |

UI 비율: 1280×720, 1920×1080, 2400×1080, 1080×1920. 큰 글씨 115%, 소지금 int 최대, DAY 999998, 핵심 상태 100도 별도로 표시해 확인했다.

`EDITOR_PERFORMANCE_SNAPSHOT.json`은 Editor의 한 시점 샘플이다: 전체 SpriteRenderer 159개 중 해당 카메라 내 15개, 카메라 1개, Rigidbody2D 1개, drawCalls 25. 전체 Editor 메모리는 플레이어 메모리 예산/실기기 성능 결과로 해석하지 않는다.

수정 코드의 주요 Play 오류는 발견하지 못했다. 도메인 재로딩 시 기존 Unity AI NoSubscription/UERP 패키지 로그가 발생할 수 있으며 게임 코드 오류와 구분했다.

## 주요 수정 파일

- 신규: LifeWorldRules.cs, LifeWorldService.cs, LifeWorldController.cs, WorldPlayerMotor.cs, WorldCameraController.cs, WorldInteractable.cs, WorldNPCScheduler.cs, WorldHUD.cs, CompactWorldStatusBar.cs.
- 기존: GameManager.cs, ActionSystem.cs, SaveManager.cs, Phase23Services.cs, SceneTransitionService.cs, SceneCatalog.cs, IdleActivityService.cs, BirthIntroductionPopup.cs, UIShell.cs, MissionUI.cs, PixelHomePresenter.cs.
- Editor: LifeWorldBuilder.cs, LifeWorldArtBuilder.cs, Phase26EditorBridge.cs.
- 검증: Development/Phase26Verification.cs.
- 실제 연결: Scenes/LifeWorld.unity, 기존 City/Workplace/StockMarket 상태바, Prefabs/GameContext.prefab, Data/Phase26/LifeWorldRules.asset, Data/SceneRoutes.asset, ProjectSettings/EditorBuildSettings.asset.
- 아트: Art/LifeWorld/*.png 및 메타데이터. 공식 캐릭터 원본 및 기존 완성된 배경/애니메이션은 교체하지 않았다.

## 실제 캡처

`Docs/Phase26/Screenshots/`: Home-World, Bathroom-World, Alley-World, City-World, City-World-Night, Workspace-World, Work-Context, Shop-Context, Housing-Furniture, World-Portrait, Birth-Large.
이전 화면 비교 자료는 기존 `Docs/Phase256/Screenshots/`에 보존했다.

## 남은 작업과 다음 재개 지점

1. 공식 이안의 정식 방향별/생활별 프레임 제작. 현재 기본 스프라이트·미러링·transform 동작/물 효과는 임시 연출이다. 기존 CharacterIdentity의 `idle-north/south/east/west`, `walk-north/south/east/west` 및 MotionLibrary의 행동 프레임을 연결할 수 있다.
2. 코드로 만든 개별 픽셀 가구/건물/바닥의 최종 아트 교체. 현재 기본 생활 기능과 충돌/상호작용이 연결된 대체 에셋이며 최종 상업용 아트 승인 상태는 아니다.
3. Windows 플레이어 빌드 → 실기기 입력/DPI/장시간 플레이 검증. 실제 키보드 Editor Play 외의 물리 게임패드·터치 조작은 미검증이다.
4. 필요 시 현재 preset 자리 변경을 자유 배치 UI로 확장하고 직업별 실내 표현을 보강한다. 이번 작업에서 Phase 2.7의 새 경제/부동산/NPC AI 시스템을 만들지는 않는다.

일반 실행: Bootstrap 씬에서 Play → 새 게임/이어하기 → E 상호작용, WASD 이동, Tab 상세 메뉴.
빌더 재생성이 필요할 때만 `Ian/Life World/Build Connected Life World`를 사용한다. Scene/Rules를 Inspector에서 조정한 뒤 빌더를 다시 실행하면 생성 기본값이 재적용되므로 변경을 먼저 보존한다.
검사를 재실행할 때는 Phase26EditorBridge의 stageA/cycle/ui/edge/integration/capture 명령을 선택한다. 원인 없이 전체 검사를 반복하지 않는다.

## 2026-11-09 긴급 집 레이아웃 / 카메라 복구
- 이전 115개 quality 검증은 이전 집/RenderTexture 경로 결과. 이번 변경의 증거로 대체하지 않는다.
- 카메라 원인: Main Camera 2D가 RenderTexture로만 출력, Overlay RawImage로 월드 표시. Display 1 직접 출력 카메라가 없어 Editor 경고 발생. 동일 카메라의 targetTexture를 해제하고 Display 1 직접 렌더링으로 변경. 카메라 추가 없음.
- 실제 LifeWorld 씬 저장: 15×10 집, 좌측 침실/책상/옷장, 우측 조리대/캐비닛 싱크/냉장고/식탁. 화장실은 별도 문으로 연결된 7.5×7 옥색 공간. 현관은 낮은 문턱으로 교체.
- 새 아트: StudioShell/StudioShellNight, HomeFittingsAtlas, EntryThreshold. 원본/이전 화면 유지. DailyActions 생성본은 아직 미연결이며 완료로 분류하지 않음.
- HomeLayoutPlan: .25 그리드, 공간 구역, 가구 점유영역, 문앞 예약 영역, 접근점, 플레이어 반경을 고려한 BFS 동선 검사. EconomyHousing의 배치 확정 전 순수 데이터 검사 + 실제 Physics2D 검사. 기존 보유권 유지, 구 배치만 안전한 슬롯으로 마이그레이션, 자리가 없으면 보관.
- HOME_DAY_CYCLE_RESULTS.json: Unity Play Mode 119개 통과, 실패 없음. 새 배치의 침대→샤워→음식 준비/식사→상점/가구 구매→배치→근무/임금→NPC대화→귀가→수면/다음날→저장/복원 확인. 컨트롤러 호출/UI 버튼 기반 통합검증이며 물리 키 입력 자동검증이 아님.
- 최종 시각 검사에서 칸막이 그림과 물리 점유 영역을 일치하도록 보정. 최종 보정 후 낮/밤/화장실 캡처와 접근성만 재검증 중(하루 전체 중복 실행하지 않음).
- 수정: Phase26HomeSetup.cs, HomeLayoutPlan.cs, EconomyHousing.cs, HousingCatalog.cs, HomeManagementPresenter.cs, LifeWorldController.cs, WorldPixelViewport.cs, WorldCameraController.cs, Phase26ArtSetup.cs, Phase26Verification.cs, Phase26EditorBridge.cs, LifeWorld 씬/규칙/주거 카탈로그.
- 남음: 실제 사용자 조작으로 새 집 동선 체감 확인, 미연결 일상 애니메이션 정리, 픽셀 밀도와 원근감의 추가 미술 검토. 상업용 아트 최종 승인/Phase 2.6 전체 완료를 선언하지 않음.
- Editor Console에는 Unity AI(UERP) ThreadAbort/NoSubscription 오류가 별도로 남아 있음. 게임 스크립트 컴파일 오류와 구분해서 추적.
### 긴급 집 최종 확인
- HOME_RESULTS.json: 마지막 칸막이 보정 후 31개 통과, UI issue 0. 1280×720 낮/밤 집 전체 및 옥색 화장실 캡처 확인.
- 실제 활성 카메라: Main Camera 2D / enabled / MainCamera tag / targetDisplay 0 / cullingMask -1 / targetTexture null / URP Base. 활성 Display 카메라 정확히 1개.
- 이전 Recovery-Home-Art.png와 Urgent-Home-1280x720.png를 직접 비교: 침실의 냉장고/노출된 세면대/야외 벤치 제거, 주방 가구와 침실 가구 크기 조정, 현관 통로 확보, 상단 HUD 폭 축소 유지.
- 마지막 테스트 후 Play 종료, 씬과 에셋 저장 완료. 기존 사용자 모델 및 저장 슬롯은 검사기가 원복함. 키보드 물리 입력/사용자 체감은 이번 집 수정 후 미검증.
- 다음 작업: 새 집에서 실제 WASD 이동/E 문·가구 조작 확인 → 미연결 일상 애니메이션 선별/연결 → 픽셀 밀도/원근감 추가 검토. Phase 2.6 전체 완료 아님.
## 2026-11-09 추가 보정 — 작은 집과 실제 물리 이동
- 현재 집 최종 크기 13.5×9 (앞선 15×10에서 균일 축소). 바닥/벽 아트의 종횡비 유지, 가구별 크기는 유지해 빈 공간을 축소. 침대/협탁/옷장/책상과 주방/식탁/현관 위치, 벽 충돌 및 복귀 위치를 함께 맞춤.
- 주방 조리대·싱크·냉장고 간격을 좁혀 하나의 작업 구역으로 연결. 문 안내를 '밖으로 나가기', '화장실 문 열기', '집으로 돌아가기'로 정리.
- HomeLayoutPlan v3: 기존 v2 배치를 새 방 경계에 맞게 마이그레이션. 소유권/화폐/다른 저장 데이터 유지. 필수 상호작용 접근점 자체가 점유된 경우 주변 셀의 접근성으로 대신 통과시키지 않음.
- HomeTraversalVerification: 실제 플레이어 Rigidbody2D를 현관부터 옷장/침대/책상/욕실 문/조리대/싱크/냉장고/식탁/의자/현관으로 이동. 고정 가구 점유영역 겹침 및 실제 외벽 충돌 확인. 키보드 입력을 주입하거나 게임의 포커스 조건을 우회하지 않음. 검증 중에만 모터를 중지해 Rigidbody를 직접 구동하므로 WASD 검증으로 분류하지 않음.
- HOME_ROUTE_RESULTS.json: 최종 소형 집 물리/배치/카메라/낮밤/HUD 검사 108개 통과, UI issue 0. 초기 경로 검사기는 실제 시작점을 벽 안의 그리드 셀로 반올림해 실패했음(HOME_ROUTE_START_GRID_FAILED.json 보존). 실제 시작점에서 연결 가능한 빈 셀을 선택하도록 검사기를 수정 후 통과. 게임 이동 오류로 잘못 보고하지 않음.
- 검증 실패 시 중첩 IEnumerator를 Dispose해 모터/임시 상태가 남지 않도록 검사기 복원 처리 보완.
- 최종 소형 집의 생활 행동/구매/저장은 HOME_COMPACT_CYCLE_RESULTS.json에서 119개 통과. 기존 HOME_DAY_CYCLE_RESULTS.json의 119개는 이전 배치 결과로 보존.
- 최종 검토 편의: LifeWorld 씬을 Editor에 열어 두었고, Edit Mode 카메라도 Play와 동일한 orthographic 4.8 / 집 중심으로 저장. 씬 dirty=false, 활성 Display 카메라 1개 확인.

## Visual Rebuild 再開 — 2026-11-09 (진행 중)
- E:\Projects\IanGrowing 경로 없음. 현재 연결된 실제 Unity 프로젝트는 C:\Users\user\Desktop\IanGrow\IanGrowing / Unity 6000.6.8f1. 일시 MCP handshaking 실패 후 연결 복구, 실제 씬 LifeWorld/Play 정지 상태 확인. 재실행/reset/clean 없음.
- 기존 스크린샷을 스타일 입력으로 사용하지 않음. 공식 Eastward/Fields of Mistria/Stardew/Little Witch 자료와 사용자 기준을 바탕으로 STYLE_GUIDE.md 작성.
- 새 오리지널 Furniture.png / Architecture.png를 Art/VisualRebuild에 저장. imagegen 내장 도구 사용. 4×4 모듈 아틀라스, 공통 논리 PPU40/32px 타일/Point/압축·mipmap 없음. 생성 결과의 정확한 픽셀 클러스터 품질은 별도 검토가 필요함.
- Phase26VisualRebuild.cs: 기존 상호작용 ID 유지, 방별 바닥/실제 벽 Collider/침실 문 통로/외부 노치의 L자 평면/별도 옥색 욕실로 변경. 이전 흰색 대각선 kitchen-divider 비활성화. 버전4 배치 마이그레이션은 소유권 보존.
- 첫 Game View에서 타일 검은 이음선, 큰 침실 문틀 가림, 공중 옷걸이 문제를 발견(First-GameView-Seams.png). 현재 타일 Rect 여백, 낮은 문틀/신발장 Sprite 부분 분리, 창문 높이를 보정하고 재검증 중.
- 첫 가구 검사에서 책장 슬롯이 침실 출입을 막아 실패(First-Placement-Failed.json 보존). 생활 공간 벽으로 슬롯 이동. 기본집/개선집의 접근 동선 및 가구별 유효 위치를 씬 저장 전에 검사하도록 확장.
- 반투명 배경 잔여 알파를 표준 cutout 셰이더 WorldAlphaCut으로 렌더에서 제거. 스프라이트/강조/구매 미리보기/소유 가구에 같은 material 적용. 원본 픽셀을 코드로 편집하지 않음.
- UITypeScale.baseSize까지 수정하여 HUD 글자 크기 변경이 Play 시작 시 덮어써지지 않도록 보정. 구역별 위치명 표시 추가.
- 수정 파일: Phase26VisualRebuild.cs, Phase26HomeSetup.cs, HomeLayoutPlan.cs, WorldVisualCatalog.cs, WorldPlayerMotor.cs, WorldPixelViewport.cs, LifeWorldController.cs, CompactWorldStatusBar.cs, Phase26Verification.cs, Phase26EditorBridge.cs, LifeWorld 씬/주거 카탈로그/규칙.
- 아직 완료 아님: 최종 집 화면 재확인/실제 동선/전체 생활 흐름/다양한 해상도, 도시 스타일 교체, 정식 생활 애니메이션, 물리 키 입력. 기존 통과 기록을 이번 변경의 통과 기록으로 사용하지 않음.

## 2026-11-13 World & UI Recovery
- LifeWorld의 사무실 직원 공용 목적지/동일 출입 스폰 문제 수정. 직원별 책상 9개, 공용 업무석, 실내 휴게·탕비실, 별도 화장실 구성.
- 증권·부동산 실내 및 도시 출입 경로 연결. 기존 단말기 기능과 ID 유지.
- 문 고정 프레임/경첩 메시, 개폐 상태·충돌·역방향 접근 수정. 기존 전환 서비스 유지.
- 기존 7개 씬의 공통 픽셀 UI 개선 및 반응형 부동산 설명 배치. 초기 집 및 경제 데이터 유지.
- 최초 Play 516개 검사 통과. 발견한 지갑 overflow/바닥 스케일 시각 문제 보완 후 집중 검증 중.
- 상세 파일/미완료 항목: `Docs/WorldRecovery/PROGRESS.md`. 전체 상업용 품질 완료로 판정하지 않음.

## 2026-11-13 Critical design & regression recovery
- 사용자 확인: 독립 외출은 아동기(기존 7세)부터. 취업·금융은 기존 19세. GrowthAccess를 문/이동/직접 거래/급여/테스트 씬 경로/복원에 연결. 유아기는 침대 돌봄과 기존 기초 학습 사용. 초기 집·60개 미션·보유 데이터 유지.
- 문 16개 앞뒤 실제 출입 215 PASS. 폐점 후 출구 제한, 일시정지 접근 타임아웃, 종료 시 파괴된 플레이어 접근 수정. 문 3종×3시간간격×20회 = 180회 개폐 및 모바일 구조 포함 590 PASS. 사용자 보고의 광범위한 문 고장이 모두 재현된 것은 아님.
- 기존 9명 근무 인물을 사무실5/증권2/부동산2의 실제 업무석에 연결. 기존 야간·점심·퇴근 유지, 정면 업무/서류 프레임 연결, 플레이어가 막으면 대기 후 퇴근 확인. 67개 시설/화면 경계/복원 검사 PASS.
- 성장 90 PASS, 기존 경제 40 PASS, 전체 QA 최종87 PASS/0 FAIL/미구현13/수동4. UI 감사0. 원본 저장3개 해시 일치.
- 실제 저장 씬: LifeWorld, City, Workplace, StockMarket, School, Home, MainMenu. 개별 Door prefab/Animator 새로 교체하지 않음.
- 미완료: 전면 착석/기립 연결, 풍부한 상담 AI, 상업용 미술 최종 검수, 실제 모바일 기기, Xcode 네이티브 iOS. 상세 및 다음 작업: `Docs/CriticalRecovery/RESULTS.md`.

- 최종 빌드: Android release 성공(오류0/경고1214), iOS release Xcode export 성공(오류0/경고628). iOS native/device와 실제 Android/Fold는 미검증. 구현 체크포인트 `5f4bea9`, 상세 결과·이미지·해시는 Docs/CriticalRecovery에 보존.

### 마지막 집중 수정 — 금융 고객 일정 및 NPC 문 접근
- 기존 주민2명을 증권/부동산 상담 시간표와 예약 지점에 연결. 실제 상담 사용 기록·도보 퇴장·기존 쇼핑 일정 복귀 13 PASS.
- 재현된 문 앞 정지 원인: 마지막 경로 격자점이 닫힌 문짝 쪽으로 반올림됨. WorldNPCNavigation의 마지막 구간을 실제 원형 충돌 검사로 정확한 접근점에 연결. 문 충돌/사용 계약 유지.
- 경로 수정 후 회사 퇴근·예약·4해상도·저장 복원 집중 회귀 67 PASS, UI issue0. 두 고객 Game View 화면 저장.
- 원본 자동 저장3개 SHA-256은 초기 백업과 일치. 고객/경로 수정 포함 최종 Android 릴리스 및 iOS export 성공(모두 오류0). StandaloneOSX/Edit Mode/LifeWorld dirty=false 확인. 최종 체크포인트는 Git 최신 커밋 참조.

## 2026-11-13 Door visual and entering alignment recovery
- Removed leaf/frame alpha fade and stale closed-door frame/highlight images. Existing textures now render on a thick hinged solid, fixed border mesh and separate sliding panels. WorldDoor remains the state/physics/lease owner.
- Confirmed entrance root/threshold mismatch (.45-.5 units). Aligned entrance presentation and aperture backing to physical threshold, retaining exit presentation, colliders and original arrival points. Traversal timeout now counts actual FixedUpdate simulation.
- Focused Play regression: 1,291 PASS, ten round trips per five pairs, 180 mechanism cycles, duplicate input, arrival, controls, disk save and actual NPC departures. Final visual-only backing adjustment followed by separate capture/entry/exit checks.
- Runtime changes: WorldDoorLeafVisual, WorldDoor, WorldInteractable, WorldPlayerMotor, LifeWorldController. Development verification and bridge updated. No scene/prefab/image/age/economy data regeneration. Runtime connection through existing WorldDoor instances.
- Evidence and remaining device/manual limitations: Docs/DoorVisual/RESULTS.md.

## 2026-11-14 PRE-PHASE 2.8 전체 QA 및 공식 보행 기준
- 현재 단계: Phase1–2.7 회귀 QA 종료 정리. Phase2.8 신규 개발 없음. 진입 판정 CONDITIONAL.
- 공식 보행1세 / 독립 외출7세 분리. 실제 모터의0세 이동 우회 차단, 돌봄 침대 접근 보장, 첫 걸음 안내1회·저장·터치 비겹침 적용.
- 미성년자 부채 상환/직접 납세 우회와 구버전 재저장 null 선택 블록 오류 수정. QA 생애주기 preset10개와 추가 무결성/부정 경로 검사.
- 마지막 전체 Registry139개 실행: PASS122 / FAIL0 / NI13 / MANUAL4. 기획 범위 보정: PASS111 / PARTIAL11 / NI2 / MANUAL4 / N/A11. 자연 성장 M001–M038 완료(27세/DAY297); 나이·현금·능력치 강제 지급 없음.
- 별도 실제 Play: 출생17, 금융 제한8, 공식 보행/안내44, UI95, 회사/금융시설67, 고객13, 주민/모바일/교통83 assertion 통과. 문 전체 검사 중 회사 화장실 조작권1회 실패, 해당2문32개 집중 재검사 통과; 간헐 원인 미확정.
- 최종 전체 기간 해시 대조에서 생산 자동 저장2개의 시간/시세 등 변경 발견. 현금·미션·직업·runId 동일. 테스트 종료 시 Editor 보호 해제 틈을 수정하고 변경본을 별도 보관한 뒤 시작 백업 복원. Play 및 종료 후 Edit 실제 쓰기 거부 확인. 생산3개 시작 SHA-256 복구 확인은 Docs/PrePhase28/SAVE_FINAL_HASHES.json.
- 변경 스크립트: WorldPlayerMotor, LifeWorldController, LifeProgression, GrowthAccess, WorldHUD/WorldMobileHUD, EconomySystem/EconomyFinance/FinancePresenter, SaveManager/GameManager, 기존 QA/Editor bridge와 PrePhase28 검증 코드. 씬·프리팹·미션 데이터·공식 이미지 재생성 없음.
- 기록: Docs/PRE_PHASE2_8_FULL_QA_REPORT.md, Docs/PATCH_NOTES_PRE_PHASE2_8_QA.md, Docs/PrePhase28/FEATURE_MATRIX.csv, MISSIONS_60.md, 실제 JSON/스크린샷.
- 남은 항목: 영아 전용 아트/보호자 외출, 정식 가챠/확장 장비, 후속 단계 미션, 물리 입력·실제 Android/iOS·Fold, 간헐 화장실 조작권, Play 중 코드 hot reload. 현재 범위 밖 기능을 성공으로 처리하지 않음.
- 마지막 저장: LifeWorld / Edit Mode / StandaloneOSX / dirty=false. 로컬 Git 체크포인트는 본 QA 안정화 커밋. 원격 push 없음; 작업 시작 및 관찰 세이브 모두 DevelopmentBackups/PrePhase28-20261114에 보존.


## 2026-11-14 시설 품질 표준화 — Workspace 보호
- 적용: LifeWorld 씬, LifeWorldRules, FacilityActivitySeat prefab 저장. 학교 명칭/교실, 상점 진열·계산·식사·가구 구역, 금융/부동산 업무 자료와 휴식 좌석 개선. 별도 식당/카페 씬 생성 없음.
- Workspace/StarterHome 편집 전후 구조 서명 동일. 기존 9개 정면 업무석과 초기 집 침대1개·고정 욕실 규칙 유지. 회사 레이아웃 재생성 없음.
- 기존 주민12명+교사/학생/점원3명. 새 좌석은 접근·예약·점유·착석·기립·해제 기준점을 사용. 기존 슬롯은 opt-in 미사용으로 유지. 실제 방향 이동 후 가구 방향 작업, 새 4프레임 학교/식사/음료/점원 원본 아트 연결.
- 버그 수정: 새 앵커 도착 오차와 경로 종료 오차 불일치; 바닥 잔여 scale; 상담 뒤 쇼핑 시간차로 고객 체류가 길어지는 일정. 기존 야간 근무자 저녁 일정을 보존.
- 실제 Play: 시설 통합114 / 회사·금융67 / 금융 고객13 / 실제 회사 급여10 / 점원 퇴장10 / Editor 모바일48 / 금융 실내 UI13 / 저장 보호11 assertion PASS, UI issue0. 기능 전용88도 통과했으며 최종 통합에 포함. 전체 Registry141개: PASS124 / FAIL0 / NOT IMPLEMENTED13 / MANUAL4. 실행별 중복 assertion을 고유 테스트 수로 합산하지 않음.
- 테스트 실패/수정 근거와 Game View 전후: Docs/FACILITY_QUALITY_STANDARDIZATION.md 및 Docs/FacilityStandardization/RESULTS.md. 최종 화면 캡처는 실제 Play에서 별도 수행.
- 저장: 사용자가 직접 만든1세·100G 진행을 확인받아 보존. 시작0세 백업으로 덮어쓰지 않음. SAVE_OBSERVED_BASELINE 기준 생산3파일 SHA-256 일치. 자동 검증 종료 후 저장 보호 해제 및 일반 플레이 저장 복구.
- 미검증: 실제 Android/iOS/Fold·물리 입력·저사양 발열/FPS·상업용 미술 최종 검수. 이번 수정 후 네이티브 모바일 빌드 미실행. 일부 비착석 행동의 전용 방향 아트가 없으면 올바른 방향 Idle 사용; 전 행동 완성으로 보고하지 않음.
- 주요 C#: WorldActivitySlot, WorldNPCScheduler, WorldFacilityActivityPresentation, WorldHUD, LifeWorldController, FacilityStandardizationSetup, Facility*Verification, FacilityQualityScenarios, SaveManager/Editor bridge. 공식 이안 이미지·미션60개·문 개폐 스크립트 변경 없음.
- 마지막 체크포인트: 시설 표준화 Git 커밋 참조. 로컬 백업 DevelopmentBackups/FacilityStandardization-20261114. 원격 설정은 있으나 이번 변경 push 없음.

## 2026-11-14 Family Life 및 부모님 집 구조 긴급 복구

- 최신 기획: 새 게임은 가구 완비 부모님 집. 19세 이후 기존500G 비용과 확인 창으로 선택 독립, 자동 퇴거 없음. 첫 독립 집만 침대1개/나무 장판/흰 벽지/옥색 고정 욕실. 가족 가구와 개인 소유·보관·배치를 구분하고 기존 자산·미션·직업을 보존한다.
- 초기 단일 가족 공간 시안은 잘못된 설계였다. FamilyHomeArchitecturePlan v3/FamilyArchitectureSetup으로 부모 침실·이안 방·거실·주방·욕실을 실제 벽·문·충돌로 나눴다. 부모별 초기 위치와 단일 가족 식탁을 연결했다. 같은 LifeWorld 구역이며 방마다 Scene을 만들지 않았다.
- 기존 WorldDoor와 문짝 스크립트를 교체하지 않았다. 기존 Controller의 같은 구역 통과와 NPC 내부 문 사용을 확장했다. 기존 주요 시설의 생성 전후 서명은 일치한다.
- 가족 상태·관계·식사·분담 가사·외출은 기존 World snapshot과 scheduler를 사용한다. 수면/기상/냉장고/식사 연출 어댑터는 기존 효과·경제·저장을 호출한다. 보행1세/독립 외출7세/선택 독립19세를 구분한다.
- 실제 Play: 공간48, 부모 생활/공동 식사30, 생활 연출·취소26, 독립/저장35, 주요 문60, 회사67, 고객13, Editor 모바일48, 금융UI13 assertion 통과. 전체 QA146개 최종 PASS128 / FAIL0 / NI13 / MANUAL5. 전체 실행의 신규 평면 검사1개는 대기 보완 후 해당 항목만 재검증했다. 마지막35개에는 미성년 독립 플래그 복원 방어와 자산 보존을 포함한다.
- 기록/수정 파일/8개 요청 화면: Docs/FAMILY_HOME_ARCHITECTURE_RECOVERY.md 및 Docs/FamilyLife/RESULTS.md, PROGRESS.md, FLOOR_PLAN.json. 새 원본4PNG와 프롬프트를 보존하고 공식 이안 원본은 변경하지 않았다.
- 사용자1세·100G 생산 저장3개 해시를 유지했다. DevelopmentBackups/FamilyLife-20261114에 시작 파일/씬/규칙을 백업했다. 최종 씬 저장·일반 저장 권한 복구·로컬 Git 체크포인트로 보존하며 원격 push는 하지 않는다.
- 남은 전체 생활 범위: 부모 돌봄 호출 동선, 가사/TV/연령별 전용 동작, 완전한 오프라인 조리 이력, 실제 기기/키보드/미술 검수. 긴급 공간 복구를 모든 생활 모션 완성으로 보고하지 않는다.


## 2026-11-14 독립 Scene 및 주거 공간 개편

- 최신 요청을 반영하여 이전 단일 가족 집 방 구성을 일반 플레이 경로에서 독립 Scene16개로 전환. 가족5방, 개인 스튜디오/욕실, 도시/골목 및 기존 시설. 원본 LifeWorld/Rules는 백업과 바이트 동일, 기존 SceneId 숫자 유지.
- 거실9.6×7, 가족 욕실5.2×4.6 등으로 축소. 방별 벽·문·충돌·가구 배치·NPC 목적지를 저장. 잘못된 하단 Pivot과 Static sorting을 실제 배치 기준으로 맞춤. 기존 회사5개 업무석 및 금융4개 업무석을 재설계하지 않음.
- 전역 GameContext/시계/성장/경제/미션/관계 공유. 현재 공간만 활성화한 뒤 이전 씬 언로드. NPC17명 논리 데이터와 현재 방 실제 인물을 분리하며 기존 좌석·경로·문 스크립트를 재사용.
- 독립 전 개인 집 문과 두 표시판 숨김, 내부 실행 제한 유지. 보행1세/독립 외출7세/선택 독립19세·500G 및 첫 개인 집 침대1개를 유지. 개인 가구 RoomId/HomeId/ownership/installed/direction 기본값 마이그레이션.
- 실제 Play:16씬 상태160,30문222, 저장/조리/모바일109, 축소 주거/가족 문/독립157, 최종 UI/새 게임69, 가족 캡처20, 공동 식탁10 assertion 통과. 실행 간 중복이 있어 합산 고유 테스트 수로 보고하지 않음. 초기 UI5건 수정 후 최종0건. QA Center 새 Scene4항목 포함11개 선택 재실행 PASS; 누적150개132PASS/13NI/5MANUAL은 과거 이력 포함.
- 새 게임 전환에서 출생 팝업 보호 때문에 종료되지 않는 while을 실제 발견. Editor 강제 종료 후 저장된 프로젝트로 복구하고 진행 여부 검사로 수정. 최종69개 실행에서 도시→0세 방/출생/돌봄 접근까지 재검증 통과. 생산3파일 해시 유지.
- Android IL2CPP arm64 Development APK 생성 성공, iOS는 iPhoneOS SDK 미설치로 네이티브 빌드 BLOCKED. 실제 Android/iOS/Fold·성능·전체 생활 제스처·정확한 전체 오프라인 조리 이력은 미검증/잔여 범위.
- 상세: Docs/RESIDENTIAL_SCENE_ARCHITECTURE.md, Docs/SceneArchitecture/RESULTS.md, Docs/PATCH_NOTES_SCENE_ARCHITECTURE_2026-11-14.md. 재개 지점: Docs/SCENE_ARCHITECTURE_PROGRESS.md.
- 사용자1세/100G 세이브를 시작 해시로 보존. 로컬 백업 DevelopmentBackups/SceneArchitecture-20261114. 이번 변경 원격 push 없음.


## 2026-11-14 — ROOM VISUAL & CAMERA QUALITY RECOVERY

- 분리 씬 유지. 가족 욕실5.2×3.8/후벽1.0/720p ortho3.214286, 실제14실 공통 카메라 설정 저장.
- 낮은 대비의 주택 외곽 표현, 밝은 문 안쪽 면·문턱, 중복 장식 제거, 독립 욕실 disabled/Scale 잔재 복구.
- 기존 샤워2개에 부스 유리문·내부/퇴장 Anchor·실제 actor/물 모션 연결. 기존 효과/연령 제한/출입 WorldDoor는 보존.
- 실내6개 최종 Play 시나리오457개 검사 통과.30경로 왕복/14실/샤워2개/저장·가족 조리·모바일 시뮬레이션 확인. 별도3종 문 반복120회 상태·좌표 검사 근거 보존.
- Workspace 가구5업무석/NPC 일정 유지. 기존 프리팹은 재사용하고 수정하지 않음.
- 상세 변경·검증·미검증: `Docs/RoomVisual/RESULTS.md`. 패치노트: `Docs/PATCH_NOTES_ROOM_VISUAL_2026_11_14.md`.
- 로컬 기준 백업: `DevelopmentBackups/RoomVisual-20261114` (운영 세이브 포함, Git 제외). 마지막 Git 체크포인트는 해당 보고서 참조.
