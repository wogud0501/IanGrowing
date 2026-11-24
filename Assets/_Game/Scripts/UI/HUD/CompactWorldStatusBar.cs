using TMPro;
using UnityEngine;

namespace IanGame
{
    // Three independent layout elements replace the cramped clock/message sentence.
    public sealed class CompactWorldStatusBar : MonoBehaviour
    {
        public TMP_Text place;
        public TMP_Text time;
        public TMP_Text message;
        public string fixedPlace;
        public bool world;
        float next;
        (int age, int day, int minute) displayed = (-1, -1, -1);

        static string HomePlace(GameManager game)
        {
            var host = game.world.Host;
            var plan = game.economy.housingCatalog?.placementPlan;
            if (host != null && plan?.regions != null)
            {
                Vector2 p = host.Player.Position - game.world.rules.Zone("home").origin;
                if (Vector2.Distance(p, plan.entrance) < .8f)
                {
                    return "집 · 현관";
                }

                foreach (var room in plan.regions)
                {
                    if (room.bounds.Contains(p))
                    {
                        return "집 · " + room.title;
                    }
                }
            }

            return "집";
        }

        void Update()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            var game = GameContext.Instance.game;
            int minuteOfDay = (int)(game.clock.SubDaySeconds / 60);
            var date = (game.growth.Age, game.clock.Day, minuteOfDay);
            if (displayed != date)
            {
                displayed = date;
                time.text = $"{date.Age}세 · DAY {date.Day} · {game.clock.WeekdayName} · {minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
            }

            if (Time.unscaledTime < next)
            {
                return;
            }

            next = Time.unscaledTime + .25f;
            var w = game.world;
            place.text =
                world && w != null && w.Active
                    ? (w.State.zone == "home" ? HomePlace(game) : w.rules.Zone(w.State.zone).title)
                    : fixedPlace;

            if (message && !string.IsNullOrEmpty(message.text))
            {
                message.text = "";
            }
        }
    }
}
