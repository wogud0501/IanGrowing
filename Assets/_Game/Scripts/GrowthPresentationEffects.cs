using UnityEngine;
namespace IanGame {
// Small pooled visual response. Controller, Collider and authoritative age are
// never replaced, hidden or scaled by the transition.
[DefaultExecutionOrder(200)]public sealed class GrowthPresentationEffects:MonoBehaviour {
 public PlayerGrowthVisual growth;public Sprite sparkle;
 public int PlayedCount{get;private set;}
 SpriteRenderer ghost;readonly SpriteRenderer[] motes=new SpriteRenderer[12];
 System.IDisposable controlLease;float elapsed=1;Vector2 origin;bool subscribed;
 void Start(){if(growth==null||sparkle==null)return;ghost=Layer("Previous growth silhouette");for(int i=0;i<motes.Length;i++){motes[i]=Layer("Growth sparkle "+i);motes[i].sprite=sparkle;}growth.VisualChanged+=Changed;subscribed=true;}
 SpriteRenderer Layer(string name){var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(growth.motor.world.transform,false);var r=go.GetComponent<SpriteRenderer>();r.sharedMaterial=growth.motor.sprite.sharedMaterial;r.enabled=false;return r;}
 void Changed(GrowthVisualProfile prior,GrowthVisualProfile next){if(prior==null||ghost==null||UIShell.ReducedMotion)return;controlLease?.Dispose();controlLease=growth.motor.controlLocks?.Acquire("growth-animation");growth.motor.StopForControlLock();origin=growth.motor.Position;elapsed=0;PlayedCount++;ghost.sprite=prior.idlePreview;ghost.transform.position=(Vector3)origin;ghost.transform.localScale=Vector3.one*growth.motor.visualScale;ghost.sortingOrder=growth.motor.sprite.sortingOrder-1;ghost.enabled=true;foreach(var mote in motes)mote.enabled=true;}
 void LateUpdate(){if(elapsed>=.55f||ghost==null){controlLease?.Dispose();controlLease=null;return;}elapsed+=Time.unscaledDeltaTime;float f=Mathf.Clamp01(elapsed/.55f);ghost.color=new Color(.72f,.81f,.9f,(1-f)*.4f);ghost.enabled=f<.7f;for(int i=0;i<motes.Length;i++){float angle=i*Mathf.PI*2/motes.Length;float distance=.2f+f*.55f;var r=motes[i];Vector2 p=origin+Vector2.up*.75f+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;float ppu=growth.motor.world.follow.target.pixelHeight/(2*growth.motor.world.follow.target.orthographicSize);r.transform.position=new Vector3(Mathf.Round(p.x*ppu)/ppu,Mathf.Round(p.y*ppu)/ppu,0);r.sortingOrder=growth.motor.sprite.sortingOrder+2;r.color=new Color(.96f,.88f,.61f,1-f);r.enabled=f<1;}}
 void OnDisable(){controlLease?.Dispose();controlLease=null;}
 void OnDestroy(){controlLease?.Dispose();if(subscribed&&growth!=null)growth.VisualChanged-=Changed;if(ghost!=null)Destroy(ghost.gameObject);foreach(var r in motes)if(r!=null)Destroy(r.gameObject);}
}
}
