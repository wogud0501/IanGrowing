using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class SurvivalMeters
    {
        static readonly string[] Names = { "허기", "수분", "체력", "멘탈" };
        static readonly Sprite[] icons = new Sprite[4];
        readonly TMP_Text[] labels = new TMP_Text[4];
        readonly Image[] meters = new Image[4];
        readonly MonoBehaviour host;
        readonly Coroutine[] pulses = new Coroutine[4];
        readonly int[] previous = { -1, -1, -1, -1 };

        public SurvivalMeters(TMP_Text original, MonoBehaviour host)
        {
            this.host = host;
            var frame = (RectTransform)original.transform.parent;
            var image = frame.GetComponent<Image>();
            LifeRecordUI.Skin(image, RecordSurface.Header);
            image.color = new Color(1, 1, 1, .92f);
            original.enabled = false;
            frame.sizeDelta = new Vector2(470, 59);
            for (int i = 0; i < labels.Length; i++)
            {
                float x = .035f + i * .24f;
                labels[i] = LifeRecordUI.Label(frame, original.font, Names[i], 17, LifeRecordUI.Heading, x + .040f, .29f, x + .228f, .94f);
                if (!icons[i])
                {
                    var atlas = Resources.Load<Texture2D>("IanLedger/Vitals");
                    icons[i] = Sprite.Create(atlas, new Rect(i * 16, 0, 16, 16), Vector2.one * .5f, 100);
                }

                var icon = LifeRecordUI.Rect(frame, Names[i] + " icon", x, .38f, x + .033f, .82f)
                    .gameObject.AddComponent<Image>();
                icon.sprite = icons[i];
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var track = LifeRecordUI.Rect(frame, Names[i] + " track", x, .17f, x + .20f, .235f);
                var baseImage = track.gameObject.AddComponent<Image>();
                baseImage.raycastTarget = false;
                baseImage.color = LifeRecordUI.Track;
                meters[i] = LifeRecordUI.Rect(track, "Level", 0, 0, 1, 1).gameObject.AddComponent<Image>();
                meters[i].raycastTarget = false;
            }
        }

        public void Focus(int index)
        {
            if (host.isActiveAndEnabled)
            {
                host.StartCoroutine(FocusLabel(labels[index]));
            }
        }

        System.Collections.IEnumerator FocusLabel(TMP_Text label)
        {
            label.fontStyle |= FontStyles.Underline;
            yield return new WaitForSecondsRealtime(2);
            label.fontStyle &= ~FontStyles.Underline;
        }

        public void Set(int index, int value)
        {
            if (previous[index] == value)
            {
                return;
            }

            int old = previous[index];
            previous[index] = value;
            labels[index].text = (value <= 25 ? "! " : "") + Names[index] + " " + value;
            meters[index].color = LifeRecordUI.Heading;
            meters[index].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1);
            if (old >= 0 && host.isActiveAndEnabled)
            {
                if (pulses[index] != null)
                {
                    host.StopCoroutine(pulses[index]);
                }

                pulses[index] = host.StartCoroutine(Highlight(index, value - old));
            }
        }

        System.Collections.IEnumerator Highlight(int index, int change)
        {
            labels[index].text = Names[index] + (change > 0 ? " ▲ +" : " ▼ ") + change;
            labels[index].color = Color.white;
            yield return new WaitForSecondsRealtime(.8f);
            labels[index].text = (previous[index] <= 25 ? "! " : "") + Names[index] + " " + previous[index];
            labels[index].color = LifeRecordUI.Heading;
            pulses[index] = null;
        }
    }
}
