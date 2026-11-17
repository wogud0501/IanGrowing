using System.Collections;
using UnityEngine;

namespace IanGame.Commercial
{
    // Clip playback owns frames only. Room atmosphere owns breathing; the model owns actions.
    public sealed class SpriteMotionPlayer : MonoBehaviour
    {
        public SpriteRenderer target;
        public SpriteMotionLibrary library;
        public SpriteMotion State { get; private set; }
        public bool HasAuthoredFrames => clip != null && clip.frames.Length > 1;

        SpriteMotionClip clip;
        float started;
        GameManager game;

        void Start()
        {
            game = GameContext.Instance == null ? null : GameContext.Instance.game;
            if (game != null)
            {
                game.ActionPerformed += Action;
            }

            Play(SpriteMotion.Idle);
            StartCoroutine(Animate());
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.ActionPerformed -= Action;
            }
        }

        void Action(int id)
        {
            Play(
                id == 5 ? SpriteMotion.Sleep
                : id == 3 ? SpriteMotion.Eat
                : id == 4 ? SpriteMotion.Drink
                : id == 0 ? SpriteMotion.Work
                : id >= 7 ? SpriteMotion.Study
                : SpriteMotion.Interaction
            );
        }

        public void Play(SpriteMotion state)
        {
            State = state;
            clip = library == null ? null : library.Find(state);
            started = Time.unscaledTime;
        }

        IEnumerator Animate()
        {
            var delay = new WaitForSecondsRealtime(1f / 12);
            while (true)
            {
                if (game == null || game.State != GameState.Paused)
                {
                    float elapsed = Time.unscaledTime - started;
                    if (clip != null && !clip.loop && elapsed > clip.duration)
                    {
                        Play(SpriteMotion.Idle);
                        elapsed = 0;
                    }

                    if (target != null && library != null)
                    {
                        target.sprite =
                            clip != null && clip.frames.Length > 0
                                ? clip.frames[Mathf.FloorToInt(elapsed * clip.framesPerSecond) % clip.frames.Length]
                                : library.fallback;
                    }
                }

                yield return delay;
            }
        }
    }
}
