# Pixel font system

## 출처와 배포

공식 [quiple/galmuri](https://github.com/quiple/galmuri)의 v2.404, 커밋 `71e1cacf1437a11220307120e63e30bc275312d4`에서 Galmuri11, Galmuri11-Bold, Galmuri14를 확보했다. [공식 OFL 전문](https://github.com/quiple/galmuri/blob/71e1cacf1437a11220307120e63e30bc275312d4/ofl.md)의 SIL OFL 1.1에 따라 게임 번들/재배포 시 저작권과 라이선스 전문을 함께 제공한다. 폰트 자체를 단독 판매하지 않는다. 원본 TTF를 수정하지 않았다.

위치: `Assets/_Game/UI/Pixel/Fonts/Galmuri/`. 원문 OFL.txt, 기존 Nanum Gothic fallback의 OFL과 합친 Combined-Licenses.txt를 포함했다. PixelTheme.fontLicense와 실제 게임의 라이선스 UI는 합본을 참조한다. 원본 파일 해시는 [FONT_SHA256.txt](FONT_SHA256.txt)에 있다. 추가적인 배포 법률 검토를 했다는 의미는 아니다.

## 구현

| TMP asset | 원본/역할 | raster point | atlas | 문자 수 |
|---|---|---:|---|---:|
| IanBodyPixel | Galmuri11 / 본문·알림·경고 | 12 | 2048² static Alpha8 | 11369 |
| IanHeadingPixel | Galmuri11-Bold / 메뉴·제목 | 12 | 1024² static Alpha8 | 474 |
| IanTitlePixel | Galmuri14 / 타이틀 | 16 | 1024² static Alpha8 | 475 |
| IanNumbersPixel | Galmuri11 / 숫자·시간·금액 | 12 | 512² static Alpha8 | 19 |
| IanSymbolsFallback | Nanum Gothic / 추가 기호 | 12 | 1024² dynamic Alpha8 | 초기 최소 문자 |

RASTER_HINTED + TMP Bitmap 셰이더, Point, padding1을 사용한다. 모두 아틀라스 한 장이다. 제목/숫자/굵은 메뉴 → 본문 → 기호 fallback 순서다. 정적 폰트는 sourceFontFile=null이며, 동적 fallback만 원본 Nanum Gothic을 런타임에 포함한다. 빌드 시 동적 글리프 데이터를 지우도록 설정했다. 기존 큰 SDF 폰트는 주요 씬 의존성에서 제외했다. 측정 설정은 [FONT_AND_ATLAS_FINAL.txt](FONT_AND_ATLAS_FINAL.txt).

Galmuri의 숫자 1은 기본 advance가 좁다. 생성된 TMP 글리프의 0~9 advance를 동일하게 하고 좁은 숫자 bearing을 중앙으로 옮겼다. TTF 원본이나 실제 글자 모양을 바꾸지 않았다. 숫자/시계 이름이 붙은 텍스트에는 Numbers 역할을 적용했다.

MainMenu 및 네 장소, 기존 게임 prefab의 TMP에 공통 역할을 적용했다. 합성 Bold를 제거하고 최소 글자 크기 14와 기존 반응형 레이아웃을 유지했다. 별도의 장식용 경고 폰트를 더 늘리지는 않았으며, 경고도 같은 레트로 본문으로 읽기 쉽게 유지했다.

## 검증 범위

현대 한글 11,172개, 호환 자모, ASCII와 현재 코드/데이터 문자 corpus를 검사했다. 주요 씬의 모든 TMP에 대해 실제 문자열의 fallback 포함 글리프 누락 0, 잘못된 폰트 역할 0을 확인했다. 모든 유니코드/옛한글/이모지 지원을 약속하는 것은 아니다. 새 언어나 기호를 추가할 때 corpus 검사를 다시 실행한다.

1280×720 / 1920×1080 / 2400×1080의 실제 Game View로 확인했다. Bitmap + Point로 SDF 흐림은 피하지만, Canvas 배율에 따라 UI 획 간격이 완전히 같은 정수 픽셀 폭은 아닐 수 있다. 720px 미만 높이·세로 화면·실제 모바일 화면의 읽기 검수는 남아 있다. 재생성은 `PixelFontSystemBuilder.Build()` 후 `Complete()`이며, 두 함수는 씬/prefab을 수정하므로 Editor Play를 끄고 체크포인트에서 실행한다.
