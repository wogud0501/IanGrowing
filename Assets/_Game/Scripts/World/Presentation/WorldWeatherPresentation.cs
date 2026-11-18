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
            bool reduced = UIShell.ReducedMotion || PlayerPrefs.GetInt("ian.visual.effects", 1) == 0;
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

            if (weather == applied)
            {
                return;
            }

            applied = weather;
            foreach (var reflection in wetReflections)
            {
                if (reflection != null)
                {
                    reflection.enabled = weather == VisualWeather.Rain;
                }
            }
        }
    }
}
