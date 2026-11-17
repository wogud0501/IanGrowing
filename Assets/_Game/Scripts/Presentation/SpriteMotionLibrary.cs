using System;
using UnityEngine;

namespace IanGame.Commercial
{
    public enum SpriteMotion
    {
        Idle,
        Blink,
        Sleep,
        Eat,
        Drink,
        Study,
        Work,
        Walk,
        Happy,
        Sad,
        Tired,
        Stress,
        Interaction,
    }

    [Serializable]
    public class SpriteMotionClip
    {
        public SpriteMotion state;
        public Sprite[] frames = new Sprite[0];

        [Min(1)]
        public float framesPerSecond = 6;

        [Min(.1f)]
        public float duration = 2.4f;
        public bool loop;
    }

    [CreateAssetMenu(menuName = "Ian/Presentation/Sprite Motion Library")]
    public sealed class SpriteMotionLibrary : ScriptableObject
    {
        public Sprite fallback;
        public SpriteMotionClip[] clips = new SpriteMotionClip[0];

        public SpriteMotionClip Find(SpriteMotion state) => Array.Find(clips, c => c.state == state);
    }
}
