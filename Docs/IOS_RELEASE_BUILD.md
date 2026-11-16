# iOS Release Xcode export

2026-11-17 · Unity iOS Support 설치 확인. 실제 Release export 성공. Development OFF / Script Debugging OFF. **법적 검토용 export이며 Store 배포 승인 상태가 아닙니다.**

최종 Unity 결과: 오류0/경고37, 출력 약1.7GB. 첫 전체 export 경고641과 캐시 조건이 다르므로 경고를 전부 해결했다는 뜻은 아닙니다.

실제 경로: `Builds/iOS/Xcode/Unity-iPhone.xcodeproj`.
Unity 빌드와 Xcode compile/Archive는 별개의 결과입니다. 원시 Unity 결과와 최종 export 결과는 [IOS_RESULT.json](Release/IOS_RESULT.json)에 기록합니다.

현재 Bundle ID는 `com.Unity-Technologies.com.unity.template.urp-blank`, version0.1.0 / iOS build0, deployment target15.0, ARM64/Metal, 기존 portrait/landscape 방향 유지. Team ID 공란, CODE_SIGN_STYLE Automatic. Production Bundle ID와 양의 build number가 필요합니다.

**IOS DISTRIBUTION SIGNING CREDENTIALS REQUIRED**.
현재 `security find-identity -v -p codesigning` 결과 유효 identity0. 전체 Xcode 및 iPhoneOS SDK가 없고 xcode-select는 CommandLineTools입니다. 따라서 Xcode compile·Archive·IPA export·Device launch는 BLOCKED/NOT VERIFIED입니다. `.xcarchive`와 `.ipa`를 생성했다고 보고하지 않습니다.

필요 환경: 전체 Xcode와 iPhoneOS SDK, 실제 Team ID, 해당 Bundle ID에 맞는 Apple Distribution certificate/private key, provisioning 또는 정상 Automatic Signing 계정, export 설정. 인증서/Team을 임의 생성하거나 변경하지 않았습니다.

Privacy: 생성된 UnityFramework 및 UnityRuntime.framework의 PrivacyInfo.xcprivacy2개를 파싱했습니다. UserDefaults/FileTimestamp/DiskSpace/SystemBootTime 관련 reason이 포함됩니다. 앱 Info.plist에는 카메라·마이크·위치 Usage Description을 추가하지 않았습니다. 실제 사용 SDK와 Apple required reason 허용 목적의 최종 검토는 필요합니다. [manifest](Release/IOS_PRIVACY_MANIFESTS.json), [Info.plist 감사](Release/IOS_INFO_AUDIT.json), [서명 설정](Release/IOS_SIGNING_AUDIT.json).

Release 생성 IL2CPP 소스에서 RubyWallet/ReleaseUI/LegalDocumentService가 존재하고 MockPurchaseProvider/DeveloperQACenter/ReleaseFoundationVerification은 제외됐음을 확인했습니다. [코드 감사](Release/RELEASE_CODE_AUDIT.json). 이것이 실제 기기에서의 동작·성능 검증을 대체하지 않습니다.

다시 export: `Ian > Release > Export iOS Review Xcode Project`. 배포용 최종 빌드는 [Legal 체크리스트](RELEASE_LEGAL_CHECKLIST.md)를 완료한 뒤 생성해야 합니다.
