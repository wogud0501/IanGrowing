using TMPro;using UnityEngine;
namespace IanGame {
public class UITypeScale:MonoBehaviour {public float baseSize=22;void OnEnable(){Apply();}public void Apply(){var t=GetComponent<TMP_Text>();if(t!=null)t.fontSize=baseSize*(PlayerPrefs.GetInt("ian.ui.largeText",0)==1?1.15f:1);}}
}
