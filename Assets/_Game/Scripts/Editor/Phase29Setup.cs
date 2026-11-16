#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace IanGame.Editor {
public static class Phase29Setup {
 [MenuItem("Ian/Phase 2.9/Bind existing playable scenes")]
 public static void Bind(){if(Application.isPlaying)throw new Exception("Stop Play first");var data=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Resources/IanCollection.asset");if(data==null)throw new Exception("Create catalog first");var boot=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>();if(manager.items==null)manager.items=manager.GetComponent<ItemService>()??manager.gameObject.AddComponent<ItemService>();manager.items.catalog=data;UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(boot);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(boot);
 var rooms=AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>("Assets/_Game/Data/SceneArchitecture/RoomScenes.asset");foreach(var room in rooms.rooms){var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/"+room.sceneName+".unity");var w=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();var ui=w.hud.GetComponent<CollectionUI>()??w.hud.gameObject.AddComponent<CollectionUI>();ui.world=w;var appearance=w.Player.GetComponent<ItemAppearanceController>()??w.Player.gameObject.AddComponent<ItemAppearanceController>();appearance.player=w.Player;UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);}AssetDatabase.SaveAssets();File.WriteAllText("Docs/Phase29/SCENE_BINDINGS.txt","Bootstrap: existing GameManager + ItemService/catalog\n"+string.Join("\n",rooms.rooms.Select(r=>r.sceneName+": existing HUD + CollectionUI, player + appearance adapter")));UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/FamilyHome_IanBedroom.unity");
 }
 [MenuItem("Ian/Phase 2.9/Create collection data")]
 public static void BuildCatalog(){if(Application.isPlaying)throw new Exception("Stop Play first");Directory.CreateDirectory("Assets/Resources");const string path="Assets/Resources/IanCollection.asset";if(AssetDatabase.LoadAssetAtPath<ItemCatalog>(path)!=null)throw new Exception("Catalog already exists; edit its data rather than resetting it");var catalog=ScriptableObject.CreateInstance<ItemCatalog>();
 string[] ids={"common","uncommon","rare","epic","legendary"},titles={"일반","고급","희귀","영웅","전설"};int[] salvage={2,5,12,30,100};Color32[] colors={new Color32(172,176,157,255),new Color32(124,166,113,255),new Color32(96,149,177,255),new Color32(162,119,173,255),new Color32(215,165,69,255)};catalog.rarities=ids.Select((id,i)=>new RarityDefinition{id=id,displayName=titles[i],rank=i,color=colors[i],dismantleMultiplier=salvage[i],revealEffectId=id}).ToArray();var list=new List<ItemDefinition>();
 StatModifier M(ModifierTarget stat,float value,bool percent=false)=>new StatModifier{stat=stat,value=value,operation=percent?ModifierOperation.Percent:ModifierOperation.Add};
 void Item(string id,string title,ItemType type,string rarity,string description,params StatModifier[] effects){string icon=type==ItemType.Headphone?"Activity":type==ItemType.Equipment?"Gear":type==ItemType.Drink?"Water":"Life";list.Add(new ItemDefinition{itemId=id,displayName=title,itemType=type,rarityId=rarity,description=description,statModifiers=effects,icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/UI/Pixel/Icon-"+icon+".png"),minAge=type==ItemType.Headphone||type==ItemType.Equipment?13:0,equipmentSlot=type==ItemType.Headphone?EquipmentSlot.Headphone:type==ItemType.Cosmetic?EquipmentSlot.Cosmetic:EquipmentSlot.Equipment,appearanceId=type==ItemType.Headphone?id:type==ItemType.Cosmetic?"ian/official-default":"",isCosmetic=type==ItemType.Cosmetic,isConsumable=type==ItemType.Drink,dismantlable=type!=ItemType.Drink});}
 Item("starter-headphones","스타터 헤드폰",ItemType.Headphone,"common","첫 소음 차단. 기존 미션 지급품을 그대로 이어갑니다.",M(ModifierTarget.Mental,2));
 Item("headphone-wired","유선의 평화",ItemType.Headphone,"common","끊어질 듯 이어지는 평온.",M(ModifierTarget.Mental,3));
 Item("headphone-used","중고의 위로",ItemType.Headphone,"uncommon","사용감은 있지만 위로는 새것입니다.",M(ModifierTarget.Knowledge,2),M(ModifierTarget.Mental,3));
 Item("headphone-focus","집중 모드",ItemType.Headphone,"rare","공부 소음만 골라 지웁니다.",M(ModifierTarget.Mental,4),M(ModifierTarget.StudyEfficiency,8,true));
 Item("headphone-quiet","정적의 퇴근길",ItemType.Headphone,"epic","퇴근은 아직이지만 소음부터 퇴근합니다.",M(ModifierTarget.Mental,6),M(ModifierTarget.WorkEfficiency,3,true));
 Item("headphone-silence","세상 음소거",ItemType.Headphone,"legendary","300회 보장 대상. 현실은 남고 소음만 사라집니다.",M(ModifierTarget.Mental,8),M(ModifierTarget.StudyEfficiency,10,true),M(ModifierTarget.IdleEfficiency,5,true));
 Item("starter-equipment","스타터 장비",ItemType.Equipment,"common","처음 지급받은 작은 생활 보조 장비.",M(ModifierTarget.Health,2));
 Item("desk-charm","책상 부적",ItemType.Equipment,"uncommon","마감 대신 지식을 불러옵니다.",M(ModifierTarget.Knowledge,3));
 Item("work-badge","월급 배지",ItemType.Equipment,"rare","업무 효율이 급여 계산에 반영됩니다.",M(ModifierTarget.WorkEfficiency,5,true));
 Item("overtime-guard","야근 방호구",ItemType.Equipment,"epic","피로를 지우지는 않지만 버틸 힘을 보탭니다.",M(ModifierTarget.Health,4),M(ModifierTarget.Mental,4));
 Item("equipment-redline","마감 생존 키트",ItemType.Equipment,"legendary","전투 공격·방어 및 전투 금화 보상을 보조합니다.",M(ModifierTarget.CombatAttack,4),M(ModifierTarget.CombatDefense,2),M(ModifierTarget.RewardEfficiency,5,true));
 Item("drink-water","생수 캔",ItemType.Drink,"common","수분 +25. 마신 뒤에도 도감은 남습니다.");list.Last().recovery=new StatDelta{thirst=25};
 Item("drink-tea","따뜻한 차",ItemType.Drink,"uncommon","수분 +20 / 멘탈 +10 / 행복 +5.");list.Last().recovery=new StatDelta{thirst=20,mental=10,happiness=5};
 Item("drink-coffee","퇴근 전 커피",ItemType.Drink,"rare","수분 +15 / 체력 +8. 야근을 권장하지 않습니다.");list.Last().recovery=new StatDelta{thirst=15,health=8};
 Item("costume-daily","이안의 기본 복장",ItemType.Cosmetic,"rare","공식 기본 의상 선택권. 원본 외형을 유지합니다. 신규 의상 원화는 포함하지 않습니다.");
 var drawItems=list.Select(i=>new PoolItem{itemId=i.itemId,weight=i.itemId=="drink-water"?3:1}).ToArray();
 Item("gacha-ticket","가챠권",ItemType.Ticket,"common","미션 보상으로 받는 1회권. 현금보다 먼저 사용합니다.");list.Last().dismantlable=false;list.Last().maxStack=1000000;
 Item("collection-scrap","수집 조각",ItemType.Material,"common","중복 분해 재료. 30개로 1회 뽑기.");list.Last().dismantlable=false;list.Last().maxStack=1000000;
 catalog.items=list.ToArray();catalog.pools=new[]{new GachaPoolDefinition{poolId="life-collection",displayName="인생의 작은 위안",description="헤드폰 · 장비 · 음료 · 기본 복장",availableItems=drawItems,rarityWeights=ids.Select((id,i)=>new RarityWeight{rarityId=id,weight=new[]{6000,2500,1000,400,100}[i]}).ToArray(),pityRules=new[]{new PityRule{id="epic50",threshold=50,minimumRarityId="epic",priority=50,reset=PityReset.Threshold},new PityRule{id="legend100",threshold=100,minimumRarityId="legendary",priority=100,reset=PityReset.Threshold},new PityRule{id="headphone300",threshold=300,guaranteedItem="headphone-silence",priority=300,reset=PityReset.Threshold}}}};
 string error=catalog.Validate();if(error!=null)throw new Exception(error);AssetDatabase.CreateAsset(catalog,path);AssetDatabase.SaveAssets();Directory.CreateDirectory("Docs/Phase29");File.WriteAllText("Docs/Phase29/CATALOG_CREATED.txt",$"{DateTime.UtcNow:O}\nItems {list.Count}\nCollectible {drawItems.Length}\nFive rarity probabilities 60/25/10/4/1%\nCash100 / scrap30 / ticket-first\nPity50 epic+ /100 legendary /300 headphone-silence; independent repeated counters.\n");
 }
}
}
#endif
