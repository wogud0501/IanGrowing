# 이안 성장 외형 적용 기록

외형의 유일한 입력은 `LifeProgression.Age`다. 새 게임, 나이 변경, 방 입장, 저장 복원 시 `PlayerGrowthVisual`이 기존 성장 카탈로그에서 연령 구간 하나를 선택한다. 정면 스프라이트, 동작 라이브러리, 보폭, 이동 속도 계수 및 발 충돌체를 그 프로필에서 읽는다. 가구를 사용하는 중에는 기존 동작이 끝나는 시점에 프로필을 함께 바꾼다. 같은 단계를 다시 읽어도 크기를 곱하거나 성장 연출을 반복하지 않는다.

## 기존 문제와 수정 근거

- 직전 작업본은 서로 다른 연령의 프로필에 같은 원화와 동작 라이브러리를 연결했다. 그보다 앞선 생성기는 불규칙한 포즈 시트의 불투명 경계를 프레임마다 잘라내고 두 정지 프레임의 평균 높이로 단계 전체의 PPU를 정했다. 기존 학령기 보행 프레임 높이는 140–143px, 청소년기는 137–141px로 달랐다. 정면 정지 원화도 학령기에는 75×141과 74×142를 번갈아 표시했다.
- 이번 검사에서도 S20의 측면·대각선 아틀라스에서 별도의 재현 가능한 축소를 발견했다. 2056px 텍스처가 Unity 임포트 한도 2048px를 넘어 297px 프레임이 약 295.83px로 임포트됐다. 빈 열을 제거한 결과 아틀라스 폭은 1800px, 실제 임포트 높이는 원화와 같은 297px가 됐다.
- 학교와 다른 방은 같은 성장 카탈로그를 사용한다. 과거 제보 영상에 해당하는 빌드 번호나 당시 저장 파일에서 Y축 배율 누적을 입증하는 기록은 확보되지 않았다. 확인한 원인은 위 자산 선택·정규화·임포트 경로에 한정한다.

## 적용 파일

| 용도 | 프로젝트 파일 |
|---|---|
| 원본 분리·동작 아틀라스 제작 | [ian_growth.py](../Tools/Art/ian_growth.py), [보조 방향 및 자세 제작 원본](../ArtSource/Ian/Growth/) |
| 실제 Sprite, 피벗, PPU 및 단계별 동작 임포트 | [IanGaitImport.cs](../Assets/_Game/Scripts/Editor/Characters/IanGaitImport.cs), [20개 동작 라이브러리](../Assets/_Game/Data/Characters/Ian/GrowthStages/) |
| 방에서 사용하는 직렬화 데이터 | [GrowthVisuals.asset](../Assets/_Game/Data/Characters/Growth/GrowthVisuals.asset), [GrowthVisualCatalog.cs](../Assets/_Game/Scripts/Life/GrowthVisualCatalog.cs) |
| 캐릭터 재생 및 방향 전환 | [PlayerGrowthVisual.cs](../Assets/_Game/Scripts/Player/PlayerGrowthVisual.cs), [WorldPlayerMotor.cs](../Assets/_Game/Scripts/Player/WorldPlayerMotor.cs), [ActorAnimationSet.cs](../Assets/_Game/Scripts/Presentation/ActorAnimationSet.cs), [WorldActorAnimation.cs](../Assets/_Game/Scripts/Presentation/WorldActorAnimation.cs) |
| 착석 및 수면 | 기존 `WorldLifeAction`·`SeatBend`의 접근점과 골반/발 접촉을 사용한다. [BedQuilt.cs](../Assets/_Game/Scripts/Interaction/BedQuilt.cs)가 같은 단계의 앉기·기대기·눕기 프레임을 골라 베개 및 이불에 정렬한다. |
| 기존 제작 메뉴의 재생성 경로 | [GrowthVisualSetup.cs](../Assets/_Game/Scripts/Editor/Characters/GrowthVisualSetup.cs), [Phase30Setup.cs](../Assets/_Game/Scripts/Editor/World/Phase30Setup.cs), [Phase31Setup.Production.cs](../Assets/_Game/Scripts/Editor/Characters/Phase31Setup.Production.cs) |

프로덕션 방의 Scene 파일 18개가 원래 카탈로그 GUID를 직렬화하고 있다. 인스턴스별 복제 없이 그 카탈로그의 참조를 교체했다. 이안의 실제 프레임 재생기는 `WorldActorAnimation`과 `ActorAnimationSet`이며 스프라이트 적용 시 레거시 Animator를 비활성화한다. 액세서리는 원본 각 단계에 그려진 상태를 따른다. 구형 체형에 맞춘 장비 오버레이의 시각 적용은 보류되어 있다.

## 재현 및 유지보수

원본을 추가·변경한 경우 프로젝트 루트에서 `uv run --with pillow python Tools/Art/ian_growth.py`를 실행한다. Unity 메뉴 **Ian → Growth → Import official 20-stage growth sheet**가 Sprite와 카탈로그를 갱신한다. PlayMode의 `GrowthVisualCatalogTests`에서 나이 경계, 소속 라이브러리, 프레임 높이, 발 피벗 및 영아의 보행 제한을 검증한다. [원본별 매핑](IAN_GROWTH_SPRITE_MAPPING.md) · [실행 검증](IAN_GROWTH_ANIMATION_QA.md)
