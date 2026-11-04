using UnityEngine;using UnityEngine.UI;
namespace IanGame {
[ExecuteAlways]
public class PixelCanvasScale:MonoBehaviour {
 public CanvasScaler scaler;public UISafeLayout safe;public UIAdaptiveLayout header,main;public LayoutElement headerSize,hero,rail,mainSize,quick,nav;
 public int AppliedIntegerScale{get;private set;}int lastW,lastH;
 void OnEnable(){Apply();}void OnRectTransformDimensionsChange(){Apply();}void Update(){if(lastW!=Screen.width||lastH!=Screen.height)Apply();}
 public void Apply(){if(scaler==null||Screen.width==0||Screen.height==0)return;lastW=Screen.width;lastH=Screen.height;bool portrait=lastH>lastW;int scale=Mathf.Max(1,Mathf.FloorToInt(portrait?lastW/480f:lastH/540f));AppliedIntegerScale=scale;scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=scale;var canvas=scaler.GetComponent<Canvas>();canvas.pixelPerfect=true;
 if(safe!=null){safe.maxWidth=portrait?640:1120;safe.Apply(true);}if(main==null)return;
 header.vertical=portrait;main.vertical=portrait;headerSize.minHeight=headerSize.preferredHeight=portrait?76:52;mainSize.minHeight=portrait?500:280;mainSize.flexibleHeight=1;
 hero.minWidth=0;hero.preferredWidth=-1;hero.flexibleWidth=1;hero.preferredHeight=portrait?200:-1;hero.minHeight=portrait?200:0;hero.flexibleHeight=1;
 rail.minWidth=0;rail.preferredWidth=portrait?-1:290;rail.flexibleWidth=portrait?1:0;rail.preferredHeight=portrait?294:-1;rail.minHeight=portrait?294:0;rail.flexibleHeight=portrait?0:1;
 quick.preferredHeight=quick.minHeight=48;nav.preferredHeight=nav.minHeight=48;header.Reflow();main.Reflow();}
}
}
