using System;
using System.Linq;

namespace IanGame
{
    public static class MissionRewardService
    {
        public static bool TryGrant(
            MissionData mission,
            MissionSnapshot wallet,
            EconomySystem economy,
            LifeProgression growth,
            int xpPerLevel,
            out string error
        )
        {
            error = null;
            var r = mission.rewards;
            string receipt = "mission-reward:" + mission.Id;
            if (r == null || r.gold < 0 || r.experience < 0 || r.growthPoints < 0 || r.gachaTickets < 0)
            {
                error = "Invalid reward data";
                return false;
            }

            bool walletDone = wallet.walletReceiptIds.Contains(receipt);
            if (!economy.HasTransaction(receipt) && r.gold > int.MaxValue - economy.Cash)
            {
                error = "현금 저장 한도에 도달했습니다.";
                return false;
            }

            if (!walletDone && r.growthPoints > long.MaxValue - wallet.growthPoints)
            {
                error = "성장 포인트 한도";
                return false;
            }

            if (
                !economy.HasTransaction(receipt)
                && !economy.TryTransaction(receipt, TransactionKind.Income, r.gold, "미션 " + mission.Id)
            )
            {
                error = "보상 거래 실패";
                return false;
            }

            growth.GrantMissionExperience("mission-xp:" + mission.Id, r.experience, xpPerLevel);
            if (!walletDone)
            {
                wallet.walletReceiptIds.Add(receipt);
                wallet.growthPoints += r.growthPoints;
                if (r.gachaTickets > 0)
                {
                    wallet.pendingRewards.Add(
                        new PendingMissionReward
                        {
                            id = receipt + ":tickets",
                            kind = PendingMissionRewardKind.GachaTicket,
                            amount = r.gachaTickets,
                        }
                    );
                }

                if (r.items != null)
                {
                    for (int i = 0; i < r.items.Length; i++)
                    {
                        var item = r.items[i];
                        wallet.pendingRewards.Add(
                            new PendingMissionReward
                            {
                                id = receipt + ":item:" + i,
                                kind = PendingMissionRewardKind.Item,
                                item = item.item,
                                amount = item.amount,
                            }
                        );
                    }
                }
            }

            if (r.unlocks != null)
            {
                foreach (var unlock in r.unlocks)
                {
                    if (unlock != ContentUnlock.None && !wallet.unlocks.Contains(unlock))
                    {
                        wallet.unlocks.Add(unlock);
                    }
                }
            }

            return true;
        }

        public static bool TryDeliver(MissionSnapshot wallet, string id, IPendingMissionRewardGateway gateway)
        {
            var value = wallet.pendingRewards.FirstOrDefault(r => r.id == id);
            if (value == null || value.delivered || gateway == null)
            {
                return false;
            }

            if (!gateway.TryDeliver(value))
            {
                return false;
            }

            value.delivered = true;
            return true;
        }
    }
}
