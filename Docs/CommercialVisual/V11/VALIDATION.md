# Validation — v1.1

## 도구 및 보존

공식 Unity 플러그인의 unity-cli, craft-game-floors와 아트/피드백 참고 문서, 2d-pixel-perfect, ui/ui-ugui, sprite-editor, manage-sprite-atlas, optimize-text-mesh-pro 및 로컬 game-ui-ux 문서를 실제 읽고 적용했다. CLI beta13 실행은 가능하지만 Editor descriptor 접근 제한 때문에 연결하지 못했다. 연결된 Unity MCP에서 해당 프로젝트·버전을 확인하고 C# Editor 작업, Play, 캡처, Console 검증을 수행했다. 이미지 제작은 built-in imagegen, 공식 폰트 출처 확인은 Firecrawl을 사용했다. Unity AI 서비스 생성 기능은 사용하지 않았다.

체크포인트 태그 `visual-addendum-v1.1-start` 기준 GameManager, ActionSystem, PlayerStats, EconomySystem, SaveManager, MissionService와 공식 이안 원본을 수정하지 않았다. 씬 이름과 기존 서비스/저장 스키마를 유지했다. 렌더러, UI/prefab, 표현 계층과 Editor 제작/검증 도구를 변경했다.

## 실제 실행 결과

| 검사 | 결과 | 증거 |
|---|---|---|
| 새 addendum 통합 | **204 통과, 실패 없음** | [TEST_RESULTS.json](TEST_RESULTS.json) |
| 기존 Phase2.1 | **45 통과** | [PHASE21_RESULTS.txt](PHASE21_RESULTS.txt) |
| 기존 Phase2.2 | **169 통과** | [PHASE22_RESULTS.txt](PHASE22_RESULTS.txt) |
| Phase23 기존 통합 | **440 통과 후 중단, 전체 green 아님** | [PHASE23_RESULTS.json](PHASE23_RESULTS.json) |
| 구형 Stage1 UI | `FAIL: Work button` | 위 파일의 failure/stack trace |
| 컴파일 | Editor scriptCompilationFailed=false / isCompiling=false | 최종 MCP 확인 |
| Console | 해당 최종 검증 및 Play 종료 구간 error/warning 0 | 최종 Console 조회 |

구형 Stage1은 교체 전 UIManager.workButton을 현재 씬에서 직접 누르는 화면 계약이다. 새 UI는 실제 Workplace 근무 메뉴로 옮겼기 때문에 이 검사가 실패한다. 기존 테스트를 약화하거나 전체 통과로 바꾸지 않았다. 실제 Workplace 근무 실행·급여·중복 지급 방지·지급 연출은 새 테스트와 Phase23 본문에서 통과했다. 현재 남은 테스트 마이그레이션 작업이다.

Phase2.1/2.2는 원래 선택적 기능 계약에 맞춰 Phase23 catalog를 잠시 비활성화해 실행했다. 이 두 기존 검사의 복원 스냅샷에는 phase23 블록이 없어, 바깥에서 **phase23 포함 전체 스냅샷 + 별도 임시 슬롯**으로 감쌌다. Phase23 검사 후에도 전체 모델·원래 슬롯·autosave/realtime 플래그를 복원했다. 사용자 원본 저장 파일을 테스트 fixture로 덮어쓰지 않았다.

## 새 통합 검사의 범위

- MainMenu 및 네 씬의 TMP 역할과 현재 문자열 글리프, 현대 한글 11,172개, Bitmap/Point/Static 설정, 숫자 동일 advance.
- Home→City, City→Workplace, Workplace→Home, Home→School, School→City 실로드. 중복 요청 거부, EventSystem 동시 활성 최대1, manager 동일 인스턴스, 모든 스탯/경제/생존/시간/미션/성장/직업 스냅샷 동일.
- 공통 PixelPerfect 640×360/100PPU, 공식 이안 참조, 메인 화면 활성 버튼 최대12. 활동창 탭 중복 없음, 단일 페이지, 닫기 실제 touch raycast/pointerClick.
- 3개 독립 렌더 깊이 밴드, 실제 walk 위치/프레임 변화, actor4프레임, high24/low8/off0 비 효과, 낮/저녁/밤 색조 변화.
- office/developer/analyst/convenience 기존 job ID로 theme 전환. convenience만 별도 실내인지 확인.
- 실제 근무 action0 수행, 기록된 wage만큼 1회 현금 증가, 중복 급여 false, 명세서·8개 pooled coin 노출. 과로 health threshold 조명 변화.
- 저장/로드 전후 모든 이산 상태 및 급여 일치. JsonUtility double 왕복에서 생존 hunger/thirst remainder에 약 1e-17 차이가 있어 **그 두 값만 1e-12 허용**, 나머지 전체 JSON은 동일. 원본/로드 결과 [Save-Expected.json](Save-Expected.json), [Save-Actual.json](Save-Actual.json). 저장 코드를 바꾸지 않았다.
- 60개 미션 유지. 1280×720,1920×1080,2400×1080 실제 Game View 캡처와 네 장소 UI 경계/글리프 검사. 1920은 공통3배,1280은2배,2400은16:9 월드 여백과 독립 UI 스케일 확인.

기존 슬라이스의 로딩 실패 복구 42개 결과는 상위 [기존 검증 기록](../VALIDATION.md)에 남아 있다. 이번 변경은 실패 복구/게임 데이터 로직을 교체하지 않았다. 새 전환 검사는 프로필12개, 지정 경로, 중복 입력과 상태 보존을 확인한다. 매 프레임 영상 분석이나 모든 기기에서 무깜빡임을 보증하는 것은 아니다.

## 성능과 실행 한계

City/Workplace는 장소당 12Hz 환경 coroutine 하나로 prop/frame/rain을 관리한다. 오브젝트별 Update를 추가하지 않았다. rain LineRenderer24개, 급여 coin8개를 재사용한다. 시간 UI는 분 단위 변경 때만 갱신한다. 정지/효과0/감소 동작 설정에 따라 효과를 제한한다. 씬별 환경은 해당 씬과 함께 해제되고 공유 상태는 persistent context에 남는다.

43 sprite V2 pack과 runtime sprite.packed=true,512×1024 RGBA32를 확인했다. 폰트 정적 Alpha8 및 배경 파생본 사용은 메모리 의도를 반영한 구현이며, FPS·전력·열 개선 실측 수치를 주장하지 않는다. Windows player build, APK/AAB, 실제 Android 터치·발열·메모리·GPU 측정은 미실행이다.

20:9 확대 검수에서 URP crop 바깥에 반복된 테두리 픽셀이 남는 문제를 발견했다. 네 씬에 PixelViewportMatte를 추가해 정수 배율 월드 바깥만 공통 남색으로 덮었다. HUD보다 아래에 놓이고 raycast를 받지 않는다. Canvas render 이벤트에서 화면 크기가 바뀔 때만 네 RectTransform을 갱신한다. 정수 월드 크기나 UI 메뉴 배치를 바꾸지 않는다. 수정 후 세 해상도 통합 검사를 다시 실행하고 캡처를 갱신했다.

임포트 과정의 기존 Unity AI 서비스 `NoSubscription` 및 기존 obsolete API 경고는 런타임 검증과 구분한다. 서비스 구독을 임의 변경하지 않았다. 최종 정상 구간만 0건이라고 기록한다.

## 시각 검수 판정

Home 갈색/호박·푸른 그림자, School 청록·찬 햇빛, City 남색/보라 네온, Workplace 회청색·형광등으로 장소 구분이 생겼다. 같은 프레임·폰트·PPU로 연결된다. City/Workplace는 이전 평면 임시 공간에서 디테일한 실제 이미지로 개선됐다. 배경 밀도와 가독성은 좋아졌지만 공식 이안의 원본 묘사/해상도와 배경의 차이, 일부 NPC 등록점/알파, 구워진 교실 학생/방 가구, 완전 분리 레이어·전용 동작·소리 부재가 남아 있다. 모든 항목을 최종 상업용 품질로 승인하지 않는다. 정확한 제작 목록은 [ASSET_REGISTER.md](ASSET_REGISTER.md).
