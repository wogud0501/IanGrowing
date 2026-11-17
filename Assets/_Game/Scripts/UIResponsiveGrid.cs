using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    [ExecuteAlways]
    public class UIResponsiveGrid : MonoBehaviour
    {
        public GridLayoutGroup grid;
        public float rowHeight = 72;
        public float minCellWidth = 320;
        public int maxColumns = 3;

        void OnEnable()
        {
            Resize();
        }

        void OnRectTransformDimensionsChange()
        {
            Resize();
        }

        void Resize()
        {
            if (grid == null)
            {
                return;
            }

            var r = (RectTransform)grid.transform;
            float width = r.rect.width - grid.padding.horizontal;
            int count = Mathf.Clamp(
                Mathf.FloorToInt((width + grid.spacing.x) / (minCellWidth + grid.spacing.x)),
                1,
                maxColumns
            );
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = count;
            grid.cellSize = new Vector2(Mathf.Max(1, (width - (count - 1) * grid.spacing.x) / count), rowHeight);
        }
    }
}
