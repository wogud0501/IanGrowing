using System;using UnityEngine;
namespace IanGame {
[Serializable] public class CharacterArtVariant {public string key;public Sprite sprite;[TextArea]public string notes;}
[CreateAssetMenu(menuName="Ian/Official Character Identity")]
public class CharacterIdentity:ScriptableObject {
 public string characterName="이안"; public Texture2D officialReference; public Sprite baseSprite;
 [TextArea(5,12)]public string designRules;
 [Tooltip("Optional dedicated portrait. If empty the base sprite texture is displayed using portraitRegion.")]public Texture2D portraitTexture;
 public Rect portraitRegion=new Rect(.12f,.51f,.76f,.45f);
 public CharacterArtVariant[] expressions=new CharacterArtVariant[0],states=new CharacterArtVariant[0],eventIllustrations=new CharacterArtVariant[0];
 public Sprite Expression(string key)=>Resolve(expressions,key);
 public Sprite State(string key)=>Resolve(states,key);
 public Sprite EventIllustration(string key)=>Resolve(eventIllustrations,key);
 Sprite Resolve(CharacterArtVariant[] variants,string key){if(variants!=null)foreach(var item in variants)if(item!=null&&item.key==key&&item.sprite!=null)return item.sprite;return baseSprite;}
}
}