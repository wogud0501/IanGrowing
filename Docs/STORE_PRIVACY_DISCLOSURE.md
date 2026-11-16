# Store Privacy Disclosure — 출시 전 확정 필요

LEGAL REVIEW REQUIRED BEFORE RELEASE. [감사](PRIVACY_DATA_AUDIT.md)에 근거한 작성 체크리스트이며 Store에 제출된 최종 답변이 아닙니다.

| 질문 | 현재 근거/판정 |
|---|---|
| 게임 계정 | 없음. 계정 삭제 기능을 임의 추가하지 않음 |
| 이름·연락처·실제 생년월일·위치·광고 ID | 게임 코드에서 수집/서버 전송 없음 |
| 진행·약관 확인·Ruby 원장 | 로컬 파일/PlayerPrefs. 운영자 서버 없음 |
| Purchase History | 실제 구매 미연결. 향후 Store 거래/서버 검증 도입 시 수집·연결 여부 갱신 |
| Analytics/Crash/Ads | 현재 서비스 비활성. 엔진·전이 SDK 최종 빌드 및 통신 확인은 남음 |
| Tracking | 게임 코드에서 광고 추적 구현 없음. 최종 SDK 처리 확인 전 Store 답변 확정 불가 |
| Data linked to user | 운영자 계정 없음. 향후 Store/서버 식별자 연결 여부 검토 필요 |
| Encryption in transit | 게임 자체 서버 통신 없음. 네이티브 SDK가 포함하면 실제 전송 프로토콜 확인 필요. 일괄 Yes 답변 금지 |
| Deletion | OS 앱 데이터 삭제/앱 제거. 계정 삭제 대상 없음. OS 백업은 해당 플랫폼 설정에 따름 |
| Privacy Policy URL | 미등록, 출시 차단 |
| Support URL | 미등록, 출시 차단 |

Apple: App Privacy의 실제 collected/linked/tracking 항목, Privacy Policy URL, 제3자 SDK manifest·서명, required reason API, 연령등급을 최종 아카이브 기준으로 확인합니다. 현재 카메라·마이크·위치 권한 문구는 추가하지 않았습니다.

Google Play: Data Safety는 코드뿐 아니라 포함 SDK의 실제 처리를 포함합니다. 데이터 수집·공유·암호화·삭제·계정·구매 기록을 최종 AAB와 기기 네트워크 결과로 확정합니다. 아직 Signed AAB와 기기 launch 결과가 없습니다.

[Apple 심사 가이드](https://developer.apple.com/app-store/review/guidelines/), [Google Play 정책](https://support.google.com/googleplay/android-developer/answer/18265346)을 출시 시 다시 검토합니다. 제출은 이번 작업에서 수행하지 않았습니다.
