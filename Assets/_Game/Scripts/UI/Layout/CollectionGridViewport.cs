using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IanGame
{
    [DisallowMultipleComponent]
    public sealed class CollectionGridViewport : UIBehaviour
    {
        public event Action WidthChanged;

        float measuredWidth;

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            float width = ((RectTransform)transform).rect.width;
            if (!isActiveAndEnabled || Mathf.Approximately(width, measuredWidth))
            {
                return;
            }

            measuredWidth = width;
            WidthChanged?.Invoke();
        }
    }
}
