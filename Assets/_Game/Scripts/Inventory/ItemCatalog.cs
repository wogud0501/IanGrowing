using System;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public enum ItemType
    {
        Headphone,
        Equipment,
        Drink,
        Cosmetic,
        Material,
        Ticket,
    }

    public enum EquipmentSlot
    {
        Headphone,
        Equipment,
        Cosmetic,
    }

    public enum ModifierOperation
    {
        Add,
        Percent,
    }

    public enum ModifierTarget
    {
        Hunger,
        Thirst,
        Knowledge,
        Health,
        Mental,
        Social,
        Credit,
        Happiness,
        StudyEfficiency,
        WorkEfficiency,
        IdleEfficiency,
        CombatAttack,
        CombatDefense,
        RewardEfficiency,
    }

    [Serializable]
    public class StatModifier
    {
        public ModifierTarget stat;
        public ModifierOperation operation;
        public float value;
    }

    [Serializable]
    public class RarityDefinition
    {
        public string id;
        public string displayName;
        public int rank;
        public Color color = Color.white;
        public int dismantleMultiplier = 1;
        public string revealEffectId;
    }

    [Serializable]
    public class ItemDefinition
    {
        public string itemId;
        public string displayName;
        public string description;
        public string rarityId;
        public string appearanceId;
        public ItemType itemType;
        public Sprite icon;
        public Sprite appearanceSprite;
        public bool stackable = true;
        public bool isConsumable;
        public bool isCosmetic;
        public bool dismantlable = true;
        public bool protectLast = true;
        public int maxStack = 9999;
        public int sellValue;
        public int dismantleValue = 1;
        public int minAge;
        public ItemAppearanceFrame[] appearanceFrames = Array.Empty<ItemAppearanceFrame>();
        public EquipmentSlot equipmentSlot;
        public StatModifier[] statModifiers = Array.Empty<StatModifier>();
        public StatDelta recovery;
        public string[] tags = Array.Empty<string>();
        public bool Wearable =>
            itemType == ItemType.Headphone || itemType == ItemType.Equipment || itemType == ItemType.Cosmetic;
    }

    [Serializable]
    public class RarityWeight
    {
        public string rarityId;
        public int weight;
    }

    [Serializable]
    public class PoolItem
    {
        public string itemId;
        public int weight = 1;
    }

    public enum PityReset
    {
        Threshold,
        MatchingDrop,
        Never,
    }

    [Serializable]
    public class PityRule
    {
        public string id;
        public int threshold;
        public int priority;
        public string minimumRarityId;
        public string guaranteedItem;
        public PityReset reset;
    }

    [Serializable]
    public class GachaPoolDefinition
    {
        public string poolId;
        public string displayName;
        public string description;
        public string ticketItemId = "gacha-ticket";
        public string materialItemId = "collection-scrap";
        public GachaCurrency costCurrency;
        public int costAmount = 100;
        public int materialCost = 30;
        public int minAge = 13;
        public bool ticketFirst = true;
        public PoolItem[] availableItems = Array.Empty<PoolItem>();
        public RarityWeight[] rarityWeights = Array.Empty<RarityWeight>();
        public PityRule[] pityRules = Array.Empty<PityRule>();
    }

    [CreateAssetMenu(menuName = "Ian/Collection catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        public int schema = 1;
        public int unlockAge = 13;
        public string starterHeadphone = "starter-headphones";
        public string starterEquipment = "starter-equipment";
        public ItemDefinition[] items = Array.Empty<ItemDefinition>();
        public RarityDefinition[] rarities = Array.Empty<RarityDefinition>();
        public GachaPoolDefinition[] pools = Array.Empty<GachaPoolDefinition>();

        public ItemDefinition Item(string id) => items.FirstOrDefault(i => i.itemId == id);

        public RarityDefinition Rarity(string id) => rarities.FirstOrDefault(r => r.id == id);

        public GachaPoolDefinition Pool(string id) => pools.FirstOrDefault(p => p.poolId == id);

        public string Validate()
        {
            if (
                schema != 1
                || items == null
                || rarities == null
                || pools == null
                || items.Length == 0
                || items.Select(i => i?.itemId).Distinct().Count() != items.Length
                || rarities.Select(r => r?.id).Distinct().Count() != rarities.Length
                || pools.Select(p => p?.poolId).Distinct().Count() != pools.Length
            )
            {
                return "카탈로그 ID/구조 오류";
            }

            foreach (var r in rarities)
            {
                if (r == null || string.IsNullOrEmpty(r.id) || r.rank < 0 || r.dismantleMultiplier < 0)
                {
                    return "등급 데이터 오류";
                }
            }

            foreach (var i in items)
            {
                if (
                    i == null
                    || string.IsNullOrEmpty(i.itemId)
                    || Rarity(i.rarityId) == null
                    || i.maxStack < 1
                    || i.maxStack > 1000000
                    || i.minAge < 0
                    || i.dismantleValue < 0
                    || i.statModifiers == null
                    || i.statModifiers.Any(m =>
                        m == null
                        || !Enum.IsDefined(typeof(ModifierTarget), m.stat)
                        || !float.IsFinite(m.value)
                        || Math.Abs(m.value) > 100
                    )
                )
                {
                    return "아이템 데이터 오류";
                }
            }

            foreach (var p in pools)
            {
                if (
                    p == null
                    || !Enum.IsDefined(typeof(GachaCurrency), p.costCurrency)
                    || p.costAmount < 0
                    || p.materialCost < 1
                    || p.availableItems.Length == 0
                    || p.rarityWeights.Length == 0
                    || p.availableItems.Any(i => i.weight <= 0 || Item(i.itemId) == null)
                    || p.availableItems.Select(i => i.itemId).Distinct().Count() != p.availableItems.Length
                    || p.rarityWeights.Any(r =>
                        r.weight <= 0
                        || Rarity(r.rarityId) == null
                        || !p.availableItems.Any(i => Item(i.itemId).rarityId == r.rarityId)
                    )
                    || p.rarityWeights.Select(r => r.rarityId).Distinct().Count() != p.rarityWeights.Length
                )
                {
                    return "추첨 Pool 데이터 오류";
                }

                if (
                    p.pityRules.Any(r =>
                        r == null
                        || string.IsNullOrEmpty(r.id)
                        || r.threshold < 1
                        || !Enum.IsDefined(typeof(PityReset), r.reset)
                        || !string.IsNullOrEmpty(r.minimumRarityId) && Rarity(r.minimumRarityId) == null
                        || !string.IsNullOrEmpty(r.guaranteedItem)
                            && !p.availableItems.Any(i => i.itemId == r.guaranteedItem)
                    )
                    || p.pityRules.Select(r => r.id).Distinct().Count() != p.pityRules.Length
                )
                {
                    return "천장 규칙 오류";
                }
            }

            return null;
        }
    }
}
