using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    // Idempotent fitting of the existing home: retain interaction IDs, economy and doors.
    public static class Phase26HomeSetup
    {
        const string Data = "Assets/_Game/Data/Phase26/";
        static LifeWorldController world;
        static LifeWorldRules rules;
        static WorldVisualCatalog art;
        static readonly List<HomeFixedFootprint> footprints = new List<HomeFixedFootprint>();

        [MenuItem("Ian/Life World/Fit Existing Home")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            var prior = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (prior.isDirty)
            {
                throw new Exception("Save scene first");
            }

            string path = prior.path;
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
            ApplyToWorld(UnityEngine.Object.FindAnyObjectByType<LifeWorldController>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (path != scene.path)
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        static WorldInteractable Object(string id) =>
            world.GetComponentsInChildren<WorldInteractable>(true).FirstOrDefault(o => o.id == id);

        static WorldInteractable New(
            string id,
            string title,
            WorldInteractionKind kind,
            Vector2 p,
            Sprite sprite,
            float height,
            Vector2 size,
            Vector2 approach
        )
        {
            var o = Object(id);
            if (o == null)
            {
                var go = new GameObject(id, typeof(WorldInteractable));
                go.transform.SetParent(world.transform, false);
                o = go.GetComponent<WorldInteractable>();
                o.id = id;
                o.title = title;
                o.zone = "home";
                o.kind = kind;
                world.objects = world.objects.Concat(new[] { o }).ToArray();
            }

            Fit(o, p, sprite, height, size, approach);
            return o;
        }

        static void Fit(
            WorldInteractable o,
            Vector2 p,
            Sprite sprite,
            float height,
            Vector2 size,
            Vector2 approach,
            bool improved = false
        )
        {
            o.transform.position = rules.Zone(o.zone).origin + p;
            o.transform.localScale = Vector3.one;
            o.UsePoint = approach;
            o.radius = 1.2f;
            var rootVisual = o.GetComponent<SpriteRenderer>();
            if (rootVisual != null)
            {
                rootVisual.enabled = false;
            }

            var child = o.transform.Find("Home fitted visual");
            if (child == null)
            {
                child = new GameObject("Home fitted visual", typeof(SpriteRenderer)).transform;
                child.SetParent(o.transform, false);
            }

            var sr = child.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.enabled = true;
            sr.color = Color.white;
            float scale = height / sprite.bounds.size.y;
            child.localScale = Vector3.one * scale;
            child.localPosition = Vector3.up * (-sprite.bounds.min.y * scale);
            sr.sortingOrder = 1000 - Mathf.RoundToInt(o.transform.position.y * 100);
            o.visual = sr;
            if (o.highlight != null)
            {
                o.highlight.transform.SetParent(child, false);
                o.highlight.transform.localPosition = Vector3.zero;
                o.highlight.transform.localScale = Vector3.one * 1.025f;
                o.highlight.sprite = sprite;
                o.highlight.sortingOrder = sr.sortingOrder - 1;
                o.highlight.enabled = false;
            }

            var col = o.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = o.gameObject.AddComponent<BoxCollider2D>();
            }

            col.enabled = size.x > 0;
            col.size = size.x > 0 ? size : Vector2.one * .1f;
            col.offset = Vector2.up * (size.y / 2);
            if (o.zone == "home" && size.x > 0)
            {
                footprints.Add(
                    new HomeFixedFootprint
                    {
                        id = o.id,
                        bounds = new Rect(p.x - size.x / 2, p.y, size.x, size.y),
                        improvedOnly = improved,
                    }
                );
            }

            o.shadow = Phase26ArtSetup.Mask(
                o.transform,
                "Contact shadow",
                Vector2.zero,
                new Vector2(Mathf.Max(.4f, size.x), .18f),
                new Color(0, 0, 0, .2f),
                sr.sortingOrder - 2
            );
        }

        static void Room(string id, Vector2 size, float top, Sprite shell, float camera)
        {
            var zone = rules.Zone(id);
            zone.size = size;
            zone.walkableTop = top;
            zone.cameraSize = camera;
            zone.spawn = id == "home" ? new Vector2(0, -3.3f) : new Vector2(0, -2.35f);
            var floor = world
                .GetComponentsInChildren<SpriteRenderer>(true)
                .First(s => s.name == "Walkable floor" && s.transform.parent.name.EndsWith(" / " + id));
            floor.sprite = shell;
            floor.transform.localScale = new Vector3(size.x / shell.bounds.size.x, size.y / shell.bounds.size.y, 1);
            foreach (var c in floor.transform.parent.GetComponentsInChildren<BoxCollider2D>())
            {
                Vector2 pos,
                    dim;
                switch (c.name)
                {
                    case "Back wall":
                        pos = new Vector2(0, top);
                        dim = new Vector2(size.x, .25f);
                        break;
                    case "Front cutaway":
                        pos = new Vector2(0, -size.y / 2);
                        dim = new Vector2(size.x, .25f);
                        break;
                    case "Left wall":
                        pos = new Vector2(-size.x / 2, 0);
                        dim = new Vector2(.25f, size.y);
                        break;
                    case "Right wall":
                        pos = new Vector2(size.x / 2, 0);
                        dim = new Vector2(.25f, size.y);
                        break;
                    default:
                        continue;
                }

                c.transform.localScale = Vector3.one;
                c.transform.localPosition = pos;
                c.size = dim;
                c.offset = Vector2.zero;
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = false;
                }
            }
        }

        public static void ApplyToWorld(LifeWorldController target)
        {
            world = target;
            rules = AssetDatabase.LoadAssetAtPath<LifeWorldRules>(Data + "LifeWorldRules.asset");
            art = AssetDatabase.LoadAssetAtPath<WorldVisualCatalog>(Data + "WorldVisualCatalog.asset");
            footprints.Clear();
            var shell = Phase26ArtSetup.Floor("StudioShell.png");
            var fittings = Phase26ArtSetup.Grid(
                "HomeFittingsAtlas.png",
                4,
                2,
                new[]
                {
                    "KitchenSink",
                    "CookCounter",
                    "WritingDesk",
                    "WoodChair",
                    "Nightstand",
                    "Divider",
                    "KitchenShelf",
                    "ShoeRack",
                },
                2.5f
            );
            var threshold = Phase26ArtSetup.Grid("EntryThreshold.png", 1, 1, new[] { "Threshold" }, 1)[0];
            Room("home", new Vector2(13.5f, 9), 2.25f, shell, 4.8f);
            Room("bathroom", new Vector2(7.5f, 7), 1.05f, art.bathroomShell, 4.2f);
            var backdrop = world
                .GetComponentsInChildren<WorldRoomBackdrop>(true)
                .First(x => x.transform.parent.name.EndsWith(" / home"));
            backdrop.day = shell;
            backdrop.night = System.IO.File.Exists("Assets/_Game/Art/Phase26Recovery/StudioShellNight.png")
                ? Phase26ArtSetup.Floor("StudioShellNight.png")
                : shell;
            Fit(
                Object("bed"),
                new Vector2(-5, .25f),
                art.furniture[0],
                2.6f,
                new Vector2(1.7f, 1.6f),
                new Vector2(1.3f, -.6f)
            );
            Fit(
                Object("closet"),
                new Vector2(-5.75f, -2.5f),
                art.furniture[3],
                2.2f,
                new Vector2(1.4f, .5f),
                Vector2.down * .8f
            );
            Fit(
                Object("fridge"),
                new Vector2(5.75f, 1),
                art.furniture[1],
                2.1f,
                new Vector2(1.1f, .6f),
                Vector2.down * .8f
            );
            Fit(
                Object("tap-home"),
                new Vector2(4.25f, 1),
                fittings[0],
                1.5f,
                new Vector2(1.5f, .65f),
                Vector2.down * .8f
            );
            New(
                "kitchen-prep",
                "음식 준비하기",
                WorldInteractionKind.Fridge,
                new Vector2(2.75f, 1),
                fittings[1],
                1.5f,
                new Vector2(1.5f, .65f),
                Vector2.down * .8f
            );
            Fit(
                Object("meal-table"),
                new Vector2(4, -1.25f),
                art.furniture[2],
                1.35f,
                new Vector2(1.6f, .7f),
                new Vector2(-1.25f, .15f)
            );
            Fit(
                Object("home-chair"),
                new Vector2(4, -2.5f),
                fittings[3],
                1.3f,
                new Vector2(.7f, .55f),
                Vector2.down * .7f
            );
            var desk = New(
                "starter-desk",
                "책상에서 쉬기",
                WorldInteractionKind.Rest,
                new Vector2(-1.25f, 1),
                fittings[2],
                1.5f,
                new Vector2(1.8f, .6f),
                new Vector2(-1.2f, -.5f)
            );
            world.starterWritingDesk = desk.gameObject;
            New(
                "writing-chair",
                "의자에 앉아 쉬기",
                WorldInteractionKind.Rest,
                new Vector2(-1.25f, -.25f),
                fittings[3],
                1.2f,
                new Vector2(.65f, .5f),
                Vector2.down * .8f
            );
            New(
                "bedside-table",
                "침대 옆에서 쉬기",
                WorldInteractionKind.Rest,
                new Vector2(-3.25f, .5f),
                fittings[4],
                1.25f,
                new Vector2(.9f, .5f),
                Vector2.down * .8f
            );
            New(
                "entry-shoes",
                "외출 준비하기",
                WorldInteractionKind.Wardrobe,
                new Vector2(1.4f, -3.5f),
                fittings[7],
                .85f,
                new Vector2(1.1f, .45f),
                Vector2.down * .6f
            );
            var divider = New(
                "kitchen-divider",
                "주방 칸막이",
                WorldInteractionKind.Rest,
                new Vector2(1.5f, .5f),
                fittings[5],
                1.6f,
                new Vector2(1.5f, .35f),
                Vector2.left * .8f
            );
            divider.enabled = false;
            var shelf = New(
                "kitchen-shelf",
                "주방 선반",
                WorldInteractionKind.Rest,
                new Vector2(4.25f, 2.8f),
                fittings[6],
                .7f,
                Vector2.zero,
                Vector2.zero
            );
            shelf.enabled = false;
            Fit(
                Object("upgrade-desk"),
                new Vector2(-1.25f, 1),
                art.furniture[8],
                1.7f,
                new Vector2(1.8f, .6f),
                new Vector2(-1.2f, -.5f),
                true
            );
            Fit(
                Object("upgrade-storage"),
                new Vector2(-3.6f, -2.5f),
                art.furniture[9],
                2,
                new Vector2(1.4f, .45f),
                Vector2.down * .8f,
                true
            );
            Fit(Object("front-door"), new Vector2(0, -4.25f), threshold, .5f, Vector2.zero, Vector2.up * .9f);
            Object("front-door").animateDoorLeaf = false;
            Object("front-door").title = "밖으로 나가기";
            Fit(Object("bath-door"), new Vector2(0, 2.15f), art.furniture[7], 2.2f, Vector2.zero, Vector2.down * .85f);
            Object("bath-door").arrival = new Vector2(0, -2.3f);
            Object("bath-door").title = "화장실 문 열기";
            Fit(Object("bath-exit"), new Vector2(0, -3.3f), threshold, .4f, Vector2.zero, Vector2.up * .8f);
            Object("bath-exit").arrival = new Vector2(0, 1.25f);
            Object("bath-exit").animateDoorLeaf = false;
            Object("bath-exit").title = "집으로 돌아가기";
            Object("house-entrance").arrival = new Vector2(0, -3.3f);
            Fit(
                Object("sink"),
                new Vector2(-2.45f, 0),
                art.furniture[4],
                1.9f,
                new Vector2(1.1f, .45f),
                Vector2.down * .8f
            );
            Fit(
                Object("toilet"),
                new Vector2(0, 0),
                art.furniture[5],
                1.25f,
                new Vector2(.85f, .6f),
                Vector2.down * .8f
            );
            Fit(
                Object("shower"),
                new Vector2(2.2f, -.1f),
                art.furniture[6],
                2.4f,
                new Vector2(1.5f, .8f),
                Vector2.down * .8f
            );
            Plan();
            var camera = world.follow.target;
            camera.enabled = true;
            camera.gameObject.SetActive(true);
            camera.tag = "MainCamera";
            camera.targetTexture = null;
            camera.targetDisplay = 0;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.cullingMask = -1;
            camera.orthographicSize = rules.Zone("home").cameraSize;
            camera.transform.position = new Vector3(rules.Zone("home").origin.x, rules.Zone("home").origin.y, -10);
            camera.GetComponent<WorldPixelViewport>().enabled = true;
            Phase26VisualRebuild.ApplyIfReady(world);
            world.roomBackdrops = world.GetComponentsInChildren<WorldRoomBackdrop>(true);
            EditorUtility.SetDirty(rules);
            EditorUtility.SetDirty(world);
            Physics2D.SyncTransforms();
        }

        static void Plan()
        {
            var plan = AssetDatabase.LoadAssetAtPath<HomeLayoutPlan>(Data + "HomeLayoutPlan.asset");
            if (plan == null)
            {
                plan = ScriptableObject.CreateInstance<HomeLayoutPlan>();
                AssetDatabase.CreateAsset(plan, Data + "HomeLayoutPlan.asset");
            }

            plan.version = 3;
            plan.floor = new Rect(-6.6f, -4.35f, 13.2f, 6.45f);
            plan.entrance = new Vector2(0, -3.3f);
            plan.fixedFootprints = footprints.Where(f => f.id != "upgrade-desk").ToArray();
            plan.regions = new[]
            {
                new HomeAreaRegion
                {
                    title = "침실·생활·책상",
                    area = HomeArea.Bedroom | HomeArea.Living | HomeArea.Study,
                    bounds = new Rect(-6.55f, -4.1f, 7.15f, 6.2f),
                },
                new HomeAreaRegion
                {
                    title = "주방·식사",
                    area = HomeArea.Kitchen | HomeArea.Dining,
                    bounds = new Rect(1.6f, -4.1f, 5, 6.2f),
                },
            };
            plan.reserved = new[] { new Rect(-.6f, -3.9f, 1.2f, 5), new Rect(-.6f, 1.1f, 1.2f, 1.0f) };
            plan.accessPoints = new[]
            {
                new Vector2(-3.7f, -.35f),
                new Vector2(5.75f, .2f),
                new Vector2(4.25f, .2f),
                new Vector2(2.75f, .2f),
                new Vector2(2.75f, -1.1f),
                new Vector2(0, 1.3f),
                new Vector2(-5.75f, -3.3f),
                new Vector2(-1.25f, -1.05f),
            };
            plan.furniture = new[]
            {
                new HomeFurnitureRule
                {
                    id = "bookshelf",
                    areas = HomeArea.Bedroom | HomeArea.Study,
                    footprint = new Vector2(1.4f, .45f),
                    height = 2.1f,
                    slots = new[] { new Vector2(-5.75f, -.75f) },
                },
                new HomeFurnitureRule
                {
                    id = "lamp",
                    areas = HomeArea.Living | HomeArea.Bedroom,
                    footprint = new Vector2(.45f, .4f),
                    height = 1.6f,
                    slots = new[] { new Vector2(-4, -1.25f), new Vector2(-1, -2.5f) },
                },
                new HomeFurnitureRule
                {
                    id = "chair",
                    areas = HomeArea.Living | HomeArea.Study,
                    footprint = new Vector2(.8f, .6f),
                    height = 1.3f,
                    slots = new[] { new Vector2(-2.5f, -3), new Vector2(-1.75f, -2.5f), new Vector2(-4, -2.5f) },
                },
            };
            var housing = AssetDatabase.LoadAssetAtPath<HousingCatalog>(
                "Assets/_Game/Data/Phase256/HousingCatalog.asset"
            );
            housing.placementPlan = plan;
            foreach (var f in housing.furniture)
            {
                var r = plan.Rule(f.id);
                if (r != null)
                {
                    f.position = r.slots[0] / 2;
                }
            }

            EditorUtility.SetDirty(plan);
            EditorUtility.SetDirty(housing);
        }
    }
}
