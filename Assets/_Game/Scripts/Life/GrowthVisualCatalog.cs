using System;
using UnityEngine;

namespace IanGame
{
    // Append only: the existing values are serialized by item appearance profiles.
    public enum VisualGrowthStage
    {
        Newborn,
        Toddler,
        Preschool,
        EarlyChild,
        SchoolChild,
        Preteen,
        Teen,
        LateTeen,
        Adult,
        MiddleAge,
        Elder,
    }

    [Serializable]
    public sealed class GrowthVisualProfile
    {
        public VisualGrowthStage stage;
        public string id;
        public int minAge;
        public int maxAge;
        public ActorAnimationSet animations;
        public float height;
        public float colliderRadius;
        public float speedFactor = 1;
        public float stride = 1;
        public Vector2 colliderOffset;
        public Vector2 foot;
        public Vector2 hand;
        public Vector2 eye;
        public Vector2 seatOffset;
        public Vector2 showerOffset;
        public Vector2 basinOffset;
        public Sprite idlePreview;
        public bool bodyInteractions;
        public Vector2 headphoneAnchorOffset;
        public float headphoneDisplayHeight;
        public bool sleepHorizontal;
        public string artNotes;
    }

    [CreateAssetMenu(menuName = "Ian/Growth/Visual catalog")]
    public sealed class GrowthVisualCatalog : ScriptableObject
    {
        public Texture2D growthSheet;
        public GrowthVisualProfile[] profiles = Array.Empty<GrowthVisualProfile>();
        public Sprite emptyCrib;
        public Sprite cribFront;
        public Sprite toddlerBed;
        public Sprite booster;
        public Sprite stepStool;
        public Sprite babyBath;

        public GrowthVisualProfile AtAge(int age) => Array.Find(profiles, p => age >= p.minAge && age <= p.maxAge);
    }
}
