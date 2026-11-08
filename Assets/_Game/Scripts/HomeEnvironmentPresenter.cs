using System.Collections.Generic;
using UnityEngine;
using IanGame.Commercial;
namespace IanGame {
public sealed class HomeEnvironmentPresenter:MonoBehaviour {
 public RoomAtmosphere room;public HousingCatalog catalog;public bool inBathroom;GameManager game;
 readonly List<GameObject> props=new List<GameObject>();
 void Start(){if(GameContext.Instance==null)return;game=GameContext.Instance.game;game.economy.Changed+=Apply;Apply();}
 void OnDestroy(){if(game!=null)game.economy.Changed-=Apply;}
 public void ToggleBathroom(){inBathroom=!inBathroom;Apply();}
 public void Apply(){if(game==null)return;var h=game.economy.Housing;var r=catalog.Residence(h.residenceId);if(inBathroom)room.background.sprite=catalog.bathroom;else if(r!=null&&h.wallpaper=="warm-white"&&h.floor=="wood-linoleum")room.background.sprite=r.preview;
  if(room.animatedDetail!=null)room.animatedDetail.gameObject.SetActive(!inBathroom&&h.residenceId=="improved");foreach(var mote in room.motes)if(mote!=null)mote.gameObject.SetActive(!inBathroom&&h.residenceId=="improved");
  var hud=GetComponent<CommercialHUD>();if(hud!=null)hud.title.text=inBathroom?"화장실 / BATHROOM":(r?.title??"이안의 집")+" / HOME";
  foreach(var p in props)Destroy(p);props.Clear();if(inBathroom)return;foreach(var p in h.placed){var f=catalog.Furniture(p.id);if(f==null||f.preview==null)continue;var go=new GameObject("Owned furniture / "+p.id,typeof(SpriteRenderer));go.transform.SetParent(room.transform,false);go.transform.localPosition=new Vector3(p.position.x,p.position.y,0);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=f.preview;sr.sortingOrder=p.position.y<room.characterOrigin.y?12:5;props.Add(go);}
 }
}
}
