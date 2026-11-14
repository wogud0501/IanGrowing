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
  float walletWidth=Mathf.Max(212,cashWidth+104);
  wallet.anchorMin=wallet.anchorMax=Vector2.one;wallet.pivot=Vector2.one;
  wallet.anchoredPosition=new Vector2(-width*.018f,-12);wallet.sizeDelta=new Vector2(walletWidth,compactStack?64:Mathf.Max(48,hud.cash.GetPreferredValues(hud.cash.text).y+8));
  hud.cash.rectTransform.anchorMin=new Vector2(0,0);hud.cash.rectTransform.anchorMax=new Vector2(1,1);
  hud.cash.rectTransform.offsetMin=new Vector2(16,4);hud.cash.rectTransform.offsetMax=new Vector2(-104,-4);
  var button=(RectTransform)hud.menuButton.transform;button.anchorMin=new Vector2(1,.12f);button.anchorMax=new Vector2(1,.88f);button.pivot=new Vector2(1,.5f);button.anchoredPosition=new Vector2(-8,0);button.sizeDelta=new Vector2(80,0);
  float clockWidth=bar.time.GetPreferredValues(bar.time.text).x+12;
  var clock=bar.time.GetComponent<LayoutElement>();clock.minWidth=clock.preferredWidth=clockWidth;clock.flexibleWidth=0;
  var place=bar.place.GetComponent<LayoutElement>();place.minWidth=120;place.preferredWidth=Mathf.Max(120,bar.place.GetPreferredValues(bar.place.text).x+8);place.flexibleWidth=1;
  float available=compactStack?width*.964f:width*.964f-walletWidth-18;
  float wanted=place.preferredWidth+clockWidth+58;
  if(compactStack){wallet.anchoredPosition=new Vector2(-width*.018f,-84);wallet.sizeDelta=new Vector2(Mathf.Min(walletWidth,width*.964f),64);}
  header.anchorMin=header.anchorMax=new Vector2(0,1);header.pivot=new Vector2(0,1);header.anchoredPosition=new Vector2(width*.018f,-12);header.sizeDelta=new Vector2(Mathf.Min(available,wanted),compactStack?64:48);
  var mobile=hud.GetComponent<WorldMobileHUD>();if(mobile==null||!mobile.IsMobile){var needs=(RectTransform)hud.needs.transform.parent;needs.anchorMin=needs.anchorMax=new Vector2(.018f,.02f);needs.pivot=Vector2.zero;needs.anchoredPosition=Vector2.zero;needs.sizeDelta=new Vector2(Mathf.Min(width*.55f,hud.needs.GetPreferredValues(hud.needs.text).x+32),40);}
 }
}
}
