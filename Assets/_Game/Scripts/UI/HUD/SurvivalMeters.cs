using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class SurvivalMeters
    {
        static readonly string[] Names = { "허기", "수분", "체력", "멘탈" };
        readonly TMP_Text[] labels = new TMP_Text[4];
        readonly Image[] meters = new Image[4];
        readonly int[] previous = { -1, -1, -1, -1 };

        public SurvivalMeters(TMP_Text original)
        {
            var frame = (RectTransform)original.transform.parent;
            var image = frame.GetComponent<Image>();
            LifeRecordUI.Skin(image, RecordSurface.Charcoal);
            original.enabled = false;
            frame.sizeDelta = new Vector2(470, 59);
            for (int i = 0; i < labels.Length; i++)
            {
                float x = .035f + i * .24f;
                labels[i] = LifeRecordUI.Label(frame, original.font, Names[i], 17, LifeRecordUI.Paper, x, .29f, x + .225f, .94f);
                var track = LifeRecordUI.Rect(frame, Names[i] + " track", x, .17f, x + .20f, .235f);
                var baseImage = track.gameObject.AddComponent<Image>();
                baseImage.raycastTarget = false;
                baseImage.color = new Color32(74, 94, 90, 255);
                meters[i] = LifeRecordUI.Rect(track, "Level", 0, 0, 1, 1).gameObject.AddComponent<Image>();
                meters[i].raycastTarget = false;
            }
        }

        public void Set(int index, int value)
        {
            if (previous[index] == value)
            {
                return;
            }

            previous[index] = value;
            labels[index].text = Names[index] + " " + value;
            meters[index].color = value <= 25 ? new Color32(247, 170, 113, 255) : new Color32(169, 200, 166, 255);
            meters[index].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1);
        }
    }
}
