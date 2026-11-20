namespace IanGame
{
    public static class NPCActivityLocation
    {
        public static bool RequiresAnchor(NPCDailyAction action)
        {
            return action
                is NPCDailyAction.Rest
                    or NPCDailyAction.Wake
                    or NPCDailyAction.Work
                    or NPCDailyAction.Study
                    or NPCDailyAction.Eat
                    or NPCDailyAction.Sleep
                    or NPCDailyAction.Shop
                    or NPCDailyAction.GetIngredients
                    or NPCDailyAction.Cook
                    or NPCDailyAction.ServeMeal
                    or NPCDailyAction.WashDishes
                    or NPCDailyAction.WatchTV
                    or NPCDailyAction.Wash
                    or NPCDailyAction.Clean
                    or NPCDailyAction.StockShelf;
        }

        public static bool Allows(WorldNPCDefinition resident, NPCScheduleEntry entry)
        {
            if (resident == null || entry == null || string.IsNullOrEmpty(entry.zone))
            {
                return false;
            }

            bool atHome = entry.zone == resident.homeZone;
            bool familyRoom = FamilyLife.Parent(resident.npcId) && entry.zone.StartsWith("family-");
            if (RequiresAnchor(entry.action) && string.IsNullOrEmpty(entry.objectId))
            {
                return resident.ambient && atHome && entry.action == NPCDailyAction.Sleep;
            }

            return entry.action switch
            {
                NPCDailyAction.Work => (atHome || familyRoom)
                    ? resident.worksFromHome
                    : entry.zone == resident.workZone,
                NPCDailyAction.Study => entry.zone == resident.workZone && resident.jobId == "student",
                NPCDailyAction.Sleep or NPCDailyAction.Wake => atHome,
                NPCDailyAction.StockShelf => resident.workZone == "shop" && entry.zone == resident.workZone,
                NPCDailyAction.Shop => entry.zone == "shop",
                NPCDailyAction.GetIngredients
                or NPCDailyAction.Cook
                or NPCDailyAction.ServeMeal
                or NPCDailyAction.WashDishes => familyRoom && entry.zone is "family-kitchen" or "family-home",
                NPCDailyAction.Clean => familyRoom
                    && entry.zone is "family-living" or "family-home" or "family-ian-bedroom" or "family-kitchen",
                NPCDailyAction.WatchTV => familyRoom && entry.zone is "family-living" or "family-home",
                NPCDailyAction.Wash => entry.zone == "family-bathroom",
                _ => true,
            };
        }

        public static bool Matches(NPCDailyAction action, WorldInteractable anchor)
        {
            return action switch
            {
                NPCDailyAction.Work => anchor.kind == WorldInteractionKind.Work,
                NPCDailyAction.Study => anchor.kind == WorldInteractionKind.Study
                    || anchor.id.StartsWith("school-seat-"),
                NPCDailyAction.Sleep or NPCDailyAction.Wake => anchor.kind == WorldInteractionKind.Sleep,
                NPCDailyAction.Eat => anchor.kind == WorldInteractionKind.Meal,
                NPCDailyAction.StockShelf => anchor.id.StartsWith("shop-browse-"),
                NPCDailyAction.Shop => anchor.kind == WorldInteractionKind.FoodShop
                    || anchor.id.StartsWith("shop-browse-"),
                NPCDailyAction.GetIngredients => anchor.kind == WorldInteractionKind.Fridge,
                NPCDailyAction.Cook => anchor.id == "family-cook",
                NPCDailyAction.ServeMeal => anchor.id == "family-serve",
                NPCDailyAction.WashDishes => anchor.id == "family-dishes"
                    && anchor.TryGetComponent<ResidentActivityStation>(out var sink)
                    && sink.HasWorkRoute,
                NPCDailyAction.Clean => anchor.TryGetComponent<ResidentActivityStation>(out var task)
                    && task.HasWorkRoute,
                NPCDailyAction.Rest => anchor.TryGetComponent<ResidentActivityStation>(out var station)
                    && station.HasSeat,
                NPCDailyAction.WatchTV => anchor.id.StartsWith("family-tv-"),
                NPCDailyAction.Wash => anchor.kind == WorldInteractionKind.Wash,
                _ => true,
            };
        }
    }
}
