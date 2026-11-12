using System;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
public enum PropertyType {Goshiwon,Studio,Villa,Officetel,Apartment,LuxuryApartment}
public enum HousingTenure {Jeonse,MonthlyRent}
public enum PropertyOccupancy {Vacant,OwnerOccupied,Leased}
[Serializable]public class PropertyDefinition {
 public string PropertyId,PropertyName,Location,residenceId="starter";public PropertyType PropertyType;
 public int PurchasePrice,JeonseDeposit,MonthlyDeposit,MonthlyRent,MaintenanceCost,minAge=19;
 [Range(0,.1f)]public float volatility=.015f;[Range(-.03f,.03f)]public float regionalTrend;[Range(0,2)]public float newsSensitivity=1;
 public StatDelta sleepBonus,restBonus;public Sprite preview;
}
[CreateAssetMenu(menuName="Ian/Property catalog")]
public sealed class PropertyCatalog:ScriptableObject {
 public PropertyDefinition[] properties=Array.Empty<PropertyDefinition>();
 [Min(1)]public int paymentPeriodDays=30,contractPeriods=3,marketIntervalSeconds=300,historyLimit=60,maximumCatchupTicks=288;
 public int minimumLoanCredit=40,baseLoanLimit=200000,graceDays=3;[Range(0,2000)]public int marketInterestBasisPoints=100;
 public PropertyDefinition Find(string id)=>Array.Find(properties,p=>p.PropertyId==id);
}
[Serializable]public class PropertyQuote {public string PropertyId;public int MarketPrice,PreviousPrice;public List<int> PriceHistory=new List<int>();}
[Serializable]public class PropertyHolding {public string PropertyId;public long costBasis;public int acquiredDay,nextMaintenanceDay;}
[Serializable]public class HousingContract {public string id,PropertyId;public HousingTenure tenure;public long deposit;public int monthlyRent,maintenance,startDay,endDay,nextPaymentDay;}
[Serializable]public class PropertyBill {public string id,PropertyId,contractId;public int dueDay,periods=1;public long rent,maintenance,paid;public bool lateCreditApplied;public long Remaining=>rent+maintenance-paid;}
[Serializable]public class HousingLoan {public string id;public long originalPrincipal,remainingPrincipal;public int day;}
[Serializable]public class PropertySnapshot {
 public int schema=1,lastSettledDay;public long marketUtcTicks,marketSequence,realizedPnL;public string occupiedPropertyId="";
 public List<PropertyQuote> quotes=new List<PropertyQuote>();public List<PropertyHolding> owned=new List<PropertyHolding>();
 public HousingContract contract;public List<PropertyBill> bills=new List<PropertyBill>();public List<HousingLoan> loans=new List<HousingLoan>();
}
}
