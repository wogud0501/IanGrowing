using System;
using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public class LifeWorldSnapshot
    {
        public int schema = 1;
        public string zone = "home";
        public Vector2 position = new Vector2(0, -1.5f);
        public Vector2 facing = Vector2.down;
        public float fatigue = 15;
        public float hygiene = 85;
        public int rice = 3;
        public int bread = 2;
        public string prepared = "";
        public string pendingAction = "";
        public int lastWorkDay = -1;
        public int lastSummaryDay = 1;
        public List<NPCResidentSnapshot> residents = new List<NPCResidentSnapshot>();
        public CityLifeProgress cityLife;
        public FamilyLifeSnapshot family;
        public long dailyIncome;
        public string lastSummary = "";
    }

    [Serializable]
    public class WorldZoneDefinition
    {
        public string id;
        public string title;
        public SceneId location;
        public Vector2 origin;
        public Vector2 size = new Vector2(18, 12);
        public Vector2 spawn = new Vector2(0, -1.5f);
        public bool outdoor;
        public float walkableTop = 1000;
        public float cameraSize;
    }

    public enum NPCDailyAction
    {
        Idle,
        Wake,
        Walk,
        Work,
        Study,
        Eat,
        Shop,
        Rest,
        Talk,
        Sleep,
    }

    public enum NPCPersonality
    {
        EarlyBird,
        NightWorker,
        Student,
        OfficeWorker,
        ShopWorker,
        Homebody,
        WeekendVisitor,
    }

    [Serializable]
    public class NPCResidentSnapshot
    {
        public string id;
        public string zone;
        public string destinationZone;
        public string objectId;
        public Vector2 position;
        public Vector2 facing = Vector2.down;
        public NPCDailyAction action;
        public int scheduleIndex;
        public long minute;
        public long completedUseKey = -1;
    }

    [Serializable]
    public class NPCScheduleEntry
    {
        public int hour;
        public int minute;
        public int dayMask = 127;
        public string zone;
        public string objectId;
        public Vector2 position;
        public NPCDailyAction action;
    }

    [Serializable]
    public class WorldNPCDefinition
    {
        public string npcId;
        public string title;
        public string homeZone = "residence";
        public string jobId;
        public string workZone;
        public string companionId;
        public bool hasHomePosition;
        public Vector2 homePosition;
        public NPCPersonality personality;
        public float speed = 1.3f;
        public ActorAnimationSet animations;
        public Sprite[] frames;
        public NPCScheduleEntry[] schedule;
    }

    [CreateAssetMenu(menuName = "Ian/Life World Rules")]
    public class LifeWorldRules : ScriptableObject
    {
        public int neighbourhoodHelpReward = 30;
        public int neighbourhoodHelpMinutes = 30;
        public float moveSpeed = 3.2f;
        public float cameraDamping = .16f;
        public float indoorCameraSize = 5.8f;
        public float outdoorCameraSize = 7.2f;
        public float realSecondsPerGameDay = 1200;
        public float activityAnimationScale = 1;
        public float fatiguePerDay = 40;
        public float hygieneLossPerDay = 15;
        public float interactionRadius = 1.65f;
        public int ricePrice = 35;
        public int breadPrice = 20;
        public int shopOpen = 7;
        public int shopClose = 23;
        public int workOpen = 9;
        public int workLastStart = 10;
        public int shiftHours = 8;
        public WorldZoneDefinition[] zones = new WorldZoneDefinition[0];
        public WorldNPCDefinition[] npcs = new WorldNPCDefinition[0];

        public WorldZoneDefinition Zone(string id) => Array.Find(zones, z => z.id == id);
    }

    public enum WorldInteractionKind
    {
        Door,
        Sleep,
        Fridge,
        Meal,
        Drink,
        Wash,
        Shower,
        Toilet,
        Rest,
        Wardrobe,
        Work,
        Study,
        NPC,
        FoodShop,
        FurnitureShop,
        StockMarket,
        RealEstate,
    }
}
