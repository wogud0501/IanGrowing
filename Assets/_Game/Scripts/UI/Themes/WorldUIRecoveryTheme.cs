using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // A shared pixel HUD, with existing pixel buttons and the shared game typeface.
    // Applied to existing widgets: no alternate menus or input ownership.
    public sealed class WorldUIRecoveryTheme : MonoBehaviour
    {
        public Sprite paper;
        public Color ink = LifeRecordUI.Body;

        void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            var hud = GetComponent<WorldHUD>();
            if (hud == null)
            {
                return;
            }

            var status = GetComponentInChildren<CompactWorldStatusBar>(true);
            if (status != null)
            {
                Paper(status.GetComponent<Image>());
                status.place.color = LifeRecordUI.Body;
                status.time.color = LifeRecordUI.Body;
                Size(status.place, 24);
                Size(status.time, 24);
                var row = status.GetComponent<HorizontalLayoutGroup>();
                if (row != null)
                {
                    row.padding = new RectOffset(16, 16, 5, 5);
                    row.spacing = 16;
                }
            }

            Paper(hud.cash.transform.parent.GetComponent<Image>());
            hud.cash.color = LifeRecordUI.Body;
            Size(hud.cash, 24);
            Paper(hud.needs.transform.parent.GetComponent<Image>());
            hud.needs.color = LifeRecordUI.Body;
            Size(hud.needs, 22);
            Paper(hud.promptPanel.GetComponent<Image>());
            hud.prompt.color = LifeRecordUI.Body;
            Size(hud.prompt, 22);
        }

        static void Size(TMP_Text text, float size)
        {
            var type = text.GetComponent<UITypeScale>();
            if (type != null)
            {
                type.baseSize = size;
                type.Apply();
            }
            else
            {
                text.fontSize = size;
            }
        }

        void Paper(Image image)
        {
            if (image == null || paper == null)
            {
                return;
            }

            image.sprite = paper;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
    }
}
