using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
namespace IanGame {
// A small overlay owns input while the ghost is visible. Closing through Esc,
// a menu or scene unload always removes the ghost without charging the wallet.
public sealed class WorldFurniturePlacement : MonoBehaviour {
 public WorldHUD hud;public GameObject panel;public TMP_Text instructions;public Button confirm,cancel;
 SpriteRenderer ghost;string furnitureId;Vector2 position,lastMouse;float ready,nextStep;bool valid;
 void Start(){confirm.onClick.AddListener(Confirm);cancel.onClick.AddListener(Cancel);}
 public bool Begin(string id){
  var g=GameContext.Instance.game;var world=hud.world;
  if(world.Busy||g.world.State.zone!="home"||!g.economy.Housing.owned.Contains(id)){hud.Notify("집으로 돌아와 보유 가구를 배치하세요.");return false;}
  furnitureId=id;string why;if(!g.economy.FindFurniturePlacement(id,false,out var model,out why)){hud.Notify(why);return false;}
  position=model*2;var definition=g.economy.housingCatalog.Furniture(id);var rule=g.economy.housingCatalog.placementPlan.Rule(id);
  var go=new GameObject("Furniture placement preview");ghost=go.AddComponent<SpriteRenderer>();ghost.sprite=definition.preview;ghost.sharedMaterial=g.economy.housingCatalog.placementPlan.furnitureMaterial;go.transform.localScale=Vector3.one*(rule.height/definition.preview.bounds.size.y);
  hud.shell.Close(hud.home.panel);hud.shell.Push(panel);ready=Time.unscaledTime+.2f;lastMouse=Mouse.current==null?Vector2.zero:Mouse.current.position.ReadValue();Draw();return true;
 }
 void Update(){
  if(ghost==null)return;if(hud.shell.Top!=panel){Cleanup();return;}
  if(Time.unscaledTime<ready)return;
  var world=hud.world;var g=GameContext.Instance.game;var plan=g.economy.housingCatalog.placementPlan;
  if(Mouse.current!=null){var pointer=Mouse.current.position.ReadValue();if((pointer-lastMouse).sqrMagnitude>1){lastMouse=pointer;var p=world.follow.target.ScreenToWorldPoint(new Vector3(pointer.x,pointer.y,10));position=plan.Snap((Vector2)p-g.world.rules.Zone("home").origin);Draw();}}
  var keyboard=Keyboard.current;if(keyboard!=null){if(Time.unscaledTime>=nextStep){Vector2 step=new Vector2((keyboard.rightArrowKey.isPressed?1:0)-(keyboard.leftArrowKey.isPressed?1:0),(keyboard.upArrowKey.isPressed?1:0)-(keyboard.downArrowKey.isPressed?1:0));if(step!=Vector2.zero){position=plan.Snap(position+step*plan.grid);nextStep=Time.unscaledTime+.12f;Draw();}}if(keyboard.enterKey.wasPressedThisFrame)Confirm();}
  if(ghost!=null&&Mouse.current!=null&&Mouse.current.leftButton.wasReleasedThisFrame&&(UnityEngine.EventSystems.EventSystem.current==null||!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))Confirm();
 }
 public bool PreviewAt(Vector2 local){if(ghost==null)return false;position=GameContext.Instance.game.economy.housingCatalog.placementPlan.Snap(local);Draw();return valid;}
 void Draw(){
  var g=GameContext.Instance.game;var definition=g.economy.housingCatalog.Furniture(furnitureId);string reason;valid=g.economy.CanPlaceFurniture(furnitureId,position/2,out reason);
  ghost.transform.position=(Vector3)(g.world.rules.Zone("home").origin+position)+Vector3.up*(-definition.preview.bounds.min.y*ghost.transform.localScale.y);ghost.sortingOrder=4000;ghost.color=valid?new Color(.55f,1,.65f,.7f):new Color(1,.35f,.3f,.7f);
  instructions.text=definition.title+" · "+(valid?"배치 가능":reason)+"\n마우스 / 방향키 이동 · Enter 배치 · Esc 취소";confirm.interactable=valid;
 }
 public void Confirm(){if(ghost==null||!valid||Time.unscaledTime<ready)return;string id=furnitureId;Vector2 model=position/2;if(hud.home.Durable(()=>GameContext.Instance.game.economy.PlaceFurniture(id,model),"가구 배치 완료 · 무료")){hud.shell.Close(panel);Cleanup();}else Draw();}
 public void Cancel(){if(panel!=null)hud.shell.Close(panel);Cleanup();}
 void Cleanup(){if(ghost!=null)Destroy(ghost.gameObject);ghost=null;furnitureId=null;}
 void OnDisable(){Cleanup();}
}
}
