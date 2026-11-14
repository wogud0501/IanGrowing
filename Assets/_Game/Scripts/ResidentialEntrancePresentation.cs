using UnityEngine;
namespace IanGame {
// The ordinary neighbourhood remains visible. Only the player's unowned door and
// personal-house sign are absent before the existing paid independence transaction.
public sealed class ResidentialEntrancePresentation:MonoBehaviour {
 public GameObject[] personalEntranceObjects;
 LifeWorldController world;
 void Start(){world=GetComponent<LifeWorldController>();world.Service.Game.economy.Changed+=Apply;world.Service.Game.growth.AgeChanged+=Age;Apply();}
 void Age(int age,LifeStage stage)=>Apply();
 void Apply(){if(world?.Service==null)return;bool active=new ResidenceAccess(world.Service.Game).IsActiveResidence;foreach(var o in personalEntranceObjects)if(o!=null)o.SetActive(active);}
 void OnDestroy(){if(world?.Service==null)return;world.Service.Game.economy.Changed-=Apply;world.Service.Game.growth.AgeChanged-=Age;}
}
}
