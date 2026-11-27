using IanGame.Commercial;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IanGame
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class WorldPlayerMotor : MonoBehaviour
    {
        public PlayerAnimationPresentation presentation;
        public PlayerControlLocks controlLocks;
        public string PoseName => pose;

        public LifeWorldController world;
        public SpriteRenderer sprite;
        public SpriteMotionLibrary motions;
        public CharacterIdentity identity;
        public Transform bodyVisual;
        public Vector2 Facing = Vector2.down;
        public InputActionAsset controls;
        public Animator locomotion;
        public WorldWalkSet walkSet;
        public SpriteRenderer contactShadow;
        public WorldActorAnimation advanced;

        [Min(.1f)]
        public float visualScale = 1;

        [Min(1)]
        public float acceleration = 40;

        [Min(1)]
        public float braking = 64;

        [Min(1)]
        public float runMultiplier = 1.65f;
        Rigidbody2D body;
        WorldNPCScheduler residents;
        Vector2 input;
        Vector2 velocity;
        Vector2 observedPosition;
        Vector2 observedDirection;
        float actualSpeed;
        float pendingDistance;
        bool observed;
        bool wasMoving;
        string pose = "Idle";
        float stopUntil;
        float turnUntil;
        float reactionRemaining;
        ActorMotion reaction;
        InputActionAsset runtimeControls;
        InputActionMap gameplay;
        InputAction move;
        InputAction interact;
        InputAction inventory;
        InputAction menu;
        InputAction run;
        bool scripted;
        Vector2 traversalTarget;
        float traversalSpeed;

        // Walking (1+) and independent outdoor travel (7+) have separate policies.
        public PlayerGrowthVisual growthVisual;
        public bool MovementAllowed =>
            world != null && world.Service != null && world.Service.Game.growth.WalkingUnlocked;
        public float TraversalSeconds { get; private set; }
        public bool TraversalArrived { get; private set; }
        public bool IsTraversing => scripted;
        public float ActualSpeed => actualSpeed;
        public Vector2 Movement => velocity;

        public bool TextInputFocused
        {
            get
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                var selected = es == null ? null : es.currentSelectedGameObject;
                var field = selected == null ? null : selected.GetComponent<TMPro.TMP_InputField>();
                return field != null && field.isFocused;
            }
        }

        public bool InteractPressed => OwnsKeyboardFocus() && interact != null && interact.WasPressedThisFrame();
        public bool InventoryPressed => OwnsKeyboardFocus() && inventory != null && inventory.WasPressedThisFrame();
        public bool MenuPressed => OwnsKeyboardFocus() && menu != null && menu.WasPressedThisFrame();

        void Awake()
        {
            controlLocks = GetComponent<PlayerControlLocks>() ?? gameObject.AddComponent<PlayerControlLocks>();
            presentation =
                GetComponent<PlayerAnimationPresentation>() ?? gameObject.AddComponent<PlayerAnimationPresentation>();
            presentation.motor = this;
            body = GetComponent<Rigidbody2D>();
            residents = world ? world.GetComponent<WorldNPCScheduler>() : null;
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void OnEnable()
        {
            if (controls == null)
            {
                Debug.LogError("LifeWorld: gameplay InputActionAsset is missing.", this);
                return;
            }

            runtimeControls = Instantiate(controls);
            gameplay = runtimeControls.FindActionMap("World", true);
            move = gameplay.FindAction("Move", true);
            interact = gameplay.FindAction("Interact", true);
            inventory = gameplay.FindAction("Inventory", true);
            menu = gameplay.FindAction("Menu", true);
            run = gameplay.FindAction("Run");
            gameplay.Enable();
        }

        void OnDisable()
        {
            CancelTraversal();
            StopMotion();
            gameplay?.Disable();
            if (runtimeControls != null)
            {
                Destroy(runtimeControls);
            }

            runtimeControls = null;
            gameplay = null;
            move = interact = inventory = menu = run = null;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                StopMotion();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                StopMotion();
            }
        }

        void StopMotion()
        {
            input = velocity = Vector2.zero;
            actualSpeed = 0;
            pendingDistance = 0;
            observed = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public Vector2 Position => body == null ? (Vector2)transform.position : body.position;

        bool OwnsKeyboardFocus()
        {
            if (!Application.isFocused)
            {
                return false;
            }

#if UNITY_EDITOR
            if (
                UnityEditor.EditorWindow.focusedWindow == null
                || UnityEditor.EditorWindow.focusedWindow.GetType().Name != "GameView"
            )
            {
                return false;
            }

#endif
            return true;
        }

        public void BeginTraversal(Vector2 destination, float speed = 2.2f)
        {
            if (!MovementAllowed)
            {
                CancelTraversal();
                return;
            }

            StopMotion();
            traversalTarget = destination;
            traversalSpeed = Mathf.Max(.1f, speed) * (growthVisual == null ? 1 : growthVisual.Speed);
            TraversalArrived = false;
            TraversalSeconds = 0;
            scripted = true;
            pose = "Idle";
        }

        public void CancelTraversal()
        {
            scripted = false;
            TraversalArrived = false;
            StopMotion();
        }

        void Update()
        {
            growthVisual?.RefreshBinding();
            bool controlling = MovementAllowed && world.CanControl && OwnsKeyboardFocus();
            input = controlling && move != null ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) : Vector2.zero;
            if (!MovementAllowed)
            {
                if (scripted)
                {
                    CancelTraversal();
                }

                StopMotion();
            }

            if (scripted)
            {
                input = Vector2.ClampMagnitude(traversalTarget - Position, 1);
            }

            if (actualSpeed > .03f && observedDirection.sqrMagnitude > 0)
            {
                Facing = observedDirection;
            }

            bool moving = actualSpeed > .06f;
            bool running = controlling && run != null && run.IsPressed() && world.Service.State.fatigue < 85;
            if (wasMoving && !moving)
            {
                stopUntil = Time.unscaledTime + .24f;
            }

            wasMoving = moving;
            ActorMotion idleMotion =
                Time.unscaledTime < stopUntil ? ActorMotion.Stop
                : Time.unscaledTime < turnUntil ? ActorMotion.Turn
                : reactionRemaining > 0 ? reaction
                : ActorMotion.Idle;
            if (pose == "Idle" && !moving && reactionRemaining > 0)
            {
                reactionRemaining = Mathf.Max(0, reactionRemaining - Time.unscaledDeltaTime);
            }

            presentation?.Render(pose, moving, running, pendingDistance, idleMotion);
            pendingDistance = 0;
        }

        void FixedUpdate()
        {
            if (
                !MovementAllowed
                || world.Service.Game.State != GameState.Playing
                || !scripted && (!world.CanControl || !OwnsKeyboardFocus())
            )
            {
                StopMotion();
                return;
            }

            if (scripted)
            {
                TraversalSeconds += Time.fixedDeltaTime;
            }

            Vector2 step = observed ? body.position - observedPosition : Vector2.zero;
            float distance = step.magnitude;
            observedPosition = body.position;
            observed = true;
            float maximum = world.Service.rules.moveSpeed * runMultiplier * Time.fixedDeltaTime * 2;
            actualSpeed = distance < maximum ? distance / Time.fixedDeltaTime : 0;
            if (actualSpeed > .03f)
            {
                observedDirection = step / distance;
            }
            if (distance < maximum)
            {
                pendingDistance += distance;
            }

            if (scripted && Vector2.Distance(body.position, traversalTarget) < .045f)
            {
                scripted = false;
                TraversalArrived = true;
                StopMotion();
                return;
            }

            float fatigue = world.Service.State.fatigue > 85 ? .7f : 1;
            bool running = !scripted && run != null && run.IsPressed() && fatigue == 1;
            Vector2 target = scripted
                ? (traversalTarget - body.position).normalized * traversalSpeed
                : input
                    * world.Service.rules.moveSpeed
                    * (growthVisual == null ? 1 : growthVisual.Speed)
                    * fatigue
                    * (running ? runMultiplier : 1);
            velocity = Vector2.MoveTowards(
                velocity,
                target,
                (target.sqrMagnitude > .001f ? acceleration : braking) * Time.fixedDeltaTime
            );
            var next = body.position + velocity * Time.fixedDeltaTime;
            if (scripted && (traversalTarget - body.position).sqrMagnitude < (next - body.position).sqrMagnitude)
            {
                next = traversalTarget;
            }

            body.MovePosition(residents ? residents.PlayerStep(body.position, next) : next);
        }

        public void StopForControlLock()
        {
            StopMotion();
        }

        public void Warp(Vector2 point)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            scripted = false;
            StopMotion();
            presentation?.ResetTransient();
            body.position = point;
            transform.position = point;
        }

        void LateUpdate()
        {
            if (contactShadow != null)
            {
                contactShadow.sortingOrder = sprite.sortingOrder - 1;
            }
        }

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude < .0001f)
            {
                return;
            }

            if (ActorAnimationSet.Direction(direction) != ActorAnimationSet.Direction(Facing))
            {
                turnUntil = Time.unscaledTime + .18f;
            }

            Facing = direction.normalized;
        }

        public void React(ActorMotion motion, float seconds = .8f)
        {
            reaction = motion;
            reactionRemaining = seconds;
        }

        public void ClearReaction()
        {
            reactionRemaining = 0;
        }

        public void Pose(string state)
        {
            if (!MovementAllowed && (state == "Walk" || state == "Run" || state == "Turn" || state == "Stop"))
            {
                state = "Idle";
            }

            pose = state;
            growthVisual?.Pose(state);
            if (advanced != null)
            {
                advanced.Play(ParsePose(state), Facing, true);
            }
        }

        public static ActorMotion ParsePose(string state)
        {
            switch (state)
            {
                case "Wake Up":
                    return ActorMotion.WakeUp;
                case "Sit":
                    return ActorMotion.SitDown;
                case "Wash":
                    return ActorMotion.WashFace;
                case "Stand Up":
                    return ActorMotion.StandUp;
                case "Open Door":
                    return ActorMotion.OpenDoor;
                case "Use Computer":
                    return ActorMotion.UseComputer;
                case "Use Phone":
                    return ActorMotion.UsePhone;
                default:
                    return System.Enum.TryParse(state.Replace(" ", ""), out ActorMotion motion)
                        ? motion
                        : ActorMotion.Interact;
            }
        }
    }
}
