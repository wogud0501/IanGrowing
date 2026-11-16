using System;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
[Serializable]public class LifeWorldSnapshot {
 public int schema=1;public string zone="home";public Vector2 position=new Vector2(0,-1.5f);public Vector2 facing=Vector2.down;
 public float fatigue=15,hygiene=85;public int rice=3,bread=2;public string prepared="",pendingAction="";public int lastWorkDay=-1,lastSummaryDay=1;
 public List<NPCResidentSnapshot> residents=new List<NPCResidentSnapshot>();
 public CityLifeProgress cityLife;public FamilyLifeSnapshot family;public long dailyIncome;public string lastSummary="";
}
[Serializable]public class WorldZoneDefinition {
 public string id,title;public SceneId location;public Vector2 origin,size=new Vector2(18,12),spawn=new Vector2(0,-1.5f);public bool outdoor;public float walkableTop=1000,cameraSize;
}
public enum NPCDailyAction {Idle,Wake,Walk,Work,Study,Eat,Shop,Rest,Talk,Sleep}
public enum NPCPersonality {EarlyBird,NightWorker,Student,OfficeWorker,ShopWorker,Homebody,WeekendVisitor}
[Serializable]public class NPCResidentSnapshot {public string id,zone,destinationZone,objectId;public Vector2 position,facing=Vector2.down;public NPCDailyAction action;public int scheduleIndex;public long minute,completedUseKey=-1;}
[Serializable]public class NPCScheduleEntry {public int hour,minute;public int dayMask=127;public string zone,objectId;public Vector2 position;public NPCDailyAction action;}
[Serializable]public class WorldNPCDefinition {public string npcId,title,homeZone="residence",jobId,workZone,companionId;public bool hasHomePosition;public Vector2 homePosition;public NPCPersonality personality;public float speed=1.3f;public ActorAnimationSet animations;public Sprite[] frames;public NPCScheduleEntry[] schedule;}
[CreateAssetMenu(menuName="Ian/Life World Rules")]
public class LifeWorldRules:ScriptableObject {
 public int neighbourhoodHelpReward=30,neighbourhoodHelpMinutes=30;
 public float moveSpeed=3.2f,cameraDamping=.16f,indoorCameraSize=5.8f,outdoorCameraSize=7.2f,realSecondsPerGameDay=1200,activityAnimationScale=1;
 public float fatiguePerDay=40,hygieneLossPerDay=15,interactionRadius=1.65f;public int ricePrice=35,breadPrice=20,shopOpen=7,shopClose=23,workOpen=9,workLastStart=10,shiftHours=8;
 public WorldZoneDefinition[] zones=new WorldZoneDefinition[0];public WorldNPCDefinition[] npcs=new WorldNPCDefinition[0];
 public WorldZoneDefinition Zone(string id)=>Array.Find(zones,z=>z.id==id);
}
public enum WorldInteractionKind {Door,Sleep,Fridge,Meal,Drink,Wash,Shower,Toilet,Rest,Wardrobe,Work,Study,NPC,FoodShop,FurnitureShop,StockMarket,RealEstate}
}
