using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace IanGame {
// Applies the same existing pixel assets to menus across all playable scenes.
// Charts, fill bars, portraits, item art and transparent overlays are preserved.
public sealed class PixelWindowPalette:MonoBehaviour {
 public PixelUITheme theme;public Sprite normal,hover,pressed,disabled;
 void Awake(){Apply();}
 public void Apply(){if(theme==null)return;
  foreach(var image in GetComponentsInChildren<Image>(true)){
   if(image.sprite==null||image.sprite.name!="Panel_64")continue;
   var button=image.GetComponent<Button>();image.sprite=button==null?theme.panel:normal;image.type=Image.Type.Sliced;image.color=Color.white;
   if(button!=null){var states=button.spriteState;states.highlightedSprite=hover;states.pressedSprite=pressed;states.selectedSprite=hover;states.disabledSprite=disabled;button.spriteState=states;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.selectedColor=Color.white;colors.pressedColor=Color.white;colors.disabledColor=new Color(.65f,.65f,.65f);button.colors=colors;}
  }
  foreach(var text in GetComponentsInChildren<TMP_Text>(true)){
   // Find the background actually behind this label. Keep text on dark charts light.
   for(var parent=text.transform.parent;parent!=null&&parent.IsChildOf(transform);parent=parent.parent){var image=parent.GetComponent<Image>();if(image==null||image.color.a<.1f)continue;var mask=image.GetComponent<Mask>();if(mask!=null&&!mask.showMaskGraphic)continue;
    bool paper=image.sprite==theme.panel||image.sprite==normal||image.sprite==hover||image.sprite==pressed||image.sprite==disabled||image.sprite==theme.tab;
    text.color=paper?new Color32(57,53,47,255):theme.cream;break;
   }
  }
 }
}
}
