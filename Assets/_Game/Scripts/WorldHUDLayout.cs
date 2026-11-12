using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace IanGame {
// Measure the actual localized text at its chosen font size. Reserve room before
// assigning the rest to the place label; never solve overflow by shrinking type.
[RequireComponent(typeof(WorldHUD))]
public sealed class WorldHUDLayout : MonoBehaviour {
 public bool compactStack;WorldHUD hud; CompactWorldStatusBar bar; string measured;
 void Awake(){hud=GetComponent<WorldHUD>();bar=GetComponentInChildren<CompactWorldStatusBar>(true);}
 void LateUpdate(){
  if(bar==null||hud.cash==null)return;
  var header=(RectTransform)bar.transform;var wallet=(RectTransform)hud.cash.transform.parent;
  float width=((RectTransform)header.parent).rect.width;
  if(width<=0)return;
  string key=width+"/"+hud.cash.text+"/"+hud.cash.fontSize+"/"+bar.time.text+"/"+bar.time.fontSize+"/"+bar.place.text+"/"+bar.place.fontSize+"/"+compactStack;
  if(key==measured)return;measured=key;
  float cashWidth=hud.cash.GetPreferredValues(hud.cash.text).x+30;
  float walletWidth=Mathf.Max(300,cashWidth+104);
  wallet.anchorMin=new Vector2(1,.89f);wallet.anchorMax=new Vector2(1,.98f);wallet.pivot=new Vector2(1,.5f);
  wallet.anchoredPosition=new Vector2(-width*.018f,0);wallet.sizeDelta=new Vector2(walletWidth,0);
  hud.cash.rectTransform.anchorMin=new Vector2(0,0);hud.cash.rectTransform.anchorMax=new Vector2(1,1);
  hud.cash.rectTransform.offsetMin=new Vector2(16,8);hud.cash.rectTransform.offsetMax=new Vector2(-104,-8);
  var button=(RectTransform)hud.menuButton.transform;button.anchorMin=new Vector2(1,.12f);button.anchorMax=new Vector2(1,.88f);button.pivot=new Vector2(1,.5f);button.anchoredPosition=new Vector2(-8,0);button.sizeDelta=new Vector2(80,0);
  float clockWidth=bar.time.GetPreferredValues(bar.time.text).x+12;
  var clock=bar.time.GetComponent<LayoutElement>();clock.minWidth=clock.preferredWidth=clockWidth;clock.flexibleWidth=0;
  var place=bar.place.GetComponent<LayoutElement>();place.minWidth=120;place.preferredWidth=Mathf.Max(160,bar.place.GetPreferredValues(bar.place.text).x+8);place.flexibleWidth=1;
  float available=compactStack?width*.964f:width*.964f-walletWidth-18;
  float wanted=place.preferredWidth+clockWidth+58;
  if(compactStack){wallet.anchorMin=new Vector2(1,.81f);wallet.anchorMax=new Vector2(1,.88f);wallet.sizeDelta=new Vector2(Mathf.Min(walletWidth,width*.964f),0);}
  header.anchorMin=new Vector2(0,.89f);header.anchorMax=new Vector2(0,.98f);header.pivot=new Vector2(0,.5f);header.anchoredPosition=new Vector2(width*.018f,0);header.sizeDelta=new Vector2(Mathf.Min(available,wanted),0);
 }
}
}
