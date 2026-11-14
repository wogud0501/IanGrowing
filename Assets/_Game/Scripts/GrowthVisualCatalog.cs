using System;
using UnityEngine;
namespace IanGame {
public enum VisualGrowthStage {Newborn,Toddler,Preschool,EarlyChild,SchoolChild,Preteen,Teen,LateTeen,Adult}
[Serializable] public sealed class GrowthVisualProfile {
 public VisualGrowthStage stage; public string id; public int minAge,maxAge;
 public ActorAnimationSet animations; public float height,colliderRadius,speedFactor=1,stride=1;
 public Vector2 colliderOffset,foot,hand,eye,seatOffset,showerOffset,basinOffset;
 public Sprite idlePreview; public bool sleepHorizontal; public string artNotes;
}
[CreateAssetMenu(menuName="Ian/Growth/Visual catalog")]
public sealed class GrowthVisualCatalog:ScriptableObject {
 public GrowthVisualProfile[] profiles=Array.Empty<GrowthVisualProfile>();
 public Sprite emptyCrib,cribFront,toddlerBed,booster,stepStool,babyBath;
 public GrowthVisualProfile AtAge(int age)=>Array.Find(profiles,p=>age>=p.minAge&&age<=p.maxAge);
}
}
