namespace IanGame
{
    public static class ItemAchievementDefinitions
    {
        public static readonly CityAchievement[] All =
        {
            new CityAchievement
            {
                id = "item/pull",
                title = "첫 뽑기",
                description = "실제 재화 또는 가챠권으로 뽑기",
                minAge = 13,
            },
            new CityAchievement
            {
                id = "item/headphone",
                title = "나만의 소음 차단",
                description = "첫 헤드폰 획득",
                minAge = 13,
            },
            new CityAchievement
            {
                id = "item/equipment",
                title = "인생 보조 장치",
                description = "첫 장비 획득",
                minAge = 13,
            },
            new CityAchievement
            {
                id = "item/equip-headphone",
                title = "착용 완료",
                description = "헤드폰 슬롯에 실제 장착",
                minAge = 13,
            },
            new CityAchievement
            {
                id = "item/duplicate",
                title = "이미 있는 행복",
                description = "중복 아이템 획득",
                minAge = 13,
            },
            new CityAchievement
            {
                id = "item/dismantle",
                title = "재활용하는 인생",
                description = "중복을 수집 조각으로 분해",
                minAge = 13,
            },
            new CityAchievement
            {
                id = "item/collection10",
                title = "열 가지 위안",
                description = "서로 다른 수집 아이템 10종 발견",
                minAge = 13,
                target = 10,
            },
            new CityAchievement
            {
                id = "item/headphones5",
                title = "세상 음소거",
                description = "헤드폰 5종 발견",
                minAge = 13,
                target = 5,
            },
            new CityAchievement
            {
                id = "item/pull50",
                title = "오십 번의 기대",
                description = "누적 50회 뽑기",
                minAge = 13,
                target = 50,
            },
            new CityAchievement
            {
                id = "item/pull100",
                title = "백 번의 희망",
                description = "누적 100회 뽑기",
                minAge = 13,
                target = 100,
            },
            new CityAchievement
            {
                id = "item/pull300",
                title = "천장에 닿은 인생",
                description = "누적 300회 뽑기",
                minAge = 13,
                target = 300,
            },
        };
    }
}
