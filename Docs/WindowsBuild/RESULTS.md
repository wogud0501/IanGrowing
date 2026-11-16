# Windows validation fix — 실행 결과

2026-11-17 KST · Unity6000.6.8f1. 기존 ReleaseBuildValidation의 비개발=Production 판정을 target/channel/intent 기반으로 분리했습니다. Gameplay·약관 내용·실제 사업자/URL/모바일 ID는 변경하지 않았습니다.

| 실제 Windows x64 빌드 | 결과 | 오류 | 경고 | 출력 |
|---|---|---:|---:|---|
| Development ON | Succeeded | 0 | 741 | Builds/Windows/Development/IanGrowing.exe |
| InternalQA, Development/Script Debugging OFF | Succeeded | 0 | 736 | Builds/Windows/IanGrowing.exe |

[Development 원시 BuildReport](Development_RESULT.json), [InternalQA 원시 BuildReport](InternalQA_RESULT.json). 두 exe 모두 실제 파일이며 PE32+ x86-64(0x8664), UnityPlayer.dll/Data/Managed assembly 확인. [아티팩트·SHA-256](ARTIFACTS.json). 내부 QA 빌드에서는 MockPurchaseProvider/DeveloperQACenter가 컴파일되지 않았고 Development에서는 개발 도구가 허용됩니다.

두 빌드 모두 법률 검토 미완료·사업자 Placeholder·Privacy 승인 미완료·미등록 URL을 유지한 상태였습니다. 법적 진단30개는 한 번의 묶음 경고이며 Fatal0, 모바일 ID/Store build-number 진단0입니다. [개발 검증](Development_VALIDATION.json), [내부 QA 검증](InternalQA_VALIDATION.json).

공유 Validator 실행 테스트14개 PASS: A 개발 허용/B 모바일 ID 검사 제외/C Android Production 법적 정보 누락 거부/D iOS Production URL 누락 거부/E Windows Startup 누락 거부. 추가로 명시적 Production/Store intent 우선, 모바일에 잘못 지정된 WindowsTest·옛 review 토글 우회 거부, release-style InternalQA 허용, 깨진 가챠 데이터 Fatal을 확인했습니다. [정책 테스트](POLICY_TESTS.json). C/D/E는 실제 공용 검증·BuildFailedException 실행 결과이며 전체 모바일 Native 빌드나 실제 Scene 삭제를 수행한 결과는 아닙니다. 프로젝트의 모바일 식별자는 비어 있는 정식 ID 대신 기존 Unity 템플릿 상태를 유지했습니다.

남은 경고: 법적 미확정 묶음, 기존 FindObjects API 폐기 예정, MissionCondition 직렬화 깊이, Unity AI Inference/Sentis Shader 경고 등. [Development 전체](Development_WARNINGS.txt), [InternalQA 전체](InternalQA_WARNINGS.txt). 이번 수정에서 관련 시스템을 재작성하거나 경고를 숨기지 않았습니다.

현재 macOS에서 빌드와 PE/동반 파일 검증을 수행했습니다. Windows 실제 launch·플레이·Save는 **NOT VERIFIED**입니다. exe만 단독 복사하지 말고 해당 출력 폴더의 UnityPlayer.dll/Data/기타 의존 파일을 함께 사용해야 합니다. 내부 테스트용이며 공개 배포 승인은 아닙니다.

[정책·프리셋·CLI](../BUILD_VALIDATION_POLICY.md). 수정: ReleaseFoundationSetup.cs, BuildValidationContext.cs, WindowsBuildValidationVerification.cs 및 기존 Editor 명령 브리지. 씬·프리팹·게임 데이터 수정 없음.

Editor는 원래 FamilyHome_IanBedroom / StandaloneWindows64, Play 종료, scene dirty=false, 이전 빌드 옵션 복원. 기존 사용자 작업 에셋을 작업 전 바이트로 보존하고 PlayerSettings/EditorSettings 변경이 없음을 확인했습니다. [Editor](EDITOR_FINAL.json), [기존 세이브3개 해시 동일](SAVE_VERIFICATION.json), [저장 보호 해제](SAVE_GUARD_RELEASED.json).
