using System;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public sealed class ItemAppearanceFrame
    {
        public VisualGrowthStage growthStage;
        public ActorDirection direction;
        public ActorMotion motion;
        public Sprite[] frames = Array.Empty<Sprite>();
        public Vector2 offset;
        public int sortingOffset = 1;
    }

    // Authored per-stage/per-direction overlays follow the existing sprite renderer.
    // Missing art preserves official Ian; it is never substituted with a UI icon.
    [DefaultExecutionOrder(100)]
    public sealed class ItemAppearanceController : MonoBehaviour
    {
        public WorldPlayerMotor player;
        public string HeadphoneAppearanceId { get; private set; }
        public string CosmeticAppearanceId { get; private set; }
        public bool HasRenderedOverride { get; private set; }

        ItemService items;
        GameManager game;
        SpriteRenderer headphone;
        SpriteRenderer cosmetic;

        sealed class FrameCache
        {
            public string id;
            public VisualGrowthStage stage;
            public ActorDirection direction;
            public ActorMotion motion;
            public ItemAppearanceFrame frame;
            public bool valid;
        }

        readonly FrameCache headphoneCache = new FrameCache();
        readonly FrameCache cosmeticCache = new FrameCache();

        void Start()
        {
            game = GameContext.Instance?.game;
            if (game?.items == null)
            {
                return;
            }

            items = game.items;
            items.AppearanceChanged += Selected;
            Selected(null, null);
            headphone = Layer("Equipped headphone art");
            cosmetic = Layer("Equipped cosmetic art");
        }

        SpriteRenderer Layer(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(player.sprite.transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = player.sprite.sharedMaterial;
            return renderer;
        }

        void Selected(string h, string c)
        {
            HeadphoneAppearanceId = items.catalog.Item(items.Equipment.Current(EquipmentSlot.Headphone))?.appearanceId;
            CosmeticAppearanceId = items.catalog.Item(items.Equipment.Current(EquipmentSlot.Cosmetic))?.appearanceId;
        }

        void OnDestroy()
        {
            if (items != null)
            {
                items.AppearanceChanged -= Selected;
            }
        }

        void LateUpdate()
        {
            if (items == null || player?.advanced == null || headphone == null)
            {
                return;
            }

            var stage = player.growthVisual?.Current?.stage ?? VisualGrowthStage.Adult;
            HasRenderedOverride =
                Draw(headphone, items.Equipment.Current(EquipmentSlot.Headphone), stage, headphoneCache)
                | Draw(cosmetic, items.Equipment.Current(EquipmentSlot.Cosmetic), stage, cosmeticCache);
        }

        bool Draw(SpriteRenderer target, string id, VisualGrowthStage stage, FrameCache cache)
        {
            var direction = player.advanced.Direction;
            var motion = player.advanced.Motion;
            if (
                !cache.valid
                || cache.id != id
                || cache.stage != stage
                || cache.direction != direction
                || cache.motion != motion
            )
            {
                cache.valid = true;
                cache.id = id;
                cache.stage = stage;
                cache.direction = direction;
                cache.motion = motion;
                cache.frame = null;
                var frames = items.catalog.Item(id)?.appearanceFrames;
                if (frames != null)
                {
                    foreach (var f in frames)
                    {
                        if (
                            f.growthStage == stage
                            && f.direction == direction
                            && f.motion == motion
                            && f.frames.Length > 0
                        )
                        {
                            cache.frame = f;
                            break;
                        }
                    }
                }
            }

            var frame = cache.frame;
            target.enabled = frame != null && player.sprite.enabled;
            if (frame == null)
            {
                return false;
            }

            target.sprite = frame.frames[player.advanced.Frame % frame.frames.Length];
            target.transform.localPosition =
                frame.offset + (player.growthVisual?.Current?.headphoneAnchorOffset ?? Vector2.zero);
            var profile = player.growthVisual?.Current;
            float scale =
                target == headphone && profile != null && profile.headphoneDisplayHeight > 0
                    ? profile.headphoneDisplayHeight / target.sprite.bounds.size.y
                    : 1;
            target.transform.localScale = Vector3.one * scale;
            target.sortingLayerID = player.sprite.sortingLayerID;
            target.sortingOrder = player.sprite.sortingOrder + frame.sortingOffset;
            target.color = player.sprite.color;
            return true;
        }
    }
}
