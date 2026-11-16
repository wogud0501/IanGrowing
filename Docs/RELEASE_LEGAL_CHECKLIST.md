# Release Legal Checklist

LEGAL REVIEW REQUIRED BEFORE RELEASE. 2026-11-17, 현재 배포 판정 **NOT READY**.

구현: 기존 Settings→약관 및 정책/루비 안내, 최초 이용약관 동의+Privacy 확인, 문서 버전 재확인, 한국어7문서, 긴 본문 스크롤/뒤로가기. 마케팅/가짜 계정 삭제 없음. 본문의 개발 검토 마커는 UI에서 제거합니다.

문서 파일: `Assets/Resources/Legal/ko/{terms,privacy,paid,refund,probability,operation,licenses}.txt`. 향후 locale별 문서를 추가할 수 있으며 한국어를 영어 문서라고 표시하지 않습니다. 현재 한국어만 제공하며 다국어 출시 전 실제 번역이 필요합니다.

## 사람이 반드시 확정할 정보

- 회사명, 대표자명, 사업자등록번호, 통신판매업 신고번호, 사업장 주소
- 고객센터 이메일·전화, 개인정보 보호책임자·문의 이메일
- 시행일, 지원 문의 보유기간, 출시 국가·연령등급, 미성년자 고지
- Terms/Privacy/Support/Refund Help 실제 HTTPS URL
- 최종 SDK 수탁자·해외 이전·법적 근거·보존기간, 실제 사용 에셋 전체 라이선스
- 최종 법률 검토 승인, Privacy 감사 승인

`Assets/Resources/IanRelease.asset` 및 문서 안의 실제 Placeholder가 남아 있습니다. [자동 검사](Release/VALIDATION.json)에서 명시적으로 차단합니다. 회사/Bundle ID도 Unity 기본 템플릿 상태이므로 임의 사업자나 앱 ID를 생성하지 않았습니다.

확률: 기존 가챠 실행 전 '확률정보'에서 실제 Pool·등급·개별 확률·다음 천장·50/100/300·중복·변경 적용 시점을 표시합니다. 기본/각 천장/중첩 천장의 확률 합계 검사를 빌드 전 실행하며 실제 sampler와 같은 Compute 함수를 사용합니다. 국내 표시 방식과 대상 적용은 최종 출시 검토가 필요합니다.

환불은 디지털 상품 일괄 불가 문구를 사용하지 않았으며 Store 절차와 법정 권리를 구분했습니다. 실제 Store 환불 실행 기능은 없습니다.

근거: [Apple 심사 가이드](https://developer.apple.com/app-store/review/guidelines/), [Google 정책](https://support.google.com/googleplay/android-developer/answer/18265346), [전자상거래법 제17조](https://www.law.go.kr/lsLinkCommonInfo.do?chrClsCd=010202&lsJoLnkSeq=1027063337), [게임산업법 표시의무](https://www.law.go.kr/LSW/lsLinkCommonInfo.do?chrClsCd=010202&lsJoLnkSeq=1029708933), [Google 환불 안내](https://support.google.com/googleplay/answer/2479637). 이는 검토 참고자료이며 초안이 법률 검토를 완료했다는 뜻은 아닙니다.
