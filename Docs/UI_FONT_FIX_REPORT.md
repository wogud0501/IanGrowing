# UI 폰트 및 출생 연출 수정 보고서

2026-11-06 · Unity 6000.6.7f1 / URP · 시작 체크포인트 `ui-phase24-start` (`d642276`). 기존 픽셀아트 배경·공식 이안 디자인·씬 전환을 유지했다.

## 원인과 수정

기존 버튼에 Auto Size가 남아 있었고, 12pt 비트맵을 14/16/18pt 등으로 확대하면서 획 간격이 고르지 않았다. 일부 긴 문단에는 고정 높이, Ellipsis 또는 maxVisibleLines=3이 적용돼 있었다. 폰트 크기만 줄이는 대신 아틀라스와 컨테이너를 수정했다.

- Galmuri11 / Galmuri11-Bold를 **18pt RASTER_HINTED 비트맵**으로 다시 베이크했다. 본문·메뉴·숫자 역할을 분리하고, 0~9의 TMP advance를 동일하게 맞췄다. 원본 TTF는 변경하지 않았다.
- `IanBodyPixel18`, `IanHeadingPixel18`, `IanNumbersPixel18`을 주요 씬·게임 prefab에 적용했다. 본문은 현대 한글 11,172개, 호환 자모, ASCII와 필요한 기호를 포함한다. 추가 기호는 기존 Nanum Gothic 동적 fallback을 사용한다.
- Point / Bitmap 셰이더 / 정적 아틀라스 / 기존 정수 Canvas 확대를 유지했다. 기본 본문 18, 큰 제목 36으로 정리하고, 버튼 Auto Size를 껐다. 작은 글자를 자동 축소하지 않는다.
- Character/Word spacing 0, 본문 line spacing 2 / paragraph spacing 6, 한국어 modern Hangul 줄바꿈을 적용했다. 버튼 문구는 중앙 정렬과 별도 여백을 사용한다.
- 스크롤 안 문단은 `PixelTextFlow`가 preferred height를 갱신한다. 화면 우측 최근 메시지도 ScrollRect로 바꿔 원문을 유지한다. 알림은 내용에 맞춰 높이를 늘린다. TMP mesh 재구축 중 레이아웃을 변경하지 않도록 측정은 다음 LateUpdate에 수행한다.
- TMP 기본 폰트와 기존 IndieTheme 폰트 참조도 새 본문으로 맞췄다. 이전 폰트 파일은 삭제하지 않았다.

Bitmap은 현재 정수 UI 배율에서 작은 글자가 선명하므로 유지했다. SDF로 전환하지 않았다. 사용자 큰 글자 옵션은 읽기 쉬운 크기를 우선하며, 모든 임의 해상도/언어에서 완전히 같은 물리 픽셀 획 폭을 보장하는 것은 아니다.

## 라이선스

공식 [Galmuri 저장소](https://github.com/quiple/galmuri), 고정 커밋 `71e1cacf1437a11220307120e63e30bc275312d4`의 기존 TTF와 [SIL OFL 1.1](https://github.com/quiple/galmuri/blob/71e1cacf1437a11220307120e63e30bc275312d4/ofl.md)을 재사용했다. 게임 번들에 저작권·라이선스 전문을 포함하고 폰트 단독 판매는 하지 않는다. Nanum Gothic fallback의 OFL도 포함한다. 게임 내 라이선스 UI는 합본을 유지한다. [원본 해시](CommercialVisual/V11/FONT_SHA256.txt), [기존 출처 기록](CommercialVisual/V11/FONT_SYSTEM.md).

## BirthIntroductionPopup

사용자의 후속 지시에 따라 출생 메시지를 삭제하지 않고 전용 팝업으로 분리했다. `GameManager.BirthMessage()`의 긴 일반 로그 출력을 중단했으며, 랜덤 이벤트·미션·경고 로그 경로는 유지했다. 과거 저장 데이터를 일괄 삭제하지 않는다. 기존 최근 로그는 저장되는 게임 이력이 아니므로, 다른 기록을 지우는 마이그레이션은 추가하지 않았다.

명시적인 새 게임 → 새 run ID/초기 데이터 → Home 전환 완료 → 배경 dim → 출생 신고 팝업 순서다. 중앙 9-slice 프레임, 기존 아이콘·픽셀 버튼·한글 서체를 사용한다. 짧은 fade와 0.98→1의 제한된 scale, Safe Area 대응을 적용했다. 확인 전에는 시계와 행동을 차단하며 ESC로 건너뛰지 않는다.

표시는 요청한 출생 정보(0세, 0G, 부채 1,000,000G, 사회적 지위 없음)와 풍자 문구다. `[인생 시작하기]`의 자동 저장이 성공한 뒤 닫고 플레이를 재개한다. 실패 시 미완료 상태를 유지한다.

v4 JSON에 선택적 `birthIntroVersion=1`, `birthIntroCompleted`를 추가했다. 새 게임은 false, 확인하면 true다. 확장 필드가 없는 기존 세이브는 완료로 간주한다. 같은 run에서 과거 수동 체크포인트를 읽어도 이미 확인한 소개는 다시 열지 않는다. 미확인 상태로 종료했다면 저장된 false에 의해 재실행 후 다시 표시할 수 있다. 환생은 구현하지 않았으며 이 전용 컴포넌트/플래그를 일반 이벤트와 분리해 향후 별도 연출을 연결할 수 있게 했다.

## 실제 검증 및 캡처

[출생/폰트 검사](Phase24/BIRTH_FONT_TESTS.json)는 메뉴의 실제 새 게임 → Home, 확인 전 행동/시계 차단, ESC 차단, 미확인 저장/로드, 실제 버튼 raycast/pointerClick, 완료 저장, 재진입 미표시, 구세이브 마이그레이션, 다른 로그 유지, 새 게임 재표시와 한글 전체 글리프를 검사한다. 1280×720 / 1920×1080 / 2400×1080에서 팝업 문단 높이와 글리프를 확인한다.

- [출생 팝업](Phase24/Screenshots/Birth-1920x1080.png)
- [720p 출생 팝업](Phase24/Screenshots/Birth-1280x720.png)
- [20:9 출생 팝업](Phase24/Screenshots/Birth-2400x1080.png)
- [빠른 행동 텍스트](Phase24/Screenshots/Quick-action-font.png)
- [주요 창 텍스트 컨테이너 검사](Phase24/UI_OVERFLOW_AUDIT.txt)

이 요청에서 언급한 첨부 스크린샷 파일 두 장은 새 첨부로 전달되지 않아, 기존 Game View와 실제 동일 문구를 재현해 비교했다. 실제 모바일 기기의 글자 가독성/노치·터치 하드웨어 검사는 Editor 해상도 검증과 구분한다.
