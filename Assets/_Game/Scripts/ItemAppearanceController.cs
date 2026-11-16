using System;
using System.Linq;
using UnityEngine;
namespace IanGame {
[Serializable] public sealed class ItemAppearanceFrame {
 public VisualGrowthStage growthStage;public ActorDirection direction;public ActorMotion motion;public Sprite[] frames=Array.Empty<Sprite>();public Vector2 offset;public int sortingOffset=1;
}
// Authored per-stage/per-direction overlays follow the existing sprite renderer.
// Missing art preserves official Ian; it is never substituted with a UI icon.
[DefaultExecutionOrder(100)] public sealed class ItemAppearanceController:MonoBehaviour {
 public WorldPlayerMotor player;public string HeadphoneAppearanceId{get;private set;}public string CosmeticAppearanceId{get;private set;}public bool HasRenderedOverride{get;private set;}
 ItemService items;GameManager game;SpriteRenderer headphone,cosmetic;
 void Start(){game=GameContext.Instance?.game;if(game?.items==null)return;items=game.items;items.AppearanceChanged+=Selected;Selected(null,null);headphone=Layer("Equipped headphone art");cosmetic=Layer("Equipped cosmetic art");}
 SpriteRenderer Layer(string name){var go=new GameObject(name);go.transform.SetParent(player.sprite.transform,false);return go.AddComponent<SpriteRenderer>();}
 void Selected(string h,string c){HeadphoneAppearanceId=items.catalog.Item(items.Equipment.Current(EquipmentSlot.Headphone))?.appearanceId;CosmeticAppearanceId=items.catalog.Item(items.Equipment.Current(EquipmentSlot.Cosmetic))?.appearanceId;}
 void OnDestroy(){if(items!=null)items.AppearanceChanged-=Selected;}
 void LateUpdate(){if(items==null||player?.advanced==null||headphone==null)return;var growth=player.GetComponent<PlayerGrowthVisual>();var stage=growth?.Current?.stage??VisualGrowthStage.Adult;HasRenderedOverride=Draw(headphone,items.Equipment.Current(EquipmentSlot.Headphone),stage)|Draw(cosmetic,items.Equipment.Current(EquipmentSlot.Cosmetic),stage);}
 bool Draw(SpriteRenderer target,string id,VisualGrowthStage stage){var item=items.catalog.Item(id);var frame=item?.appearanceFrames?.FirstOrDefault(f=>f.growthStage==stage&&f.direction==player.advanced.Direction&&f.motion==player.advanced.Motion&&f.frames.Length>0);target.enabled=frame!=null&&player.sprite.enabled;if(frame==null)return false;target.sprite=frame.frames[player.advanced.Frame%frame.frames.Length];target.transform.localPosition=frame.offset;target.sortingLayerID=player.sprite.sortingLayerID;target.sortingOrder=player.sprite.sortingOrder+frame.sortingOffset;target.color=player.sprite.color;return true;}
}
}
