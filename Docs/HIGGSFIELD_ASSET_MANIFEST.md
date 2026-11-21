# Higgsfield asset manifest

2026-11-21 UI. [Monochrome production manifest, 40 generations](../ArtSource/UI/Monochrome/Concepts/Manifest.md) · [Unity comparison](../ArtSource/UI/Monochrome/Review/MasterToGame.jpg)

2026-11-18 · 64 candidates, 30 selected production references, 28 separated sprites/emissive masks (+ 2 authored rain VFX textures).

Original Phase 3.3 run only. [Production72](HIGGSFIELD_PRODUCTION72.md) is tracked separately.

Selections passed Codex visual QA; final human art approval remains open. RAW is outside Unity. Only separated, cleaned objects enter Assets.

| Reference | Candidate | Unity outputs |
|---|---|---|
| [residential_day](../ArtSource/Higgsfield/Selected/residential_day.png) | residential_day | 0 |
| [independent_home](../ArtSource/Higgsfield/Selected/independent_home.png) | independent_home_b | 0 |
| [apartment_exterior](../ArtSource/Higgsfield/Selected/apartment_exterior.png) | apartment_exterior_b | 0 |
| [convenience_exterior](../ArtSource/Higgsfield/Selected/convenience_exterior.png) | convenience_exterior_b | 0 |
| [urban_props](../ArtSource/Higgsfield/Selected/urban_props.png) | urban_props_a | 12 |
| [vehicles](../ArtSource/Higgsfield/Selected/vehicles.png) | vehicles_a | 6 |
| [apartment_module](../ArtSource/Higgsfield/Selected/apartment_module.png) | apartment_module_b | 2 |
| [convenience_module](../ArtSource/Higgsfield/Selected/convenience_module.png) | convenience_module_b | 2 |
| [parents_home](../ArtSource/Higgsfield/Selected/parents_home.png) | parents_home_b | 0 |
| [city_day](../ArtSource/Higgsfield/Selected/city_day.png) | city_day_b | 0 |
| [school_exterior](../ArtSource/Higgsfield/Selected/school_exterior.png) | school_exterior_a | 0 |
| [classroom](../ArtSource/Higgsfield/Selected/classroom.png) | classroom_a | 0 |
| [school_hallway](../ArtSource/Higgsfield/Selected/school_hallway.png) | school_hallway_b | 0 |
| [office_day](../ArtSource/Higgsfield/Selected/office_day.png) | office_day_b | 0 |
| [cafe](../ArtSource/Higgsfield/Selected/cafe.png) | cafe_a | 0 |
| [finance](../ArtSource/Higgsfield/Selected/finance.png) | finance_a | 0 |
| [realestate](../ArtSource/Higgsfield/Selected/realestate.png) | realestate_b | 0 |
| [furniture_store](../ArtSource/Higgsfield/Selected/furniture_store.png) | furniture_store_a | 0 |
| [clinic](../ArtSource/Higgsfield/Selected/clinic.png) | clinic_b | 0 |
| [irony_props](../ArtSource/Higgsfield/Selected/irony_props.png) | irony_props_a | 0 |
| [convenience_interior_clean](../ArtSource/Higgsfield/Selected/convenience_interior_clean.png) | convenience_interior_clean_b | 0 |
| [npc_clean](../ArtSource/Higgsfield/Selected/npc_clean.png) | npc_clean_b | 0 |
| [furnished_home](../ArtSource/Higgsfield/Selected/furnished_home.png) | furnished_home_b | 0 |
| [residential_evening](../ArtSource/Higgsfield/Selected/residential_evening.png) | residential_evening_a | 0 |
| [residential_rain](../ArtSource/Higgsfield/Selected/residential_rain.png) | residential_rain_b | 0 |
| [city_evening](../ArtSource/Higgsfield/Selected/city_evening.png) | city_evening_b | 0 |
| [city_night](../ArtSource/Higgsfield/Selected/city_night.png) | city_night_a | 0 |
| [city_rain](../ArtSource/Higgsfield/Selected/city_rain.png) | city_rain_b | 0 |
| [office_night](../ArtSource/Higgsfield/Selected/office_night.png) | office_night_a | 0 |
| [store_props](../ArtSource/Higgsfield/Selected/store_props.png) | store_props_a | 6 |

- Full prompts, references, timestamps, IDs, QA status, output URLs and SHA-256: [manifest.json](../ArtSource/Higgsfield/manifest.json).
- Crop boxes, logical dimensions and alpha/palette rules: [conversion.json](../ArtSource/Higgsfield/conversion.json).
- Four unsuitable initial sheet candidates were rejected. Each affected prompt received one focused revision and two replacements. No automatic generation retry.
- Popcorn / Canvas / Moodboard endpoints were absent from the discovered catalog. Variants explicitly referenced the selected base image. The local master board contains all selected original references.
- Initial CLI batch_size=2 requested two candidates but returned one result. Thus 65 candidates were requested, 64 completed result IDs were returned/downloaded, and one initial result ID remains unavailable. Later candidates used separately recorded single requests; no hidden result is claimed as verified.
- Candidate rasters remain local outside Unity and Git. Selected source images, requests, results and download URLs are retained for reproduction.
- No third-party game screenshots or real logos were used.

## Master Reference 12

2026-11-19 · Master12. 원화와 함께 입력한 실제 이미지, 각 후보의 ID·URL·날짜·SHA-256 및 Unity 경로는 [전체 제작 기록](../ArtSource/Higgsfield/Selected/Master12/manifest.json)에 보존했다. 선정은 제작 단계의 시각 검수 결과다.
[12장 콘택트 시트](../ArtSource/Higgsfield/Selected/Master12/Review/Master_Contact_Sheet.jpg) · [Game View 비교](../ArtSource/Higgsfield/Selected/Master12/Review/Game_View_vs_Master.jpg) · [스타일 기준 및 검수](../ArtSource/Higgsfield/Selected/Master12/STYLE_LOCK.md)

| 마스터 / 선정 | Higgsfield ID · 모델 | 입력 이미지 | 검수 근거 · 적용 |
|---|---|---|---|
| [IAN_MASTER_YOUNGADULT](../ArtSource/Higgsfield/Selected/Master12/IAN_MASTER_YOUNGADULT.png) · b_edit1 | 7a79cb0b-5aa1-41f3-a1b0-dc2febc041c0 · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/IAN_MASTER_YOUNGADULT_b_edit1.txt) | IAN_MASTER_YOUNGADULT_b, Ian_OfficialDesign | B의 네 방향, 처진 눈, 목의 이어컵과 슬리퍼가 선명하다. A와 B 모두 머리가 커서 B를 한 번 교정했다. 교정본은 긴 팔다리 덕분에 19세 이안으로 읽힌다. · 제작 레퍼런스 |
| [HOME_INDEPENDENT_MASTER](../ArtSource/Higgsfield/Selected/Master12/HOME_INDEPENDENT_MASTER.png) · a_edit1 | f7623bbe-d07d-465a-b01b-6c249f90adb5 · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/HOME_INDEPENDENT_MASTER_a_edit1.txt) | HOME_INDEPENDENT_MASTER_a | A의 빈 바닥, 침대 한 개와 옥색 욕실을 유지했다. 교정본에서 중앙 인물이 사라지고 앞문이 열려 이삿짐과 고지서 사이 통로가 읽힌다. · 제작 레퍼런스 |
| [CITY_RESIDENTIAL_MASTER](../ArtSource/Higgsfield/Selected/Master12/CITY_RESIDENTIAL_MASTER.png) · a | ad2ed060-edea-4a9a-a24a-180769ac51fe · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/CITY_RESIDENTIAL_MASTER_a.txt) | 015, HOME_INDEPENDENT_MASTER, IAN_MASTER_YOUNGADULT | 중앙 현관부터 버스 정류장까지 보도가 이어지고, 등교생·행인·배달 스쿠터가 다른 속도로 읽힌다. B의 두 스쿠터보다 통로가 명확하다. · 제작 레퍼런스 |
| [SHOP_CONVENIENCE_MASTER](../ArtSource/Higgsfield/Selected/Master12/SHOP_CONVENIENCE_MASTER.png) · a | f973dcfa-ea57-4723-a8db-ea964076747e · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/SHOP_CONVENIENCE_MASTER_a.txt) | 041, CITY_RESIDENTIAL_MASTER, IAN_MASTER_YOUNGADULT | A는 현관 유리문에서 왼쪽 계산대와 냉장고까지 동선을 추적할 수 있다. 형광등, 진열색 블록, 창가 좌석이 작은 점포의 밀도를 만든다. · 제작 레퍼런스 |
| [NPC_ARCHETYPE_MASTER](../ArtSource/Higgsfield/Selected/Master12/NPC_ARCHETYPE_MASTER.png) · a | c432aa68-69e0-4a80-a73c-824903540686 · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/NPC_ARCHETYPE_MASTER_a.txt) | IAN_MASTER_YOUNGADULT, CITY_RESIDENTIAL_MASTER | A는 지팡이·배달 헬멧·서류와 열쇠의 윤곽이 배경에서 잘 분리된다. 2×5 배열의 열 직군과 동일 발 기준을 확인했다. · 제작 레퍼런스 |
| [VEHICLE_MASTER](../ArtSource/Higgsfield/Selected/Master12/VEHICLE_MASTER.png) · a | 519b9b99-0cd0-4c8f-ad1f-c01d9dfbbb40 · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/VEHICLE_MASTER_a.txt) | CITY_RESIDENTIAL_MASTER, IAN_MASTER_YOUNGADULT | A는 승용 3종·버스·밴·스쿠터·자전거 7종이 겹치지 않는다. 창문과 버스 출입문이 같은 거리의 측면 카메라에서 읽힌다. · 제작 레퍼런스 |
| [URBAN_PROP_MASTER](../ArtSource/Higgsfield/Selected/Master12/URBAN_PROP_MASTER.png) · a | 8263a217-9b79-42fc-883e-3374aad6f1cf · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/URBAN_PROP_MASTER_a.txt) | CITY_RESIDENTIAL_MASTER, VEHICLE_MASTER | A의 등받이·팔걸이·두 다리가 긴 의자의 앞뒤를 구분한다. 신호등, 분리수거함, 실외기 등 14종이 겹치지 않아 개별 실루엣을 추출할 수 있다. · ENV_City_Bench_02, ENV_City_UtilityBox_02 |
| [PARENTS_HOME_MASTER](../ArtSource/Higgsfield/Selected/Master12/PARENTS_HOME_MASTER.png) · b | 74773a71-13f7-4574-8a18-a26388c2cc5f · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/PARENTS_HOME_MASTER_b.txt) | HOME_INDEPENDENT_MASTER, parents_home, IAN_MASTER_YOUNGADULT | B의 조리대·밥솥·소파·반찬 놓인 식탁에서 오래 산 가족의 집이 느껴진다. 앞문과 식탁 오른쪽 통로가 A보다 또렷하다. · 제작 레퍼런스 |
| [CITY_DOWNTOWN_MASTER](../ArtSource/Higgsfield/Selected/Master12/CITY_DOWNTOWN_MASTER.png) · a | 2263dd08-5483-4cfe-a54f-f2795589c786 · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/CITY_DOWNTOWN_MASTER_a.txt) | city_day, CITY_RESIDENTIAL_MASTER, IAN_MASTER_YOUNGADULT | A는 보행자가 정류장으로 모이고 택시·버스·배달차가 도로 안에서 분리된다. 은행 기호와 유리 외벽의 생활 장면이 도심의 기능을 보여준다. · 제작 레퍼런스 |
| [SCHOOL_MASTER](../ArtSource/Higgsfield/Selected/Master12/SCHOOL_MASTER.png) · b | 4e25f581-e696-4756-9cb2-e016ecaf2bf3 · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/SCHOOL_MASTER_b.txt) | classroom, HOME_INDEPENDENT_MASTER, NPC_ARCHETYPE_MASTER | B는 한 화면 안에서 교실과 열린 복도가 이어진다. 빈 칠판과 밝은 진로 게시판 아래에 엎드린 학생이 있어 수업 분위기가 읽힌다. · 제작 레퍼런스 |
| [WORKSPACE_MASTER](../ArtSource/Higgsfield/Selected/Master12/WORKSPACE_MASTER.png) · b | 3c6ab547-5857-4af2-93fb-62d753aa20e7 · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/WORKSPACE_MASTER_b.txt) | office_day, CITY_DOWNTOWN_MASTER, NPC_ARCHETYPE_MASTER | B에는 유리 회의실, 프린터 대기, 탕비실, 타이핑 중인 직원이 각각 구분된다. 빈 햇살 포스터와 바쁜 책상이 한 동선에서 보인다. · 제작 레퍼런스 |
| [FINANCE_MASTER](../ArtSource/Higgsfield/Selected/Master12/FINANCE_MASTER.png) · b_edit1 | d649c6cc-a1d7-42dd-bf16-b642c49d55b9 · gpt_image_2_6 · [prompt](../ArtSource/Higgsfield/Prompts/Master12/FINANCE_MASTER_b_edit1.txt) | FINANCE_MASTER_b | 거리 비중이 컸던 B를 상담실 중심으로 교정했다. 입구·대기석·상담 창구·쌓인 서류가 한눈에 들어오고 차트에 읽을 문구가 없다. · 제작 레퍼런스 |

### Master12 후속 변형

18장 선정. 결과 파일 33건, 결과 미확인 요청 4건. [변형 기록](../ArtSource/Higgsfield/Selected/Master12/Variations/manifest.json) · [건축 표식 대조](../ArtSource/Higgsfield/Selected/Master12/Review/Location_Alignment.json) · [생활 장면 보드](../ArtSource/Higgsfield/Selected/Master12/Review/Black_Comedy.jpg)

| 변형 | 선정 이미지 | 생성 ID · 입력 장소 |
|---|---|---|
| RESIDENTIAL_MORNING | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/RESIDENTIAL_MORNING.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/RESIDENTIAL_MORNING_b.txt) | b41fbb4d-076e-404d-ad6a-2c7a5dd2426d · CITY_RESIDENTIAL_MASTER |
| RESIDENTIAL_EVENING | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/RESIDENTIAL_EVENING.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/RESIDENTIAL_EVENING_reference_edit.txt) | f7cd3baa-0e13-49e9-9761-48584adf263c · CITY_RESIDENTIAL_MASTER |
| RESIDENTIAL_NIGHT | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/RESIDENTIAL_NIGHT.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/RESIDENTIAL_NIGHT_b.txt) | 3cb19dbb-eba1-4ac9-84ca-b457edfbc99f · CITY_RESIDENTIAL_MASTER |
| RESIDENTIAL_RAIN | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/RESIDENTIAL_RAIN.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/RESIDENTIAL_RAIN_b.txt) | 06297ecb-4bb5-470a-a595-5ca44d808eec · CITY_RESIDENTIAL_MASTER |
| DOWNTOWN_MORNING_RUSH | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/DOWNTOWN_MORNING_RUSH.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/DOWNTOWN_MORNING_RUSH_b.txt) | 66db6554-2947-4a75-96c1-e9323648ea1f · CITY_DOWNTOWN_MASTER |
| DOWNTOWN_LUNCH | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/DOWNTOWN_LUNCH.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/DOWNTOWN_LUNCH_b.txt) | 71b5c704-28b2-4a44-b40c-8220261343d0 · CITY_DOWNTOWN_MASTER |
| DOWNTOWN_EVENING_RUSH | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/DOWNTOWN_EVENING_RUSH.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/DOWNTOWN_EVENING_RUSH_b.txt) | 8030c0e8-acbb-4b0f-a912-7e57a5d64949 · CITY_DOWNTOWN_MASTER |
| DOWNTOWN_NIGHT | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/DOWNTOWN_NIGHT.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/DOWNTOWN_NIGHT_b.txt) | 3b5d5b04-3ce0-4ef0-998e-b005590ae192 · CITY_DOWNTOWN_MASTER |
| DOWNTOWN_RAIN | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/DOWNTOWN_RAIN.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/DOWNTOWN_RAIN_a.txt) | 332e9f61-172c-4f02-8af0-a3f92fbd838d · CITY_DOWNTOWN_MASTER |
| OFFICE_MORNING | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/OFFICE_MORNING.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/OFFICE_MORNING_a.txt) | f7e7c6f7-a66f-4f54-9926-66f1e9701a92 · WORKSPACE_MASTER |
| OFFICE_OVERTIME | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/OFFICE_OVERTIME.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/OFFICE_OVERTIME_b.txt) | 5989213b-92d0-40d7-a12b-50e73a5fbca2 · WORKSPACE_MASTER |
| OFFICE_LATE_EMPTY | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/OFFICE_LATE_EMPTY.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/OFFICE_LATE_EMPTY_b.txt) | 7aa6bd49-d905-438f-a5f1-63dd2b6bc157 · WORKSPACE_MASTER |
| SCHOOL_MORNING | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/SCHOOL_MORNING.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/SCHOOL_MORNING_a.txt) | 25478647-fca8-4211-acfb-75d2c2d00fbc · SCHOOL_MASTER |
| SCHOOL_LUNCH | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/SCHOOL_LUNCH.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/SCHOOL_LUNCH_b.txt) | ee765e2b-799a-4972-a0da-00aa133ced3d · SCHOOL_MASTER |
| SCHOOL_AFTER_CLASS | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/SCHOOL_AFTER_CLASS.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/SCHOOL_AFTER_CLASS_a.txt) | 66a26f89-7df1-4ea2-985a-3da2d3036040 · SCHOOL_MASTER |
| HOME_BILLS | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/HOME_BILLS.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/HOME_BILLS_b.txt) | 7e8489ba-c345-41d9-85f8-a0124eeb77c7 · HOME_INDEPENDENT_MASTER |
| BANK_PAPERWORK | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/BANK_PAPERWORK.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/BANK_PAPERWORK_b.txt) | ae55581d-3458-40a0-a39d-99fbff458fda · FINANCE_MASTER |
| SHOP_2AM | [선정본](../ArtSource/Higgsfield/Selected/Master12/Variations/SHOP_2AM.png) · [prompt](../ArtSource/Higgsfield/Prompts/Master12/SHOP_2AM_a.txt) | ba99b2a9-2640-4017-bf66-f56cfdaed6f4 · SHOP_CONVENIENCE_MASTER |

## 휴게 공간과 생활 도구

2026-11-19에 기존 마스터를 참조해 GPT Image 2.6으로 6건을 생성했다. 공간 5장은 통로, 착석면, 직원·손님 동선 및 집기 배치의 기준으로 선정했다. 도구 시트에서는 개별 도구를 분리하고 픽셀 경계와 투명도를 정리해 NPC 손에 표시한다.

| 선정본 | 참조 및 적용 | Generation ID | 생성 기록 |
| --- | --- | --- | --- |
| [Office](../ArtSource/Higgsfield/Selected/RestAreas/Office.png) | WORKSPACE_MASTER · 서쪽 탕비실과 소파 | `b969a889-5466-492a-8b01-9a11563beb49` | [프롬프트](../ArtSource/Higgsfield/Raw/RestAreas/Office.prompt.txt) · [생성 원본](https://d8j0ntlcm91z4.cloudfront.net/user_3MGx08m3U7arfm3zQaLFfUL3gdY/hf_20261119_133758_b969a889-5466-492a-8b01-9a11563beb49.png) |
| [School](../ArtSource/Higgsfield/Selected/RestAreas/School.png) | SCHOOL_MASTER · 교실 측면 벤치 | `c6bcf8df-78f3-48b7-9353-51fdd75854ee` | [프롬프트](../ArtSource/Higgsfield/Raw/RestAreas/School.prompt.txt) · [생성 원본](https://d8j0ntlcm91z4.cloudfront.net/user_3MGx08m3U7arfm3zQaLFfUL3gdY/hf_20261119_133800_c6bcf8df-78f3-48b7-9353-51fdd75854ee.png) |
| [Finance](../ArtSource/Higgsfield/Selected/RestAreas/Finance.png) | FINANCE_MASTER · 상담 공간 뒤 직원 소파 | `3a7f2e9a-2fc2-4fcf-a47c-856b0c864f74` | [프롬프트](../ArtSource/Higgsfield/Raw/RestAreas/Finance.prompt.txt) · [생성 원본](https://d8j0ntlcm91z4.cloudfront.net/user_3MGx08m3U7arfm3zQaLFfUL3gdY/hf_20261119_133835_3a7f2e9a-2fc2-4fcf-a47c-856b0c864f74.png) |
| [Store](../ArtSource/Higgsfield/Selected/RestAreas/Store.png) | SHOP_CONVENIENCE_MASTER · 창고 쪽 직원석 | `8e2ebe37-0b9f-4aa4-af7e-78c5cd487646` | [프롬프트](../ArtSource/Higgsfield/Raw/RestAreas/Store.prompt.txt) · [생성 원본](https://d8j0ntlcm91z4.cloudfront.net/user_3MGx08m3U7arfm3zQaLFfUL3gdY/hf_20261119_133842_8e2ebe37-0b9f-4aa4-af7e-78c5cd487646.png) |
| [Cafe](../ArtSource/Higgsfield/Selected/RestAreas/Cafe.png) | SHOP_CONVENIENCE_MASTER · 같은 건물 내 카페 직원석 | `a2bd35d9-ac18-4ee3-91c6-6f8f7a0e36d2` | [프롬프트](../ArtSource/Higgsfield/Raw/RestAreas/Cafe.prompt.txt) · [생성 원본](https://d8j0ntlcm91z4.cloudfront.net/user_3MGx08m3U7arfm3zQaLFfUL3gdY/hf_20261119_133922_a2bd35d9-ac18-4ee3-91c6-6f8f7a0e36d2.png) |
| [Chores](../ArtSource/Higgsfield/Selected/RestAreas/Chores.png) | PARENTS_HOME_MASTER · HouseholdTools.png | `d873e479-6417-40cc-b824-a16aa44fdda7` | [프롬프트](../ArtSource/Higgsfield/Raw/RestAreas/Chores.prompt.txt) · [생성 원본](https://d8j0ntlcm91z4.cloudfront.net/user_3MGx08m3U7arfm3zQaLFfUL3gdY/hf_20261119_133938_d873e479-6417-40cc-b824-a16aa44fdda7.png) |
