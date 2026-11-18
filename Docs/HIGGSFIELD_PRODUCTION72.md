# Higgsfield Production72 — Batch 1

2026-11-18 · 사용자 제공 72종 원문 등록, 첫 12종 생성/검수 및 일부 Unity 소품 제작 완료. 나머지 60종은 미생성 대기다.

[72종 원문/번호표](../ArtSource/Higgsfield/PromptPacks/Production72/INDEX.md) · [선택 레퍼런스 보드](../ArtSource/Higgsfield/Selected/Production72/BATCH1_REFERENCE_BOARD.png) · [개별 픽셀 소품 보드](../ArtSource/Higgsfield/Selected/Production72/PROPS_CLEANUP_BOARD.png) · [실제 Home Game View](Production72/Home-GameView.png).

## 제작 결과

- Batch 1: **01, 02, 15, 16, 18, 41, 42, 59, 60, 63, 67, 71**. 원문 MASTER/본문/NEGATIVE와 기존 STYLE FORMULA를 보존하고 생산 제약만 뒤에 추가했다.
- 실제 모델/워크플로 조회: 117개 모델, 22개 워크플로. Popcorn/Canvas/Moodboard 엔드포인트는 목록에 없었다. 사용 가능한 GPT Image 2.6의 참조 이미지 기능으로 위치를 고정했다. [조회 기록](../ArtSource/Higgsfield/PromptPacks/Production72/discovery.json).
- 총 **40회 명시적 후보 요청 시도 → 반환 ID/완료 다운로드 38개 → 선택 레퍼런스 12개**. 초기 16개는 부드러운 일러스트/품목 구성 문제로 탈락했고 해당 8종에만 픽셀 격자 중심 보정 후보를 2개씩 만들었다. 미선택 대안 10개도 추적한다. 무한 재시도나 대량 자동 생성은 없다.
- 주거지역 R2 B는 업로드 HTTP 520, 아침 A는 요청 HTTP 503으로 결과 ID가 반환되지 않았다. 완료·과금 여부를 추정하지 않는다. 각각 다른 후보를 선택했고 오류 요청을 재시도하지 않았다.
- 집/주거지역/편의점은 기존 프로젝트 소유 선택 이미지를 첨부해 보정한 뒤 고정했다. 02는 새 01 + 공식 이안 원본, 16/18은 새 15, 42는 새 41을 재사용한다. 42의 진열 재고 모습은 달라져 조명/이용객 레퍼런스로만 사용한다.
- 새 59/60은 각각 학생 6명/직장인 7명 디자인, 63은 차량 9종, 67은 도시 소품 14종, 71은 생존 소품 12종이다. 새 NPC 시트가 방향별 애니메이션 완성을 의미하지 않는다. 현 게임은 기존 완성된 NPC 애니메이션을 유지한다.
- 선택은 **Codex 시각 QA를 통과한 제작 레퍼런스**이며 최종 사람의 상업 아트 승인은 남아 있다. RAW Scene 이미지를 런타임 배경으로 넣지 않았다.

## Unity 적용

- 71의 생활 소품 12개 + 67의 재활용함/도로 차단물/소화전 3개를 **독립 스프라이트 15개 + Prefab 15개**로 제작했다. 투명 키 제거, 완전한 실루엣 분리, 최근접 리사이즈, 최대 96색, 이진 알파/2px 투명 여백을 확인했다. 고르지 않은 시트 행 때문에 생긴 인접 물체 조각은 연결 실루엣 기준으로 제거했다.
- `Assets/_Game/Art/WorldLife/`, `Assets/_Game/Prefabs/World/LivingCity/`. Point / 40 PPU / 무압축 / Mipmap 없음. 종이·컵류는 실제 Game View에서 캐릭터 대비 크기를 낮췄다. 기존 V2 Atlas는 **45개** 스프라이트를 패킹한다.
- `IndependentHome_Studio.unity`에는 현관 옆 고지서와 이삿짐 상자 두 장식만 추가했다. 시작 집에 책상/냉장고/소파를 미리 지급하지 않는다. Gameplay C#·공식 이안 원본·문/방 ID·기존 콜라이더·Save 키/버전·미션/아이템 ID를 바꾸지 않았다.
- 나머지 새 소품 Prefab은 배치 가능한 제작 자산이다. 모든 장소에 이미 배치했다고 주장하지 않는다.
- 원본/보정 후보·선택·모델·참조 해시·생성 ID/URL·검수 사유·Unity 출력은 [manifest.json](../ArtSource/Higgsfield/PromptPacks/Production72/manifest.json), 분리 좌표/크기/출력 해시는 [conversion.json](../ArtSource/Higgsfield/PromptPacks/Production72/conversion.json)에 있다. RAW PNG는 Unity/Git 밖 로컬에만 보관하며 선택 원본과 다운로드 URL은 보존한다.

## 검증

- 원문/프롬프트/참조/의존성/공식 이안/선택 해시 **561개 검사 통과**: [audit.json](../ArtSource/Higgsfield/PromptPacks/Production72/audit.json).
- 픽셀 소품 15개 알파/팔레트/여백 검사 통과: [pixel_audit.json](../ArtSource/Higgsfield/PromptPacks/Production72/pixel_audit.json). Unity 임포트/Prefab 참조 15개 및 45개 Atlas 패킹 확인: [IMPORT_RESULTS.json](Production72/IMPORT_RESULTS.json).
- Unity 프로젝트 컴파일 실패 없음, Gameplay C# 변경 없음. Console의 Unity AI Assistant `NoSubscription` 5건은 기존 외부 패키지 오류이며 이번 게임 코드 컴파일 오류가 아니다.
- 최종 크기 조정 후 **Play Mode 18개 통과**: 집 진입, 새 Sprite 표시, 이동, 원래 E 문 처리, 골목 왕복, 중복 장식 없음, 격리 Save/Load, 성장 유지, 원본 Save 보호. [SMOKE_RESULTS.json](Production72/SMOKE_RESULTS.json).
- Scene 28개/Prefab 91개 참조 검사: Missing Script 0, 새 Missing Reference 0. 기존 미해결 `m_Sprite` 32건은 동일하다. [비교 결과](Production72/REFERENCE_COMPARISON.json).
- 기존 GUID 1,936개, 원본 Save 파일 SHA-256 3개, Build Scene 목록 유지: [GUID_SAVE_BUILD_SCENES.json](Production72/GUID_SAVE_BUILD_SCENES.json).
- 실제 키보드/터치/모바일 성능, 새 NPC 방향별 애니메이션, 남은 60종 생성, 전체 도시 재제작, 전 지역 상업 아트 마감: **미검증 또는 미완료**. 이동은 실제 Rigidbody traversal, 상호작용은 실제 E 처리 경로를 호출했다.

## 다음 Batch

다음은 21 도심을 먼저 선택하고 그 이미지를 22–26에 연결한다. 이후 부모님 집/회사/학교 → 경제/주택 → 추가 소품 순서다. Batch 5에 다시 등장하는 63/67/71은 재생성하지 않는다. 보너스 이안 일상 8프레임은 원문만 등록했으며 아직 생성하지 않았다.

도구와 실행 방법: [production pack README](../ArtSource/Higgsfield/PromptPacks/Production72/README.md), [제작 도구](../Tools/Higgsfield/README.md). 원문 수정, 기준 장소 교체 또는 대량 재생성은 자동으로 수행하지 않는다.
