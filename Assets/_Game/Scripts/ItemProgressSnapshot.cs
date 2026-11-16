using System;
using System.Collections.Generic;
namespace IanGame {
[Serializable] public sealed class InventoryEntry {public string itemId;public int count;public bool isNew;public long acquired;public int firstDay;}
[Serializable] public sealed class EquipmentEntry {public EquipmentSlot slot;public string itemId;}
[Serializable] public sealed class PityProgress {public string ruleId;public long progress,reached,applied;}
[Serializable] public sealed class PoolProgress {public string poolId;public long total;public List<PityProgress> pity=new List<PityProgress>();}
[Serializable] public sealed class ItemProgressSnapshot {
 public int schema=1;public bool unlocked;public List<InventoryEntry> inventory=new List<InventoryEntry>();public List<EquipmentEntry> equipment=new List<EquipmentEntry>();public List<PoolProgress> pools=new List<PoolProgress>();public List<string> receipts=new List<string>();
}
public sealed class ItemEvent {public string transactionId,itemId,kind,poolId,ruleId;public int amount;public bool isNew,duplicate;public EquipmentSlot slot;}
public sealed class GachaResult {public string itemId,rarityId;public bool isNew,duplicate;public string[] pityRules;}
public sealed class GachaBatch {public string transactionId,poolId;public int tickets,cash,material;public GachaResult[] results;}
public sealed class GachaQuote {public int count,tickets,cash,material;public string reason;public bool CanPull=>reason==null;}
}
