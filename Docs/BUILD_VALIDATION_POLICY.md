# Build Validation Policy

Windows 빌드가 모바일 Store 출시 정보 때문에 차단되던 문제를 기존 `ReleaseBuildValidation`에서 수정했습니다. 약관·개인정보 시스템과 게임 코드는 재작성하지 않았습니다.

| 대상·목적 | 법적·Privacy·URL·사업자 미확정 | 모바일 ID/스토어 번호 |
|---|---|---|
| Windows Development | 묶음 Warning, 빌드 허용 | 검사하지 않음 |
| Windows InternalQA / WindowsTest (Development OFF 포함) | 묶음 Warning, 빌드 허용 | 검사하지 않음 |
| 명시적 Production (Windows 포함) | Fatal | 해당 플랫폼만 검사 |
| Android Store Release | Fatal, 서명 검사 유지 | Android ID/versionCode만 검사 |
| iOS Store Release | Fatal | iOS ID/CFBundleVersion만 검사 |

`BuildValidationContext.IsProductionRelease`가 단일 정책 판단 지점입니다. 실제 BuildReport의 target/options, release channel, internal flag, Store 배포 의도를 사용합니다. 명시적 Production 또는 Store 배포 의도는 Development/Internal보다 우선합니다. 모바일의 비개발 빌드는 WindowsTest 채널을 잘못 지정해도 Production으로 남습니다.

Unity 일반 Windows Build는 Development ON이면 Development, OFF이면 WindowsTest로 기본 판정합니다. 공개 배포용 Windows는 반드시 Production을 명시해야 합니다. 다른 플랫폼의 비개발 빌드는 기본 Production입니다. Production 채널과 Development 컴파일 플래그를 함께 지정하는 조합 자체도 Fatal입니다.

기존 명시적 `Export iOS Review Xcode Project`만 대상 iOS·InternalQA·ReviewExport·Store 배포 아님이 함께 지정된 검토용 export입니다. 일반 iOS Production에 적용되지 않습니다. 기존 전역 `ian.release.exportReview` 토글은 더 이상 검증을 우회하지 못합니다.

Editor 프리셋:

- `Ian > Build > Windows Development` → `Builds/Windows/Development/IanGrowing.exe`
- `Ian > Build > Windows Internal QA` → `Builds/Windows/IanGrowing.exe` (Development/Script Debugging OFF)

CI/기존 BuildPipeline 호출에서도 `IAN_BUILD_CHANNEL=development|internalqa|windowstest|production` 또는 `-releaseChannel Production`을 인식합니다. CLI 인자가 환경변수보다 우선하며 알 수 없는 값은 실패합니다. 이 옵션은 검증 목적을 지정하며 실제 Development 컴파일 플래그는 BuildPlayerOptions로 별도 결정됩니다.

공통 Fatal 유지: 깨진 가챠 데이터/분포, 필수 시작 Scene 누락·순서 오류, 존재하지 않는 Scene, 잘못된 Ruby 보상 데이터. 실제 처리 Scene에서도 missing script, Bootstrap의 단일 GameContext 및 GameManager·전환 서비스·필수 모델/Save 참조를 검사합니다. Unity 컴파일·직렬화·에셋 빌드의 기존 오류도 그대로 유지됩니다. 런타임의 모든 오류를 정적 검증만으로 검출한다고 주장하지 않습니다.

미확정 법률·사업자·고객지원·시행일·Privacy 승인·HTTPS URL은 내부 빌드 시작 시 한 경고에 묶어 표시합니다. Production에서는 BuildFailedException입니다. 기존 Legal UI에는 외부 URL 버튼이 없어 빈 URL을 브라우저로 여는 경로를 추가하지 않았습니다.

[판정 테스트](WindowsBuild/POLICY_TESTS.json) · [실제 Windows 빌드 결과](WindowsBuild/RESULTS.md)
