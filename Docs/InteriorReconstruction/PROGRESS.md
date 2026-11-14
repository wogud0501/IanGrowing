# Pixel interior reconstruction — 진행 기록

기준 커밋: d651cef. 시작 시 사용자 편집은 LIVE_KEY_TRACE.json 한 파일이며 바이트 백업을 보존했다.

- 실제 수정 전 Game View 촬영 완료(성인 QA 전용 슬롯). 사용자 첨부 콘셉트와 구분해 보관.
- 확인한 참고자료/접근 실패: REFERENCES.md.
- 가족 욕실을 낮은 시점에 맞춘5.2×2.6 동선으로 구성. 후벽 민트/아이보리, 실제 측벽 두께, 전면 벽·문설주, 접지 그림자. 분리 씬 유지.
- 자체 생성6모듈 아틀라스를 분리 Sprite/GameObject로 저장. 원본 이안과 욕실 설비 재사용. Point filtering, mipmap off,1024 상한. 단일 통배경 없음.
- 욕실 인스턴스 전용 문 스타일로 크기 조정. 문 Root/Threshold/Scene 목적지와 상태 제어는 기존 구조 사용.
- 실제 확인된 배경판 sorting899 > door leaf630으로 문짝을 덮는 결함을 WorldDoorLeafVisual의 렌더 순서3줄로 수정. 공통 회귀 검사 추가.
- 샤워 안쪽 Anchor를 유리 중앙 문살에서 비켜 정렬. 유리/물에 흰 텍스처를 사용해 기존 세라믹 텍스처의 색이 배우를 탁하게 덮던 현상 제거.
-14실 공통 외곽을 질감 반복 대신 무늬 없는 명암 배경과 건물 접지 그림자로 통일. Workspace 가구와 업무석은 재구축하지 않음.
- 지갑/장소 HUD와 행동 안내의 불필요한 너비 축소. Safe Area/터치/메뉴 구조 유지.

최종 검증7시나리오/621개 assertion 통과, 최종 UI 문제0. Android 빌드 오류0/경고1223, iOS SDK 없음. 결과는 Results/의 최종 실행 파일에 기록. 실제 실행하지 않은 기능/기기는 완료 처리하지 않음.

중단 시: Editor Play를 먼저 중지하고 이 문서와 Results/를 확인. 테스트는 보호된 phase26 슬롯을 사용한다. 모든 자동 검증이 끝난 뒤 마지막 Editor 명령은 releaseSaveGuard여야 한다.

마지막 상태: Editor macOS/Play 중지/dirty=False, 자동화 보호 해제, 운영3세이브 해시 일치. 다음 작업은 실제 기기 확인과 남은 돌봄 전용 아트이며, 이번 구현은 별도 Git 체크포인트로 저장한다.
