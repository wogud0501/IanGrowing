using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static partial class WorldQualityRecoverySetup
    {
        static Sprite[] familyIan;
        static Sprite[] familyAppliances;
        static Sprite[] familyEmpty;

        static Sprite[] FamilyAtlas(string name)
        {
            string path = "Assets/_Game/Art/FamilyLife/" + name + ".png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            ti.spritePixelsPerUnit = w / 4f / 2.7f;
            ti.maxTextureSize = 2048;
            ti.spritesheet = Enumerable
                .Range(0, 16)
                .Select(i => new SpriteMetaData
                {
                    name = name + "_" + i.ToString("D2"),
                    rect = new Rect(i % 4 * w / 4f, h - (i / 4 + 1) * h / 4f, w / 4f, h / 4f),
                    alignment = 9,
                    pivot = new Vector2(.5f, .04f),
                })
                .ToArray();
            ti.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        static WorldLifeAction FamilyAction(
            WorldInteractable o,
            LifeActionVisual mode,
            Vector2 point,
            Vector2 bedAnchor
        )
        {
            var a = Component<WorldLifeAction>(o.gameObject);
            a.mode = mode;
            a.familyOwned = o.zone == FamilyLife.Home || o.zone == FamilyLife.Bath;
            a.approach = Child(o.transform, "InteractionApproach");
            a.anchor = Child(o.transform, "ActionAnchor");
            a.standUp = Child(o.transform, "WakeOrStandUpPoint");
            a.approach.localPosition = point;
            a.standUp.localPosition = point;
            a.anchor.localPosition = bedAnchor;
            a.facing = Vector2.up;
            o.UsePoint = point;
            a.frames = mode == LifeActionVisual.Appliance ? familyAppliances : familyIan;
            a.overlay = Image(o.transform, "Life action occupied", a.frames[0], Vector2.zero);
            a.overlay.enabled = false;
            return a;
        }

        static WorldInteractable FamilyProp(
            string id,
            string title,
            Vector2 p,
            Sprite image,
            WorldInteractionKind kind,
            float width = 2.7f
        )
        {
            var o = Prop(id, title, FamilyLife.Home, p, image, kind, width / Mathf.Max(.01f, image.bounds.size.x));
            return o;
        }

        static WorldInteractable FamilySeat(string id, string title, Vector2 p, string owner)
        {
            var o = FamilyProp(id, title, p, familyAppliances[14], WorldInteractionKind.Meal);
            var slot = Component<WorldActivitySlot>(o.gameObject);
            slot.staffId = owner;
            FacilityAnchors(
                slot,
                new Vector2(0, .15f),
                new Vector2(.9f, -.65f),
                new Vector2(.9f, -.65f),
                true,
                ActorMotion.Eat,
                Vector2.down
            );
            var c = o.GetComponent<BoxCollider2D>();
            c.offset = new Vector2(0, .85f);
            c.size = new Vector2(1.5f, .4f);
            return o;
        }

        static void FamilyPerson(string id, string title, int bank, string seat, string bed)
        {
            var scheduler = world.GetComponent<WorldNPCScheduler>();
            var d = rules.npcs.FirstOrDefault(n => n.npcId == id);
            if (d == null)
            {
                var a = UnityEngine.Object.Instantiate(scheduler.actors.First(), world.transform);
                a.name = id;
                a.id = a.npcId = id;
                a.title = title;
                a.zone = FamilyLife.Home;
                var library = AssetDatabase
                    .FindAssets("t:ActorAnimationSet")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(p => AssetDatabase.LoadAssetAtPath<ActorAnimationSet>(p))
                    .First(x => x.name == "Citizen" + bank);
                a.GetComponent<WorldActorAnimation>().library = library;
                a.visual.sprite = library.Find(ActorMotion.Idle, ActorDirection.Down).frames[0];
                a.transform.position = rules.Zone(FamilyLife.Home).origin + new Vector2(-7 + bank * 3, 2);
                a.visual.enabled = true;
                scheduler.actors = scheduler.actors.Concat(new[] { a }).ToArray();
                world.objects = world.objects.Concat(new[] { a }).ToArray();
                var label = UnityEngine.Object.Instantiate(
                    scheduler.activityLabels[0],
                    scheduler.activityLabels[0].transform.parent
                );
                label.name = id + " activity";
                label.text = "";
                label.gameObject.SetActive(false);
                scheduler.activityLabels = scheduler.activityLabels.Concat(new[] { label }).ToArray();
                d = new WorldNPCDefinition
                {
                    npcId = id,
                    title = title,
                    homeZone = FamilyLife.Home,
                    workZone = "",
                    jobId = "parent",
                    personality = NPCPersonality.Homebody,
                    speed = 1.45f,
                    animations = library,
                    frames = library.Find(ActorMotion.Walk, ActorDirection.Right).frames,
                };
                rules.npcs = rules.npcs.Concat(new[] { d }).ToArray();
            }

            var s = new List<NPCScheduleEntry>();
            void At(int h, int m, string dest, NPCDailyAction action, string zone = null)
            {
                s.Add(FacilityAt(h * 60 + m, zone ?? FamilyLife.Home, dest, action));
            }

            At(0, 0, bed, NPCDailyAction.Sleep);
            At(6, bank == 0 ? 45 : 55, bed, NPCDailyAction.Wake);
            if (bank == 0)
            {
                At(7, 0, "family-fridge", NPCDailyAction.GetIngredients);
                At(7, 10, "family-cook", NPCDailyAction.Cook);
                At(7, 30, "family-serve", NPCDailyAction.ServeMeal);
                At(7, 35, seat, NPCDailyAction.Eat);
                At(8, 0, "family-dishes", NPCDailyAction.WashDishes);
                At(9, 0, "family-clean", NPCDailyAction.Clean);
                At(10, 30, "", NPCDailyAction.Walk, "city");
                At(11, 0, "counter", NPCDailyAction.Shop, "shop");
                At(11, 20, "", NPCDailyAction.Walk, "city");
                At(18, 10, "family-fridge", NPCDailyAction.GetIngredients);
                At(18, 20, "family-cook", NPCDailyAction.Cook);
                At(18, 40, "family-serve", NPCDailyAction.ServeMeal);
                At(18, 45, seat, NPCDailyAction.Eat);
                At(19, 10, "family-dishes", NPCDailyAction.WashDishes);
                At(20, 0, "family-tv-mother", NPCDailyAction.Rest);
                At(22, 30, bed, NPCDailyAction.Sleep);
            }
            else
            {
                At(7, 5, "family-care", NPCDailyAction.Clean);
                At(7, 38, seat, NPCDailyAction.Eat);
                At(8, 5, "family-clean", NPCDailyAction.Clean);
                At(9, 0, "", NPCDailyAction.Walk, "city");
                At(12, 0, "family-fridge", NPCDailyAction.GetIngredients);
                At(12, 10, "family-cook", NPCDailyAction.Cook);
                At(12, 30, "family-serve", NPCDailyAction.ServeMeal);
                At(12, 35, seat, NPCDailyAction.Eat);
                At(13, 0, "family-dishes", NPCDailyAction.WashDishes);
                At(15, 30, "counter", NPCDailyAction.Shop, "shop");
                At(15, 50, "", NPCDailyAction.Walk, "city");
                At(17, 0, "family-clean", NPCDailyAction.Clean);
                At(18, 48, seat, NPCDailyAction.Eat);
                At(19, 0, "family-care", NPCDailyAction.Clean);
                At(21, 0, "family-tv-father", NPCDailyAction.Rest);
                At(23, 0, bed, NPCDailyAction.Sleep);
            }

            d.schedule = s.OrderBy(e => e.hour * 60 + e.minute).ToArray();
        }

        [MenuItem("Ian/Life World/Apply Family Life")]
        public static void BuildFamilyLife()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
            world = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
            rules = AssetDatabase.LoadAssetAtPath<LifeWorldRules>(
                "Assets/_Game/Data/World/Shared/LifeWorldRules.asset"
            );
            art = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Architecture.png")
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
            var protectedZones = new[] { "workplace", "school", "shop", "finance", "realestate" };
            var before = protectedZones.ToDictionary(z => z, FacilitySignature);
            familyIan = FamilyAtlas("IanLife");
            familyAppliances = FamilyAtlas("Appliances");
            familyEmpty = FamilyAtlas("EmptyAppliances");
            var furniture = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Furniture.png")
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
            facilityDining = FacilityAtlas("Dining");
            var room = Room(FamilyLife.Home, "부모님 집", new Vector2(500, 0), new Vector2(20, 14));
            rules.Zone(FamilyLife.Home).location = SceneId.Home;
            rules.Zone(FamilyLife.Home).cameraSize = 8;
            rules.Zone(FamilyLife.Home).spawn = new Vector2(-2, 1.8f);
            var bed = FamilyProp(
                "family-bed",
                "이안의 잠자리 · 돌봄",
                new Vector2(-2, 3),
                familyAppliances[12],
                WorldInteractionKind.Sleep
            );
            FamilyAction(bed, LifeActionVisual.Bed, new Vector2(0, -.75f), new Vector2(0, 1.2f));
            FamilyProp(
                "family-mother-bed",
                "엄마의 잠자리",
                new Vector2(-8, 3),
                familyAppliances[12],
                WorldInteractionKind.Sleep
            ).enabled = false;
            FamilyProp(
                "family-father-bed",
                "아빠의 잠자리",
                new Vector2(-5, 3),
                familyAppliances[12],
                WorldInteractionKind.Sleep
            ).enabled = false;
            var fridge = FamilyProp(
                "family-fridge",
                "냉장고에서 음식 꺼내기",
                new Vector2(7.5f, 3.2f),
                familyAppliances[0],
                WorldInteractionKind.Fridge
            );
            FamilyAction(fridge, LifeActionVisual.Appliance, new Vector2(0, -.7f), Vector2.zero);
            var fridgeSlot = Component<WorldActivitySlot>(fridge.gameObject);
            FacilityAnchors(
                fridgeSlot,
                fridge.UsePoint,
                fridge.UsePoint,
                fridge.UsePoint,
                false,
                ActorMotion.Interact,
                Vector2.up
            );
            fridge.GetComponent<WorldLifeAction>().seat = fridgeSlot;
            var cook = FamilyProp(
                "family-cook",
                "가족 조리대",
                new Vector2(4, 3.2f),
                familyEmpty[4],
                WorldInteractionKind.Rest
            );
            cook.enabled = false;
            var cooking = Component<WorldActivitySlot>(cook.gameObject);
            FacilityAnchors(
                cooking,
                new Vector2(0, .1f),
                new Vector2(1, -.6f),
                new Vector2(1, -.6f),
                false,
                ActorMotion.Interact,
                Vector2.down
            );
            var cp = Component<WorldFacilityActivityPresentation>(cook.gameObject);
            cp.slot = cooking;
            cp.empty = cook.visual;
            cp.frames = familyAppliances.Skip(4).Take(8).Concat(familyAppliances.Skip(4).Take(8)).ToArray();
            cp.citizenRows = true;
            cp.occupied = Image(cook.transform, "Parent cooking", cp.frames[0], Vector2.zero);
            cp.occupied.enabled = false;
            var c = cook.GetComponent<BoxCollider2D>();
            c.offset = new Vector2(0, 1);
            c.size = new Vector2(1.6f, .4f);
            FamilyProp(
                "family-dishes",
                "설거지 · 손 씻기",
                new Vector2(8, 0),
                furniture["KitchenSink"],
                WorldInteractionKind.Wash,
                2
            );
            FamilyAction(Find("family-dishes"), LifeActionVisual.AlignOnly, new Vector2(0, -.75f), Vector2.zero);
            for (int i = 0; i < 3; i++)
            {
                string id = i == 0 ? "family-player-seat" : "family-dine-" + i;
                var o = FamilySeat(
                    id,
                    i == 0 ? "가족 식탁에서 식사" : "가족 식사 자리",
                    new Vector2(i * 2.35f, -1.7f),
                    i == 0 ? "player"
                        : i == 1 ? "family-mother"
                        : "family-father"
                );
                if (i == 0)
                {
                    var a = FamilyAction(o, LifeActionVisual.Dining, new Vector2(.9f, -.65f), new Vector2(0, .15f));
                    a.seat = o.GetComponent<WorldActivitySlot>();
                    a.facing = Vector2.down;
                }
                else
                {
                    var p = Component<WorldFacilityActivityPresentation>(o.gameObject);
                    p.slot = o.GetComponent<WorldActivitySlot>();
                    p.empty = o.visual;
                    p.frames = facilityDining;
                    p.citizenRows = true;
                    p.occupied = Image(o.transform, "Family dining", facilityDining[0], Vector2.zero);
                    p.occupied.enabled = false;
                    o.enabled = false;
                }
            }

            var divider = Image(
                room,
                "Sleeping area cutaway",
                art["IvoryWall"],
                new Vector2(-6.5f, 1),
                new Vector2(6.8f, .6f)
            );
            divider.enabled = true;
            divider.sortingOrder = 900;
            Boundary(room, "Sleeping area boundary", new Vector2(-6.5f, 1), new Vector2(6.8f, .15f));
            FamilyProp(
                "family-sofa",
                "TV를 보며 쉬기",
                new Vector2(-6, -3),
                furniture["Settee"],
                WorldInteractionKind.Rest,
                3
            );
            FamilyAction(Find("family-sofa"), LifeActionVisual.AlignOnly, new Vector2(0, -.6f), Vector2.zero);
            var tv = Image(room, "Family TV", familyAppliances[15], new Vector2(-7, 0));
            tv.enabled = true;
            FamilyProp(
                "family-closet",
                "가족 옷장",
                new Vector2(-9, -1),
                furniture["Wardrobe"],
                WorldInteractionKind.Wardrobe,
                1.8f
            );
            foreach (
                var entry in new[]
                {
                    new { Id = "family-serve", P = new Vector2(1, -.8f) },
                    new { Id = "family-clean", P = new Vector2(-3, -1) },
                    new { Id = "family-care", P = new Vector2(-2, 1.4f) },
                    new { Id = "family-tv-mother", P = new Vector2(-6, -2.9f) },
                    new { Id = "family-tv-father", P = new Vector2(-4.8f, -2.9f) },
                }
            )
            {
                FacilityPoint(
                    entry.Id,
                    "가족 생활",
                    FamilyLife.Home,
                    entry.P,
                    "",
                    entry.Id.StartsWith("family-tv") ? Vector2.up : Vector2.down,
                    entry.Id == "family-care" ? ActorMotion.Interact
                        : entry.Id == "family-serve" ? ActorMotion.PutDownItem
                        : entry.Id.StartsWith("family-tv") ? ActorMotion.SitDown
                        : ActorMotion.Work
                );
            }

            var bath = Room(FamilyLife.Bath, "가족 욕실", new Vector2(530, 0), new Vector2(8, 8), true);
            rules.Zone(FamilyLife.Bath).location = SceneId.Home;
            rules.Zone(FamilyLife.Bath).cameraSize = 5;
            foreach (
                var entry in new[]
                {
                    new
                    {
                        Id = "family-basin",
                        Key = "Basin",
                        Kind = WorldInteractionKind.Wash,
                        X = -2f,
                    },
                    new
                    {
                        Id = "family-shower",
                        Key = "Shower",
                        Kind = WorldInteractionKind.Shower,
                        X = 2f,
                    },
                    new
                    {
                        Id = "family-toilet",
                        Key = "Toilet",
                        Kind = WorldInteractionKind.Toilet,
                        X = 0f,
                    },
                }
            )
            {
                var o = Prop(
                    entry.Id,
                    entry.Kind == WorldInteractionKind.Wash ? "세수하기"
                        : entry.Kind == WorldInteractionKind.Shower ? "샤워하기"
                        : "화장실 사용",
                    FamilyLife.Bath,
                    new Vector2(entry.X, 1),
                    furniture[entry.Key],
                    entry.Kind,
                    .8f
                );
                FamilyAction(o, LifeActionVisual.AlignOnly, new Vector2(0, -.8f), Vector2.zero);
            }

            Door(
                "family-bath-front",
                "가족 욕실로",
                FamilyLife.Home,
                new Vector2(9, 2.4f),
                FamilyLife.Bath,
                new Vector2(0, -2.6f)
            );
            Door(
                "family-bath-exit",
                "부모님 집으로",
                FamilyLife.Bath,
                new Vector2(0, -3.7f),
                FamilyLife.Home,
                new Vector2(8.5f, 1)
            );
            Door("family-exit", "주거 골목으로", FamilyLife.Home, new Vector2(0, -6.7f), "alley", new Vector2(2, 2.5f));
            Door(
                "family-front",
                "부모님 집으로",
                "alley",
                new Vector2(2, 4),
                FamilyLife.Home,
                new Vector2(0, -5.5f),
                true
            );
            Caption(room, "Family home title", "부모님 집 · 함께 살아가는 시간", new Vector2(0, 5.5f), 10);
            FamilyPerson("family-mother", "엄마", 0, "family-dine-1", "family-mother-bed");
            FamilyPerson("family-father", "아빠", 1, "family-dine-2", "family-father-bed");
            var runtime = Component<FamilyHomeRuntime>(world.gameObject);
            runtime.world = world;
            runtime.crib = Image(bed.transform, "Newborn crib", familyIan[12], Vector2.zero);
            runtime.crib.enabled = false;
            runtime.cribFrames = familyIan.Skip(12).Take(4).ToArray();
            runtime.emptyBed = familyAppliances[12];
            FamilyAction(Find("bed"), LifeActionVisual.Bed, Find("bed").UsePoint, new Vector2(0, 1.2f));
            foreach (var id in new[] { "fridge", "meal-table", "sink", "shower", "toilet", "starter-desk" })
            {
                var o = Find(id);
                if (o != null)
                {
                    var a = FamilyAction(
                        o,
                        id == "fridge" ? LifeActionVisual.Appliance
                            : id == "meal-table" ? LifeActionVisual.Dining
                            : LifeActionVisual.AlignOnly,
                        o.UsePoint,
                        Vector2.zero
                    );
                    if (id == "fridge")
                    {
                        o.visual.sprite = familyAppliances[0];
                    }
                }
            }

            FamilyArchitecture();
            foreach (string z in protectedZones)
            {
                if (before[z] != FacilitySignature(z))
                {
                    throw new Exception("Protected facility changed " + z);
                }
            }

            Directory.CreateDirectory("Docs/FamilyLife");
            File.WriteAllText(
                "Docs/FamilyLife/PROTECTED_LAYOUT.json",
                JsonUtility.ToJson(
                    new ProtectedFamilyLayout
                    {
                        zones = protectedZones,
                        signatures = protectedZones.Select(FacilitySignature).ToArray(),
                    },
                    true
                )
            );
            EditorUtility.SetDirty(rules);
            EditorUtility.SetDirty(world);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        [Serializable]
        class ProtectedFamilyLayout
        {
            public string[] zones;
            public string[] signatures;
        }
    }
}
