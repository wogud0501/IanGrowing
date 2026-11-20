using System;
using UnityEngine;

namespace IanGame
{
    public enum ActorMotion
    {
        Idle,
        Walk,
        Run,
        Turn,
        Stop,
        Sleep,
        WakeUp,
        SitDown,
        StandUp,
        Eat,
        Drink,
        WashFace,
        Shower,
        Work,
        UseComputer,
        UsePhone,
        Interact,
        OpenDoor,
        PickUpItem,
        PutDownItem,
        Happy,
        Sad,
        Tired,
        Angry,
        Stressed,
        Surprised,
        Thinking,
        Study,
        Talk,
        Play,
        InfantCry,
        Carried,
        CareFeed,
        CareBath,
        SeatedIdle,
        Sick,
        Exhausted,
        LieDown,
        SitRelax,
        SeatedDrink,
        SeatedPhone,
        TalkSit,
        Sweep,
        Mop,
        Vacuum,
        WipeTable,
        WashDishes,
        CarryTrash,
        CarryLaundry,
    }

    public enum ActorDirection
    {
        Down,
        Up,
        Left,
        Right,
    }

    [Serializable]
    public sealed class ActorSpriteClip
    {
        public ActorMotion motion;
        public ActorDirection direction;
        public Sprite[] frames = Array.Empty<Sprite>();
        public bool loop;
        public bool mirror;
        public int loopFrom;
        public float framesPerSecond = 8;
        public float strideLength = 1.6f;
        public bool Available => frames != null && frames.Length > 1 && Array.TrueForAll(frames, s => s != null);
    }

    [CreateAssetMenu(menuName = "Ian/World/Actor animation set")]
    public sealed class ActorAnimationSet : ScriptableObject
    {
        public bool ageScoped;
        public ActorSpriteClip[] clips = Array.Empty<ActorSpriteClip>();

        public ActorSpriteClip Find(ActorMotion motion, ActorDirection direction, bool allowFallback = true)
        {
            var exact = Array.Find(clips, c => c.motion == motion && c.direction == direction && c.Available);
            if (exact != null || !allowFallback)
            {
                return exact;
            }

            // Missing directional actions stay explicitly queryable; fallback is not a claim
            // that a full set of authored interaction directions exists.
            return Array.Find(clips, c => c.motion == motion && c.Available)
                ?? (ageScoped ? Array.Find(clips, c => c.motion == ActorMotion.Idle && c.Available) : null);
        }

        public static ActorDirection Direction(Vector2 vector, ActorDirection previous = ActorDirection.Down)
        {
            if (vector.sqrMagnitude < .0000001f)
            {
                return previous;
            }

            bool horizontal = previous == ActorDirection.Left || previous == ActorDirection.Right;
            if (Mathf.Abs(vector.x) > Mathf.Abs(vector.y) * 1.12f)
            {
                horizontal = true;
            }
            else if (Mathf.Abs(vector.y) > Mathf.Abs(vector.x) * 1.12f)
            {
                horizontal = false;
            }

            return horizontal
                ? (vector.x < 0 ? ActorDirection.Left : ActorDirection.Right)
                : (vector.y > 0 ? ActorDirection.Up : ActorDirection.Down);
        }
    }
}
