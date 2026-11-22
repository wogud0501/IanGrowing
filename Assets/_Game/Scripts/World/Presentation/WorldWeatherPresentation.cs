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

            if (weather == applied && previousDetail == tier)
            {
                return;
            }

            applied = weather;
            previousDetail = tier;
            foreach (var reflection in wetReflections)
            {
                if (reflection != null)
                {
                    reflection.enabled = weather == VisualWeather.Rain && tier >= 2;
                }
            }
        }
    }
}
