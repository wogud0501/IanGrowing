# 개인정보·SDK 감사 — 2026-11-17

LEGAL REVIEW REQUIRED BEFORE RELEASE. 소스/설정 감사이며 최종 기기 네트워크 계측은 미실시입니다.

| 영역 | 실제 확인 | 공개/출시 처리 |
|---|---|---|
| 게임 진행 | SaveManager의 앱 전용 로컬 JSON: 능력치, 캐릭터 나이, 시간, 돈, 인벤토리, 미션, 관계, 방 | 운영자 서버 업로드 코드 없음. 캐릭터 나이는 실제 이용자 나이가 아님 |
| Ruby | 로컬 무료/유료 원장, 거래 ID·출처·UTC 시각·지급/소비량 | 현재 실제 구매 없음. 향후 Store 거래 ID/검증 서버 도입 시 정책 갱신 |
| 약관 | PlayerPrefs 및 SaveData.legal의 문서 ID·언어·버전·확인 시각 | 마케팅 동의 없음. 개인정보 수집 포괄 동의로 사용하지 않음 |
| 계정 | 게임 계정 생성/인증 없음 | 가짜 계정 삭제 메뉴 없음. OS 앱 데이터 삭제/앱 제거 안내 |
| Unity Analytics/Ads/Cloud Diagnostics | UnityConnectSettings에서 활성화 꺼짐. 게임 코드에 Initialize/수집 호출 없음 | 엔진 자체 처리까지 '수집 없음' 단정 금지. 최종 바이너리 계측 필요 |
| Unity IAP 5.4.4 | 기존 패키지 설치됨. Purchasing 서비스 꺼짐, 게임 구매 호출·IAPProductCatalog 없음. Codeless 자동 초기화는 비어 있지 않은 카탈로그 및 enableCodelessAutoInitialization 필요 | 신규 추상화는 SDK 호출 없음. 실제 SDK 구매·영수증 검증 미구현 |
| Unity Services Core | IAP 등 전이 의존성, PrivacyInfo 포함 | 설치만으로 모든 서비스가 활성화된다고 판정하지 않음. 최종 포함/통신 확인 필요 |
| Unity AI/Inference, Microsoft GDK, MCP | 기존 패키지 보존. 게임에서 AI·GDK 로그인/모델/네트워크 호출 없음. SENTIS_ANALYTICS_ENABLED Android define 존재 | Editor 기능과 Player를 구분. 불필요한 런타임 DLL/플러그인의 최종 포함 및 통신 검토 필요 |
| Discord RPC | 패키지의 Editor/UERP.cs에서 Editor 초기화 | Editor 개발 정보 전송과 모바일 앱 사용자 데이터 처리를 구분 |
| Firebase/Auth/Push/광고 SDK | manifest 및 게임 소스에서 명시적인 Firebase/게임 Auth/Push/Ad 초기화 없음 | 출시 전 최종 전이 패키지/네이티브 바이너리 재확인 |
| 고객지원 | 현재 문의 자동 전송 기능 없음 | 고객센터 실제 운영 시 문의 정보·보유기간·수탁자 확정 필요 |

현재 게임 C# 소스에서 UnityWebRequest/HttpClient/AnalyticsService/UnityServices.Initialize/AuthenticationService 호출을 찾지 못했습니다. 이것은 엔진이나 네이티브 SDK의 통신 부재 증명이 아닙니다.

SDK Privacy Manifest: 설치 IAP의 iOS PrivacyInfo에는 tracking=false, 수집 항목 빈 배열, 파일 타임스탬프 C617.1/디스크 공간 E174.1이 기재되어 있습니다. Services Core에는 tracking=false와 UserDefaults CA92.1이 있습니다. 실제 Xcode export의 UnityFramework/UnityRuntime manifest2개를 확인했습니다. [파싱 결과](Release/IOS_PRIVACY_MANIFESTS.json). 모든 required-reason API를 이미 충족한다고 단정하지 않습니다.

로컬 JSON은 암호화되지 않습니다. 계정/서버 삭제 기능·네트워크 암호화를 구현했다고 주장하지 않습니다. 사용자 기기 접근에 의한 Save rollback/변조 방어에는 서버 권위 원장이 필요합니다.

참고: [Unity 플레이어 개인정보 정책](https://unity.com/legal/game-player-and-app-user-privacy-policy), [Apple Privacy Manifest](https://developer.apple.com/documentation/bundleresources/privacy-manifest-files), [Unity SDK 개인정보 관리](https://docs.unity.com/en-us/analytics/privacy-and-consent/manage-data-privacy). 정책은 공급자 일반 설명이며 이 앱의 실제 활성화 상태는 위 소스 감사와 별도로 판단합니다.

패키지 원문 고지 55개와 폰트 라이선스를 앱 내 읽기 화면에 포함했습니다. [고지 목록](Release/LICENSE_INVENTORY.json). 전체 사용 아트/상업 에셋의 라이선스 검토 완료를 의미하지 않습니다.

2026-11-24 변경 사항은 기기 내 오류 보고서와 명시적 공유, Unity Engine Diagnostics 전송 설정에 관한 [진단 개인정보 검토](DIAGNOSTIC_PRIVACY_REVIEW.md)에 기록했습니다.
