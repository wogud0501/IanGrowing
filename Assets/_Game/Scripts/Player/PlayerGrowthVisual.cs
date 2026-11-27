using UnityEngine;

namespace IanGame
{
    // Presentation is derived from the authoritative LifeProgression age, never saved
    // as a second mutable age/stage. Existing gameplay permissions remain separate.
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerGrowthVisual : MonoBehaviour
    {
        public WorldPlayerMotor motor;
        public GrowthVisualCatalog catalog;
        public GrowthVisualProfile Current { get; private set; }

        // Kept for existing furniture adapters: this means a dedicated age-art bank,
        // including middle/elder art, rather than a gameplay age permission.
        public bool BodyInteractions => Current != null && (Young || Current.bodyInteractions);
        public bool Young => Current != null && Current.stage != VisualGrowthStage.Adult;

        public event System.Action<GrowthVisualProfile, GrowthVisualProfile> VisualChanged;
        public bool Deferred { get; private set; }

        public Vector2 ActionOffset;
        public float Recline;
        public int ActionOrder = int.MinValue;
        public bool InBed { get; set; }
        public bool Seated { get; set; }
        public string ActivePose { get; private set; } = "Idle";
        public Vector2 DisplayOffset => ActionOffset + (BodyInteractions && Seated ? Current.seatOffset : Vector2.zero);
        public Quaternion DisplayRotation =>
            BodyInteractions
            && InBed
            && (ActivePose == "Sleep" || ActivePose == "Lie Down" || ActivePose == "Wake Up")
            && Current.sleepHorizontal
                ? Quaternion.Euler(0, 0, -90 * (ActivePose == "Sleep" ? 1 : Recline))
                : Quaternion.identity;
        public float Speed => Young ? Current.speedFactor : 1;

        int lastAge = int.MinValue;
        GameManager game;

        void OnEnable()
        {
            RefreshBinding();
        }

        void Start()
        {
            RefreshBinding();
        }

        void Update()
        {
            RefreshBinding();
            if (
                Current?.stage == VisualGrowthStage.Newborn
                && game != null
                && !motor.world.Busy
                && motor.world.CanControl
            )
            {
                string next = game.stats.Hunger <= 20 || game.stats.Thirst <= 20 ? "InfantCry" : "Idle";
                if (ActivePose != next)
                {
                    motor.Pose(next);
                }
            }
        }

        public void RefreshBinding()
        {
            var next = GameContext.Instance?.game;
            if (next == null || catalog == null || motor == null)
            {
                return;
            }

            if (game != next)
            {
                if (game != null)
                {
                    game.growth.Changed -= RefreshAge;
                }

                game = next;
                game.growth.Changed += RefreshAge;
            }

            RefreshAge();
        }

        public void RefreshAge()
        {
            if (game == null || catalog == null)
            {
                return;
            }

            if (Current != null && lastAge == game.growth.Age && !Deferred && motor.advanced.library == Current.animations)
            {
                return;
            }

            var p = catalog.AtAge(game.growth.Age);
            if (p == null)
            {
                return;
            }

            if (p == Current && motor.advanced.library == p.animations)
            {
                Deferred = false;
                lastAge = game.growth.Age;
                return;
            }

            if (Current != null && motor.world.Busy)
            {
                Deferred = true;
                return;
            }

            Deferred = false;
            var previous = Current;
            using (var growthLock = motor.controlLocks?.Acquire("growth-apply"))
            {
                motor.StopForControlLock();
                Current = p;
                lastAge = game.growth.Age;
                bool changeArt = previous == null || motor.advanced.library != p.animations;
                motor.advanced.library = p.animations;
                motor.GetComponent<CircleCollider2D>().radius = p.colliderRadius;
                motor.GetComponent<CircleCollider2D>().offset = p.colliderOffset;
                if (changeArt)
                {
                    motor.advanced.Play(ActorMotion.Idle, motor.Facing, true);
                    motor.advanced.Render(ActorMotion.Idle, motor.Facing);
                }
                ActionOffset = Vector2.zero;
                ActionOrder = int.MinValue;
                InBed = Seated = false;
                if (previous != p)
                {
                    VisualChanged?.Invoke(previous, p);
                }
            }
        }

        public string CarePose(int action) =>
            action == -4 ? "CareBath"
            : action == 3 ? "CareFeed"
            : action == 4 ? (Current?.stage == VisualGrowthStage.Newborn ? "CareFeed" : "Drink")
            : "Play";

        public void Pose(string pose)
        {
            ActivePose = pose;
        }

        public Vector2 Inside(WorldLifeAction a) =>
            a.anchor == null
                ? a.GetComponent<WorldInteractable>().Point
                : (Vector2)a.anchor.position
                    + (Young && a.mode == LifeActionVisual.Shower ? Current.showerOffset : Vector2.zero);

        public Vector2 SleepPoint(WorldLifeAction a)
        {
            var b = a.GetComponent<WorldInteractable>().visual.bounds;
            if (!Current.bodyInteractions)
            {
                return new Vector2(b.center.x, b.max.y - .22f - Current.height * .45f);
            }

            var sleep = Current.animations?.Find(ActorMotion.Sleep, ActorDirection.Down);
            float length =
                sleep != null && sleep.frames.Length > 0
                    ? sleep.frames[0].bounds.size.x * motor.visualScale
                    : Current.height * .9f;
            return new Vector2(b.center.x, b.max.y - .13f - length * .5f);
        }

        public void ClearAction()
        {
            ActionOffset = Vector2.zero;
            Recline = 0;
            ActionOrder = int.MinValue;
            InBed = Seated = false;
        }

        void OnDisable()
        {
            if (game != null)
            {
                game.growth.Changed -= RefreshAge;
            }

            game = null;
            ClearAction();
        }
    }
}
