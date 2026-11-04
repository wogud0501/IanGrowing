using UnityEngine;using TMPro;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/UI/Indie Theme")]
public class IndieUITheme:ScriptableObject {
 public Color ink=new Color32(28,37,55,255),paper=new Color32(250,245,231,255),coral=new Color32(255,119,98,255),lime=new Color32(221,239,105,255),lavender=new Color32(204,194,245,255),mint=new Color32(164,218,201,255),white=new Color32(255,253,245,255);
 public TMP_FontAsset font;public Sprite rounded;public float quick=.12f,modal=.18f,number=.24f;
 public float[] spacing={4,8,12,16,24,32,48};public float[] typography={18,22,26,34,48,72};
}
}
