# 게임 날짜와 요일 HUD

게임 시계의 Day 1은 기존 주민 스케줄의 월요일 비트에 해당한다. TimeManager에서 요일을 계산하며 게임 HUD, 주민 스케줄과 스케줄 검사기가 같은 규칙을 사용한다. 저장 데이터의 기존 DAY와 하루 내 경과 시간으로 복원한다. 신규 저장 필드는 없다.

CompactWorldStatusBar는 나이, DAY 또는 게임 분이 바뀔 때 표시를 갱신한다. 장소 검색 주기와 독립적으로 갱신하므로 자정과 기상 직후에도 새 날짜가 표시된다. WorldHUDLayout은 실제 폰트의 선호 너비를 측정하고 한 줄에 부족하면 장소와 시각을 두 줄로 나눈다. 세로 화면에서는 지갑과 미니맵을 아래로 이어 배치한다. 이전 HUD의 날짜 표시도 같은 시계를 사용한다.

| 확인한 상황 | 결과 |
|---|---|
| DAY 5의 23시대 | 금요일 |
| DAY 5 23시 59분에서 자정 통과 | DAY 6 토요일 00시 01분 |
| DAY 7 일요일에서 다음 날로 이동 | DAY 8 월요일 |
| 독립 집에서 DAY 1 밤 침대 사용 | DAY 2 화요일 07시 55분. 침대 옆에서 입력 복귀 |
| 부모님 집에서 DAY 8 밤 침대 사용 | DAY 9 화요일 07시대 |
| 도시 벤치에서 저장·종료·재실행·이어하기 | DAY 9 화요일, 저장한 벤치 옆 위치 복원 |
| 긴 주택 주소, 다섯 자리 DAY, 세로·폴더블 비율 | 날짜와 장소를 다시 측정. Android 1080×2400, iPhone 1206×2622에서도 실제 표시 확인 |

[TimeManager](../Assets/_Game/Scripts/Core/TimeManager.cs) · [날짜 HUD](../Assets/_Game/Scripts/UI/HUD/CompactWorldStatusBar.cs) · [화면 배치](../Assets/_Game/Scripts/UI/Layout/WorldHUDLayout.cs) · [모바일 화면](FontInteractionQA/Mobile.jpg)
