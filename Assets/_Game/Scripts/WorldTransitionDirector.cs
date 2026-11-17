using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public enum WorldTransitionStyle
    {
        PixelFade,
        ShortSlide,
        LightingReveal,
    }

    // Native UI geometry is appropriate for transition masks. One reusable set of
    // strips avoids per-trip allocations and leaves all world objects persistent.
    public sealed class WorldTransitionDirector : MonoBehaviour
    {
        public WorldHUD hud;
        public RectTransform mask;
        public Image[] strips;
        public WorldCameraController cameraRig;
        public WorldTransitionStyle Style { get; private set; }
        public bool Active { get; private set; }

        public void Begin(string from, string to)
        {
            var profile = cameraRig?.world?.GetComponent<SceneVisualPresentation>()?.profile;
            Style =
                profile != null ? profile.transitionStyle
                : to.EndsWith("bathroom") || from.EndsWith("bathroom") ? WorldTransitionStyle.LightingReveal
                : to == "shop" || to == "workplace" || to == "school" ? WorldTransitionStyle.ShortSlide
                : WorldTransitionStyle.PixelFade;
            Active = true;
            mask.gameObject.SetActive(true);
            hud.Dim(0);
        }

        public void Cover(float value)
        {
            value = Mathf.Clamp01(value);
            float stepped = Mathf.Round(value * 12) / 12;
            for (int i = 0; i < strips.Length; i++)
            {
                var r = strips[i].rectTransform;
                float x = i / (float)strips.Length,
                    w = 1f / strips.Length;
                if (Style == WorldTransitionStyle.ShortSlide)
                {
                    r.anchorMin = new Vector2(x, 0);
                    r.anchorMax = new Vector2(x + w, value);
                    strips[i].color = new Color(.06f, .08f, .12f, 1);
                }
                else
                {
                    r.anchorMin = new Vector2(x, 0);
                    r.anchorMax = new Vector2(x + w, 1);
                    float alpha =
                        Style == WorldTransitionStyle.LightingReveal
                            ? value
                            : Mathf.Clamp01(stepped + (i % 3 - 1) / 12f);
                    strips[i].color = new Color(.06f, .08f, .12f, value >= .99f ? 1 : alpha);
                }

                r.offsetMin = r.offsetMax = Vector2.zero;
            }

            if (cameraRig != null)
            {
                cameraRig.transitionZoom =
                    Style == WorldTransitionStyle.ShortSlide ? Mathf.Sin(value * Mathf.PI) * .18f : 0;
            }
        }

        public void End()
        {
            Active = false;
            if (mask != null)
            {
                mask.gameObject.SetActive(false);
            }

            if (cameraRig != null)
            {
                cameraRig.transitionZoom = 0;
            }

            hud.Dim(0);
        }

        void OnDisable()
        {
            End();
        }
    }
}
