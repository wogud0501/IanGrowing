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
            if (Time.unscaledTime < next || GameContext.Instance == null)
            {
                return;
            }

            next = Time.unscaledTime + .25f;
            var game = GameContext.Instance.game;
            var w = game.world;
            int hour = (int)(game.clock.SubDaySeconds / 3600),
                minute = (int)(game.clock.SubDaySeconds / 60) % 60;
            place.text =
                world && w != null && w.Active
                    ? (w.State.zone == "home" ? HomePlace(game) : w.rules.Zone(w.State.zone).title)
                    : fixedPlace;
            string clock = $"{game.growth.Age}세 · DAY {game.clock.Day}  {hour:00}:{minute:00}";
            if (time.text != clock)
            {
                time.text = clock;
            }

            if (message && !string.IsNullOrEmpty(message.text))
            {
                message.text = "";
            }
        }
    }
}
