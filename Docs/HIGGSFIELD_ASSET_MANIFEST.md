# Higgsfield asset manifest

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
