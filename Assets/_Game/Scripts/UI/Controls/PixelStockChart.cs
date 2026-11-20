using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // Mesh-only pixel chart: no texture generation, materials or per-frame objects.
    [RequireComponent(typeof(CanvasRenderer))]
    public class PixelStockChart : MaskableGraphic
    {
        public List<int> prices = new List<int>();

        public void SetPrices(List<int> values)
        {
            if (values is IList<int> incoming && incoming.Count == prices.Count)
            {
                bool same = true;
                for (int i = 0; i < incoming.Count && same; i++)
                {
                    same = prices[i] == incoming[i];
                }
                if (same)
                {
                    return;
                }
            }
            prices = new List<int>(values);
            SetVerticesDirty();
        }

        void Box(VertexHelper vh, float x, float y, float w, float h, Color c)
        {
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(x, y), c, Vector2.zero);
            vh.AddVert(new Vector3(x + w, y), c, Vector2.zero);
            vh.AddVert(new Vector3(x + w, y + h), c, Vector2.zero);
            vh.AddVert(new Vector3(x, y + h), c, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            for (int i = 0; i < 5; i++)
            {
                Box(vh, r.x, r.y + r.height * i / 4, r.width, 1, new Color(.8f, .8f, .6f, .15f));
            }

            if (prices.Count < 2)
            {
                return;
            }

            int low = int.MaxValue,
                high = 0;
            foreach (int p in prices)
            {
                low = Mathf.Min(low, p);
                high = Mathf.Max(high, p);
            }

            float range = Mathf.Max(1, high - low),
                step = r.width / (prices.Count - 1);
            for (int i = 1; i < prices.Count; i++)
            {
                float x = r.x + Mathf.Round((i - 1) * step),
                    y = r.y + 4 + Mathf.Round((prices[i - 1] - low) / range * (r.height - 8)),
                    next = r.y + 4 + Mathf.Round((prices[i] - low) / range * (r.height - 8));
                Box(vh, x, y, Mathf.Ceil(step) + 1, 2, color);
                Box(vh, x + Mathf.Ceil(step), Mathf.Min(y, next), 2, Mathf.Abs(next - y) + 2, color);
            }
        }
    }
}
