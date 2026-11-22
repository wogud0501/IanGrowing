using System;
using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    // Light() in the existing world delegates here. This is the only tint writer
    // for bound scenes; it never changes game time, stats, permissions or saves.
    public sealed class SceneVisualPresentation : MonoBehaviour
    {
        public LifeWorldController world;
        public SceneVisualProfile profile;
        public SpriteRenderer[] emissive = Array.Empty<SpriteRenderer>();
        public SpriteRenderer[] nightWindows = Array.Empty<SpriteRenderer>();
        public Color CharacterTint { get; private set; } = Color.white;
        public event Action<string> AudioCueRequested;
        public VisualWeather Weather => profile == null ? VisualWeather.Clear : profile.weather;

        public void ApplyLighting(
            int hour,
            Dictionary<SpriteRenderer, Color> bases,
            SpriteRenderer[] lamps,
            bool lowDetail
        )
        {
            if (profile == null)
            {
                return;
            }

            WorldGraphicsSettings.Bind(world?.follow?.target);
            int tier = WorldGraphicsSettings.Detail;
            float visualHour =
                world != null && world.Service != null ? (float)(world.Service.Game.clock.SubDaySeconds / 3600) : hour;
            var tint = Color.Lerp(Color.white, profile.AtHour(visualHour), Mathf.Clamp01(profile.paletteStrength * (.68f + tier * .14f)));
            foreach (var pair in bases)
            {
                var renderer = pair.Key;
                if (renderer == null || Array.IndexOf(lamps, renderer) >= 0)
                {
                    continue;
                }

                var c = pair.Value;
                renderer.color = new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a);
            }

            CharacterTint = Color.Lerp(tint, Color.white, .3f);
            bool night = hour < 6 || hour >= 19;
            foreach (var window in nightWindows)
            {
                if (window != null)
                {
                    window.enabled = night;
                    window.color = Color.Lerp(new Color(.72f, .73f, .76f),
                        new Color(1f, .91f, .76f), tier / 3f);
                }
            }
            foreach (var lamp in lamps)
            {
                if (lamp != null)
                {
                    lamp.enabled = night && !lowDetail && tier > 0;
                    if (bases.TryGetValue(lamp, out var authored))
                    {
                        authored.a *= Mathf.Lerp(.45f, 1f, tier / 3f);
                        lamp.color = authored;
                    }
                }
            }

            foreach (var light in emissive)
            {
                if (light != null)
                {
                    light.color = night ? new Color(1, .89f, .69f, 1) : Color.white;
                }
            }

            if (world.Player != null)
            {
                var c = world.Player.sprite.color;
                world.Player.sprite.color = new Color(CharacterTint.r, CharacterTint.g, CharacterTint.b, c.a);
            }

            if (world.follow?.target != null)
            {
                world.follow.target.backgroundColor = profile.surround;
            }
        }

        public void RequestAudio(string cue)
        {
            AudioCueRequested?.Invoke(cue);
        }
    }
}
