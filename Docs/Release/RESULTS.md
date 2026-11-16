# Release Compliance & Monetization Foundation — 결과

2026-11-17 KST · Unity6000.6.8f1 · 시작 체크포인트 a2bbd27.

**기능 기반 구현·Play Mode 검증 완료. 전체 Store 출시 준비는 NOT READY.** Android Signed AAB는 서명 정보 부재로 생성하지 않았고, iOS는 실제 Release Xcode 프로젝트를 생성했습니다. 실제 결제·Store 등록·Archive·IPA·기기 검증은 수행하지 않았습니다.

## 변경 시스템

- EconomySystem 소유 RubyWallet: 무료/유료 분리, 출처·UTC·관련 ID·증감 원장, 무료 우선 소비, 거래/Store ID 중복 거절, 만료 없음. 기존 Save version4의 선택 블록 확장.
- 최초 보상: 미션6개19 Ruby, 도전과제2개2 Ruby. 기존 Mission Claim/CityAchievement의 최초 성공 경로에 연결. 노동·반복 행동·Scene 출입에 지급하지 않음. 새 인생에서도 원장을 보존.
- 가챠: 기존 Gold Pool 보존. Pool별 Gold/Ruby 선택과 티켓 우선 사용, 실제 ItemService 비용→결과→아이템→저장 트랜잭션. Ruby Pool은 QA에서만 임시로 사용했으며 기존 Production 배너를 유료화하지 않음.
- PurchaseService는 기존 GameManager의 전역 서비스. IPurchaseProvider/Receipt/검증 인터페이스/상품 매핑/Restore-Reconcile hook. 기본 공급자는 NotSupported. Release에는 Mock 클래스 없음. 실제 StoreKit/Billing/서버 검증 구현 없음.
- 기존 UIShell에 최초 이용약관 동의+Privacy 확인, Settings의 약관/루비 안내. 7문서·버전·locale·스크롤·Back. Provider 미연결 상태의 가격·구매 버튼 숨김. 마케팅과 가짜 계정 삭제 기능 없음.
- 실제 가챠 확률 계산을 공용 Compute로 추출하여 sampler·확률 UI·빌드 검증에서 공유. 기본/각 천장/중첩 보장의 합계 검사. 50/100/300·중복·다음 회차·변경 적용 안내.
- 배포 검증, 실제 Android 서명 preflight, iOS Release export 명령. 비밀번호/키를 생성·기록하지 않음. 비밀 파일 Git 제외 추가.

신규 데이터: `Assets/Resources/IanRelease.asset`, `Assets/Resources/Legal/ko/`. 기존 Scene/Prefab/Animator/아트는 변경하지 않았습니다. UI는 기존 runtime 생성 패턴을 확장하며 씬을 재생성하지 않았습니다. 기존 두 작업 변경(폰트·입력 추적)은 작업 전 바이트로 보존했습니다.

## 실제 실행 결과

[Play Mode 원시 결과](PLAYMODE_RESULTS.json): **81개 PASS, FAIL0, UI 넘침0**.

- 약관7종 본문·스크롤·Back, 최초 체크/미확인 차단/확인 후 시작.
- 기존 출생 흐름→0세 새 게임→실제 부모님 집 씬 진입 보존.
- 무료/유료·보너스·소비 분리, 잔액 부족·동일 ID 재요청·원장 변조 거절.
- 실제 SaveManager QA 파일 저장/읽기, 원장/미션 복원, Ruby 없는 구 블록이 현재 지갑을 지우지 않음.
- Mission Claim M010 실제 함수와 기존 Achievement Record의 최초 Ruby 지급·중복 방지. 완료 조건/나이는 QA fixture로 구성했으며 60미션 전체를 새로 플레이한 결과가 아님.
- 실제 Ruby 가챠1회 비용/결과/중복 요청/부족 잔액, 열린 가챠의 원장 이벤트 즉시 표시.
- 검증 실패 영수증 지급 거부·검증된 테스트 Provider 수신→저장·재전달 중복 지급 방지·알 수 없는 상품 거절. 실제 Store 영수증이나 서버 검증이 아님.
- 1280×720 /1440×900 /904×2316의 Ruby 안내·Privacy 스크롤·임시 가격/구매 버튼 비노출. Editor 화면비 검사이며 물리 기기 터치/VoiceOver/모든 게임패드·시스템 글꼴 확대 조합은 NOT VERIFIED.

[iOS 최종 원시 결과](IOS_RESULT.json), [네이티브 보고서](IOS_NATIVE_REPORT.json): **Succeeded, 오류0/경고37**, Release/Script Debugging OFF,26개 기존 등록 Scene. 경고37은 최종 증분 빌드 결과입니다. 첫 전체 export의 경고641과 캐시 조건이 달라 모든 경고를 수정했다고 판정하지 않습니다.

실제 산출물: `Builds/iOS/Xcode/Unity-iPhone.xcodeproj` (현재 로컬 존재). [Release 코드 감사](RELEASE_CODE_AUDIT.json): Ruby/Legal/Purchase 포함, MockPurchaseProvider·DeveloperQACenter·SetTestRoll·DebugGive 제외. 생성된 Privacy Manifest2개 파싱.

Android: **BLOCKED — ANDROID RELEASE SIGNING CREDENTIALS REQUIRED**, AAB 없음. 프로젝트 내 .keystore/.jks도 찾지 못했으며 새 키를 만들지 않았습니다. [실제 결과](ANDROID_RESULT.json).

iOS Archive/IPA: **BLOCKED — IOS DISTRIBUTION SIGNING CREDENTIALS REQUIRED**, 유효 인증서0·Team 공란. 전체 Xcode/iPhoneOS SDK 없음. Unity export 성공을 Xcode compile/Archive 성공으로 표시하지 않습니다.

## 실제 화면

- [이용약관](Screenshots/First-Terms.png)
- [Ruby 안내](Screenshots/Ruby-1280.png)
- [세로 Privacy](Screenshots/Privacy-904.png)
- [가챠 확률정보](Screenshots/Gacha-Probability.png)

화면의 Ruby/유료 잔액은 별도 QA 슬롯의 테스트 수치입니다. 실제 결제나 Production 잔액이 아닙니다.

## 출시 차단 및 후속

[자동 검증](VALIDATION.json)은36개 항목을 보고합니다. 사업자/연락처/시행일/문의 보존기간, 실제 정책·지원 HTTPS URL, 법률·최종 SDK 감사 승인, Production Company/Bundle ID, iOS 양의 build number가 필요합니다. 정책 문구와 Store Privacy/Data Safety의 최종 확정·Store SDK 기기 계측은 미완료입니다.

[Legal 체크리스트](../RELEASE_LEGAL_CHECKLIST.md) · [Ruby와 상품/미션 목록](../RUBY_MONETIZATION.md) · [Privacy 감사](../PRIVACY_DATA_AUDIT.md) · [Store Privacy](../STORE_PRIVACY_DISCLOSURE.md) · [Android](../ANDROID_RELEASE_BUILD.md) · [iOS](../IOS_RELEASE_BUILD.md).

현재 로컬 Save는 완전한 치트 방지나 구매 복구를 제공하지 않습니다. 실제 판매 전 서버 권위 원장·영수증 검증·환불 알림·재동기화 및 Store sandbox 기기 검증을 연결해야 합니다.

## 저장 상태

Play 종료, FamilyHome_IanBedroom, StandaloneOSX, 원래 fast-play 옵션 복원, 씬 dirty=false. 테스트 저장 보호 해제. [기존 플레이 세이브3개 최종 SHA-256 동일](SAVE_VERIFICATION.json), [Editor 상태](EDITOR_FINAL.json), [보호 해제](EDITOR_SAVE_GUARD_RELEASED.json). Build 산출물·개발 백업·서명 비밀은 Git에 포함하지 않습니다.

MCP 서버 transport가 응답하지 않아 기존 프로젝트의 명시적 Editor 명령 브리지를 확장해 실제 Editor authoring/컴파일/Play/export를 수행했습니다. 미실행 테스트를 PASS로 대체하지 않았습니다.
