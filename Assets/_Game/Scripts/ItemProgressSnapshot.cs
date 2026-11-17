using System;
using System.Collections.Generic;

namespace IanGame
{
    [Serializable]
    public sealed class InventoryEntry
    {
        public string itemId;
        public int count;
        public bool isNew;
        public long acquired;
        public int firstDay;
    }

    [Serializable]
    public sealed class EquipmentEntry
    {
        public EquipmentSlot slot;
        public string itemId;
    }

    [Serializable]
    public sealed class PityProgress
    {
        public string ruleId;
        public long progress;
        public long reached;
        public long applied;
    }

    [Serializable]
    public sealed class PoolProgress
    {
        public string poolId;
        public long total;
        public List<PityProgress> pity = new List<PityProgress>();
    }

    [Serializable]
    public sealed class ItemProgressSnapshot
    {
        public int schema = 1;
        public bool unlocked;
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        public List<EquipmentEntry> equipment = new List<EquipmentEntry>();
        public List<PoolProgress> pools = new List<PoolProgress>();
        public List<string> receipts = new List<string>();
    }

    public sealed class ItemEvent
    {
        public string transactionId;
        public string itemId;
        public string kind;
        public string poolId;
        public string ruleId;
        public int amount;
        public bool isNew;
        public bool duplicate;
        public EquipmentSlot slot;
    }

    public sealed class GachaResult
    {
        public string itemId;
        public string rarityId;
        public bool isNew;
        public bool duplicate;
        public string[] pityRules;
    }

    public sealed class GachaBatch
    {
        public string transactionId;
        public string poolId;
        public int tickets;
        public int cash;
        public int material;
        public int ruby;
        public GachaResult[] results;
    }

    public sealed class GachaQuote
    {
        public int count;
        public int tickets;
        public int cash;
        public int material;
        public int ruby;
        public string reason;
        public bool CanPull => reason == null;
    }
}
