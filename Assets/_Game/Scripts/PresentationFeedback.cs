using System;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    [Serializable]
    public sealed class PresentationAudioCue
    {
        public string id;
        public AudioClip clip;
    }

    // One small HUD-scoped pool. Events come from successful existing services;
    // presentation cannot grant rewards, consume currency or advance missions.
    [DefaultExecutionOrder(250)]
    public sealed class PresentationFeedback : MonoBehaviour
    {
        public LifeWorldController world;
        public Sprite sparkle;
        public PresentationAudioCue[] audioCues = Array.Empty<PresentationAudioCue>();
        public event Action<string> AudioRequested;
        public int BurstCount { get; private set; }
        public int ActiveParticles { get; private set; }

        readonly Image[] particles = new Image[32];
        RectTransform root;
        ItemService items;
        GameManager game;
        CollectionUI collection;
        float age = 2;
        float duration = .65f;
        float lastHealth;
        Color color;
        int lastFrame = -1;

        void Start()
        {
            if (world?.Service == null || sparkle == null)
            {
                return;
            }

            game = world.Service.Game;
            items = game.items;
            collection = GetComponent<CollectionUI>();
            root = new GameObject("Pixel feedback pool", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(world.hud.shell.overlayRoot, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            for (int i = 0; i < particles.Length; i++)
            {
                var go = new GameObject("Feedback particle " + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                var p = particles[i] = go.GetComponent<Image>();
                p.sprite = sparkle;
                p.raycastTarget = false;
                p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(.5f, .5f);
                p.rectTransform.sizeDelta = new Vector2(9, 9);
                p.enabled = false;
            }

            items.AudioRequested += Cue;
            items.ItemEquipped += Equipped;
            items.NewItemDiscovered += Discovered;
            items.GachaPulled += Pulled;
            items.GachaMultiPulled += Pulled;
            if (collection != null)
            {
                collection.RareReveal += Rare;
            }

            game.missions.Completed += Mission;
            game.stats.Changed += Stats;
            game.economy.TransactionCompleted += Money;
            lastHealth = game.stats.Health;
        }

        public void Cue(string id)
        {
            AudioRequested?.Invoke(id);
            var cue = Array.Find(audioCues, c => c.id == id);
            if (cue?.clip != null && world.audio.effects != null)
            {
                world.audio.effects.PlayOneShot(cue.clip, world.audio.volume);
            }
        }

        public void Burst(Color tint, float seconds = .65f)
        {
            if (root == null || UIShell.ReducedMotion || lastFrame == Time.frameCount)
            {
                return;
            }

            lastFrame = Time.frameCount;
            age = 0;
            duration = seconds;
            color = tint;
            BurstCount++;
            root.SetAsLastSibling();
        }

        void Equipped(ItemEvent e)
        {
            Burst(new Color(.65f, .85f, 1));
        }

        void Discovered(ItemEvent e)
        {
            Burst(new Color(.86f, .72f, 1));
        }

        void Pulled(GachaBatch b)
        {
            Burst(new Color(.76f, .5f, 1));
        }

        void Rare(GachaResult result)
        {
            Cue("RareReveal");
            Burst(new Color(1, .79f, .3f), .85f);
        }

        void Mission(MissionData mission)
        {
            Cue("Mission");
            Burst(new Color(1, .85f, .34f));
        }

        void Stats()
        {
            float h = game.stats.Health;
            if (h > lastHealth + 1)
            {
                Cue("Heal");
                Burst(new Color(.5f, .95f, .7f));
            }
            else if (h < lastHealth - 1)
            {
                Cue("Damage");
            }

            lastHealth = h;
        }

        void Money(int before, int after, string reason)
        {
            Cue("Money");
            if (reason != null && reason.Contains("세금"))
            {
                world.hud.Notify("납부 완료 · 정부가 행복해졌습니다.", 1);
            }
        }

        void LateUpdate()
        {
            if (root == null)
            {
                return;
            }

            ActiveParticles = 0;
            age += Time.unscaledDeltaTime;
            float f = Mathf.Clamp01(age / duration);
            for (int i = 0; i < particles.Length; i++)
            {
                var p = particles[i];
                bool active = f < 1 && i < (UIShell.ReducedMotion ? 0 : 24);
                p.enabled = active;
                if (!active)
                {
                    continue;
                }

                ActiveParticles++;
                float angle = i * 2.399963f,
                    distance = 20 + (i % 5) * 9 + f * 110;
                var position = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance - f * f * 40);
                p.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Round(position.x / 2) * 2,
                    Mathf.Round(position.y / 2) * 2
                );
                p.color = new Color(color.r, color.g, color.b, 1 - f);
            }
        }

        void OnDestroy()
        {
            if (items != null)
            {
                items.AudioRequested -= Cue;
                items.ItemEquipped -= Equipped;
                items.NewItemDiscovered -= Discovered;
                items.GachaPulled -= Pulled;
                items.GachaMultiPulled -= Pulled;
            }

            if (collection != null)
            {
                collection.RareReveal -= Rare;
            }

            if (game != null)
            {
                game.missions.Completed -= Mission;
                game.stats.Changed -= Stats;
                game.economy.TransactionCompleted -= Money;
            }

            if (root != null)
            {
                Destroy(root.gameObject);
            }
        }
    }
}
