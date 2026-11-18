using System;
using UnityEngine;

namespace IanGame
{
    public enum UrbanNPCArchetype
    {
        Resident,
        Student,
        OfficeWorker,
        ShopWorker,
        Delivery,
        Parent,
        Elder,
        JobSeeker,
        FinanceWorker,
        Visitor,
    }

    public enum UrbanVehicleKind
    {
        Sedan,
        Taxi,
        Bus,
        DeliveryVan,
        DeliveryScooter,
        Bicycle,
    }

    [Serializable]
    public sealed class PopulationBand
    {
        [Range(0, 23)]
        public int fromHour;

        [Min(0)]
        public int ambientResidents;

        [Min(0)]
        public int vehicles;
    }

    [Serializable]
    public sealed class ZoneLifeRule
    {
        public string zoneId;
        public string identity;

        [Min(0)]
        public int ambientCapacity = 6;
        public PopulationBand[] bands = Array.Empty<PopulationBand>();

        public PopulationBand AtHour(int hour)
        {
            PopulationBand selected = null;
            int latest = -1;
            int wrapped = -1;
            PopulationBand previous = null;
            hour = ((hour % 24) + 24) % 24;
            foreach (var band in bands)
            {
                if (band.fromHour > wrapped)
                {
                    wrapped = band.fromHour;
                    previous = band;
                }

                if (band.fromHour <= hour && band.fromHour >= latest)
                {
                    latest = band.fromHour;
                    selected = band;
                }
            }

            return selected ?? previous;
        }
    }

    // Data only. Existing resident and street controllers remain the simulation owners.
    [CreateAssetMenu(menuName = "Ian/World/Living city profile")]
    public sealed class WorldLifeProfile : ScriptableObject
    {
        public ZoneLifeRule[] zones = Array.Empty<ZoneLifeRule>();

        [Min(0.1f)]
        public float populationTickSeconds = 1;

        [Min(1)]
        public int maxArrivalsPerTick = 2;

        [Min(0.1f)]
        public float trafficDecisionSeconds = 0.25f;

        [Min(0)]
        public float offscreenMargin = 2;

        public ZoneLifeRule Zone(string id)
        {
            foreach (var rule in zones)
            {
                if (rule.zoneId == id)
                {
                    return rule;
                }
            }

            return null;
        }

        public int AmbientLimit(string zone, int hour)
        {
            var rule = Zone(zone);
            return rule == null ? 0 : Mathf.Min(rule.ambientCapacity, rule.AtHour(hour)?.ambientResidents ?? 0);
        }

        public int TrafficLimit(string zone, int hour, int fallback)
        {
            return Zone(zone)?.AtHour(hour)?.vehicles ?? fallback;
        }
    }
}
