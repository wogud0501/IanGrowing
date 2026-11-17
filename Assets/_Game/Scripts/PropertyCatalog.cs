using System;
using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    public enum PropertyType
    {
        Goshiwon,
        Studio,
        Villa,
        Officetel,
        Apartment,
        LuxuryApartment,
    }

    public enum HousingTenure
    {
        Jeonse,
        MonthlyRent,
    }

    public enum PropertyOccupancy
    {
        Vacant,
        OwnerOccupied,
        Leased,
    }

    [Serializable]
    public class PropertyDefinition
    {
        public string PropertyId;
        public string PropertyName;
        public string Location;
        public string residenceId = "starter";
        public PropertyType PropertyType;
        public int PurchasePrice;
        public int JeonseDeposit;
        public int MonthlyDeposit;
        public int MonthlyRent;
        public int MaintenanceCost;
        public int minAge = 19;

        [Range(0, .1f)]
        public float volatility = .015f;

        [Range(-.03f, .03f)]
        public float regionalTrend;

        [Range(0, 2)]
        public float newsSensitivity = 1;
        public StatDelta sleepBonus;
        public StatDelta restBonus;
        public Sprite preview;
    }

    [CreateAssetMenu(menuName = "Ian/Property catalog")]
    public sealed class PropertyCatalog : ScriptableObject
    {
        public PropertyDefinition[] properties = Array.Empty<PropertyDefinition>();

        [Min(1)]
        public int paymentPeriodDays = 30;

        [Min(1)]
        public int contractPeriods = 3;

        [Min(1)]
        public int marketIntervalSeconds = 300;

        [Min(1)]
        public int historyLimit = 60;

        [Min(1)]
        public int maximumCatchupTicks = 288;
        public int minimumLoanCredit = 40;
        public int baseLoanLimit = 200000;
        public int graceDays = 3;

        [Range(0, 2000)]
        public int marketInterestBasisPoints = 100;

        public PropertyDefinition Find(string id) => Array.Find(properties, p => p.PropertyId == id);
    }

    [Serializable]
    public class PropertyQuote
    {
        public string PropertyId;
        public int MarketPrice;
        public int PreviousPrice;
        public List<int> PriceHistory = new List<int>();
    }

    [Serializable]
    public class PropertyHolding
    {
        public string PropertyId;
        public long costBasis;
        public int acquiredDay;
        public int nextMaintenanceDay;
    }

    [Serializable]
    public class HousingContract
    {
        public string id;
        public string PropertyId;
        public HousingTenure tenure;
        public long deposit;
        public int monthlyRent;
        public int maintenance;
        public int startDay;
        public int endDay;
        public int nextPaymentDay;
    }

    [Serializable]
    public class PropertyBill
    {
        public string id;
        public string PropertyId;
        public string contractId;
        public int dueDay;
        public int periods = 1;
        public long rent;
        public long maintenance;
        public long paid;
        public bool lateCreditApplied;
        public long Remaining => rent + maintenance - paid;
    }

    [Serializable]
    public class HousingLoan
    {
        public string id;
        public long originalPrincipal;
        public long remainingPrincipal;
        public int day;
    }

    [Serializable]
    public class PropertySnapshot
    {
        public int schema = 1;
        public int lastSettledDay;
        public long marketUtcTicks;
        public long marketSequence;
        public long realizedPnL;
        public string occupiedPropertyId = "";
        public List<PropertyQuote> quotes = new List<PropertyQuote>();
        public List<PropertyHolding> owned = new List<PropertyHolding>();
        public HousingContract contract;
        public List<PropertyBill> bills = new List<PropertyBill>();
        public List<HousingLoan> loans = new List<HousingLoan>();
    }
}
