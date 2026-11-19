# 출시 준비도 · 2026-11-19

**출시 판정 HOLD.** M039의 조건을 플레이로 충족할 수 없어 신규 인생을 최종 생애 평가까지 진행할 수 없다. Windows 개발 빌드 생성과 iOS 내부 검토용 내보내기는 성공했다.

## 결함 수

| 심각도 | 발견 | 수정·재검증 | 남음 |
|---|---:|---:|---:|
| BLOCKER | 1 | 0 | 1 |
| CRITICAL | 1 | 1 | 0 |
| HIGH | 1 | 1 | 0 |
| MEDIUM | 4 | 4 | 0 |
| LOW | 2 | 2 | 0 |

총 9개 결함 그룹 중 8개를 수정했다. 미제공 콘텐츠, 프로젝트 법적 승인, 서명 인증 정보 및 물리 기기 미검증은 별도로 추적한다. 확인한 데이터 손상 위험은 대형 저장의 쓰기·읽기 크기 불일치였고, 수정 후 원본 보존까지 재검증했다. [결함별 원인·재현](BUG_FIX_REPORT.md)

## 실제로 확인한 범위

| 영역 | 결과 |
|---|---|
| 신규 인생·미션 | 같은 저장으로 M001–M038을 실제 행동·선행 조건·보상 경로로 완료·저장·복원. M039에서 차단. M040–050은 개별 fixture, M051–060은 기능 공급원 미구현. [E2E](QAFull/e2e-final.json) · [첫 차단](QAFull/gate.json) |
| 실행형 회귀 | 등록기 160개 중 142 PASS, 0 FAIL, 11개 미구현, 6개 수동, 1개 별도 수동 저장 부재. Unity Test Framework 9 PASS, 0 FAIL, 명시적 1개 SKIP. 두 집합에는 기능 중복이 있으므로 합산하지 않았다. [등록기](QAFull/registry-full.json) · [Test Runner](QAFull/framework-final.json) |
| 경제·세이브 | v1–4 디스크 복원, 손상/미래 버전 거절, 기존 실제 저장 3개 해시 불변. 10 MiB 대형 저장의 쓰기·읽기 불일치를 수정했고, 새 16 MiB 한도 초과 시 이전 체크포인트가 그대로 남는다. [대형 저장](QAFull/size-after.json) 장비·가챠 88개, 루비·약관 84개 assertion. 분할 매수·평단·실현 손익·시장 주기·뉴스 중복과 600초 정산·재로드 22개 검사. 설정된 근무 주기 1200초여서 첫 600초의 급여는 0이었다. [보존](QAFull/save-preservation.json) · [정산](QAFull/market-and-clock.json) |
| 씬·UI | 16개 방과 7개 구 경로, Bootstrap·MainMenu. 실제 문 이동과 9단계 주요 루트, 10개 필수 화면 카테고리. UI 통합 60개 검사와 6개 렌더 Canvas 크기. 물리 디스플레이는 별도다. [UI](QAFull/ux-retest.json) · [화면](QAFull/Screenshots/) |
| 입력·월드 | 별도 macOS 개발 플레이어에서 17개 실제 Input System 검사. 도시 출퇴근/야간 인구·근무·버스·상점·귀가 47개 검사. [입력](QAFull/input-player.json) · [월드](QAFull/world.json) |
| 성능 | 도심 08시 NPC 8·차량 6에서 draw call 104, Editor Game View 표본 약 333 FPS, 최대 프레임 78.1ms. 별도 왕복 중 도시 순간 GC 할당도 관측됐다. 도시↔상점 6회 왕복에서 이전 방 환경음 중첩은 없었다. 가챠 창 120프레임의 최대 GC 할당은 12,029바이트였다. 실기기 목표 FPS와 긴 세션의 누수 판정은 보류했다. [가챠·메모리 표본](QAFull/memory-audio-samples.json) [월드 표본](QAFull/world.json) · [CPU·GC](QAFull/route-profiler.json) |
| 데이터·화면 | 직렬화 참조 32→0, Missing Script 0. 60개 미션 조건 트리 동일. 야간 보행 영역 중앙 휘도 19→48. [참조](QAFull/refs-after.json) · [야간 비교](QAFull/night-contrast.json) |

## 빌드 및 배포

| 대상 | 상태 | 산출물·남은 조건 |
|---|---|---|
| Windows x64 개발 | PASS | [IanGrowing.exe](../Builds/QAFull/StandaloneWindows64/IanGrowing.exe)와 같은 폴더의 Data·DLL·UnityPlayer가 한 세트다. Unity 오류 0, 경고 536건. Wine 11 null graphics에서 엔진·입력 초기화 및 51초 기동을 관찰했다. 네이티브 Windows 그래픽·실제 키보드는 미검증. |
| Android Release | BLOCKED | 비개발 스크립트 컴파일로 75개 어셈블리 생성. Production에 법적 승인·사업자 정보·URL·앱 식별자 등 31건 차단. 릴리스 서명 정보 미확보. 서명 AAB·기기 실행 결과 없음. [검사](QAFull/build-Android.json) |
| iOS 내부 검토 | PASS | [Unity-iPhone.xcodeproj](../Builds/QAFull/iOS/Xcode/Unity-iPhone.xcodeproj). Unity 오류 0, 경고 461건. Production 검증은 별도 엄격 모드로 유지. |
| iOS 네이티브·배포 | BLOCKED | Release 기기 컴파일 명령은 설치된 Xcode의 라이선스 미동의로 종료 코드 69. 키체인 유효 코드서명 신원 0개. Archive, IPA, 기기 실행 미검증. [네이티브 결과](QAFull/native-ios.json) · [검증 로그 요약](QAFull/build-verification.json) |

Android 비개발 DLL에서 개발 콘솔·관리 명령·QA 등록기·가짜 결제 provider 형식이 제외된 것을 확인했다. 양 플랫폼의 서명·법무 승인은 여전히 필요하다. 최종 빌드에는 Unity API 사용 중단 및 분석기 경고가 남는다. [릴리스 형식 검사](QAFull/release-types.json) · [타깃별 정책](QAFull/data.json)

출시 판정을 다시 내리려면 M038 저장에서 실제 이벤트 선택·은퇴·생애 평가를 끝까지 재시험하고, 스토어 법무·서명·기기 실행, OS 종료 중 저장 복구, 장시간 성능과 청취 검수를 마쳐야 한다. 완료율 백분율은 산정하지 않았다. [전체 케이스와 미검증 항목](QA_FULL_PROJECT.md)

Unity Editor의 AI 서비스에서 NoSubscription 메시지가 반복됐다. 이번 검사 구간의 NullReferenceException·MissingReferenceException·IndexOutOfRangeException은 0건이었다. 최종 콘솔 스냅샷에는 오류와 경고가 없었다. [구간 집계](QAFull/editor-log-summary.json) · [최종 콘솔](QAFull/console-final.json)
