# Phase 3.0 — Presentation production pass

2026-11-16. 시작 체크포인트 `8cf50fd`. Unity 6000.6.8f1. Gameplay/Save version4 유지.

## 현재 작업 단위

- 3.0.1: Point/uncompressed/no-mipmap 임포트 및 실제 렌더 픽셀 기준 카메라 정렬. 기존 40 PPU 재정렬 충돌 제거.
- 3.0.2: 기존 0~18세 8개 외형과 성인 원화 보존. 중년/노년 원화·방향별 Idle/Walk/FastWalk·Down 방향 생활 프레임 추가. 기존 LifeStage의 19~29/30~59/60+를 사용하며 이미지의 예시 나이를 게임 권한으로 복사하지 않음.
- 성장 이벤트: 기존 컨트롤러/나이 상태 유지, 12개 재사용 파티클과 이전 실루엣의 짧은 전환.
- 3.0.3: 새 독립 집용 연속 바닥·흰 벽·건축 외곽 제작. 기존 가구 저장 좌표·문 연결 보존. 예전 가구가 있는 세이브는 기존 건축 루트를 사용.
- 실제 적용은 원본 Scene/ScriptableObject에 저장. 한 장의 방 이미지를 배경으로 사용하지 않음.

## 원화·리소스

`Assets/_Game/Art/Phase30`: Ian_MiddleAge / Ian_Elder 8×8 아틀라스, InteriorSurfaces_Source와 독립 재료 패치, GrowthSparkle.

중년/노년의 Turn/Stop은 Idle, Work는 타이핑, PutDown은 PickUp 공유. 생활 행동의 별도 방향 아트는 Down만 있으며 나머지는 기존 안전 fallback. 개별 행동 전체를 신규 제작 완료로 판정하지 않음.

[생성 프롬프트](Phase30/ART_PROMPTS.md) · [원화 출처](Phase30/ART_PROVENANCE.md)

## 검증

- 수정 전: 실제 Play Mode에서 Home/가족 거실/이안 방/City 캡처 완료.
- 첫 검사 실행: 테스트 코드의 폐기 API를 Unity 6.6 호환 참조 비교로 수정. 컴파일 중 시작된 테스트 세션은 종료하고 안정된 임포트 후 재시작.
- 수정 후 Play Mode 검증 진행 중. 실행 결과가 없는 항목은 PASS 아님.

## 남은 범위

우선 세 작업의 Game View 검증 후 부모님 집 → City → 학교 → 회사 확장. 기타 시설·UI Motion·VFX 이벤트·Audio hook·성능 검사는 이후 단위로 진행. 실제 Android/iOS/폴더블 기기는 미검증. 기존 기능을 삭제하지 않고 fallback 유지.
