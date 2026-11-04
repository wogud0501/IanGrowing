using UnityEngine;using TMPro;
namespace IanGame {
public enum PixelIcon {Coin,Heart,Water,Meal,Moon,Book,Social,Work,Home,Life,Growth,Activity,Gear,Quest,Arrow,Warning}
[CreateAssetMenu(menuName="Ian/UI/Pixel Theme")]
public class PixelUITheme:ScriptableObject {
 public Color ink=new Color32(28,38,58,255),cream=new Color32(255,241,207,255),gold=new Color32(240,188,99,255),coral=new Color32(220,119,91,255),mint=new Color32(127,182,150,255),blue=new Color32(106,153,173,255),lilac=new Color32(170,157,197,255);
 public Sprite panel,panelDark,panelBlue,button,hover,pressed,disabled,tab,track,fill;public Sprite[] icons=new Sprite[16];public TMP_FontAsset font;public int pixelsPerUnit=100;
 public Sprite Icon(PixelIcon id)=>icons!=null&&(int)id<icons.Length?icons[(int)id]:null;
}
}
