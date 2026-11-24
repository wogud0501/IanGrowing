# 로컬 오류 진단 QA

2026-11-24, Unity 6000.6.8f1. 원본 플레이어 저장 슬롯과 분리한 제품 ID로 Development Player를 빌드했다. 실제 타이틀 설정의 버튼에서 미리보기와 내보내기까지 실행했다. [생성된 TXT](Diagnostics/macos-development-exception.txt) · [같은 사건의 JSON](Diagnostics/macos-development-exception.json)

| 시험 | 관찰 결과 |
|---|---|
| macOS 관리 예외 | MainMenu에서 null을 역참조하고 같은 예외를 5회 기록했다. 생성 1건. 보고서 ID ERR-20261124-105836-ac622e2e6b2d4a49b81c95d704af4fbb, LocalDiagnosticsProbe.ReadLength, LocalDiagnosticsProbe.cs 36행. MainMenu, DAY 1, 나이 0 및 직전 장면 진입 기록 포함 |
| Error, Assert, 수동 생성 | 각 1건. 기술 상세와 내보낸 TXT의 내용 일치. 정상 오류 메시지는 유지하고 시험용 가짜 Bearer 토큰·이메일·사용자 디렉터리는 TXT/JSON에서 제외 |
| 다른 스레드 | Development Player 작업 스레드에서 발생한 Error가 메인 스레드 큐 배출 후 1건 추가됨 |
| 정상 종료와 재실행 | macOS 첫 실행 0건 → 정상 종료·재실행 4건, 중단 안내 없음. SIGKILL 후 재실행 5건, 이전 실행 확인 표시. 확인되지 않은 Native Crash를 확정하지 않음 |
| 보존 제한 | 실제 저장소 코드로 24건을 써서 20건 유지. 15일 전 디렉터리 제거. 11 MiB 관리 파일을 넣은 보고서 제거 |
| 저장 실패 | Errors 자리를 파일로 막은 경우 실패 반환. 별도 12 MiB HFS+ 디스크 이미지의 가용 블록을 0으로 만든 시험에서도 보고서 쓰기 실패를 반환했고 Player가 계속 실행됨 |
| 삭제 및 Save | 개별 삭제 후 해당 로컬 내보내기 사본 제거. 설정의 전체 삭제 → 확인 후 목록 0건. 테스트 게임 Save가 삭제 전후 모두 로드됨 |
| Windows | Windows x64 Development 빌드를 Wine의 headless 모드로 실행. 예외 1건, 36행, 수동 보고서와 Exports TXT 확인. 네이티브 Windows 데스크톱에서 Explorer 표시와 OS 덤프 생성·전달은 NOT VERIFIED |
| Android | API 35 ARM64 에뮬레이터에서 IL2CPP Development APK 실행. 36행 확인. 설정에서 시스템 ChooserActivity 진입, 뒤로 돌아온 후 위치 지정 내보내기에서 DocumentsUI 선택기로 Downloads에 저장. 저장 파일과 미리보기 바이트 일치. 앱을 백그라운드로 보내고 에뮬레이터 재부팅 후 4건의 SHA-256 모두 동일, 종료 의심 알림 없음 |
| iOS | iPhone 17e ARM64 시뮬레이터에서 IL2CPP Development Player 실행. 실제 예외 한 건과 36행을 확인했다. 수동 보고서 선택·확인 후 시스템 공유 시트에 TXT와 ‘파일에 저장’이 표시됐으며 공유에 건넨 파일과 미리보기 내용이 일치했다. [공유 화면](Diagnostics/ios-share.png) |

물리 Android/iOS 기기와 네이티브 Windows 기기, 드라이버·메모리 부족으로 인한 OS 덤프, 출시 바이너리의 SDK 네트워크 패킷은 NOT VERIFIED. 운영체제 재부팅은 Android 에뮬레이터에서 수행했다. 파일 수와 용량은 게임이 관리하는 보고서에 대한 측정이다.
