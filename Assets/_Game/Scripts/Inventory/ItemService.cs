using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    // One model and transaction owner. The inventory/equipment/gacha facades below
    // expose this model; they never own duplicate wallets or cloned definitions.
    public sealed class ItemService : MonoBehaviour, IMissionFeatureSource, IPendingMissionRewardGateway
    {
        public string LastFailure { get; private set; }

        public ItemCatalog catalog;
        public GameManager Game { get; private set; }

        ItemProgressSnapshot state = new ItemProgressSnapshot();
        bool busy;
        bool pending;
        bool unlockGuide;
        float retry;
        readonly List<ItemEvent> events = new List<ItemEvent>();
        public InventoryService Inventory { get; private set; }
        public EquipmentService Equipment { get; private set; }
        public GachaService Gacha { get; private set; }
        public MissionFeature Features => MissionFeature.Gacha | MissionFeature.Equipment;
        public bool IsAvailable => catalog != null;
        public bool Unlocked => Game != null && catalog != null && Game.growth.Age >= catalog.unlockAge;

        public event Action<MissionActivityEvent> ActivityProduced;
        public event Action Changed;
        public event Action<ItemEvent> ItemConsumed;
        public event Action<ItemEvent> ItemAcquired;
        public event Action<ItemEvent> NewItemDiscovered;
        public event Action<ItemEvent> ItemEquipped;
        public event Action<ItemEvent> ItemUnequipped;
        public event Action<ItemEvent> DuplicateAcquired;
        public event Action<ItemEvent> ItemDismantled;
        public event Action<ItemEvent> PityReached;
        public event Action<ItemEvent> CollectionUpdated;
        public event Action<GachaBatch> GachaPulled;
        public event Action<GachaBatch> GachaMultiPulled;
        public event Action<string> AudioRequested;
        public event Action<string, string> AppearanceChanged;
        internal ItemProgressSnapshot Model => state;

        public void Configure(GameManager game)
        {
            Game = game;
            if (catalog == null)
            {
                catalog = Resources.Load<ItemCatalog>("IanCollection");
            }

            Inventory = new InventoryService(this);
            Equipment = new EquipmentService(this);
            Gacha = new GachaService(this);
            game.missions.RegisterSource(this);
            game.missions.RewardClaimed += Reward;
            game.growth.Changed += AgeChanged;
        }

        void OnDestroy()
        {
            if (Game == null)
            {
                return;
            }

            Game.missions.UnregisterSource(this);
            Game.missions.RewardClaimed -= Reward;
            Game.growth.Changed -= AgeChanged;
        }

        void AgeChanged()
        {
            Equipment?.RefreshModifiers();
            if (Unlocked && !state.unlocked)
            {
                state.unlocked = true;
                pending = true;
                unlockGuide = true;
            }

            Changed?.Invoke();
        }

        void Reward(MissionData unused)
        {
            pending = true;
        }

        void LateUpdate()
        {
            if (
                unlockGuide
                && Game != null
                && Game.State == GameState.Playing
                && Unlocked
                && !Game.BirthIntroductionPending
            )
            {
                unlockGuide = false;
                if (!Game.world.City.Seen("items/unlocked"))
                {
                    Game.world.City.MarkGuide("items/unlocked");
                    Game.Log("수집 기능이 열렸습니다. 가방에서 장비와 가챠를 살펴보세요.");
                }
            }

            if (
                !pending
                || Time.unscaledTime < retry
                || Game == null
                || Game.State != GameState.Playing
                || Game.BirthIntroductionPending
                || Game.world.Busy
                || Game.missions.IsBusy
            )
            {
                return;
            }

            if (!DeliverPending())
            {
                retry = Time.unscaledTime + 5;
            }
        }

        internal bool CanUse(out string reason)
        {
            reason =
                catalog == null ? "아이템 데이터가 연결되지 않았습니다."
                : Game.State != GameState.Playing || Game.BirthIntroductionPending ? "정상 플레이 상태에서 이용하세요."
                : !Unlocked ? catalog.unlockAge + "세부터 수집 기능을 이용할 수 있습니다."
                : GameContext.Instance != null && GameContext.Instance.transitions.IsTransitioning
                || Game.world.Busy
                || Game.idle.State.active
                    ? "현재 행동 또는 이동을 먼저 마쳐 주세요."
                : null;
            return reason == null;
        }

        internal bool Transaction(string receipt, Func<bool> mutation, out string reason, bool requireUnlock = true)
        {
            reason = null;
            if (
                busy
                || string.IsNullOrWhiteSpace(receipt)
                || receipt.Length > 160
                || state.receipts.Contains(receipt)
                || state.receipts.Count >= 65536
            )
            {
                reason = "처리 중이거나 이미 처리된 요청입니다.";
                return false;
            }

            if (requireUnlock && !CanUse(out reason))
            {
                return false;
            }

            if (Game.State != GameState.Playing || Game.missions.IsBusy)
            {
                reason = "준비 또는 보상 처리 중입니다.";
                return false;
            }

            var before = Game.CaptureState();
            busy = true;
            events.Clear();
            bool committed = false;
            try
            {
                if (!mutation())
                {
                    reason = "아이템 조건을 확인하세요.";
                    Game.RestoreState(before);
                    return false;
                }

                state.receipts.Add(receipt);
                state.unlocked |= Unlocked;
                Equipment.RefreshModifiers();
                SyncAchievements(receipt);
                if (!Game.SaveAutomaticCheckpoint(out reason))
                {
#if UNITY_EDITOR
                    if (
                        System.IO.Path.GetFileName(Game.saves.BasePath).StartsWith("phase26-")
                        || System.IO.Path.GetFileName(Game.saves.BasePath).StartsWith("qa-")
                    )
                    {
                        System.IO.File.WriteAllText(
                            "Docs/Phase29/FAILED_ITEM_TRANSACTION.json",
                            JsonUtility.ToJson(Game.CaptureState(), true)
                        );
                    }

#endif
                    Game.RestoreState(before);
                    return false;
                }

                committed = true;
            }
            catch (Exception e)
            {
                Game.RestoreState(before);
                reason = "아이템 처리를 취소했습니다: " + e.Message;
                return false;
            }
            finally
            {
                busy = false;
                if (!committed)
                {
                    events.Clear();
                }
            }

            var published = events.ToArray();
            events.Clear();
            Changed?.Invoke();
            foreach (var e in published)
            {
                Publish(e);
            }

            return true;
        }

        internal bool Add(string id, int amount, string receipt, bool migration = false)
        {
            var def = catalog.Item(id);
            if (def == null || amount <= 0)
            {
                return false;
            }

            var entry = state.inventory.FirstOrDefault(i => i.itemId == id);
            bool first = entry == null;
            if (entry == null)
            {
                entry = new InventoryEntry
                {
                    itemId = id,
                    firstDay = migration ? 0 : Game.clock.Day,
                    acquired = migration ? 0 : DateTime.UtcNow.Ticks,
                };
                state.inventory.Add(entry);
            }

            if ((long)entry.count + amount > def.maxStack)
            {
                return false;
            }

            bool duplicate = !first;
            entry.count += amount;
            entry.isNew |= first;
            if (!migration)
            {
                events.Add(
                    new ItemEvent
                    {
                        kind = "acquired",
                        itemId = id,
                        amount = amount,
                        transactionId = receipt,
                        isNew = first,
                        duplicate = duplicate,
                    }
                );
            }

            return true;
        }

        internal void Queue(
            string kind,
            string id,
            string receipt,
            int amount = 1,
            EquipmentSlot slot = EquipmentSlot.Equipment
        )
        {
            events.Add(
                new ItemEvent
                {
                    kind = kind,
                    itemId = id,
                    transactionId = receipt,
                    amount = amount,
                    slot = slot,
                }
            );
        }

        internal void QueuePity(ItemEvent value)
        {
            events.Add(value);
        }

        internal void Signal(string receipt, MissionCounter counter, long amount = 1)
        {
            ActivityProduced?.Invoke(
                new MissionActivityEvent
                {
                    id = receipt,
                    counter = counter,
                    amount = amount,
                }
            );
        }

        void Publish(ItemEvent e)
        {
            try
            {
                switch (e.kind)
                {
                    case "acquired":
                        ItemAcquired?.Invoke(e);
                        CollectionUpdated?.Invoke(e);
                        if (e.isNew)
                        {
                            NewItemDiscovered?.Invoke(e);
                            AudioRequested?.Invoke("NewItem");
                        }

                        if (e.duplicate)
                        {
                            DuplicateAcquired?.Invoke(e);
                            AudioRequested?.Invoke("Duplicate");
                            if (!Game.world.City.Seen("items/duplicate"))
                            {
                                Game.world.City.MarkGuide("items/duplicate");
                                Game.Log("이미 발견한 아이템입니다. 가방에서 중복을 분해할 수 있습니다.");
                            }
                        }

                        break;
                    case "consume":
                        ItemConsumed?.Invoke(e);
                        break;
                    case "equip":
                        ItemEquipped?.Invoke(e);
                        AudioRequested?.Invoke("Equip");
                        NotifyAppearance();
                        break;
                    case "unequip":
                        ItemUnequipped?.Invoke(e);
                        AudioRequested?.Invoke("Unequip");
                        NotifyAppearance();
                        break;
                    case "dismantle":
                        ItemDismantled?.Invoke(e);
                        AudioRequested?.Invoke("Dismantle");
                        break;
                    case "pity":
                        PityReached?.Invoke(e);
                        AudioRequested?.Invoke("PityReached");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Item subscriber: " + ex.Message);
            }
        }

        internal void PublishBatch(GachaBatch batch)
        {
            try
            {
                AudioRequested?.Invoke("GachaStart");
                if (batch.results.Length == 1)
                {
                    GachaPulled?.Invoke(batch);
                }
                else
                {
                    GachaMultiPulled?.Invoke(batch);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Gacha subscriber: " + ex.Message);
            }
        }

        public void Reveal(GachaResult result)
        {
            AudioRequested?.Invoke("GachaReveal");
            if (catalog.Rarity(result.rarityId).rank >= 3)
            {
                AudioRequested?.Invoke("RareReveal");
            }
        }

        void NotifyAppearance()
        {
            AppearanceChanged?.Invoke(
                catalog.Item(Equipment.Current(EquipmentSlot.Headphone))?.appearanceId,
                catalog.Item(Equipment.Current(EquipmentSlot.Cosmetic))?.appearanceId
            );
        }

        public bool HasItem(MissionItemKey key) => Inventory != null && Inventory.Count(LegacyId(key)) > 0;

        public string LegacyId(MissionItemKey key) =>
            key == MissionItemKey.StarterHeadphones ? catalog?.starterHeadphone
            : key == MissionItemKey.StarterEquipment ? catalog?.starterEquipment
            : null;

        bool DeliverRaw(PendingMissionReward reward)
        {
            string id = reward.kind == PendingMissionRewardKind.GachaTicket ? "gacha-ticket" : LegacyId(reward.item);
            if (id == null || reward.amount > 1000000)
            {
                return false;
            }

            string receipt = "delivery/" + reward.id;
            if (state.receipts.Contains(receipt))
            {
                return true;
            }

            if (!Add(id, (int)reward.amount, receipt))
            {
                return false;
            }

            state.receipts.Add(receipt);
            return true;
        }

        public bool TryDeliver(PendingMissionReward reward)
        {
            if (reward == null || reward.amount < 1)
            {
                return false;
            }

            if (busy)
            {
                return DeliverRaw(reward);
            }

            return Transaction("reward/" + reward.id, () => DeliverRaw(reward), out _, false);
        }

        bool StarterEntitlements(bool migration = false)
        {
            if (!Game.missions.Claimed("M020"))
            {
                return false;
            }

            bool changed = false;
            foreach (var id in new[] { catalog.starterHeadphone, catalog.starterEquipment })
            {
                string receipt = "starter-entitlement/" + id;
                if (state.receipts.Contains(receipt))
                {
                    continue;
                }

                if (Inventory.Count(id) == 0 && !Add(id, 1, receipt, migration))
                {
                    continue;
                }

                state.receipts.Add(receipt);
                changed = true;
            }

            return changed;
        }

        bool MissingStarterEntitlements() =>
            catalog != null
            && Game.missions.Claimed("M020")
            && (
                !state.receipts.Contains("starter-entitlement/" + catalog.starterHeadphone)
                || !state.receipts.Contains("starter-entitlement/" + catalog.starterEquipment)
            );

        bool CanDeliver(PendingMissionReward reward) =>
            !reward.delivered && (reward.kind == PendingMissionRewardKind.GachaTicket || LegacyId(reward.item) != null);

        public bool DeliverPending()
        {
            if (busy || catalog == null)
            {
                return false;
            }

            if (!Game.missions.State.pendingRewards.Any(CanDeliver) && !MissingStarterEntitlements())
            {
                pending = false;
                LastFailure = null;
                return true;
            }

            bool result = Transaction(
                "pending/" + Guid.NewGuid().ToString("N"),
                () =>
                {
                    StarterEntitlements();
                    foreach (var reward in Game.missions.State.pendingRewards.Where(CanDeliver).ToArray())
                    {
                        if (!Game.missions.DeliverPending(reward.id, this))
                        {
                            return false;
                        }
                    }

                    return !MissingStarterEntitlements();
                },
                out var failure,
                false
            );
            LastFailure = failure;
            pending = !result;
            return result;
        }

        public ItemProgressSnapshot Capture() => JsonUtility.FromJson<ItemProgressSnapshot>(JsonUtility.ToJson(state));

        public void ResetLife()
        {
            state = new ItemProgressSnapshot();
            pending = false;
            events.Clear();
            Equipment?.RefreshModifiers();
            Changed?.Invoke();
            NotifyAppearance();
        }

        public void Restore(ItemProgressSnapshot value)
        {
            Gacha?.ClearResult();
            state =
                value == null
                    ? new ItemProgressSnapshot()
                    : JsonUtility.FromJson<ItemProgressSnapshot>(JsonUtility.ToJson(value));
            events.Clear();
            if (value == null && catalog != null)
            {
                foreach (var key in Game.phase23.Progress.owned)
                {
                    var id = LegacyId(key);
                    if (id != null && Inventory.Count(id) == 0)
                    {
                        Add(id, 1, "legacy", true);
                    }
                }

                foreach (var key in Game.phase23.Progress.equipped)
                {
                    var id = LegacyId(key);
                    var item = catalog.Item(id);
                    if (
                        item != null
                        && Inventory.Count(id) > 0
                        && !state.equipment.Any(e => e.slot == item.equipmentSlot)
                    )
                    {
                        state.equipment.Add(new EquipmentEntry { slot = item.equipmentSlot, itemId = id });
                    }
                }

                StarterEntitlements(true);
            }

            state.unlocked |= Unlocked;
            unlockGuide = Unlocked;
            Equipment.RefreshModifiers();
            retry = 0;
            pending = MissingStarterEntitlements() || Game.missions.State.pendingRewards.Any(CanDeliver);
            Changed?.Invoke();
            NotifyAppearance();
        }

        internal void SyncLegacy()
        {
            var p = Game.phase23.Progress;
            p.owned.RemoveAll(k => k == MissionItemKey.StarterEquipment || k == MissionItemKey.StarterHeadphones);
            p.equipped.RemoveAll(k => k == MissionItemKey.StarterEquipment || k == MissionItemKey.StarterHeadphones);
            foreach (var key in new[] { MissionItemKey.StarterHeadphones, MissionItemKey.StarterEquipment })
            {
                var id = LegacyId(key);
                if (Inventory.Count(id) > 0)
                {
                    p.owned.Add(key);
                }

                if (state.equipment.Any(e => e.itemId == id))
                {
                    p.equipped.Add(key);
                }
            }
        }

        internal IEnumerable<StatModifier> Modifiers =>
            state
                .equipment.Select(e => catalog.Item(e.itemId))
                .Where(i => i != null && Game.growth.Age >= i.minAge)
                .SelectMany(i => i.statModifiers);

        public float Apply(ModifierTarget target, float basis)
        {
            var mods = Modifiers.Where(m => m.stat == target);
            float add = mods.Where(m => m.operation == ModifierOperation.Add).Sum(m => m.value),
                percent = mods.Where(m => m.operation == ModifierOperation.Percent).Sum(m => m.value);
            return Mathf.Clamp(basis * (1 + percent / 100) + add, 0, 1000000);
        }

        public float Multiplier(ModifierTarget target) => Mathf.Clamp(Apply(target, 1), .1f, 5);

        void SyncAchievements(string receipt)
        {
            var city = Game.world.City;
            if (city == null || !Unlocked)
            {
                return;
            }

            void Set(string id, long value)
            {
                long prior = city.Counter(id);
                if (value > prior)
                {
                    city.Record(id, value - prior, receipt + "/" + id);
                }
            }

            Set(
                "item/collection10",
                state.inventory.Count(i =>
                    catalog.Item(i.itemId)?.itemType != ItemType.Material
                    && catalog.Item(i.itemId)?.itemType != ItemType.Ticket
                )
            );
            Set("item/headphones5", state.inventory.Count(i => catalog.Item(i.itemId)?.itemType == ItemType.Headphone));
            Set("item/headphone", state.inventory.Count(i => catalog.Item(i.itemId)?.itemType == ItemType.Headphone));
            Set("item/equipment", state.inventory.Count(i => catalog.Item(i.itemId)?.itemType == ItemType.Equipment));
            long draws = state.pools.Sum(p => p.total);
            Set("item/pull", draws);
            Set("item/pull50", draws);
            Set("item/pull100", draws);
            Set("item/pull300", draws);
            foreach (var e in events)
            {
                if (e.kind == "equip" && e.slot == EquipmentSlot.Headphone)
                {
                    Set("item/equip-headphone", 1);
                }

                if (e.kind == "acquired" && e.duplicate)
                {
                    Set("item/duplicate", 1);
                }

                if (e.kind == "dismantle")
                {
                    Set("item/dismantle", 1);
                }
            }
        }

        public static bool Valid(ItemProgressSnapshot s)
        {
            if (s == null)
            {
                return true;
            }

            if (
                s.schema != 1
                || s.inventory == null
                || s.equipment == null
                || s.pools == null
                || s.receipts == null
                || s.inventory.Count > 4096
                || s.equipment.Count > 3
                || s.pools.Count > 64
                || s.receipts.Count > 65536
            )
            {
                return false;
            }

            bool Id(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 160;
            return s.inventory.All(i =>
                    i != null
                    && Id(i.itemId)
                    && i.count >= 0
                    && i.count <= 1000000
                    && i.firstDay >= 0
                    && i.acquired >= 0
                )
                && s.inventory.Select(i => i.itemId).Distinct().Count() == s.inventory.Count
                && s.equipment.All(e =>
                    e != null
                    && Id(e.itemId)
                    && Enum.IsDefined(typeof(EquipmentSlot), e.slot)
                    && s.inventory.Any(i => i.itemId == e.itemId && i.count > 0)
                )
                && s.equipment.Select(e => e.slot).Distinct().Count() == s.equipment.Count
                && s.receipts.All(Id)
                && s.receipts.Distinct().Count() == s.receipts.Count
                && s.pools.All(p =>
                    p != null
                    && Id(p.poolId)
                    && p.total >= 0
                    && p.total <= 1000000000
                    && p.pity != null
                    && p.pity.Count <= 64
                    && p.pity.All(r =>
                        r != null
                        && Id(r.ruleId)
                        && r.progress >= 0
                        && r.reached >= 0
                        && r.applied >= 0
                        && r.applied <= r.reached
                    )
                    && p.pity.Select(r => r.ruleId).Distinct().Count() == p.pity.Count
                )
                && s.pools.Select(p => p.poolId).Distinct().Count() == s.pools.Count;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugGive(string id, int count)
        {
            if (
                !System.IO.Path.GetFileName(Game.saves.BasePath).StartsWith("qa-")
                && !System.IO.Path.GetFileName(Game.saves.BasePath).StartsWith("phase26-")
            )
            {
                return false;
            }

            return Transaction("debug/" + Guid.NewGuid().ToString("N"), () => Add(id, count, "debug"), out _, false);
        }
#endif
    }

    public sealed class InventoryService
    {
        readonly ItemService owner;

        internal InventoryService(ItemService s)
        {
            owner = s;
        }

        public InventoryEntry[] Entries =>
            owner
                .Model.inventory.Select(e => new InventoryEntry
                {
                    itemId = e.itemId,
                    count = e.count,
                    isNew = e.isNew,
                    acquired = e.acquired,
                    firstDay = e.firstDay,
                })
                .ToArray();

        public int Count(string id) => owner.Model.inventory.FirstOrDefault(i => i.itemId == id)?.count ?? 0;

        public bool Discover(string id) => owner.Model.inventory.Any(i => i.itemId == id);

        public bool MarkSeen(string id, out string reason) =>
            owner.Transaction(
                "seen/" + Guid.NewGuid().ToString("N"),
                () =>
                {
                    var e = owner.Model.inventory.FirstOrDefault(i => i.itemId == id);
                    if (e == null || !e.isNew)
                    {
                        return false;
                    }

                    e.isNew = false;
                    return true;
                },
                out reason,
                false
            );

        public bool Use(string receipt, string id, out string reason)
        {
            if (!owner.CanUse(out reason))
            {
                return false;
            }

            var d = owner.catalog.Item(id);
            if (d == null || !d.isConsumable || owner.Game.growth.Age < d.minAge || Count(id) < 1)
            {
                reason = "현재 사용할 수 없는 아이템입니다.";
                return false;
            }

            return owner.Transaction(
                receipt,
                () =>
                {
                    owner.Model.inventory.First(i => i.itemId == id).count--;
                    owner.Game.stats.ApplyCore(d.recovery);
                    owner.Queue("consume", id, receipt);
                    owner.Signal(receipt + "/drink", MissionCounter.Drink);
                    return true;
                },
                out reason
            );
        }

        public int DismantleValue(string id)
        {
            var d = owner.catalog.Item(id);
            return d == null ? 0 : d.dismantleValue * owner.catalog.Rarity(d.rarityId).dismantleMultiplier;
        }

        public bool Dismantle(string receipt, string id, int amount, out string reason)
        {
            var d = owner.catalog.Item(id);
            reason =
                d == null || !d.dismantlable || amount < 1 ? "분해할 수 없는 아이템입니다."
                : owner.Model.equipment.Any(e => e.itemId == id) ? "장착을 해제한 뒤 분해하세요."
                : Count(id) - amount < (d.protectLast ? 1 : 0) ? "마지막 한 개는 보관합니다."
                : null;
            if (reason != null)
            {
                return false;
            }

            long reward = (long)DismantleValue(id) * amount;
            if (reward > 1000000)
            {
                reason = "분해 수량 한도";
                return false;
            }

            return owner.Transaction(
                receipt,
                () =>
                {
                    owner.Model.inventory.First(i => i.itemId == id).count -= amount;
                    if (!owner.Add("collection-scrap", (int)reward, receipt + "/material"))
                    {
                        return false;
                    }

                    owner.Queue("dismantle", id, receipt, amount);
                    return true;
                },
                out reason
            );
        }
    }

    public sealed class EquipmentService
    {
        readonly ItemService owner;

        internal EquipmentService(ItemService s)
        {
            owner = s;
        }

        public EquipmentEntry[] Entries =>
            owner.Model.equipment.Select(e => new EquipmentEntry { slot = e.slot, itemId = e.itemId }).ToArray();

        public string Current(EquipmentSlot slot) => owner.Model.equipment.FirstOrDefault(e => e.slot == slot)?.itemId;

        public bool CanEquip(string id, out string reason)
        {
            var d = owner.catalog?.Item(id);
            if (!owner.CanUse(out reason))
            {
                return false;
            }

            reason =
                d == null || !d.Wearable || owner.Inventory.Count(id) < 1 ? "보유 장비를 선택하세요."
                : owner.Game.growth.Age < d.minAge ? d.minAge + "세부터 사용할 수 있습니다."
                : Current(d.equipmentSlot) == id ? "이미 장착했습니다."
                : null;
            return reason == null;
        }

        public bool Equip(string receipt, string id, out string reason)
        {
            if (!CanEquip(id, out reason))
            {
                return false;
            }

            var d = owner.catalog.Item(id);
            return owner.Transaction(
                receipt,
                () =>
                {
                    var old = Current(d.equipmentSlot);
                    owner.Model.equipment.RemoveAll(e => e.slot == d.equipmentSlot);
                    if (old != null)
                    {
                        owner.Queue("unequip", old, receipt, 1, d.equipmentSlot);
                    }

                    owner.Model.equipment.Add(new EquipmentEntry { slot = d.equipmentSlot, itemId = id });
                    owner.SyncLegacy();
                    owner.Queue("equip", id, receipt, 1, d.equipmentSlot);
                    if (d.equipmentSlot != EquipmentSlot.Cosmetic)
                    {
                        owner.Signal(
                            receipt + "/mission",
                            d.equipmentSlot == EquipmentSlot.Headphone
                                ? MissionCounter.HeadphonesEquipped
                                : MissionCounter.EquipmentEquipped
                        );
                    }

                    return true;
                },
                out reason
            );
        }

        public bool Unequip(string receipt, EquipmentSlot slot, out string reason)
        {
            string id = Current(slot);
            return owner.Transaction(
                receipt,
                () =>
                {
                    if (id == null)
                    {
                        return false;
                    }

                    owner.Model.equipment.RemoveAll(e => e.slot == slot);
                    owner.SyncLegacy();
                    owner.Queue("unequip", id, receipt, 1, slot);
                    return true;
                },
                out reason
            );
        }

        internal void RefreshModifiers()
        {
            if (owner.catalog == null)
            {
                return;
            }

            owner.Game.stats.SetItemModifiers(owner.Modifiers);
        }
    }
}
