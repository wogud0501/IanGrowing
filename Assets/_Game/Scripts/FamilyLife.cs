using System;
using UnityEngine;
namespace IanGame {
public enum HomeResidency {FamilyHome,IndependentHome}
[Serializable]public sealed class FamilyLifeSnapshot {
 public int schema;public HomeResidency residency;public bool independenceChosen;
 public int meals=3,pantry=12,motherRelationship=50,fatherRelationship=50;
 public long servedKey=-1,cookedKey=-1;public string ingredientCarrier="",cook="";
 public System.Collections.Generic.List<FamilyReceipt> receipts=new System.Collections.Generic.List<FamilyReceipt>();public int preparedMeals,housework;public string announcement="아침 식사가 준비되어 있습니다.";
}
[Serializable]public sealed class FamilyReceipt {public string parent,target;public long key;}
// State belongs to the existing LifeWorld snapshot; money and owned furniture stay in EconomySystem.
public static class FamilyLife {
 public const string Home="family-home",Bath="family-bathroom";
 public static bool IsFamilyRoom(string id)=>id==Home||id==Bath||id=="family-living"||id=="family-kitchen"||id=="family-parents-bedroom"||id=="family-ian-bedroom";
 public static bool IsNursery(string id)=>id==Home||id=="family-ian-bedroom";
 public static FamilyLifeSnapshot State(GameManager g)=>g?.world?.State?.family;
 public static bool AtFamily(GameManager g)=>State(g)?.residency==HomeResidency.FamilyHome;
 public static string DefaultHome(GameManager g)=>AtFamily(g)?Home:"home";
 public static bool Parent(string id)=>id=="family-mother"||id=="family-father";
 public static void Migrate(GameManager g,LifeWorldSnapshot world){
  if(world.family==null)world.family=new FamilyLifeSnapshot();var f=world.family;if(f.schema>0){if(g.growth.Age<19){f.residency=HomeResidency.FamilyHome;if(world.zone=="home"||world.zone=="bathroom"){world.zone=Home;world.position=new Vector2(0,3.4f);}}return;}
  // Existing adult personal homes are never reclassified, sold, or emptied.
  bool independent=g.growth.Age>=19;f.schema=1;f.residency=independent?HomeResidency.IndependentHome:HomeResidency.FamilyHome;f.independenceChosen=independent;
  if(!independent&&(world.zone=="home"||world.zone=="bathroom")){world.zone=Home;world.position=new Vector2(-5,-.7f);}
 }
 public static bool Valid(FamilyLifeSnapshot f)=>f==null||f.schema==0||f.schema==1&&Enum.IsDefined(typeof(HomeResidency),f.residency)&&f.meals>=0&&f.meals<=9&&f.pantry>=0&&f.pantry<=999&&f.preparedMeals>=0&&f.preparedMeals<=3&&f.motherRelationship>=0&&f.motherRelationship<=100&&f.fatherRelationship>=0&&f.fatherRelationship<=100&&f.housework>=0&&(f.receipts==null||f.receipts.Count<=64&&f.receipts.TrueForAll(r=>r!=null&&!string.IsNullOrEmpty(r.parent)&&!string.IsNullOrEmpty(r.target)&&r.key>=0));
 public static bool Done(GameManager g,string parent,string target,long key)=>State(g)?.receipts?.Exists(r=>r.parent==parent&&r.target==target&&r.key==key)??false;
 public static void Use(GameManager g,string parent,WorldInteractable target,long key){if(!Parent(parent)||target==null||Done(g,parent,target.id,key))return;if(g.world.Host?.GetComponent<FamilyHomeRuntime>()?.Queue(parent,target,key)==true)return;ApplyUse(g,parent,target,key);}
 public static void ApplyUse(GameManager g,string parent,WorldInteractable target,long key){
  if(!Parent(parent)||target==null||!IsFamilyRoom(target.zone)&&target.id!="counter")return;ApplyLogicalUse(g,parent,target.id,key);
 }
 public static void ApplyLogicalUse(GameManager g,string parent,string targetId,long key){if(!Parent(parent)||string.IsNullOrEmpty(targetId))return;var f=State(g);if(f==null||Done(g,parent,targetId,key))return;if(f.receipts==null)f.receipts=new System.Collections.Generic.List<FamilyReceipt>();f.receipts.RemoveAll(r=>r.parent==parent&&r.target==targetId);f.receipts.Add(new FamilyReceipt{parent=parent,target=targetId,key=key});
  if(targetId=="counter"){f.pantry=Math.Min(999,f.pantry+6);return;}
  if(targetId=="family-fridge"&&f.pantry>0&&string.IsNullOrEmpty(f.ingredientCarrier)){f.pantry--;f.ingredientCarrier=parent;}
  if(targetId=="family-cook"&&f.ingredientCarrier==parent&&f.cookedKey!=key){f.cookedKey=key;f.ingredientCarrier="";f.cook=parent;f.preparedMeals=3;}
  if(targetId=="family-serve"&&f.preparedMeals>0&&f.servedKey!=key){f.meals=Math.Min(9,f.meals+f.preparedMeals);f.preparedMeals=0;f.servedKey=key;f.announcement="식사 준비가 끝났어요. 함께 먹을까요?";g.world.Host?.hud.Notify(f.announcement);}
  if(targetId.StartsWith("family-dine-")&&f.meals>0)f.meals--;
  if(targetId=="family-dishes"||targetId=="family-clean")f.housework=Math.Min(100000,f.housework+1);
 }
 public static bool Care(GameManager g,string parent,int action){if(!Parent(parent)||g.growth.Age>=7||!GrowthAccess.Care(action))return false;return true;}
}
}
