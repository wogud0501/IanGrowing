using UnityEngine;using UnityEngine.UI;
namespace IanGame {
[ExecuteAlways]
public class UIResponsiveHUD:MonoBehaviour {
 public CanvasScaler scaler;public RectTransform frame;public UIAdaptiveLayout middleFlow,headerFlow,statsFlow;public LayoutElement middle,stats,hero,mission,header,actions,navigation;
 public GridLayoutGroup actionGrid,navGrid;public RectTransform statRows;
 int lastW,lastH;void OnEnable(){Apply();}void OnRectTransformDimensionsChange(){Apply();}void Update(){if(lastW!=Screen.width||lastH!=Screen.height)Apply();}
 public void Apply(){if(!isActiveAndEnabled||scaler==null||frame==null)return;lastW=Screen.width;lastH=Screen.height;bool portrait=Screen.height>Screen.width;scaler.referenceResolution=portrait?new Vector2(820,1600):new Vector2(1600,1000);scaler.matchWidthOrHeight=portrait?0:1;
 middleFlow.vertical=portrait;headerFlow.vertical=portrait;statsFlow.vertical=true;statsFlow.columns=portrait?4:1;statsFlow.spacing=portrait?6:8;middleFlow.Reflow();headerFlow.Reflow();statsFlow.Reflow();
 header.minHeight=header.preferredHeight=portrait?118:88;middle.minHeight=portrait?740:460;middle.flexibleHeight=1;
 stats.minWidth=portrait?0:270;stats.preferredWidth=portrait?-1:290;stats.flexibleWidth=portrait?1:0;stats.minHeight=portrait?175:0;stats.preferredHeight=portrait?175:-1;stats.flexibleHeight=portrait?0:1;
 hero.minWidth=0;hero.preferredWidth=-1;hero.flexibleWidth=1;hero.minHeight=portrait?180:0;hero.preferredHeight=portrait?180:-1;hero.flexibleHeight=1;
 mission.minWidth=portrait?0:405;mission.preferredWidth=portrait?-1:445;mission.flexibleWidth=portrait?1:0;mission.minHeight=portrait?375:0;mission.preferredHeight=portrait?375:-1;mission.flexibleHeight=portrait?0:1;
 actions.minHeight=actions.preferredHeight=portrait?214:138;navigation.minHeight=navigation.preferredHeight=portrait?116:66;
 actionGrid.constraintCount=portrait?3:6;navGrid.constraintCount=portrait?3:6;
 var fluid=actionGrid.GetComponent<UIResponsiveGrid>();if(fluid!=null){fluid.minCellWidth=220;fluid.maxColumns=6;}
 }
 void Fit(GridLayoutGroup grid,int columns,float height){if(grid==null)return;var r=(RectTransform)grid.transform;grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=columns;grid.cellSize=new Vector2(Mathf.Max(1,(r.rect.width-grid.padding.horizontal-(columns-1)*grid.spacing.x)/columns),height);}
}
}
