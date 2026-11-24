# PF스타더스트 3.0 적용

2026-11-22 기준. 제공된 Regular 원본과 프로젝트 사본의 SHA-256은 모두 `14e09873c3d5b9a0237759c9abd7ccb851eaac938ea7b05ab37e3311dff56da2`이다. 원본의 버전은 3.000이며 현대 한글 11,172자를 지원한다. 제작자는 개인·기업의 상업적 사용을 허용하며 원본 수정·재배포·판매를 제한한다. TTF의 임베딩 플래그는 0x0008이다. 게임의 라이선스 화면에도 출처를 추가했다. [제작자 배포 안내](https://m.blog.naver.com/campanula913/221366697603) · [프로젝트 보존 고지](../Assets/_Game/UI/Pixel/Fonts/PFStardust/License.txt)

| TMP 에셋 | 포함 범위와 용도 |
|---|---|
| PFStardust_Main | 게임에서 사용하는 719개 문자. 32포인트 힌팅 비트맵, Point 샘플링, 정적 1024² 한 장 |
| PFStardust_Extended | 같은 원본으로 희귀 한글이나 새 이름을 처음 표시할 때 생성. 512² 페이지를 필요할 때 확장 |
| UI_Symbols | PF에서 제공하지 않는 수학 빼기, 빈 체크 상자, 빈 마름모 세 기호만 포함. Galmuri11의 정적 256² 아틀라스와 기존 OFL 고지 사용 |

게임 씬 27개에 직렬화된 TMP 폰트 필드 8,488개와 프리팹 42개의 282개를 검사했다. 검사한 8,770개는 모두 공식 폰트를 참조하고, 빈 참조와 이전 폰트 참조는 0개다. 월드용 3D TMP 컴포넌트 156개도 포함한다. 게임 소유 씬·프리팹의 구형 UGUI Text 및 TextMesh는 0개다. 예전 폰트 원본과 라이선스는 저장소에 남아 있다.

- TMP 기본값, PixelTheme, IndieTheme, 텍스트의 직렬화된 머티리얼 참조를 함께 바꿨다. 동적 선택지·미션·상점·지도·알림은 공통 테마나 부모 HUD의 폰트를 받는다. QA 패널과 UI·월드 제작 도구도 같은 폰트를 사용한다.
- 메인 타이틀은 기존 Display 56을 사용한다. 창 제목, 본문, 버튼, 보조 문구, 숫자는 기존 역할별 크기와 접근성 배율을 유지한다. 날짜·재화는 고정 폭 숫자를 쓴다. UI는 Screen Space Canvas에서 월드 렌더 배율과 별도로 그린다.
- Android와 iPhone의 첫 세로 화면 검수에서 미니맵 제목의 세로 공간 부족과 재화 영역 겹침을 발견했다. 버튼 영역을 키우고 미니맵을 재화 아래에 배치했다. 최종 양쪽 해상도에서 해당 글자 넘침은 0건이다.
- 표시 검증에는 지정된 일곱 문자열, 메인 타이틀, NPC 선택지, 지역 표지, 게임 메뉴의 18개 화면이 포함됐다. 새 글자를 추가한 뒤 에디터 메뉴 `Ian/UI/Build PF Stardust atlases`에서 기본 문자집을 갱신할 수 있다.

[지정 문자열 실화면](FontInteractionQA/GlyphSamples.png) · [주요 화면](FontInteractionQA/UI.jpg) · [모바일 비교](FontInteractionQA/Mobile.jpg) · [에셋 생성 코드](../Assets/_Game/Scripts/Editor/UI/StardustTypography.cs)
