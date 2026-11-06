# Assets and remaining production work

## 실제 제작·적용·검증

폴더는 기존 프로젝트의 `Assets/_Game`을 유지했다. 이번 환경 이미지는 built-in imagegen으로 새로 생성한 제작 후보이며 외부 상용 에셋 팩을 가져오지 않았다. 픽셀 아트풍 원본에서 nearest 파생본을 만들었다. 모든 픽셀을 수작업 정리한 최종 납품물이라는 의미는 아니다. 기존 공식 이안 원본은 수정하지 않았다.

| 경로 (`Assets/_Game/` 기준) | 규격 / 내용 | 상태 |
|---|---|---|
| Art/Commercial/City/City_Source.png → City_640.png | 원본 보존 / 640×360 네온 도시 | 제작·씬 적용·Game View 검증 |
| Art/Commercial/Workplace/Office_Source.png → Office_640.png | 원본 보존 / 640×360 사무실 | 제작·씬 적용·검증 |
| Art/Commercial/Workplace/Convenience_Source.png → Convenience_640.png | 원본 보존 / 640×360 편의점 | 제작·직업 fixture 적용·검증 |
| Art/Commercial/Animations/CityOffice_Source.png | 1672×941 투명 원본, 보행/타이핑/차량/간판 각 4프레임 | 제작·보존 |
| Art/Commercial/Animations/CityOffice_Logical.png | 836×470 nearest 파생 시트 | native Sprite 서브에셋으로 분할·적용 |
| Data/Commercial/CityOfficeSprites.asset | 4종×4 실제 프레임 라이브러리 | 시간 진행/프레임 교체 검증 |
| Data/Commercial/Workplace-*.asset | office/developer/analyst/convenience 테마 | 기존 job ID 연동 검증 |
| Art/Commercial/CommercialUI.spriteatlasv2 | 기존 UI 27개 + 새 환경 프레임 16개 | 43개 pack, 실제 프레임 packed=true |
| UI/Pixel/Fonts/Galmuri/ | TTF/OFL/TMP font assets | 라이선스 포함·한글 검사 완료 |

학교·집의 기존 신규 배경과 도트 9-slice는 재사용했다. City의 전경·중경·후경은 **동일 원화를 깊이별 가로 밴드로 나눈 별도 렌더러**다. ±2 논리 픽셀 카메라 움직임과 서로 다른 계수로 작게 이동한다. 완전히 따로 그린 가려짐 레이어나 넓은 횡스크롤 도시가 아니다.

환경 NPC는 4프레임 보행과 정지 시 첫 프레임/1px 호흡을 사용한다. 독립적인 새 Idle 프레임을 제작한 것은 아니다. 타입/차량/전광판은 실제 4프레임이다. 프레임 중심 정렬과 알파 가장자리는 추가 수작업 검수가 필요하다.

## 미제작·임시 영역과 확장 규격

| 필요한 파일 / 영역 | 제안 규격 | 제작 상태 |
|---|---|---|
| Ian_Idle_*.png / Blink_*.png | 공식 디자인 기준 96×128, Idle4/Blink3, 공통 발 pivot | 미제작; 공식 단일 포즈+픽셀 이동 호흡 유지 |
| Ian_Walk_*.png | 96×128, 방향별6프레임, 8fps | 미제작 |
| Ian_Sleep/Eat/Drink/Study/Work_*.png | 96×128, 각4~8프레임, 동작별 fps·길이 지정 | 미제작; 상태/이벤트 훅만 재사용 |
| Ian_Happy/Sad/Tired/Stress/Interaction_*.png | 96×128, 각3~6프레임 | 미제작 |
| City_Skyline/Facades/Road/Foreground.png | 각640×360, 가려짐 알파 분리,100PPU | 독립 원화 미제작; 현재 crop depth-band |
| City_Day/Evening/Night_*.png | 같은 카메라/640×360, 창·반사 별도 마스크 | 미제작; 현재 색조 기반 시간 변화 |
| Home_Curtain_*.png / School_Student_*.png | 32×64/48×64,4프레임,알파 | 미제작; 가구·학생·교사는 배경에 구워짐 |
| Workplace_Developer/Analyst.png | 640×360, 모니터·서버/금융 화면 분리 | 고유 배경 미제작; office 원화+tint/monitor 변형 |
| Workplace_CEO.png | 640×360, desk/window/foreground 분리 | 확장 목록만; specialUnlock 직업 기능 미구현 |
| Transition_Door/SchoolGate/Elevator_*.png | 160×180,6~8프레임,정수 pivot | 미제작; 현재 프로필 셔터·entry/exit 훅 |
| Transition_*.wav / Payroll_*.wav | 짧은 원본 효과음, mixer group 지정 | 새 음원 미제작 |

배경에는 이미 풍부한 조명·가구·반사가 있지만 대부분 구워진 이미지다. 편의점 theme은 좌석 동료를 숨기고 보행/모니터 환경을 재사용한다. 일부 메뉴/보조 기능은 기존 UI 컴포넌트를 계속 쓰며, 요청된 모든 Pixel* 이름을 새 독립 prefab으로 만든 것은 아니다. 이 범위를 숨기고 Stage C/D 전체 완료로 취급하지 않는다.

## 공통 제작 정책

월드 100PPU / 640×360 / Point / no mipmap / URP PixelPerfectCamera UpscaleRT + Windowbox / MSAA·HDR·post AA 끄기. 텍스처 원본은 보존하고 런타임 씬은 파생본을 참조한다. 기본 배경은 알파 없는 640×360, 추가 소품은 투명 PNG, 프레임별 같은 캔버스·pivot을 권장한다. 현재 생성 시트의 불균일 셀은 알파 밴드와 열로 잘라 native Sprite subasset을 만들었다.

UI/환경 프레임은 V2 atlas, Point, padding4, 회전/타이트 끄기, Windows/Android RGBA32 무압축이다. 실제 pack texture 512×1024. 픽셀 가장자리 보존을 우선한 선택이며 기기 측정 후 지원 포맷을 검토한다. 새 외부 리소스를 추가할 때는 출처·상업 사용·게임 내 재배포 조건·원문 라이선스를 이 등록부에 함께 남긴다.
