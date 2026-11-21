# 이안 키우기 상용 UI 개편 기록

## 2026-11-21 무채색 전면 리빌드

Unity 6000.6.8f1에서 HUD·미션·가방·금융·설정의 원본 화면을 촬영한 이전 패스를 기준으로 삼았다. 이 다섯 화면의 무채색 구현과 실제 캡처를 먼저 검토한 뒤 나머지 화면으로 확장했다. 월드와 집 사진은 각 장소의 조명과 색을 유지한다.

| 반복 | 발견한 문제 | 실제 수정과 재검토 |
|---|---|---|
| 핵심 5개 | 이전의 종이색 바탕과 옥색 선택면이 새 방향과 달랐다. | Higgsfield에 직전 Game View와 선택 시안을 참조로 전달했다. 핵심당 3개씩 비교한 뒤 공통 회색 면·프레임·반전 규칙을 uGUI에 적용했다. [시안과 서버 기록](../ArtSource/UI/Monochrome/Concepts/Manifest.md) |
| 1차 Game View | 헤드폰이 어두운 배경에 묻히고 설정의 현재 값 및 금융의 부채와 순자산이 잘 보이지 않았다. | 아이템 전용 받침과 UI 재질, 우측 설정값, 독립된 금액 필드를 적용했다. [1차 캡처](../ArtSource/UI/Monochrome/Review/FirstPass.jpg) · [현재 화면](../ArtSource/UI/Monochrome/Review/UIFamily.jpg) |
| 확장 | 기존 희귀도 색, 일부 작성된 창과 타이틀 배경에 이전 색이 남았다. | 타이틀·탭·입력·알림·공통 테마를 연결했다. 가챠 결과는 등급별 프레임으로, 미션은 현재 챕터의 실제 달성 수로 보강했다. 새 가챠 결과와 미션 캡처에서 각각 한 가지 문제를 지정해 Higgsfield로 재검토했다. [전체 화면](../ArtSource/UI/Monochrome/Review/AllScreens.jpg) |
| 최종 읽기 | 금융 상세에서 납부 규칙이 먼저 나왔고 고지서에 내부 식별자가 표시됐다. | 납부 기한·잔액·상태를 앞에 놓고 규칙과 이전 거래를 아래에 배치했다. 실제 급여 발생 후 게임 날짜를 정산해 고지서 발행·표시를 확인했고 여섯 크기에서 안내 마지막 줄까지 스크롤했다. [발행된 고지서](../ArtSource/UI/Monochrome/GameView/FinanceBill.png) · [검사](../ArtSource/UI/Monochrome/Review/Statement.json) |
| 10회 결과 재검토 | TMP 잘림 검사는 통과했지만, 다섯 번째 카드의 글자가 뷰포트 밖으로 밀려났다. | 비활성 창의 너비로 먼저 계산하던 순서를 고쳤다. 창을 열거나 뷰포트 너비가 바뀌면 실제 폭으로 다시 배치한다. 10개 카드의 좌우 끝을 여섯 해상도에서 재검사했다. [수정 전](../ArtSource/UI/Monochrome/Review/GachaTenBeforeFit.png) · [수정 후](../ArtSource/UI/Monochrome/GameView/GachaTen.png) |

현재 화면과 직전 UI는 화면별로 직접 비교할 수 있다. 원본 Higgsfield 이미지는 ArtSource에 보관하고 Unity에서는 텍스트·컨트롤·아이콘·9-slice·메시·재질로 구성했다. [화면 목록](UI_SCREEN_INVENTORY.md) · [직전 UI와 비교](../ArtSource/UI/Monochrome/Review/BeforeAfter.jpg) · [스타일 가이드](UI_STYLE_GUIDE.md) · [구성 요소](UI_COMPONENT_LIBRARY.md) · [검증과 한계](UI_COMMERCIAL_QUALITY_REVIEW.md)

최종 Game View 48개 상태와 실제 30분 5초의 자동 플레이를 기록했다. [전체 화면](../ArtSource/UI/Monochrome/Review/AllScreens.jpg) · [연속 Play Mode](../ArtSource/UI/Monochrome/Review/PlaySession.json)

## 2026-11-20 이전 상용 UI 작업 기록

2026년 11월 20일, Unity 6000.6.8f1. 기존 미션, 장비, 금융, 가챠와 저장 서비스에 연결된 uGUI와 TMP를 개편했다. 기준 소스는 `bff373b`다.

| 순서 | 판단과 구현 | 증거 |
|---|---|---|
| 개편 전 | 실제 타이틀, 월드, 메뉴와 시스템 창을 열어 기록했다. 빈 가방과 수치가 몰린 금융 창, 밀도가 높은 HUD를 먼저 다뤘다. | [개편 전 캡처](../ArtSource/UI/Before) |
| 방향 확정 | 기존 이안·도시 마스터로 무드보드를 제작하고 HUD·미션·가방·금융·설정을 각각 세 가지로 비교했다. 종이 장부, 짙은 제목 띠, 옥색 선택면을 공통으로 사용했다. | [무드보드](../ArtSource/UI/Concepts/Moodboard.png) · [첫 시안 비교](../ArtSource/UI/Review/FirstConcepts.jpg) |
| 첫 구현 | HUD에 네 생존 게이지, 미션에 챕터와 보상표, 가방에 선택 상세, 금융에 실제 잔액과 부채, 설정에 항목별 탐색을 배치했다. | [첫 Game View](../ArtSource/UI/Review/FirstImplementation.jpg) |
| 첫 재검토 | 금융 창이 비활성 상태에서 조립될 때 ScrollRect를 찾지 못하는 문제, 세로 화면의 미션 창 위치, 메뉴가 열린 상태에서 HUD가 비치는 문제를 고쳤다. 실제 캡처를 Higgsfield에 다시 넣어 문제별로 편집했다. | [다섯 마스터](../ArtSource/UI/Masters) |
| 확장 | 타이틀·상태·장비·가챠·주식·직업·학교·도전과제·상점·주거를 두 시안씩 비교했다. 네이티브 버튼, 게임 데이터, 한글 텍스트를 유지하고 삽화와 판형을 공유했다. | [확장 시안](../ArtSource/UI/Review/ExpansionConcepts.jpg) |
| 두 번째 재설계 | 상태창의 숫자 나열, 장비 착용자 식별, 상점 영수증, 첫 시세, 학교 활동을 각각 별도 편집했다. 학교와 회사는 현재 장소와 활동 카탈로그로 화면을 구성한다. | [집중 수정 시안](../ArtSource/UI/Review/Refinements.jpg) |
| 플레이 검증 | 월드 → 메뉴 → 월드, 가족 집·편의점·학교·회사·금융 이동, 입력 잠금, 구매·장착·추첨을 격리된 실제 Play Mode에서 30분 9초 동안 확인했다. | [품질 검토](UI_COMMERCIAL_QUALITY_REVIEW.md) |

Higgsfield CLI에서 모델을 조회한 뒤 스크린샷과 시안을 함께 참조할 수 있는 GPT Image 2.6을 사용했다. 무드보드 1장, 첫 시안 15장, 첫 수정 5장, 삽화·픽토그램 시트 2장, 확장 시안 20장, 집중 수정 5장으로 총 48장이다. 각 PNG 옆 JSON에 실제 생성 ID, 모델, 시간, 프롬프트와 참조 파일을 보관했다. 사용한 CLI에는 Canvas 템플릿 작성 명령이 없어 로컬 파일과 생성 ID로 계보를 남겼다. [시안과 생성 기록](../ArtSource/UI/Concepts)

게임에 들어가는 결과물은 타일 표면 아틀라스와 분리한 삽화·아이콘이다. 창, 선택지, 슬라이더 역할의 설정값, 장착 비교, 숫자, 한글은 Unity에서 그리며 기존 입력과 서비스 명령을 사용한다. [화면 목록](UI_SCREEN_INVENTORY.md) · [스타일 가이드](UI_STYLE_GUIDE.md) · [구성 요소](UI_COMPONENT_LIBRARY.md)

개편 전후 각각 42개 화면 상태를 같은 이름으로 보관했다. 1차 구현 뒤 세로 화면과 금융 창을 고쳤고, 다음 검토에서 상태·장비·상점·주식·학교를 집중 수정했다. 마지막으로 대화창 높이와 증권 화면의 비활성 메뉴를 정리하고 가챠 고지를 세 탭으로 나누었다. 각 수정은 실제 Game View에서 다시 열어 확인했다. [개편 전후](../ArtSource/UI/Review/BeforeAfter.jpg) · [내부 품질 판정](UI_COMMERCIAL_QUALITY_REVIEW.md)

열린 창을 회전하는 검사에서 가챠·장비 창의 크기 갱신, 약관과 확률 고지의 본문 높이 문제를 추가로 고쳤다. 학교에서 여러 행동의 잠금 사유가 같을 때는 사유를 한 번만 읽으면 된다. [세로 화면](../ArtSource/UI/Review/PhoneScreens.jpg) · [본문 높이 재검증](../ArtSource/UI/Review/ReadingAfter.json)
