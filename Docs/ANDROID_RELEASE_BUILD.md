# Android Release AAB

2026-11-17 · **BLOCKED — ANDROID RELEASE SIGNING CREDENTIALS REQUIRED**.

Signed AAB는 생성하지 않았습니다. 기존 설정의 keystore/alias가 공란이며, 빌더가 유효한 서명 경로·별칭·비밀번호를 확보하지 못해 BuildPipeline 호출 전에 중단했습니다. 키·비밀번호를 새로 만들거나 Git에 저장하지 않았습니다. 이전 Phase의 Development APK를 Release AAB로 재표시하지 않습니다.

현재 설정: Unity6000.6.8f1, product Ian_Growing / company DefaultCompany, Bundle ID `com.UnityTechnologies.com.unity.template.urpblank`, version0.1.0 / code1, minAPI26 / targetAPI Auto, IL2CPP / ARM64. 기존 방향·그래픽·권한 설정을 임의 변경하지 않았습니다. 배포용 앱 식별자와 사업자 명칭은 아직 기본값입니다.

구현된 빌드 명령: `Ian > Release > Build Signed Android AAB`. 배포 검증을 통과하고 기존 유효 키가 있으면 Development OFF / Script Debugging OFF / Build App Bundle ON / Custom Keystore로 빌드하며 기존 Editor 설정을 복원합니다.

출력 예정 경로: `Builds/Android/IanGrowing-0.1.0-release.aab`. **현재 존재하는 산출물이 아닙니다.** Symbol zip 생성·AAB 크기·서명·bundle metadata·기기 launch·save·legal·Ruby 검사는 서명 후 수행해야 합니다.

안전한 빌드 환경 변수:

- IAN_ANDROID_KEYSTORE — 이미 존재하는 keystore 경로
- IAN_ANDROID_KEY_ALIAS — 기존 alias
- IAN_ANDROID_STORE_PASSWORD — keystore 암호
- IAN_ANDROID_KEY_PASSWORD — key 암호

비밀번호를 채팅·코드·문서·SO·Git에 넣지 마세요. Unity 기존 세션 값이 있으면 이를 사용할 수 있으며 비밀번호는 로그에 출력하지 않습니다. 안전한 빌드 환경이 마련되면 같은 빌드 명령으로 이어갈 수 있습니다.

[실제 차단 결과](Release/ANDROID_RESULT.json), [배포 검증](Release/VALIDATION.json). 서명 정보 외에도 법적 Placeholder·URL·기본 Bundle ID·사업자 명칭이 남아 있어 현재 Store 제출은 불가합니다.
