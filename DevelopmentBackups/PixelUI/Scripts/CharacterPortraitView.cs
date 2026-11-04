using UnityEngine;using UnityEngine.UI;
namespace IanGame {
[RequireComponent(typeof(RawImage))]
public class CharacterPortraitView:MonoBehaviour {
 public CharacterIdentity identity;public RawImage image;
 void OnEnable(){Refresh();}
 public void Refresh(){if(image==null)image=GetComponent<RawImage>();if(image==null||identity==null)return;
 image.texture=identity.portraitTexture!=null?identity.portraitTexture:identity.baseSprite!=null?identity.baseSprite.texture:null;
 image.uvRect=identity.portraitTexture!=null?new Rect(0,0,1,1):identity.portraitRegion;image.raycastTarget=false;}
}
}