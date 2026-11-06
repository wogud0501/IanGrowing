using System.Linq;using UnityEngine;using UnityEngine.UI;using TMPro;
namespace IanGame {
public class PixelTabGroup:MonoBehaviour {
 public GameObject[] pages;public Button[] tabs;public PixelUITheme theme;public int selected;bool wired;
 void OnEnable(){Wire();Select(selected);}void Wire(){if(wired||tabs==null)return;wired=true;for(int i=0;i<tabs.Length;i++){int index=i;tabs[i].onClick.AddListener(()=>Select(index));}}
 public void Select(int index){if(pages==null||pages.Length==0)return;selected=Mathf.Clamp(index,0,pages.Length-1);for(int i=0;i<pages.Length;i++)if(pages[i]!=null)pages[i].SetActive(i==selected);for(int i=0;i<tabs.Length;i++){if(tabs[i]==null)continue;var image=tabs[i].GetComponent<Image>();if(image!=null)image.sprite=i==selected?theme.tab:theme.button;var label=tabs[i].GetComponentInChildren<TMP_Text>();if(label!=null)label.color=i==selected?theme.gold:theme.cream;}var shell=UIShell.For(this);if(Application.isPlaying&&shell!=null&&shell.Top!=null)UIShell.Focus(shell.Top);}
}
}
