# Phase 3.3 — Living city / Higgsfield

2026-11-18 · 첫 생활 동선과 월드 확장 기반 구현. 최종 상업 아트 승인과 실기 성능 판정은 별도다.

## 구현

- 기존 `WorldNPCScheduler` 확장. 원래 17명 데이터/일정 유지, 배경 주민 8명 추가, 현재 방만 물리 스폰, 시간대·구역별 상한, 1초 인구 점검과 2명씩 입장, 개인 공간, 고정 순환 동선/대기, 휴대폰·짧은 인사, 화면 밖 저빈도 이동, 주민 오브젝트 풀 재사용. 배경 인사는 친구 선택이나 미션 보상을 변경하지 않는다.
- 기존 `WorldStreetLife` 확장. 세단·택시·버스·배달 밴·스쿠터 5종, 6대 재사용, 시간대별 교통량, 차량 길이를 고려한 횡단보도 대기, 앞차 간격, 버스 3.5초 정차/출발, 도로 끝에서 야간 초과 차량 퇴장. 기존 양방향 직선 도로를 사용한다.
- 골목·도시·편의점에 분리한 건물 외벽, 상품 진열, 냉장 진열, 소품과 차량 적용. 문 좌표와 기존 콜라이더 크기는 유지하고 새 외벽의 양쪽 바닥에 얇은 충돌 영역을 추가했다. 외벽/창문/문 뒤판/문짝을 기준 위치와 정렬 순서에 맞췄다. 반복 소품·차량·배경 건물은 Prefab이다.
- 기존 Scene Visual Profile에 연속 시각 팔레트, 캐릭터 틴트, 창문 발광과 카메라 지붕 여백 연결. 이동/내비게이션 범위는 바꾸지 않는다. 비는 공개 Visual Hook + 최대 96개 픽셀 입자/3개 약한 노면 반사이며 자동 날씨 게임플레이는 추가하지 않았다.
- 실제 한글은 Unity TMP 라벨과 데이터 대사로 표시. 독립/고지서, 직원 행복 캠페인/근무, 교대/장보기/버스 대기의 일상적 아이러니를 사용한다.

## 아트 / 파일

- Higgsfield 실제 반환·다운로드 64개 → 검수 선택 레퍼런스 30개. 초기 `batch_size=2` 호출은 결과 1개만 반환했다. 요청 수는 65개이며 반환되지 않은 1개의 생성 ID/완료 여부는 확인되지 않았다. 이후 단일 요청으로 기록하고 자동 재시도하지 않았다.
- 선택된 시트/모듈을 키 제거·개별 분리·최근접 리사이즈·96색/이진 알파로 정리한 스프라이트/마스크 28개 + 직접 만든 픽셀 비 VFX 2개. 임포트는 Point / 40 PPU / 무압축, V2 Atlas 30개 패킹 확인(512×1024, RGBA32).
- RAW는 `ArtSource/Higgsfield/Raw`에만 존재하며 Unity/Git에서 제외. 선택 원본/프롬프트/결과 ID/URL/검수 이력은 보존. 거대한 생성 Scene 이미지를 새 런타임 배경으로 사용하지 않는다.
- 새 Prefab 33개, 자체 C# 15개 신규/수정, 기존 Room Scene 16개 연결 갱신. 새 Scene 또는 패키지/SDK 없음.
- 주요 코드: `Scripts/NPC/ResidentPopulation.cs`, `NPCCrowdSpace.cs`, `SceneScopedResidents.cs`, `WorldNPCScheduler.cs`; `Scripts/World/Population/WorldLifeProfile.cs`, `WorldStreetLife.cs`, `SceneVisualProfile.cs`, `SceneVisualPresentation.cs`, `WorldCameraController.cs`, `Presentation/WorldWeatherPresentation.cs`; `Scripts/UI/HUD/WorldHUD.cs`; `Scripts/Editor/World/Phase33Setup.cs`, `Phase33Verification.cs`.
- 자산: `Art/WorldLife`, `Data/World/LivingCity`, `Prefabs/NPC/Ambient`, `Prefabs/World/LivingCity`, `Prefabs/World/Vehicles`.
- [아트 방향](HIGGSFIELD_ART_DIRECTION.md) · [전체 생성/변환 Manifest](HIGGSFIELD_ASSET_MANIFEST.md) · [제작 도구](../Tools/Higgsfield/README.md).

## 검증

- Unity 6000.6.8f1 컴파일 성공. 변경/신규 C# 15개 CSharpier 검사 통과.
- [Play Mode 통합 47개](Phase33/INTEGRATION_RESULTS.json): 19세 독립 집 → 골목 주민 이동/인사 → 도시 출근 인구/차량 → 버스 정차/출발 → 편의점 직원/손님/실제 계산대 UI 구매 → 저녁/비/밤 → 귀가 → Save/Load. 풀 객체 재사용, 야간 차량 퇴장, 실제 학교 학생/회사 직원 출근도 확인.
- [데이터/공간 분할 98개](Phase33/DATA_AND_GRID_RESULTS.json): 일정/방 ID/인구 데이터/임포트/Atlas, 공간 점유·이동·제거·캐시 초기화 검증. 워밍업 후 공간 분할 갱신/질의 1만 회 관리 메모리 할당 0B. 전체 게임의 GC가 0B라는 뜻은 아니다.
- [GUID/Build Scene](Phase33/GUID_AND_BUILD_SCENES.json): 기존 `.meta` 1,938개 GUID 유지, 원본 삭제 없음, Build Scene 26개/목록 변경 없음.
- [Reference 검사](Phase33/REFERENCE_AUDIT.json): Scene 28개/Prefab 76개, 새 Missing Script/Reference 없음. 기존 `m_Sprite` 미해결 참조 32건은 그대로 남아 있다.
- [원본 저장 파일 SHA-256 3개 동일](Phase33/SAVE_HASH_VERIFICATION.json). 시작 전부터 테스트 저장 경로를 분리했으며 기존 Save 버전/키/미션·아이템 ID를 변경하지 않았다. 새 주민은 기존 resident snapshot에 안전하게 추가된다.
- 도시 Editor Game View의 FPS/Draw Call은 통합 결과의 샘플에 기록했다. 에디터 수치이며 실제 기기 보증치가 아니다.

## 실제 Game View

[독립 집](Phase33/Screenshots/01-IndependentHome.png) · [골목](Phase33/Screenshots/02-Residential-Day.png) · [출근 도시](Phase33/Screenshots/03-City-Rush.png) · [편의점 구매](Phase33/Screenshots/04-Convenience-Purchase.png) · [저녁](Phase33/Screenshots/05-City-Evening.png) · [비](Phase33/Screenshots/06-City-Rain.png) · [밤](Phase33/Screenshots/07-City-Night.png) · [귀가](Phase33/Screenshots/08-Return-Home.png) · [집 입구](Phase33/Screenshots/09-Residential-Entrance.png) · [편의점 외부](Phase33/Screenshots/10-Convenience-Exterior.png).

## 미검증 / 후속

- 원시 키보드·터치·컨트롤러 및 Android/iOS/Foldable 실기 입력/성능: **NOT VERIFIED**. 통합 이동은 실제 Rigidbody traversal, 상호작용은 동일한 E 처리 경로와 UI 버튼을 호출했다.
- 버스 승하차/도로 분기·교차로 회전은 구현하지 않았다. 현재 도로에 맞춘 직선 차선/정차 기반이다.
- 신규 NPC 디자인 시트는 제작 레퍼런스이며 현 주민은 기존 완성된 방향별 애니메이션을 재사용한다. 새 전용 NPC 방향 애니메이션을 제작했다고 주장하지 않는다.
- 학교·회사·금융 등 추가 장소 레퍼런스는 준비됐으며 전 지역의 새 모듈 제작/상업 아트 마감, 장시간 혼잡/실기 프로파일링, 기존 레거시 Sprite 참조 복구는 후속 검토 대상이다.
