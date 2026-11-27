# 학교 교실과 교무 공간

`School_Classroom.unity`의 학생 대기석은 이름과 달리 가정용 Settee 스프라이트를 썼다. 벽으로 나뉘지 않은 교실 안에서 소파처럼 보였다. 목재 벤치를 서쪽 동선에 두고 그 북쪽에 교사용 책상·의자를 배치했다. 벽 두 구간 사이로 교실에 들어갈 수 있다. 교사 수업 자리는 칠판 앞, 학생 지정 좌석 네 곳은 칠판을 향한다. 기존 식수 상호작용은 정수기 스프라이트와 새 충돌체를 쓴다. [실제 Scene](../Assets/_Game/Scenes/Rooms/School_Classroom.unity) · [배치 코드](../Assets/_Game/Scripts/Editor/World/TransitStopsAuthoring.cs)

수업 시간표에 따라 방문했을 때 `classmate`와 `school-pupil`이 각각 `school-seat-0`, `school-seat-1`을 점유했고 `school-teacher`는 `teacher-lectern`에 있었다. 교사의 평일 15:18 휴식은 `faculty-break`에 연결했다. 동일 의자에 학생 둘을 배정할 수 없는지 7일 시간표와 접근 자격도 검사했다. [Android 실제 수업 기록](WorldReconstructionQA/android-world-results.tsv.gz) · [학교를 포함한 Game View](WorldReconstructionQA/editor-rooms.jpg)

도심의 `school-front`로 실제 입장하고 `school-exit`로 돌아오는 경로와 도착 좌표를 기록했다. 테스트 도구는 문 앞에서 시작하며 실제 문의 열림과 Scene 전환을 실행한다. 하루 종일 모든 학생·교사를 개별 추적하는 영상은 미검증이다. [통합 검증](ANDROID_PC_FINAL_QA.md)

## 복도와 교실의 실제 시야

교실 중앙의 전후 화면과 Android에서 서쪽 정수기까지 이동한 뒤의 시야를 보관했다. 복도 칸막이와 목재 벤치, 교실 쪽 학생 가구의 위치를 각각 확인할 수 있다. [교실 전후 비교](WorldReconstructionQA/school-before-after.jpg) · [서쪽 복도 실기기 화면](WorldReconstructionQA/android-west-corridor.jpg) · [입구·출구 실행 결과](WorldReconstructionQA/android-public.tsv)
