using System;
using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public class FurnitureDefinition
    {
        public string id;
        public string title;
        public string description;
        public int price;
        public bool starter;
        public WorldInteractionKind interaction = WorldInteractionKind.Rest;
        public Sprite preview;
        public Vector2 position;
    }

    [Serializable]
    public class ResidenceDefinition
    {
        public string id;
        public string title;
        public string description;
        public int movingCost;
        public int minAge;
        public Sprite preview;
        public float furnishingScale = 1;
    }

    [CreateAssetMenu(menuName = "Ian/Housing catalog")]
    public class HousingCatalog : ScriptableObject
    {
        public FurnitureDefinition[] furniture = new FurnitureDefinition[0];
        public ResidenceDefinition[] residences = new ResidenceDefinition[0];
        public Sprite bathroom;
        public HomeLayoutPlan placementPlan;

        public FurnitureDefinition Furniture(string id) => Array.Find(furniture, f => f.id == id);

        public ResidenceDefinition Residence(string id) => Array.Find(residences, r => r.id == id);
    }

    [Serializable]
    public class FurniturePlacement
    {
        public string id;
        public Vector2 position;
        public string homeId;
        public string roomId;
        public string ownership;
        public int direction;
        public bool installed = true;
    }

    [Serializable]
    public class HousingSnapshot
    {
        public int schema = 1;
        public string residenceId = "starter";
        public string wallpaper = "warm-white";
        public string floor = "wood-linoleum";
        public string bathroom = "jade-tile";
        public List<string> owned = new List<string>();
        public List<FurniturePlacement> placed = new List<FurniturePlacement>();

        // Zero identifies legacy homes; their existing furnishings remain available.
        public int starterRevision;
        public int layoutVersion;
        public int layoutAdjusted;
        public string layoutResidenceId;
    }
}
