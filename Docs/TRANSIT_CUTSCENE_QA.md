# 교통 컷신 연속 재생 검증

승인된 Higgsfield GPT Image 2.6 생성 1건에 0.75크레딧을 사용했다. 작업 ID는 `4ef7423d-c540-4881-b47a-b37c84e4cb3c`이다. 한 장에 의뢰한 빈 지하철·버스·택시 객실 3종을 나누어 Unity에 적용했다. 공식 이안은 현재 성장 단계의 게임 스프라이트와 애니메이션 뱅크에서 가져온다. [보존된 생성 원본](../ArtSource/Higgsfield/Selected/Transit/Cabins.png) · [스프라이트 제작 도구](../Tools/Art/transit_layers.py)

객실별로 원근과 좌석을 맞추고 문짝, 이안, 승객, 창문 너머 풍경을 독립적으로 움직인다. 열차에서는 하차 대기, 문 열림, 승차와 짧은 밀집 압축, 도착 후 밀려 나옴을 표현한다. 버스는 앞문·뒷문을 분리했다. 택시는 뒷문과 뒷좌석의 골반 높이를 맞췄다. 창밖 이동 그림은 손잡이·창틀과 원래 유리의 반사가 남도록 유리 부분에만 합성한다. 기본 길이는 열차 3.15초, 버스·택시 3초다. 실제 승하차의 선후 관계와 손잡이 사용은 교통 운영기관 자료를 참고했다. [MTA 승객 안내](https://www.mta.info/document/76901) · [MTA 승하차 안내](https://www.mta.info/document/10296)

| 환경 | 지하철 | 버스 | 택시 | 도착과 결제 |
|---|---|---|---|---|
| Unity Game View | [연속 녹화](WorldReconstructionQA/editor-train.mp4) | [연속 녹화](WorldReconstructionQA/editor-bus.mp4) | [연속 녹화](WorldReconstructionQA/editor-taxi.mp4) | [기록](WorldReconstructionQA/editor-fares.tsv) |
| Android 실기기 | [ADB 녹화](WorldReconstructionQA/android-train.mp4) | [ADB 녹화](WorldReconstructionQA/android-bus.mp4) | [ADB 녹화](WorldReconstructionQA/android-taxi.mp4) | [기록](WorldReconstructionQA/android-fares.tsv) |
| macOS 실행 빌드 | [연속 녹화](WorldReconstructionQA/mac-train.mp4) | [연속 녹화](WorldReconstructionQA/mac-bus.mp4) | [연속 녹화](WorldReconstructionQA/mac-taxi.mp4) | [기록](WorldReconstructionQA/mac-fares.tsv) |
| iOS 시뮬레이터 | [연속 녹화](WorldReconstructionQA/ios-train.mp4) | [연속 녹화](WorldReconstructionQA/ios-bus.mp4) | [연속 녹화](WorldReconstructionQA/ios-taxi.mp4) | [기록](WorldReconstructionQA/ios-fares.tsv) |

실기기는 실제 승차 UI를 ADB 터치하고 화면을 연속 녹화했다. 자동화는 승차 시설 앞에 시험 캐릭터를 배치한 후 정상 게임 상호작용을 호출했다. 네 환경의 동영상 12개에 출발지·도착지·해상도·길이·프레임 수를 기록했다. Windows 네이티브 실행과 실제 iPhone에서의 컷신은 미검증이다. [녹화 목록](WorldReconstructionQA/video-index.tsv)

버스의 좌석은 출입문과 중앙 기둥에 맞췄다. 지하철·버스에서 한 승객은 기존 원화의 소매·손을 관절에서 구부려 기둥을 잡는다. 차가 흔들려도 손은 기둥에 남는다. [Android 객실 세 장면](WorldReconstructionQA/android-vehicle-interiors.jpg) · [버스 승하차](WorldReconstructionQA/android-bus-sequence.jpg) · [지하철 승하차](WorldReconstructionQA/android-train-sequence.jpg)

택시 승차 녹화에서 손잡이에 손을 맞출 때 발까지 바퀴 높이로 들리는 것을 발견했다. 서 있는 단계별 공식 스프라이트의 손목 위치를 읽어 어깨부터 팔을 회전시키고 발은 노면에 고정했다. 차량 안에 들어간 프레임부터 승객에 뒷문 모양 마스크를 적용해 지붕 앞에 얼굴이 보이던 현상도 고쳤다. Android에서 승차·주행·하차를 다시 녹화해 수정 전후를 비교했다. [수정 전](WorldReconstructionQA/android-taxi-before.jpg) · [Android 수정 후](WorldReconstructionQA/android-taxi-sequence.jpg) · [macOS](WorldReconstructionQA/mac-taxi-sequence.jpg) · [iOS 시뮬레이터](WorldReconstructionQA/ios-taxi-sequence.jpg) · [단계별 팔 동작 코드](../Assets/_Game/Scripts/Presentation/CabDoorReach.cs)
