using UnityEngine;

namespace IanGame
{
    public sealed class StationTrainDoors : MonoBehaviour
    {
        public Transform left;
        public Transform right;
        public float Phase { get; private set; }

        float width;
        bool called;
        float boarding;
        public float Aperture { get; private set; }

        public void HoldForBoarding(bool hold)
        {
            called = hold;
        }

        void Awake()
        {
            width = GetComponent<SpriteRenderer>().sprite.bounds.size.x;
        }

        void Update()
        {
            var game = GameContext.Instance?.game;
            if (game == null || game.State != GameState.Playing || !game.clock.IsRunning)
            {
                return;
            }
            Phase = Mathf.Repeat(Phase + Time.deltaTime, 24);
            float open =
                Mathf.SmoothStep(0, 1, Mathf.InverseLerp(8, 9, Phase))
                - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(11.8f, 12.8f, Phase))
                + Mathf.SmoothStep(0, 1, Mathf.InverseLerp(16, 17, Phase))
                - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(19.8f, 20.8f, Phase));
            boarding = Mathf.MoveTowards(boarding, called ? 1 : 0, Time.deltaTime * 3.5f);
            Aperture = Mathf.Max(open, Mathf.SmoothStep(0, 1, boarding));
            float spacing = Mathf.Lerp(.097f, .27f, Aperture) * width;
            left.localPosition = new Vector3(-spacing, left.localPosition.y, 0);
            right.localPosition = new Vector3(spacing, right.localPosition.y, 0);
        }
    }
}
