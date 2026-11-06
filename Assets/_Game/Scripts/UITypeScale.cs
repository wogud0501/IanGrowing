using TMPro;using UnityEngine;
namespace IanGame {
public class UITypeScale:MonoBehaviour {public float baseSize=18;void OnEnable(){Apply();}public void Apply(){var t=GetComponent<TMP_Text>();if(t!=null){t.enableAutoSizing=false;t.fontSize=Mathf.Max(18,baseSize)+(PlayerPrefs.GetInt("ian.ui.largeText",0)==1?6:0);}}}
}
