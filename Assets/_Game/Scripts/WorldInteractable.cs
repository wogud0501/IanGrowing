using UnityEngine;
namespace IanGame {
public sealed class WorldInteractable:MonoBehaviour {
 public string id,title,zone,targetZone,npcId;public WorldInteractionKind kind;public Vector2 arrival;public SpriteRenderer visual;public SpriteRenderer highlight;public float radius=1.65f;
 public SpriteRenderer shadow;public GameObject activityEffect;public bool animateDoorLeaf=true;public Vector2 UsePoint=Vector2.down*.8f;public Vector2 Point=>(Vector2)transform.position+UsePoint;
 public void Select(bool on){if(highlight!=null)highlight.enabled=on;}
}
}
