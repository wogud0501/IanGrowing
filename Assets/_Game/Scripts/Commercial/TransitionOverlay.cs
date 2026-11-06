using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace IanGame.Commercial {
public sealed class TransitionOverlay : MonoBehaviour {
 public RectTransform curtain; public CanvasGroup group; public TMP_Text caption; public Image image;
 public void Block(bool block){group.blocksRaycasts=block;group.interactable=block;}
 public IEnumerator Wipe(bool cover,float seconds){
  gameObject.SetActive(true);group.alpha=1;Block(true);
  float start=cover?0:1,end=cover?1:0;
  if(seconds>0)for(float elapsed=0;elapsed<seconds;elapsed+=Time.unscaledDeltaTime){Set(Mathf.Lerp(start,end,Mathf.SmoothStep(0,1,elapsed/seconds)));yield return null;}
  Set(end);if(!cover){group.alpha=0;Block(false);gameObject.SetActive(false);}
 }
 void Set(float amount){image.enabled=amount>0;curtain.anchorMax=new Vector2(amount,1);caption.enabled=amount>.8f;}
}
}
