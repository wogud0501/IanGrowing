using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // All distances are local to a side elevation. The door layers use coordinates
    // from the vehicle sprites, so changing the viewport never moves the threshold.
    public sealed class TransitRideView : MonoBehaviour
    {
        const float Width = 600f;
        readonly Image[] leaves = new Image[2];
        readonly Image[] passengers = new Image[4];
        readonly RectTransform[] windows = new RectTransform[3];
        readonly float[] windowWidths = new float[3];
        CityTravelMode mode;
        RectTransform exterior;
        RectTransform vehicle;
        RectTransform interior;
        CanvasGroup extAlpha;
        CanvasGroup intAlpha;
        Image outsideActor;
        Image insideActor;
        Image alighting;
        Image distantStreet;
        Image vehicleArt;
        RectTransform railPassenger;
        CabDoorReach cabReach;
        Vector2 rearApertureCenter;
        TMP_Text caption;
        ActorAnimationSet bank;
        Vector2 boardDoor;
        Vector2 exitDoor;
        Vector2 vehicleSize;
        float actorHeight;
        bool Rail => mode == CityTravelMode.Subway;
        bool Cab => mode == CityTravelMode.Taxi;

        public static TransitRideView Create(
            Transform parent,
            CityTravelMode mode,
            WorldPlayerMotor motor,
            PixelUXWidgets ui
        )
        {
            var area = new GameObject(
                "Vehicle journey",
                typeof(RectTransform),
                typeof(RectMask2D)
            ).GetComponent<RectTransform>();
            area.SetParent(parent, false);
            area.sizeDelta = new Vector2(Width, 290);
            var size = ((RectTransform)parent).rect.size;
            float scale = Mathf.Min(size.x * .96f / Width, size.y * .60f / 290f);
            area.localScale = Vector3.one * Mathf.Max(.1f, scale);
            var view = area.gameObject.AddComponent<TransitRideView>();
            view.mode = mode;
            view.bank = motor.growthVisual.Current.animations;
            view.actorHeight = Mathf.Clamp(motor.growthVisual.Current.height * 65f, 64f, 112f);
            view.Build();
            view.caption = ui.Text(parent, "Journey status", 20, new Vector2(.08f, .70f), new Vector2(.92f, .80f));
            view.caption.alignment = TextAlignmentOptions.Center;
            view.Sample(0);
            return view;
        }

        static RectTransform Layer(Transform parent, string label)
        {
            var rect = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(Width, 290);
            return rect;
        }

        static Sprite Art(string name) => Resources.Load<Sprite>("CityTransit/" + name);

        static Image ImageAt(Transform parent, string label, Sprite sprite, Vector2 size, Vector2 point)
        {
            var image = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = point;
            return image;
        }

        Image Actor(Transform layer, string name)
        {
            var image = ImageAt(
                layer,
                name,
                bank.Find(ActorMotion.Idle, ActorDirection.Down).frames[0],
                Vector2.one,
                Vector2.zero
            );
            image.rectTransform.pivot = new Vector2(.5f, 0);
            return image;
        }

        void Build()
        {
            exterior = Layer(transform, "Boarding point");
            extAlpha = exterior.gameObject.AddComponent<CanvasGroup>();
            distantStreet = ImageAt(
                exterior,
                "Window skyline",
                Art("CityWindow"),
                new Vector2(935, 222),
                new Vector2(0, 60)
            );
            if (Rail)
            {
                distantStreet.color = new Color(.36f, .40f, .45f);
            }
            var pavement = ImageAt(exterior, "Paving", Art("Platform"), new Vector2(700, 180), new Vector2(0, -100));
            pavement.color = Rail ? Color.white : new Color(.72f, .73f, .71f);
            vehicle = Layer(exterior, "Vehicle");
            vehicleArt = ImageAt(
                vehicle,
                "Vehicle body",
                Art(
                    Rail ? "Train"
                    : Cab ? "CabShell"
                    : "BusShell"
                ),
                Vector2.one,
                Vector2.zero
            );
            var rect = vehicleArt.sprite.rect;
            float width =
                Rail ? 424
                : Cab ? 426
                : 528;
            vehicleSize = new Vector2(width, width * rect.height / rect.width);
            vehicleArt.rectTransform.sizeDelta = vehicleSize;
            boardDoor =
                Rail ? new Vector2(0, -100)
                : Cab ? new Vector2(-66, -65)
                : new Vector2(185, -94);
            exitDoor = Rail || Cab ? boardDoor : new Vector2(-26, -94);
            Transform thresholdParent = vehicle;
            if (Cab)
            {
                var aperture = new Rect(59, 17, 54, 68);
                var sheet = new Vector2(245, 102);
                var center = aperture.center / sheet - Vector2.one * .5f;
                rearApertureCenter = Vector2.Scale(new Vector2(center.x, -center.y), vehicleSize);
                var reveal = ImageAt(
                    vehicle,
                    "Open rear doorway",
                    Art("CabLeaf"),
                    Vector2.Scale(aperture.size / sheet, vehicleSize),
                    rearApertureCenter
                );
                reveal.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                thresholdParent = reveal.transform;
            }
            insideActor = Actor(thresholdParent, "Threshold passenger");
            if (Rail)
            {
                for (int i = 0; i < 2; i++)
                {
                    leaves[i] = ImageAt(
                        vehicle,
                        "Sliding leaf",
                        Art("TrainDoor"),
                        new Vector2(width * .194f, width * .42f),
                        Vector2.zero
                    );
                }
            }
            else if (Cab)
            {
                leaves[0] = CutDoor("CabLeaf", new Rect(59, 17, 54, 68), new Vector2(245, 102));
            }
            else
            {
                leaves[0] = CutDoor("BusFront", new Rect(154, 32, 19, 47), new Vector2(192, 84));
                leaves[1] = CutDoor("BusRear", new Rect(74, 32, 26, 47), new Vector2(192, 84));
            }
            outsideActor = Actor(exterior, "Platform passenger");
            if (Rail)
            {
                alighting = ImageAt(
                    exterior,
                    "Alighting commuter",
                    Art("Passenger1"),
                    new Vector2(38, 76),
                    Vector2.zero
                );
            }
            else if (Cab)
            {
                cabReach = CabDoorReach.Create(
                    exterior,
                    bank.Find(ActorMotion.Idle, ActorDirection.Down).frames[0],
                    actorHeight * 1.63f
                );
            }

            interior = Layer(transform, "Passenger cabin");
            intAlpha = interior.gameObject.AddComponent<CanvasGroup>();
            string cabinName =
                Rail ? "RailCabin"
                : Cab ? "CabCabin"
                : "BusCabin";
            // The foreground plate retains the frames, rails and original glass reflections.
            // Pixel bounds include a small bleed behind each transparent pane.
            if (Rail)
            {
                Window(0, new Rect(100, 49, 149, 53));
                Window(1, new Rect(261, 49, 151, 53));
            }
            else if (Cab)
            {
                Window(0, new Rect(151, 19, 217, 61));
            }
            else
            {
                Window(0, new Rect(150, 40, 84, 42));
                Window(1, new Rect(250, 40, 64, 42));
                Window(2, new Rect(315, 40, 81, 42));
            }
            ImageAt(interior, cabinName, Art(cabinName), new Vector2(640, 213), Vector2.zero);
            var people = GameContext
                .Instance.game.world.rules.npcs.Where(n => n.animations && !FamilyLife.Parent(n.npcId))
                .GroupBy(n => n.animations)
                .Select(g => g.First())
                .Take(4)
                .ToArray();
            if (!Cab)
            {
                for (int i = 0; i < passengers.Length; i++)
                {
                    bool seated = i < 2;
                    var clip =
                        i < people.Length
                            ? people[i]
                                .animations.Find(
                                    seated ? ActorMotion.SitRelax : ActorMotion.UsePhone,
                                    ActorDirection.Down
                                )
                            : null;
                    var sprite = clip?.frames.FirstOrDefault() ?? Art("Passenger" + i % 2);
                    float h = seated ? 117 : 104;
                    passengers[i] = ImageAt(
                        interior,
                        seated ? "Seated commuter" : "Standing commuter",
                        sprite,
                        new Vector2(h * sprite.rect.width / sprite.rect.height, h),
                        Vector2.zero
                    );
                    passengers[i].rectTransform.pivot = new Vector2(.5f, 0);
                }
                var grip = Art("PassengerGrip");
                float height = 106f;
                var rider = ImageAt(
                    interior,
                    "Commuter holding the rail",
                    grip,
                    new Vector2(height * grip.rect.width / grip.rect.height, height),
                    Vector2.zero
                );
                railPassenger = rider.rectTransform;
                // The original fist stays on the painted pole as the rider shifts her weight.
                railPassenger.pivot = new Vector2(87.5f / 112f, 1 - 70.5f / 161f);
                railPassenger.anchoredPosition = new Vector2(
                    ((Rail ? 377f : 247f) / 512f - .5f) * 640f,
                    -83f + height * railPassenger.pivot.y
                );
            }
            cabinActor = Actor(interior, "Ian on board");
        }

        Image cabinActor;

        Image CutDoor(string asset, Rect pixels, Vector2 sheet)
        {
            var size = Vector2.Scale(vehicleSize, pixels.size / sheet);
            var left = (pixels.xMin / sheet.x - .5f) * vehicleSize.x;
            var top = (.5f - pixels.yMin / sheet.y) * vehicleSize.y;
            var image = ImageAt(vehicle, asset, Art(asset), size, new Vector2(left + size.x, top));
            image.rectTransform.pivot = Vector2.one;
            return image;
        }

        void Window(int index, Rect pixels)
        {
            Vector2 cabinSize = new Vector2(640, 213);
            Vector2 artSize = new Vector2(512, 170);
            var size = Vector2.Scale(cabinSize, pixels.size / artSize);
            var center = pixels.center / artSize - Vector2.one * .5f;
            float width = size.x;
            var mask = Layer(interior, "Window reveal");
            mask.sizeDelta = size;
            mask.anchoredPosition = Vector2.Scale(new Vector2(center.x, -center.y), cabinSize);
            mask.gameObject.AddComponent<RectMask2D>();
            var left = ImageAt(mask, "Passing buildings", Art("CityWindow"), size, Vector2.zero);
            ImageAt(left.transform, "Next block", left.sprite, left.rectTransform.sizeDelta, new Vector2(width, 0));
            left.color = Rail ? new Color(.32f, .37f, .40f) : new Color(.89f, .89f, .86f);
            left.transform.GetChild(0).GetComponent<Image>().color = left.color;
            windows[index] = left.rectTransform;
            windowWidths[index] = width;
        }

        static float Ease(float a, float b, float time) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, time));

        void Pose(
            Image actor,
            ActorMotion motion,
            ActorDirection facing,
            float frame,
            float height,
            Vector2 feet,
            float width = 1
        )
        {
            var clip = bank.Find(motion, facing);
            var sprite = clip.frames[Mathf.Min(clip.frames.Length - 1, (int)(frame * clip.frames.Length))];
            actor.sprite = sprite;
            actor.rectTransform.sizeDelta = new Vector2(height * sprite.rect.width / sprite.rect.height, height);
            actor.rectTransform.anchoredPosition = feet;
            actor.rectTransform.localScale = new Vector3(width * (clip.mirror ? -1 : 1), 1, 1);
        }

        public void Sample(float t)
        {
            float leave = Ease(.835f, .987f, t);
            float travel = Ease(.425f, .735f, t);
            float cabin = Ease(.405f, .485f, t) - Ease(.69f, .775f, t);
            float open = Ease(Cab ? .20f : .125f, Cab ? .277f : .204f, t) - Ease(.398f, .46f, t);
            float reopening = Ease(.747f, .826f, t) - Ease(.98f, 1.05f, t);
            float roll = Mathf.Sin(travel * Mathf.PI * 5) * Mathf.Sin(Mathf.PI * travel);
            Vector2 car = new Vector2((1 - Ease(0, .12f, t)) * (Cab ? -480 : -570), Cab ? 0 : roll * 2);
            vehicle.anchoredPosition = car;
            extAlpha.alpha = 1 - cabin;
            intAlpha.alpha = cabin;
            interior.anchoredPosition = new Vector2(4 * Mathf.Sin(travel * Mathf.PI), (Cab ? .7f : 2.5f) * roll);
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i])
                {
                    windows[i].anchoredPosition =
                        Vector2.left * (Mathf.Repeat(travel * (Cab ? 1.3f : 2.4f), 1) * windowWidths[i]);
                }
            }
            distantStreet.rectTransform.anchoredPosition = new Vector2(-90 * travel, 60);
            if (Rail)
            {
                for (int i = 0; i < 2; i++)
                {
                    leaves[i].rectTransform.anchoredPosition = new Vector2(
                        (i == 0 ? -1 : 1) * Mathf.Lerp(.096f, .27f, open + reopening) * vehicleSize.x,
                        -.041f * vehicleSize.x
                    );
                }
                alighting.color = new Color(1, 1, 1, Ease(.15f, .195f, t) - Ease(.285f, .335f, t));
                alighting.rectTransform.anchoredPosition = Vector2.Lerp(
                    boardDoor + new Vector2(0, 39),
                    boardDoor + new Vector2(-105, 5),
                    Ease(.165f, .32f, t)
                );
            }
            else
            {
                leaves[0].rectTransform.localScale = new Vector3(
                    1 - (Cab ? .91f * (open + reopening) : .95f * open),
                    1,
                    1
                );
                if (!Cab)
                {
                    leaves[1].rectTransform.localScale = new Vector3(1 - .95f * reopening, 1, 1);
                }
            }
            var threshold = Vector2.Lerp(boardDoor, exitDoor, travel);
            float stride = Mathf.Repeat(t * 9, 1);
            float shift =
                Cab ? 54
                : Rail ? 78
                : 55;
            Vector2 wait = boardDoor + new Vector2(-shift, -20);
            Vector2 walkIn = Vector2.Lerp(wait, boardDoor + Vector2.down * 12, Ease(.20f, .30f, t));
            Vector2 feet = Vector2.Lerp(walkIn, threshold + Vector2.up * (Cab ? 4 : 16), Ease(.30f, .405f, t));
            feet = Vector2.Lerp(feet, exitDoor + new Vector2(Rail ? 106 : 68, -24), leave);
            float cross = Ease(.342f, .374f, t) - Ease(.847f, .883f, t);
            var motion = (t > .20f && t < .405f || t > .835f && t < .99f) ? ActorMotion.Walk : ActorMotion.Idle;
            var direction =
                t > .30f && t < .405f ? ActorDirection.Up
                : t > .85f ? ActorDirection.Down
                : ActorDirection.Right;
            float height = actorHeight * (Cab ? 1.63f : 1f);
            float phase = stride;
            bool grasping = cabReach && t >= .14f && t < .292f;
            if (Cab)
            {
                // Map the painted rear-door handle and hinge to the exterior layer. Ian
                // reaches the latch while the car is stationary, then follows the opening.
                float handlePixel = Mathf.Lerp(69, 112, .91f * Mathf.Min(open, .31f));
                var handle = new Vector2(
                    (handlePixel / 245f - .5f) * vehicleSize.x,
                    (.5f - 50f / 102f) * vehicleSize.y
                );
                float curb = -vehicleSize.y * .49f;
                float extension = 0;
                Vector2 atLatch = cabReach
                    ? cabReach.StandAt(handle, curb, out extension)
                    : new Vector2(handle.x - height * .14f, curb);
                feet = Vector2.Lerp(new Vector2(boardDoor.x - 88, curb), atLatch, Ease(.075f, .195f, t));
                feet = Vector2.Lerp(feet, threshold + Vector2.up * 9, Ease(.277f, .405f, t));
                feet = Vector2.Lerp(feet, new Vector2(exitDoor.x + 70, curb - 2), leave);
                if (grasping)
                {
                    float reach = Ease(.14f, .197f, t) * (1f - Ease(.233f, .292f, t));
                    cabReach.Sample(feet, extension * reach);
                    motion = ActorMotion.Idle;
                    direction = ActorDirection.Down;
                }
                else if (t >= .292f && t < .425f)
                {
                    motion = ActorMotion.SitDown;
                    direction = ActorDirection.Down;
                    phase = Ease(.292f, .425f, t);
                }
                else if (t >= .824f && t < .905f)
                {
                    motion = ActorMotion.StandUp;
                    direction = ActorDirection.Down;
                    phase = Ease(.824f, .905f, t);
                }
                else if (t >= .425f && t < .824f)
                {
                    motion = ActorMotion.SitRelax;
                    direction = ActorDirection.Down;
                    phase = 1;
                }
                else if (t >= .075f && t < .14f)
                {
                    motion = ActorMotion.Walk;
                    direction = ActorDirection.Right;
                }
                if (cabReach && !grasping)
                {
                    cabReach.enabled = false;
                }
            }
            Pose(outsideActor, motion, direction, phase, height, feet);
            Pose(insideActor, motion, direction, phase, height, feet - car - rearApertureCenter);
            outsideActor.color = new Color(1, 1, 1, grasping ? 0 : 1 - cross);
            insideActor.color = new Color(1, 1, 1, cross);

            float crush = Rail ? Ease(.43f, .55f, t) - Ease(.77f, .87f, t) : 0;
            float floatY = Cab ? .4f : 2;
            Pose(
                cabinActor,
                Cab ? ActorMotion.SitRelax : ActorMotion.Idle,
                ActorDirection.Down,
                0,
                Cab ? actorHeight * 2.22f : actorHeight * 1.04f,
                new Vector2(Cab ? 103 : 0, (Cab ? -132 : -83) + floatY * roll),
                1 - .32f * crush
            );
            if (Rail)
            {
                outsideActor.rectTransform.localScale = new Vector3(
                    (1 - .32f * crush) * (direction == ActorDirection.Right ? -1 : 1),
                    1,
                    1
                );
            }
            for (int i = 0; i < passengers.Length; i++)
            {
                if (!passengers[i])
                {
                    continue;
                }
                float side = i % 2 == 0 ? -1 : 1;
                float x;
                if (Rail)
                {
                    x = i < 2 ? side * 175 : side * Mathf.Lerp(89, 27, crush);
                }
                else
                {
                    // The bus has asymmetric seats: the driver's door occupies its front bay.
                    // Seat centers come from the cabin plate, with standees in the aisle.
                    x = i switch
                    {
                        0 => -94,
                        1 => 126,
                        2 => -166,
                        _ => 63,
                    };
                }
                passengers[i].rectTransform.anchoredPosition = new Vector2(
                    x + (i < 2 ? 0 : 2 * roll),
                    i < 2 ? -62 : -83
                );
            }
            if (railPassenger)
            {
                railPassenger.localRotation = Quaternion.Euler(0, 0, roll * (Rail ? 1.7f : 2.8f));
            }
            caption.text =
                t < .205f
                    ? (
                        Rail ? "열차 도착"
                        : Cab ? "택시 도착"
                        : "02번 버스 도착"
                    )
                : t < .425f ? "승차"
                : t < .745f
                    ? (
                        Rail ? "빈자리는 마음속에"
                        : Cab ? "잠시 쉬어 가는 길"
                        : "다음 정류장으로"
                    )
                : "도착 · 하차";
        }
    }
}
