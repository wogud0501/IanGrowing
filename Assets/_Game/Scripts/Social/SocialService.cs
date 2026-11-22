using System;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public class SocialService
    {
        readonly Phase23Services owner;

        public SocialService(Phase23Services value)
        {
            owner = value;
        }

        public SocialProgress Progress => owner.Progress.social;

        public RelationshipProgress Relationship(string id) =>
            Progress.relationships.FirstOrDefault(r => r.npcId == id);

        bool FirstIntroduction(string npcId, int action)
        {
            return action is 9 or 18
                && owner.Game.missions?.ActiveMission?.Id == "M014"
                && !Progress.relationships.Any(r => r.friend)
                && owner.catalog.NPC(npcId) != null;
        }

        public string IntroductionHint()
        {
            return IntroductionHint(out _);
        }

        public string IntroductionHint(out string destination)
        {
            destination = null;
            var game = owner.Game;
            var world = game.world;
            if (world?.rules == null)
            {
                return "학교에서 민수에게 말을 걸어 보세요.";
            }

            string hint = null;
            foreach (var npc in world.rules.npcs)
            {
                var profile = owner.catalog.NPC(npc.npcId);
                if (profile == null || game.growth.Age < profile.minAge || game.growth.Age > profile.maxAge
                    || game.stats.Social < profile.minSocial || npc.schedule.Length == 0)
                {
                    continue;
                }

                int index = WorldNPCScheduler.Schedule(npc, game.clock.Day, (int)(game.clock.SubDaySeconds / 60));
                var visit = npc.schedule[index];
                var resident = world.State.residents.Find(r => r.id == npc.npcId);
                string zone = resident?.zone ?? visit.zone;
                if (visit.action == NPCDailyAction.Sleep || !GrowthAccess.Zone(game, zone, out _))
                {
                    continue;
                }

                string place = world.rules.Zone(zone)?.title;
                if (place == null)
                {
                    continue;
                }

                string clue = $"{place} · {npc.title}에게 말 걸기";
                if (zone == world.State.zone)
                {
                    destination = zone;
                    return clue;
                }

                if (hint == null)
                {
                    destination = zone;
                    hint = clue;
                }
            }

            if (hint == null)
            {
                destination = "school";
                return "학생들은 귀가했습니다. 평일 오전 8시 30분에 학교에서 만나요.";
            }

            return hint;
        }

        public bool Can(int action, out string reason)
        {
            reason = null;
            var npc = owner.catalog.NPC(owner.SelectedNpcId);
            var g = owner.Game;
            if (npc == null || g.growth.Age < npc.minAge || g.growth.Age > npc.maxAge || g.stats.Social < npc.minSocial)
            {
                reason = "이 NPC와 대화할 조건을 충족하지 못했습니다.";
                return false;
            }

            var resident = g.world?.State?.residents?.Find(person => person.id == npc.npcId);
            if (g.world != null && g.world.Active && resident?.action == NPCDailyAction.Sleep)
            {
                reason = npc.displayName + "는 자고 있습니다.";
                return false;
            }

            var a = (SocialActivityData)owner.catalog.Find(action);
            var r = Relationship(npc.npcId);
            bool onCooldown = r != null && g.clock.Day - r.lastInteractionDay < a.cooldownDays;
            int interactionsToday = Progress.history.Count(h => h.npcId == npc.npcId && h.day == g.clock.Day);
            if (!FirstIntroduction(npc.npcId, action) && (onCooldown || interactionsToday >= npc.interactionsPerDay))
            {
                reason = "오늘 상호작용을 완료했습니다. 다음 날 다시 대화하세요.";
                return false;
            }

            if (action == 19 && (r == null || !r.friend))
            {
                reason = "먼저 친구를 만드세요.";
                return false;
            }

            return true;
        }

        public void Complete(int action, string id, SocialActivityData data)
        {
            var npc = owner.catalog.NPC(owner.SelectedNpcId);
            int day = owner.Game.clock.Day;
            var r = Relationship(npc.npcId);
            if (r == null)
            {
                r = new RelationshipProgress { npcId = npc.npcId, lastDecayDay = day };
                Progress.relationships.Add(r);
            }

            float chance = Mathf.Clamp(
                data.successWeight
                    + npc.responseModifier
                    + owner.Game.stats.Social * .002f
                    + owner.Game.stats.Mental * .001f,
                .05f,
                .95f
            );
            bool introduction = FirstIntroduction(npc.npcId, action);
            bool success = introduction || owner.Roll() < chance;
            r.lastInteractionDay = day;
            r.lastDecayDay = day;
            r.affinity = Mathf.Clamp(r.affinity + (success ? data.affinityGain : -data.affinityLoss), 0, 100);
            if (success && (introduction || action == 9) && !r.friend)
            {
                r.friend = true;
                if (introduction)
                {
                    r.affinity = Math.Max(10, r.affinity);
                    owner.Game.Log(npc.displayName + "와 친구가 되었습니다.");
                }

                owner.Signal(id, MissionCounter.FriendsMade);
            }

            RefreshState(r);
            Progress.experience += data.experience;
            Progress.history.Add(
                new SocialRecord
                {
                    id = id,
                    npcId = npc.npcId,
                    action = action,
                    day = day,
                    success = success,
                }
            );
            if (!success)
            {
                owner.Game.stats.ApplyCore(new StatDelta { happiness = -2 });
            }

            var messages = data.successMessages;
            owner.Game.Log(
                success && messages != null && messages.Length > 0
                    ? messages[Math.Min(messages.Length - 1, (int)(owner.Roll() * messages.Length))]
                    : data.failureMessage
            );
        }

        static void RefreshState(RelationshipProgress r)
        {
            r.state =
                r.affinity < 10 && r.friend ? RelationshipState.Strained
                : r.friend ? (r.affinity >= 70 ? RelationshipState.CloseFriend : RelationshipState.Friend)
                : RelationshipState.Acquaintance;
        }

        public void Decay(int day)
        {
            foreach (var r in Progress.relationships)
            {
                var npc = owner.catalog.NPC(r.npcId);
                if (npc == null)
                {
                    continue;
                }

                int periods = (day - r.lastDecayDay) / Math.Max(1, npc.neglectDays);
                if (periods <= 0)
                {
                    continue;
                }

                r.affinity = Mathf.Clamp(r.affinity - periods * npc.neglectLoss, 0, 100);
                r.lastDecayDay += periods * Math.Max(1, npc.neglectDays);
                RefreshState(r);
            }
        }
    }
}
