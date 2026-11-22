using UnityEngine;

namespace IanGame
{
    // Visual override only. No gameplay, clock or save ownership.
    public sealed class WorldWeatherPresentation : MonoBehaviour
    {
        public SceneVisualPresentation presentation;
        public ParticleSystem rain;
        public SpriteRenderer[] wetReflections = System.Array.Empty<SpriteRenderer>();
        VisualWeather? preview;
        VisualWeather applied = (VisualWeather)(-1);
        float nextTick;
        int previousDetail = -1;
        float authoredRate;
        bool lastNight;
        SpriteRenderer[] reflectedLights;
        static Sprite reflectedInk;
        public VisualWeather Current => preview ?? (presentation == null ? VisualWeather.Clear : presentation.Weather);

        public void SetWeather(VisualWeather weather)
        {
            preview = weather;
            Apply();
        }

        public void ClearOverride()
        {
            preview = null;
            Apply();
        }

        void OnEnable()
        {
            applied = (VisualWeather)(-1);
            Apply();
        }

        void Update()
        {
            if (Time.unscaledTime < nextTick)
            {
                return;
            }

            nextTick = Time.unscaledTime + .5f;
            Apply();
        }

        void Apply()
        {
            var weather = Current;
            int tier = WorldGraphicsSettings.Detail;
            bool reduced = UIShell.ReducedMotion || tier == 0;
            if (rain && previousDetail != tier)
            {
                var emission = rain.emission;
                if (authoredRate <= 0)
                {
                    authoredRate = Mathf.Max(1, emission.rateOverTimeMultiplier);
                }

                emission.rateOverTimeMultiplier = authoredRate * (.3f + .35f * tier);
            }
            bool raining = weather == VisualWeather.Rain && !reduced;
            if (rain != null && rain.isPlaying != raining)
            {
                if (raining)
                {
                    rain.Play();
                }
                else
                {
                    rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            int hour = presentation && presentation.world && presentation.world.Service != null
                ? presentation.world.Service.Hour : 12;
            bool night = hour < 6 || hour >= 19;
            if (weather == applied && previousDetail == tier && night == lastNight)
            {
                return;
            }

            lastNight = night;
            applied = weather;
            previousDetail = tier;
            foreach (var reflection in wetReflections)
            {
                if (reflection != null)
                {
                    reflection.enabled = weather == VisualWeather.Rain && tier >= 2;
                }
            }
            bool litPavement = weather == VisualWeather.Rain && night && tier >= 2;
            if (litPavement && reflectedLights == null && presentation && presentation.world)
            {
                BindReflections(presentation.world.lamps);
            }

            if (reflectedLights != null)
            {
                foreach (var stripe in reflectedLights)
                {
                    stripe.enabled = litPavement;
                    stripe.transform.localScale = new Vector3(tier >= 3 ? 1.25f : .80f, tier >= 3 ? 1.65f : .82f, 1);
                    stripe.color = new Color(1, .82f, .56f, tier >= 3 ? .59f : .29f);
                }
            }
        }

        void BindReflections(SpriteRenderer[] lamps)
        {
            if (!reflectedInk)
            {
                var ink = Resources.Load<Texture2D>("Weather/LanternReflection");
                reflectedInk = Sprite.Create(ink, new Rect(0, 0, ink.width, ink.height), new Vector2(.5f, 1), 32);
            }

            var result = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var light in lamps)
            {
                if (!light || !light.transform.parent || !light.transform.parent.TryGetComponent<SpriteRenderer>(out var pole))
                {
                    continue;
                }

                if (!pole.name.StartsWith("Street lantern", System.StringComparison.Ordinal))
                {
                    continue;
                }

                var stripe = new GameObject("Wet pavement below lantern", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                stripe.transform.SetParent(transform, false);
                stripe.transform.position = new Vector3(pole.bounds.center.x, pole.bounds.min.y + .05f, pole.transform.position.z);
                stripe.sprite = reflectedInk;
                stripe.sharedMaterial = light.sharedMaterial;
                stripe.sortingLayerID = pole.sortingLayerID;
                stripe.sortingOrder = pole.sortingOrder - 3;
                result.Add(stripe);
            }

            reflectedLights = result.ToArray();
        }
    }
}
