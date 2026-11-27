# 이안의 공식 성장 외형

[사용자 원본](../Assets/_Game/Art/Characters/Ian/Reference/Ian_OfficialGrowth.png)은 투명 RGBA 1402 × 1122 PNG다. SHA-256은 `9bd45a29f2090db99e1de519b70e750a76761f8308ceb33697e83b9ec916d18e`다. 같은 원본을 다시 그리거나 리샘플링하는 과정 없이 정면 20장을 잘랐다. 각 PNG를 다시 읽어 원본의 해당 사각형과 RGBA 바이트가 일치하는지 검사했다.

| 단계 · 실제 파일 | 행 · 열 | 적용 나이 | 원화 너비 × 높이 (px) | 하단 기준 피벗 (px) | 프레임 월드 높이 |
|---|---|---:|---:|---:|---:|
| [S01](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S01.png) | 1 · 1 | 0 | 108 × 143 | 7 | 0.8938 |
| [S02](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S02.png) | 1 · 2 | 1–2 | 127 × 173 | 7 | 1.0813 |
| [S03](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S03.png) | 1 · 3 | 3–4 | 136 × 199 | 5 | 1.2438 |
| [S04](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S04.png) | 1 · 4 | 5–6 | 136 × 215 | 5 | 1.3438 |
| [S05](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S05.png) | 1 · 5 | 7–8 | 139 × 231 | 5 | 1.4438 |
| [S06](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S06.png) | 2 · 1 | 9–10 | 140 × 232 | 5 | 1.4500 |
| [S07](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S07.png) | 2 · 2 | 11–12 | 139 × 234 | 5 | 1.4625 |
| [S08](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S08.png) | 2 · 3 | 13–14 | 142 × 246 | 5 | 1.5375 |
| [S09](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S09.png) | 2 · 4 | 15–16 | 144 × 247 | 5 | 1.5438 |
| [S10](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S10.png) | 2 · 5 | 17–18 | 148 × 254 | 5 | 1.5875 |
| [S11](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S11.png) | 3 · 1 | 19–21 | 151 × 246 | 5 | 1.5375 |
| [S12](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S12.png) | 3 · 2 | 22–25 | 144 × 247 | 5 | 1.5438 |
| [S13](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S13.png) | 3 · 3 | 26–29 | 148 × 255 | 5 | 1.5938 |
| [S14](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S14.png) | 3 · 4 | 30–34 | 145 × 261 | 5 | 1.6313 |
| [S15](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S15.png) | 3 · 5 | 35–39 | 150 × 265 | 5 | 1.6562 |
| [S16](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S16.png) | 4 · 1 | 40–44 | 161 × 287 | 5 | 1.7938 |
| [S17](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S17.png) | 4 · 2 | 45–49 | 159 × 287 | 5 | 1.7938 |
| [S18](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S18.png) | 4 · 3 | 50–54 | 168 × 292 | 5 | 1.8250 |
| [S19](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S19.png) | 4 · 4 | 55–64 | 159 × 294 | 5 | 1.8375 |
| [S20](../Assets/_Game/Art/Characters/Ian/Growth/Official/Ian_S20.png) | 4 · 5 | 65+ | 168 × 297 | 5 | 1.8563 |

원본은 행 간격이 균일하지 않다. 실제 투명 구간을 행 경계로 쓰고 각 인물에 5px 여백을 포함했다. 발 기준은 S03부터 적용하고, 앉아 있는 S01·S02는 중앙 접촉점으로 맞췄다. 전 단계에 128 PPU와 0.8의 균등 월드 배율을 사용한다. 표의 높이는 투명 여백을 포함한다. S10에서 S11로 넘어갈 때 보이는 작은 신장 차이도 원화에 있다.

커트 좌표와 나이 하한은 [분리 명세](../Assets/_Game/Art/Characters/Ian/Growth/Official/GrowthStages.json)에 있다. 게임 규칙상 노년은 60세부터로, 60–64세는 S19, 65세부터는 S20을 표시한다. S20은 사용자가 제공한 후기 성인 그림이며 뚜렷한 노화 표현을 추가하려면 공식 원화가 필요하다.
