using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // Measure each settled grid, not only its card. A resize can settle parent and
    // content widths on different frames; retaining old cell widths clips listings.
    public sealed class PropertyUIFlow : MonoBehaviour
    {
        public GridLayoutGroup[] grids;

        void LateUpdate()
        {
            var card = (RectTransform)transform;
            foreach (var grid in grids)
            {
                if (grid == null)
                {
                    continue;
                }

                var rect = grid.GetComponent<RectTransform>();
                float width = Mathf.Max(80, Mathf.Min(card.rect.width - 32, rect.rect.width));
                int columns =
                    width < 400 ? 1
                    : width < 760 ? 2
                    : 3;
                var size = new Vector2(Mathf.Max(80, (width - grid.spacing.x * (columns - 1)) / columns), 64);
                if (grid.constraintCount == columns && (grid.cellSize - size).sqrMagnitude < .1f)
                {
                    continue;
                }

                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columns;
                grid.cellSize = size;
                LayoutRebuilder.MarkLayoutForRebuild(rect);
            }
        }
    }
}
