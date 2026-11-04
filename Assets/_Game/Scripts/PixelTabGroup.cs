using System.Linq;using UnityEngine;using UnityEngine.UI;
namespace IanGame {
public class PixelTabGroup:MonoBehaviour {
 public GameObject[] pages;public Button[] tabs;public PixelUITheme theme;public int selected;bool wired;
 void OnEnable(){Wire();Select(selected);}void Wire(){if(wired||tabs==null)return;wired=true;for(int i=0;i<tabs.Length;i++){int index=i;tabs[i].onClick.AddListener(()=>Select(index));}}
 public void Select(int index){if(pages==null||pages.Length==0)return;selected=Mathf.Clamp(index,0,pages.Length-1);for(int i=0;i<pages.Length;i++)if(pages[i]!=null)pages[i].SetActive(i==selected);for(int i=0;i<tabs.Length;i++){var image=tabs[i].GetComponent<Image>();if(image!=null)image.sprite=i==selected?theme.tab:theme.button;}var shell=UIShell.For(this);if(Application.isPlaying&&shell!=null&&shell.Top!=null)UIShell.Focus(shell.Top);}
}
}
