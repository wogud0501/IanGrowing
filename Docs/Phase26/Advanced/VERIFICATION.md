# Advanced overhaul verification — 2026-11-12

## 실제 Editor Play 결과
|검사|통과|기록|
|---|---:|---|
|행동/방향/발걸음|105|../ADVANCEDANIMATION_RESULTS.json|
|실제 문/충돌/전환/로드 중단|110|../ADVANCEDDOORS_RESULTS.json|
|터치 액션/적응형 화면/입력 복구|48|../ADVANCEDMOBILE_RESULTS.json|
|주민 일정/방향/출입/저장/오프스크린|34|../ADVANCEDRESIDENTS_RESULTS.json|
|최종 실제 귀가/야간 화면|10|../ADVANCEDRESIDENTVISUAL_RESULTS.json|
|기존 경제/구매/배치/하루 회귀|90|../MASTER_RESULTS.json|

검사는 별도 GUID 저장 슬롯에서 실행하고 원래 상태/슬롯을 복원한다. 종료 직전 런타임 자동 저장을 비활성화하여 Play 종료 이벤트가 원래 사용자 파일을 다시 쓰지 않게 했다. 원래 설정은 Edit 복귀 시 유지된다. 시작 시점 3개 저장 파일 SHA-256과 비교한 결과 v1은 동일하나 v4 주 파일과 .bak 두 개는 21:56경 변경됐다. 현재 두 파일은 별도 백업에 보존했고 원래 파일을 교체하지 않았다. 주 파일/백업 간 stats·progression·economy는 동일하지만 world·missions·survival·time·timestamp가 다르다. 시작 바이트의 복구 사본은 발견하지 못했으므로 전체 기존 진행 무변경을 입증할 수 없다. 이 차이를 발견한 후 BeforeSceneLoad에서 sceneLoaded 콜백을 등록해 GameManager.Start 이전부터 별도 슬롯과 autoLoad=false를 설정했다. 후속 검사는 보강 직후 파일 기준으로 해시를 다시 비교한다.

기존 거리 보행자 수 강제 검사 대신 주민별 실제 점심/야간 일정 검사를 적용했다. 이전 실패 기록 MASTER_DAILY_AI_EXPECTATION_CHANGE.json 및 MASTER_NIGHT_SCHEDULE_EXPECTATION.json은 보존했다. 초기 플레이어 집과 NPC 생활관은 서로 다른 구역이다. 무료 일반 가구와 NPC 임금은 플레이어에게 전달되지 않는다.

## 수동 검증 절차
1. Bootstrap Play → 새 게임 → DAY 1 확인. 첫 집의 일반 가구는 침대 하나이며 화장실 설비는 별도다.
2. Game View에 포커스를 두고 WASD/방향키, Shift, E/I/Esc를 확인한다. 문이 닫힌 동안 통과 불가, 개방 후 접근 방향에 맞게 출입하는지 관찰한다.
3. 1280×720 / 1920×1080에서 메뉴·인벤토리·가구 구매/배치·저장/로드를 확인한다.
4. 08~09시 회사, 12:30 상점, 20시 만남, 23:50 생활관에서 주민의 걷기/방향/문/작업/식사/수면을 관찰한다. 부드러운 방향 전환과 발 접지는 추가 시각 승인이 필요하다.
5. 실제 Android/iOS에서 터치/외부 키보드 혼용, 화면 회전, 백그라운드/복귀를 확인한다. 폴더블은 외부 화면→펼침→메뉴/이동→접힘을 반복하면서 위치·현금·시간이 유지되는지 확인한다.

## 남은 품질/플랫폼 범위
- 모든 생활/감정의 네 방향 원본, 수면 캐릭터와 침대의 비율/포즈 정합, 실내별 연출/상업용 아트 최종 승인은 미완료다. 방향 대체 재생은 정식 방향 아트 완성이 아니다.
- Editor 합성 OnScreen 입력 이벤트와 해상도/Safe Area 검사는 실제 터치/폴더블/블루투스 기기 검증이 아니다.
- Play 재컴파일로 Awake 없이 정적 연결이 초기화되는 현상을 확인했고 GameContext.OnEnable 재연결을 추가했다.
- 실제 Android/iOS 성능·발열·배터리·메모리·30/60FPS 안정성과 플랫폼별 호환은 기기에서 검증해야 한다.
- 로컬 Git/ZIP은 장비 손실 대비 원격 백업이 아니다. 이 작업에서 원격 push는 수행하지 않았다.

## 플랫폼 산출물
- Android: ARM64 IL2CPP 개발 APK 생성 성공, 오류 0 / 경고 1207. SHA-256과 ABI는 ANDROID_ARTIFACT.json, 경고 원문은 ANDROID_BUILD_WARNINGS.txt. 기기 실행은 미검증.
- iOS: Xcode 프로젝트 내보내기 성공. Builds/Advanced/iOS/Unity-iPhone.xcodeproj. 전체 Xcode 앱이 없어 네이티브 앱 컴파일/서명/설치 미검증. 최종 내보내기 경고는 IOS_EXPORT_WARNINGS.txt.
- 기존 MissionCondition.children의 재귀 직렬화 경고가 다수 남아 있다. 저장/미션 원본 손상을 피하기 위해 SerializeReference로 일괄 전환하지 않았다. 안전한 에셋 마이그레이션 후 별도 검증이 필요하다.
- 설치된 AI inference 패키지의 compute shader도 빌드에 포함된다. 모바일 자원·지원 기기별 셰이더 최적화와 실제 메모리 프로파일링이 남아 있다.
- 문 닫힘 완료에서 기존 문 음원을 별도 피치/음량으로 재생하도록 연결했으며 닫힘/충돌 회귀를 다시 통과했다. 문 종류별 전용 사운드 아트는 추가 제작 가능하다.

최종 Android 클린 빌드 92초 / 139.2 MiB, iOS 내보내기 32초 / 경고 616. Android 증분 패키지 빈 공간 약 90 MiB를 클린 빌드로 제거했다. 최종 UI 11개 통과/넘침 0. 보강 이후 저장 3개 해시 무변경은 SAVE_ISOLATION_VERIFIED.json이며, 시작 기준 2개 차이 기록(SAVE_AUDIT.json)을 대체하지 않는다.
