# 이안 공식 캐릭터 디자인 기준
게임: 이안 키우기 — BLACK COMEDY EDITION
이 문서 및 Reference/Ian_OfficialTurnaround.png는 이후 캐릭터 관련 작업의 기준입니다.

## 연령별 게임 외형

게임 속 이안의 정면 원화는 사용자가 제공한 5×4 성장 시트다. 왼쪽 위 S01부터 오른쪽 아래 S20까지 순서대로 적용한다. 헤드폰과 복장은 각 칸을 따른다. S01과 S02의 기본 자세는 앉기다. [공식 성장 원본](Art/Characters/Ian/Reference/Ian_OfficialGrowth.png) · [나이별 매핑](../../Docs/IAN_GROWTH_SPRITE_MAPPING.md)

## 기본 디자인과 다각도 자료
- 공식 다각도 원본은 [이안 기본 외형 시트](Art/Characters/Ian/Reference/Ian_OfficialTurnaround.png)입니다. 2026-11-22에 사용자가 첨부한 PNG를 그대로 보존합니다. 정면·사선·측면·후면과 위·아래 시점을 이 시트로 확인합니다.
- 이전 정면 자료는 Art/Characters/Ian/Reference/Ian_OfficialDesign.png에 보존합니다.
- 게임용 파생본: Art/Characters/Ian/Sprites/Ian_Base_Cutout.png (built-in image_gen으로 흰 배경을 투명하게 처리한 파생 이미지).
- 파생본은 원본 픽셀과 완전히 동일한 복사본이 아닙니다. 디테일 판단 시 공식 원본이 우선입니다.
- 중앙 데이터: Data/IanOfficialIdentity.asset.
- 이전 Art/2D/Ian_* 이미지는 보존된 구형 플레이스홀더이며, 새로운 이안 이미지의 기준으로 사용하지 않습니다.

## 필수 외형
검은 헝클어진 머리, 반쯤 감긴 피곤한 눈, 무표정하고 무기력한 인상,
목에 걸친 검은 헤드폰, 검은 반팔 상의, 검은 카고 바지, 슬리퍼,
전반적으로 어둡고 저채도인 분위기.
색/의상/신발/액세서리를 임의 교체하지 않습니다. 기본 감정은 활기차거나 해맑은 표정이 아닙니다.
플레이어, UI 초상화, 표정/상태 변화, 이벤트 일러스트, 컷씬, 애니메이션 스프라이트에 모두 적용합니다.

## 현재 적용
MainGame2D와 Ian2DCharacter 프리팹의 SpriteRenderer는 공식 디자인의 투명 파생본을 사용합니다.
Ian2DVisuals는 identity를 참조하고 Idle/Work/Rest/Play 프레임 전부 같은 기본 이미지를 사용합니다.
현재 행동 연출은 기존 위치 bob/시간 기반 반응만 유지합니다. 새로운 표정 애니메이션을 제작했다고 간주하지 않습니다.
프로토타입 UI/방 배경은 여전히 임시 디자인입니다. 캐릭터 디자인 기준은 사용자에 의해 확정되었습니다.

## 확장
CharacterIdentity의 expressions/states/eventIllustrations는 key+Sprite 배열입니다.
Expression/State/EventIllustration 조회 시 미등록 키는 baseSprite로 안전하게 대체합니다.
예시 키: tired, stressed, happy, exhausted, payday. 미등록 콘텐츠를 제작 완료로 표시하지 않습니다.
별도 UI 초상화가 없으면 IanPortrait 프리팹은 baseSprite 텍스처의 portraitRegion을 RawImage로 보여줍니다.
portraitTexture를 등록하면 전용 초상화 전체 영역을 사용합니다.
초상화 UV 표시 영역은 원본 파일을 편집하거나 별도 얼굴을 재생성하지 않습니다.
현재 전신 스프라이트의 중앙 피벗과 약 3.84 Unity unit 표시 크기를 기준으로 프레임 크기/피벗을 통일합니다.
최종 표정, 상태, 이벤트 이미지는 같은 원본을 참조해 별도로 제작/확인해야 합니다.

## 투명 배경 파생본 제작
도구: built-in image_gen / background-extraction.
프롬프트: 첨부한 공식 이안 전신 이미지에서 흰 배경과 빈 여백만 제거한다. 검은 헝클어진 머리, 피곤한 반쯤 감긴 눈, 무기력한 무표정, 목의 검은 헤드폰, 검은 반팔과 카고 바지, 슬리퍼, 포즈/선화/저채도 팔레트를 유지한다. 다른 인물/소품/텍스트를 추가하지 않는다. 머리와 발이 잘리지 않게 투명 배경 PNG로 출력한다.
