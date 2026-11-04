using UnityEngine;
namespace IanGame {
public class CharacterPresentation:MonoBehaviour {
 public GameManager game; public Animator animator;
 void OnEnable(){if(game!=null)game.ActionPerformed+=React;}
 void OnDisable(){if(game!=null)game.ActionPerformed-=React;}
 void React(int action){if(animator!=null&&animator.runtimeAnimatorController!=null)animator.CrossFade(action==0?"Work":action==1?"Rest":"Play",.15f);}
}
}