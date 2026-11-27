# 지하철·버스·택시 승하차

도심과 주거 골목은 무료 도보로 연결된다. 대중교통을 선택하면 해당 시설의 실제 승차 지점까지 걸어간다. 양쪽 역은 거리의 출입구로 들어가 노선과 목적지를 확인하고 승강장에서 열차에 탄다. 버스 정류장과 택시 승강장은 각각 고유한 출발·도착 지점이 있다.

| 교통수단 | 시험 경로 | 요금 | 문과 동작 |
|---|---|---:|---|
| 지하철 | 도심역 → 주거 골목역 | 1,400 G, 멘탈 −2 | 승객 하차 후 열린 양개문으로 입장, 혼잡 압축, 반대편 승강장 도착 |
| 02번 버스 | 도심 → 주거 골목 | 1,200 G | 정류장 정차, 앞문 승차, 주행 중 차체 흔들림, 뒷문 하차 |
| 택시 | 주거 골목 → 도심 | 15,000 G | 정차, 뒷문 열림, 몸을 숙여 승차, 뒷좌석에 앉아 이동, 목적지 하차 |

`GameplayUXPresenter.Transit`가 선택·취소·탑승 중복을 관리한다. `LifeWorldController`가 실제 경로를 따라 승차 지점에 접근하고 `StationTrainDoors`는 열차가 열려 있는 동안 탑승을 받는다. 초기 재현에서 승강장 사용점이 열차 고정 충돌체에 걸렸다. 열차 충돌체의 전면 경계에서 접근점을 구하도록 제작 코드를 바꿨다. 목적지는 별도 Scene으로 미리 불러와 유효한 방·도착점을 확인한 뒤 기존 경제 서비스에서 1회 결제한다. 선택을 취소하거나 목적지 검증이 실패한 경우 출발 상태를 복원한다. [이동 구현](../Assets/_Game/Scripts/SceneManagement/RoomSceneTransitions.cs) · [실제 요금과 도착 좌표](WorldReconstructionQA/android-fares.tsv)

세 교통수단 모두 일반 재생·단축·건너뛰기 설정을 지원한다. 기본 재생과 실제 하차를 확인하고, Android에서는 취소 시 잔액이 유지되고 건너뛰기와 연속 두 번 탭에서 한 번만 차감되는 동작도 재현했다. 로드 도중 OS 종료, 잔액 동시 변경, 모든 컷신 설정 조합에 대한 별도 실기기 장애 주입은 미검증이다. [컷신 QA](TRANSIT_CUTSCENE_QA.md)

역방향으로는 주거 골목역에서 도심역까지 지하철 단축 연출, 주거 골목에서 도심까지 버스, 도심에서 주거 골목까지 택시를 이용했다. 세 번 모두 도착 후 조작이 복구되고 해당 운임만 한 번 차감됐다. 시험 후 원래 연출 설정을 복원했다. [Android 역방향](WorldReconstructionQA/android-reverse-fares.tsv) · [Game View](WorldReconstructionQA/editor-reverse-fares.tsv) · [macOS](WorldReconstructionQA/mac-reverse-fares.tsv) · [Android 취소·연속 탭·건너뛰기](WorldReconstructionQA/android-transit-options.tsv)
