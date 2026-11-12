using UnityEngine;
using UnityEngine.UI;
namespace IanGame {
// Width-based reflow inside the existing safe-area window. No game state changes on resize.
public sealed class PropertyUIFlow:MonoBehaviour {
 public GridLayoutGroup[] grids;float width=-1;
 void OnEnable(){width=-1;}void OnRectTransformDimensionsChange(){width=-1;}
 void LateUpdate(){var r=(RectTransform)transform;if(Mathf.Abs(width-r.rect.width)<1)return;width=r.rect.width;foreach(var grid in grids){int columns=width<650?2:3;grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=columns;grid.cellSize=new Vector2(Mathf.Max(80,(grid.GetComponent<RectTransform>().rect.width-grid.spacing.x*(columns-1))/columns),64);LayoutRebuilder.MarkLayoutForRebuild(grid.GetComponent<RectTransform>());}}
}
}
