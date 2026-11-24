# 로컬 진단 및 개인정보 검토

2026-11-24 구현 검토. 게임 자체 보고서는 네트워크 클라이언트, 서버 주소, 업로드 작업 없이 앱 전용 디렉터리에 생성된다. UI에서 선택한 보고서 한 건의 미리보기만 공유 대상으로 넘긴다. 자동 전송에 관한 이 설명의 범위는 게임 자체 진단 기능이다.

| 점검 항목 | 확인한 구현 |
|---|---|
| 로그 메시지 | 프로젝트의 고정 오류 및 일부 표준 예외 메시지를 허용한다. 그 외에는 예외 이름 또는 생략 문구를 남긴다. 사용자 입력 전체를 저장한 후 단어 몇 개만 지우는 방식은 사용하지 않는다. |
| 스택과 이벤트 | 호출 인수, 절대 파일 경로, 메모리 주소를 제외한다. 기능 이벤트 종류·UTC 시각과 상호작용 종류만 보관한다. 전체 Save, 대화·채팅 원문, 연락처, 계정·결제·GPS·광고 ID·기기 고유 ID는 수집 필드에 없다. 보고서 ID는 사건마다 새로 생성한다. 빌드 GUID는 같은 빌드 전체가 공유한다. |
| 공유 | 기기 내 생성과 고객지원의 수령은 별개다. 사용자가 보고서를 선택하고 필터링된 내용을 확인해야 내보낼 수 있다. 선택한 외부 앱이나 파일 저장소의 사본을 게임이 삭제할 수 없다. |
| 저장 | iOS 진단 디렉터리는 백업 제외를 요청한다. Android 백업·기기 전송 규칙은 앱 전용 외부 저장 영역의 Diagnostics를 제외한다. 네이티브 덤프·OS 로그·엔진의 Player.log는 별도이며 게임 보고서의 필터가 적용되지 않는다. |
| Unity 서비스 | UnityConnectSettings에서 별도로 켜져 있던 Engine Diagnostics 플래그를 해제했다. Android와 Windows Build Profile의 값 2는 설치된 Editor의 ProjectSettings 상속 값이다. Cloud Diagnostics, Analytics, Ads, Performance Reporting, Purchasing 서비스 비활성 설정과 게임 코드의 초기화 호출 유무를 확인했다. |
| 기존 패키지 | IAP 및 Services Core는 설치되어 있다. iOS Development Player 시작 로그에서 IAP EarlyTransactionObserver의 생성 및 앱 수명 주기 등록을 관찰했다. 패키지 소스에서 해당 실행 콜백의 결제 큐 등록은 주석 처리되어 있다. 이 관찰자에는 게임 오류 보고서 전달 경로가 없다. Discord RPC와 MCP는 개발 Editor 경로다. AI·Inference, GDK 등 설치 패키지 전체의 네이티브 통신 부재를 소스만으로 판정할 수 없다. 최종 배포 바이너리의 패킷 계측은 미검증이다. [기존 SDK 감사](PRIVACY_DATA_AUDIT.md) |

앱 내 한국어 이용약관 제18항과 개인정보처리방침 제13항을 추가하고 두 문서만 0.2.0-draft로 갱신했다. 기존 조항과 동의·고지 방식, 출시 검증기의 검토 플래그를 유지한다. 로컬 보존 한도는 고객지원이 수령한 자료의 보유기간과 다르다. 출시 전 사업자·연락처·처리 근거·실제 문의 보유기간 및 파기 절차를 운영 방식에 맞게 확정하고 법률 검토와 스토어 신고를 진행해야 한다. [개인정보 보호법 제30조](https://www.law.go.kr/법령/개인정보보호법/제30조)

플랫폼 동작 근거는 Android의 사용자 선택 파일 API 및 iOS 공유 컨트롤러 문서에서 확인할 수 있다. [Android Storage Access Framework](https://developer.android.com/training/data-storage/shared/documents-files) · [Apple UIActivityViewController](https://developer.apple.com/documentation/uikit/uiactivityviewcontroller)
