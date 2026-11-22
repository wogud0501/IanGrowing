using System;
using System.Collections.Generic;
using System.Linq;

namespace IanGame
{
    public enum NotificationKind
    {
        Info,
        Item,
        Economy,
        Success,
        System,
        Warning,
        Achievement,
        Mission,
        Critical,
    }

    public sealed class PlayerNotice
    {
        public string text;
        public NotificationKind kind;
        public int priority;
        public long sequence;
        internal float displayedAt = float.NegativeInfinity;
    }

    // Session history survives room changes. Presentation never grants a reward.
    public sealed class NotificationService
    {
        readonly List<PlayerNotice> pending = new List<PlayerNotice>();
        readonly List<PlayerNotice> history = new List<PlayerNotice>();
        long sequence;
        public int PendingCount => pending.Count;
        public PlayerNotice[] History => history.ToArray();

        public event Action Changed;

        public void Publish(string text, NotificationKind kind = NotificationKind.Info, int priority = -1)
        {
            if (string.IsNullOrWhiteSpace(text) || pending.Any(n => n.text == text && n.kind == kind)
                || kind is NotificationKind.Info or NotificationKind.System
                    && history.Any(n => n.kind == kind && n.text == text && UnityEngine.Time.unscaledTime - n.displayedAt < 2f))
            {
                return;
            }

            var notice = new PlayerNotice
            {
                text = text,
                kind = kind,
                priority = priority < 0 ? (int)kind : priority,
                sequence = ++sequence,
            };
            pending.Add(notice);
            history.Add(notice);
            if (history.Count > 60)
            {
                history.RemoveAt(0);
            }

            if (pending.Count > 24)
            {
                var least = pending.OrderBy(n => n.priority).ThenBy(n => n.sequence).First();
                pending.Remove(least);
            }

            Changed?.Invoke();
        }

        public PlayerNotice Take()
        {
            var next = pending.OrderByDescending(n => n.priority).ThenBy(n => n.sequence).FirstOrDefault();
            if (next != null)
            {
                pending.Remove(next);
                next.displayedAt = UnityEngine.Time.unscaledTime;
            }

            return next;
        }

        public void Clear()
        {
            pending.Clear();
            history.Clear();
            Changed?.Invoke();
        }
    }
}
