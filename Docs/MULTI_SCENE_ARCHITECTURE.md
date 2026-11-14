# 멀티 씬 구조 — 현재 기준

2026-11-14 주거/Scene 개편을 반영한 상세 문서는 [RESIDENTIAL_SCENE_ARCHITECTURE.md](RESIDENTIAL_SCENE_ARCHITECTURE.md)이다. 이전 문서의 “Home/School만 존재”, “Additive 미사용”, “회사/금융 Scene 미생성” 설명은 현재 상태와 맞지 않아 갱신했다.

Bootstrap을 열고 Play한다. 기존 GameContext가 전역 게임 상태를 유지하고, 일반 플레이는16개의 방/장소 Scene으로 이동한다. 부모님 집5방, 개인 스튜디오와 욕실, 도시/골목, 회사/회사 욕실, 학교, 상점, 증권사, 부동산, 주민 주거 공간을 별도로 관리한다. 방 전환은 비동기 Additive staging 후 이전 씬을 언로드한다.

기존 Scene과 ID는 호환용으로 보존했다. SceneRoutes는 실제 Unity Scene을, RoomScenes는 RoomId·문·EntryId·구버전 좌표 매핑을 관리한다. 현재 Scene과 방은 기존 World snapshot에 저장된다. UI·경제·미션·NPC 관계와 개인 가구 소유권은 전역 서비스에 남는다.

새 게임은 부모님 집의 이안 방이다. 보행1세, 독립 외출7세, 선택 독립19세를 분리한다. 개인 집 출입구와 표시판은 기존 독립 거래가 완료되기 전에는 노출하지 않는다.

검증 근거는 [SceneArchitecture/RESULTS.md](SceneArchitecture/RESULTS.md), 중단 시 재개 지점은 [SCENE_ARCHITECTURE_PROGRESS.md](SCENE_ARCHITECTURE_PROGRESS.md)를 따른다.
