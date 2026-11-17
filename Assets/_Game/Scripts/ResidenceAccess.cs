using UnityEngine;

namespace IanGame
{
    // Derived from existing residency and economy state; no second ownership save.
    public readonly struct ResidenceAccess
    {
        public readonly string HousingId;
        public readonly string OwnerId;
        public readonly string EntranceId;
        public readonly string InteriorSceneId;
        public string CurrentResidence => HousingId;

        public readonly bool IsUnlocked;
        public readonly bool IsOwned;
        public readonly bool IsActiveResidence;

        public ResidenceAccess(GameManager game)
        {
            bool personal = !FamilyLife.AtFamily(game);
            HousingId = personal ? game.economy.Housing.residenceId : "family";
            OwnerId = personal ? "player" : "household";
            IsUnlocked = GrowthAccess.Adult(game);
            IsOwned = IsUnlocked && personal;
            IsActiveResidence = IsOwned;
            EntranceId = IsOwned ? "house-entrance" : "family-front";
            InteriorSceneId = IsOwned ? "home" : "family-living";
        }
    }
}
