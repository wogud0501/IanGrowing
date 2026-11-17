using System.Linq;
using TMPro;
using UnityEngine;

namespace IanGame
{
    public sealed class WorldFacilityInformation : MonoBehaviour
    {
        public TMP_Text label;
        public bool property;
        EconomySystem economy;

        void Start()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            economy = GameContext.Instance.game.economy;
            economy.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (economy != null)
            {
                economy.Changed -= Refresh;
            }
        }

        void Refresh()
        {
            if (label == null || economy == null)
            {
                return;
            }

            label.text = property
                ? string.Join(
                    "\n",
                    economy
                        .Properties.quotes.Take(3)
                        .Select(q =>
                            economy.propertyCatalog.Find(q.PropertyId).PropertyName
                            + "  "
                            + q.MarketPrice.ToString("N0")
                            + " G"
                        )
                )
                : string.Join(
                    "\n",
                    economy
                        .rules.stocks.Take(3)
                        .Select(s =>
                            s.CompanyName
                            + "  "
                            + (economy.GetQuote(s.StockId)?.CurrentPrice ?? s.BasePrice).ToString("N0")
                            + " G"
                        )
                );
        }
    }
}
