using UnityEngine;

namespace IanGame
{
    public enum VisualWeather
    {
        Clear,
        Rain,
        Snow,
        Fog,
    }

    [CreateAssetMenu(menuName = "Ian/Presentation/Scene visual profile")]
    public sealed class SceneVisualProfile : ScriptableObject
    {
        public string identity;
        public Color morning = new Color(1, .94f, .86f);
        public Color day = Color.white;
        public Color evening = new Color(1, .8f, .68f);
        public Color night = new Color(.65f, .71f, .86f);
        public Color surround = new Color(.12f, .14f, .18f);

        [Range(0, 1)]
        public float paletteStrength = .65f;
        public float preferredHalfHeight = 4.5f;

        [Min(0)]
        public float roofViewPadding;
        public int environmentAnimationBudget = 8;
        public VisualWeather weather = VisualWeather.Clear;
        public AudioClip ambience;
        public string transitionCue = "room";
        public string audioCue = "home";
        public WorldTransitionStyle transitionStyle = WorldTransitionStyle.PixelFade;

        public Color AtHour(float hour)
        {
            hour = Mathf.Repeat(hour, 24);
            if (hour < 5)
            {
                return night;
            }

            if (hour < 8)
            {
                return Color.Lerp(night, morning, (hour - 5) / 3);
            }

            if (hour < 11)
            {
                return Color.Lerp(morning, day, (hour - 8) / 3);
            }

            if (hour < 17)
            {
                return day;
            }

            if (hour < 20)
            {
                return Color.Lerp(day, evening, (hour - 17) / 3);
            }

            if (hour < 23)
            {
                return Color.Lerp(evening, night, (hour - 20) / 3);
            }

            return night;
        }
    }
}
