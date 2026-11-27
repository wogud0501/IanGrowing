using UnityEngine;

namespace IanGame
{
    // Background commuters use separate gate lanes. Their route ends inside
    // the carriage and resumes at the same threshold on the next arrival.
    public sealed class StationPassenger : MonoBehaviour
    {
        public int lane;
        SpriteRenderer body;
        public StationTrainDoors train;
        public WorldActorAnimation animationSet;
        WorldPlayerMotor player;
        Vector2 previousPosition;
        Vector2 direction = Vector2.up;

        Vector2 Entrance => new Vector2(lane == 0 ? -.77f : .77f, -2.58f);

        void Start()
        {
            body = GetComponent<SpriteRenderer>();
            player = GetComponentInParent<LifeWorldController>().Player;
            previousPosition = Entrance;
            transform.localPosition = previousPosition;
        }

        void Update()
        {
            var game = GameContext.Instance?.game;
            if (game == null || game.State != GameState.Playing || !game.clock.IsRunning)
            {
                return;
            }

            float phase = Mathf.Repeat(train.Phase - lane * .48f, 24);
            float side = lane == 0 ? -1 : 1;
            var entrance = Entrance;
            var queue = new Vector2(entrance.x, .53f);
            var car = new Vector2(side * .24f, 1.5f);
            Vector2 point;
            if (phase < 4)
            {
                point = Vector2.Lerp(entrance, queue, phase / 4);
            }
            else if (phase < 9)
            {
                point = queue;
            }
            else if (phase < 11)
            {
                point = Vector2.Lerp(queue, car, (phase - 9) / 2);
            }
            else if (phase < 17)
            {
                point = car;
            }
            else if (phase < 19)
            {
                point = Vector2.Lerp(car, queue, (phase - 17) / 2);
            }
            else
            {
                point = Vector2.Lerp(queue, entrance, (phase - 19) / 4);
            }

            // A blocked commuter can miss a train. Walk back toward the marked
            // lane before changing height so the return route clears the gates.
            if (previousPosition.y < .5f && point.y > .53f)
            {
                point = queue;
            }
            if (previousPosition.y > .58f && point.y < .53f)
            {
                point = queue;
            }
            bool doorsOpen = train.Aperture >= .96f;
            if (!doorsOpen && previousPosition.y <= .55f && point.y > .55f)
            {
                point = queue;
            }
            point = Vector2.MoveTowards(previousPosition, point, .94f * Time.deltaTime);
            if (player && body.enabled)
            {
                Vector2 localFeet = transform.parent.InverseTransformPoint(player.Position);
                float gap = Vector2.Distance(localFeet, point);
                if (gap < .43f && gap < Vector2.Distance(localFeet, previousPosition))
                {
                    point = previousPosition;
                }
            }
            bool insideCar = point.y >= 1.47f;
            body.enabled = !insideCar || phase < 11 || phase >= 17;
            Vector2 delta = point - previousPosition;
            bool walking = delta.sqrMagnitude > .0000001f;
            if (walking)
            {
                direction = delta.normalized;
            }
            animationSet.Render(walking ? ActorMotion.Walk : ActorMotion.Idle, direction, delta.magnitude, true);
            transform.localPosition = point;
            previousPosition = point;
        }
    }
}
