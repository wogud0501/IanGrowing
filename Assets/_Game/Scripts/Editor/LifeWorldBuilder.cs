using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IanGame.Commercial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class LifeWorldBuilder
    {
        const string Data = "Assets/_Game/Data/Phase26/";
        const string Scene = "Assets/_Game/Scenes/LifeWorld.unity";
        static Sprite frame;
        static LifeWorldRules rules;
        static List<WorldInteractable> targets;
        static List<SpriteRenderer> lamps;

        static RectTransform R(Transform p, string n, float x, float y, float w, float h) =>
            ReadabilityAndBirthBuilder.Rect(p, n, new Vector2(x, y), new Vector2(x + w, y + h));

        static TMP_Text T(Transform p, string n, string value, float x, float y, float w, float h)
        {
            var t = ReadabilityAndBirthBuilder.Label(p, n, value, new Vector2(x, y), new Vector2(x + w, y + h));
            t.margin = new Vector4(14, 6, 14, 6);
            return t;
        }

        static Button B(Transform p, string n, string value, float x, float y, float w, float h)
        {
            var b = ReadabilityAndBirthBuilder.Button(p, n, value, new Vector2(x, y), new Vector2(x + w, y + h));
            b.GetComponent<Image>().sprite = frame;
            return b;
        }

        static RectTransform Card(Transform p, string n, float x, float y, float w, float h)
        {
            var r = R(p, n, x, y, w, h);
            var image = r.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            return r;
        }

        static GameObject Window(UIShell shell, string name, out RectTransform card)
        {
            var r = R(shell.overlayRoot, name, 0, 0, 1, 1);
            r.gameObject.AddComponent<CanvasGroup>();
            r.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .65f);
            card = Card(r, "Pixel window", .12f, .15f, .76f, .72f);
            r.gameObject.SetActive(false);
            return r.gameObject;
        }

        static Transform Scroll(Transform parent)
        {
            var root = R(parent, "Choices", .03f, .03f, .94f, .76f);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 36;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = R(root, "Viewport", 0, 0, 1, 1);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = R(viewport, "Content", 0, 1, 1, 0);
            content.pivot = new Vector2(.5f, 1);
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.padding = new RectOffset(8, 8, 8, 8);
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = viewport;
            return content;
        }

        [MenuItem("Ian/Life World/Build Connected Life World")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            Directory.CreateDirectory(Data);
            frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Commercial/UI/Panel_64.png");
            rules = AssetDatabase.LoadAssetAtPath<LifeWorldRules>(Data + "LifeWorldRules.asset");
            if (rules == null)
            {
                rules = ScriptableObject.CreateInstance<LifeWorldRules>();
                AssetDatabase.CreateAsset(rules, Data + "LifeWorldRules.asset");
            }

            rules.zones = new[]
            {
                Zone("home", "이안의 집", SceneId.Home, 0, 18, 12),
                Zone("bathroom", "옥색 화장실", SceneId.Home, 40, 10, 9),
                Zone("alley", "주거 골목", SceneId.City, 80, 24, 16, true),
                Zone("city", "도시 중심", SceneId.City, 130, 32, 20, true),
                Zone("shop", "동네 상점", SceneId.Shop, 180, 18, 12),
                Zone("workplace", "Workspace", SceneId.Workplace, 220, 18, 12),
                Zone("school", "학교", SceneId.School, 260, 18, 12),
            };
            var original = EditorSceneManager.OpenScene("Assets/_Game/Scenes/City.unity");
            var env = UnityEngine.Object.FindAnyObjectByType<LayeredLocationPresentation>();
            var npcFrames =
                env == null
                    ? new Sprite[0]
                    : env.actors.Where(a =>
                            !a.seated
                            && a.frames != null
                            && a.frames.Length > 0
                            && a.frames[0].name.Contains("Pedestrian")
                        )
                        .Select(a => a.frames)
                        .FirstOrDefault() ?? new Sprite[0];
            if (npcFrames.Length == 0)
            {
                throw new Exception("Existing city NPC animation is required");
            }

            rules.npcs = new[]
            {
                new WorldNPCDefinition
                {
                    npcId = "colleague",
                    title = "동네 선배 수연",
                    frames = npcFrames,
                    schedule = new[]
                    {
                        At(0, "alley", -4, 1),
                        At(7, "city", 6, 0),
                        At(9, "workplace", 4, 1),
                        At(18, "city", 3, -3),
                        At(22, "alley", -4, 1),
                    },
                },
                new WorldNPCDefinition
                {
                    npcId = "classmate",
                    title = "옆자리 민수",
                    frames = npcFrames,
                    schedule = new[]
                    {
                        At(0, "alley", 3, 1),
                        At(8, "school", -3, 1),
                        At(16, "city", -3, -3),
                        At(21, "alley", 3, 1),
                    },
                },
            };
            EditorUtility.SetDirty(rules);
            PatchLegacyBars();
            var context = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/GameContext.prefab");
            try
            {
                var w = context.GetComponent<LifeWorldService>() ?? context.AddComponent<LifeWorldService>();
                w.rules = rules;
                context.GetComponent<GameManager>().world = w;
                PrefabUtility.SaveAsPrefabAsset(context, "Assets/_Game/Prefabs/GameContext.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(context);
            }

            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
            EditorSceneManager.SaveScene(scene, Scene);
            var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
            foreach (
                var room in UnityEngine.Object.FindObjectsByType<RoomAtmosphere>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            )
            {
                UnityEngine.Object.DestroyImmediate(room.gameObject);
            }

            foreach (
                var old in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            )
            {
                if (old != null)
                {
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
            }

            foreach (
                var matte in UnityEngine.Object.FindObjectsByType<PixelViewportMatte>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            )
            {
                UnityEngine.Object.DestroyImmediate(matte.gameObject);
            }

            foreach (
                var old in p.GetComponents<MonoBehaviour>()
                    .Where(c => c is HomeEnvironmentPresenter || c is CommercialHUD)
                    .ToArray()
            )
            {
                UnityEngine.Object.DestroyImmediate(old);
            }

            var safe = p.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Commercial HUD safe area");
            foreach (Transform child in safe)
            {
                child.gameObject.SetActive(false);
            }

            var stage = new GameObject("LIFE WORLD / connected living district");
            var world = stage.AddComponent<LifeWorldController>();
            targets = new List<WorldInteractable>();
            lamps = new List<SpriteRenderer>();
            foreach (var z in rules.zones)
            {
                Room(stage.transform, z);
            }

            Objects(stage.transform);
            DistrictDetails(stage.transform);
            var upgraded = new GameObject("Improved residence / extra storage and desk");
            upgraded.transform.SetParent(stage.transform, false);
            Prop(
                upgraded.transform,
                "upgrade-desk",
                "개선된 집 책상에서 쉬기",
                "home",
                -1,
                3,
                WorldInteractionKind.Rest,
                "Desk"
            );
            Prop(
                upgraded.transform,
                "upgrade-storage",
                "생활 도구 정리",
                "home",
                -7,
                3,
                WorldInteractionKind.Rest,
                "Books"
            );
            world.upgradedHome = upgraded;
            upgraded.SetActive(false);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            foreach (var c in camera.GetComponents<Component>())
            {
                if (c.GetType().Name == "PixelPerfectCamera")
                {
                    UnityEngine.Object.DestroyImmediate(c);
                }
            }

            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.backgroundColor = new Color32(34, 41, 51, 255);
            camera.transform.position = new Vector3(0, 0, -10);
            var follow = camera.gameObject.AddComponent<WorldCameraController>();
            follow.world = world;
            follow.target = camera;
            world.follow = follow;
            var lighting = new GameObject("World key light", typeof(Light));
            lighting.GetComponent<Light>().type = LightType.Directional;
            lighting.GetComponent<Light>().intensity = .5f;
            var player = new GameObject(
                "Ian / official playable character",
                typeof(Rigidbody2D),
                typeof(CircleCollider2D),
                typeof(WorldPlayerMotor)
            );
            var motor = player.GetComponent<WorldPlayerMotor>();
            motor.world = world;
            motor.controls = Phase26InputSetup.EnsureControls();
            motor.GetComponent<CircleCollider2D>().radius = .28f;
            motor.GetComponent<Rigidbody2D>().gravityScale = 0;
            motor.GetComponent<Rigidbody2D>().freezeRotation = true;
            var visual = new GameObject("Official sprite", typeof(SpriteRenderer));
            visual.transform.SetParent(player.transform, false);
            var sr = visual.GetComponent<SpriteRenderer>();
            var identity = AssetDatabase.LoadAssetAtPath<CharacterIdentity>(
                "Assets/_Game/Data/IanOfficialIdentity.asset"
            );
            sr.sprite = identity.baseSprite;
            visual.transform.localPosition = Vector3.up * .95f;
            float scale = 1.9f / sr.sprite.bounds.size.y;
            visual.transform.localScale = Vector3.one * scale;
            var pivot = new GameObject("Animated body pivot");
            pivot.transform.SetParent(player.transform, false);
            visual.transform.SetParent(pivot.transform, true);
            visual.transform.localPosition = Vector3.zero;
            pivot.transform.localPosition = Vector3.up * .95f;
            motor.bodyVisual = pivot.transform;
            motor.sprite = sr;
            motor.identity = identity;
            motor.motions = AssetDatabase.LoadAssetAtPath<SpriteMotionLibrary>(
                "Assets/_Game/Data/Commercial/IanMotionLibrary.asset"
            );
            world.Player = motor;
            Phase26WalkSetup.AttachIfReady(motor);
            var shadow = new GameObject("Foot shadow", typeof(SpriteRenderer));
            shadow.transform.SetParent(player.transform, false);
            var sh = shadow.GetComponent<SpriteRenderer>();
            sh.sprite = LifeWorldArtBuilder.Tile("Paving");
            sh.color = new Color(0, 0, 0, .25f);
            shadow.transform.localScale = new Vector3(.27f, .08f, 1);
            sh.sortingOrder = -100;
            world.furnishingRoot = new GameObject("Owned furniture / persistent placements").transform;
            world.furnishingRoot.SetParent(stage.transform);
            var scheduler = stage.AddComponent<WorldNPCScheduler>();
            scheduler.world = world;
            scheduler.actors = new WorldInteractable[rules.npcs.Length];
            for (int i = 0; i < rules.npcs.Length; i++)
            {
                var def = rules.npcs[i];
                var o = Interact(
                    stage.transform,
                    "npc-" + i,
                    def.title,
                    "city",
                    new Vector2(i * 3, 0),
                    WorldInteractionKind.NPC,
                    def.frames[0],
                    false
                );
                o.npcId = def.npcId;
                o.UsePoint = Vector2.down * .1f;
                scheduler.actors[i] = o;
            }

            world.objects = targets.ToArray();
            world.lamps = lamps.ToArray();
            world.hud = HUD(p, safe, world);
            Phase256Builder.Polish(p.gameObject);
            NormalizeWorldHUD(world.hud);
            Phase26ArtSetup.ApplyToWorld(world);
            var route = AssetDatabase.LoadAssetAtPath<SceneCatalog>("Assets/_Game/Data/SceneRoutes.asset");
            if (route.Find(SceneId.LifeWorld) == null)
            {
                route.routes = route
                    .routes.Concat(
                        new[]
                        {
                            new SceneRoute
                            {
                                id = SceneId.LifeWorld,
                                sceneName = "LifeWorld",
                                implemented = true,
                            },
                        }
                    )
                    .ToArray();
            }

            EditorUtility.SetDirty(route);
            if (!EditorBuildSettings.scenes.Any(s => s.path == Scene))
            {
                EditorBuildSettings.scenes = EditorBuildSettings
                    .scenes.Concat(new[] { new EditorBuildSettingsScene(Scene, true) })
                    .ToArray();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            MasterHomeSetup.Apply();
            AdvancedOverhaulSetup.Apply();
            Directory.CreateDirectory("Docs/Phase26/Screenshots");
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
        }

        static WorldZoneDefinition Zone(
            string id,
            string title,
            SceneId loc,
            float x,
            float w,
            float h,
            bool outdoor = false
        ) =>
            new WorldZoneDefinition
            {
                id = id,
                title = title,
                location = loc,
                origin = new Vector2(x, 0),
                size = new Vector2(w, h),
                outdoor = outdoor,
                spawn = new Vector2(0, -2),
            };

        static NPCScheduleEntry At(int h, string zone, float x, float y) =>
            new NPCScheduleEntry
            {
                hour = h,
                zone = zone,
                position = new Vector2(x, y),
            };

        static void Room(Transform root, WorldZoneDefinition z)
        {
            var go = new GameObject(z.title + " / " + z.id);
            go.transform.SetParent(root, false);
            go.transform.position = z.origin;
            var floor = new GameObject("Walkable floor", typeof(SpriteRenderer));
            floor.transform.SetParent(go.transform, false);
            var s = floor.GetComponent<SpriteRenderer>();
            s.sprite = LifeWorldArtBuilder.Tile(
                z.id == "bathroom" ? "Jade"
                : z.outdoor ? "Paving"
                : "Wood"
            );
            s.drawMode = SpriteDrawMode.Tiled;
            s.size = z.size;
            s.sortingOrder = -5000;
            Color wall = z.id == "bathroom" ? new Color32(139, 197, 180, 255) : new Color32(230, 221, 199, 255);
            if (z.outdoor)
            {
                wall = new Color32(82, 100, 94, 255);
            }

            Block(go.transform, "Back wall", new Vector2(0, z.size.y / 2), new Vector2(z.size.x, .35f), wall, 2);
            Block(go.transform, "Left wall", new Vector2(-z.size.x / 2, 0), new Vector2(.35f, z.size.y), wall, .2f);
            Block(go.transform, "Right wall", new Vector2(z.size.x / 2, 0), new Vector2(.35f, z.size.y), wall, .2f);
            Block(
                go.transform,
                "Front cutaway",
                new Vector2(0, -z.size.y / 2),
                new Vector2(z.size.x, .35f),
                wall,
                .15f
            );
            if (z.outdoor)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    var tree = Interact(
                        root,
                        "tree-" + z.id + i,
                        "",
                        z.id,
                        new Vector2(i * (z.size.x / 2 - 2), z.size.y / 2 - 2),
                        WorldInteractionKind.Rest,
                        LifeWorldArtBuilder.Prop("Tree")
                    );
                    tree.enabled = false;
                }

                var lamp = Interact(
                    root,
                    "lamp-" + z.id,
                    "가로등",
                    z.id,
                    new Vector2(-z.size.x / 2 + 2, -2),
                    WorldInteractionKind.Rest,
                    LifeWorldArtBuilder.Prop("Lamp")
                );
                lamp.enabled = false;
                var glow = Flat(
                    root,
                    "Night lamp glow",
                    z.origin + new Vector2(-z.size.x / 2 + 2, 0),
                    new Vector2(.65f, .6f),
                    new Color(1, .84f, .44f, .8f),
                    1400
                );
                lamps.Add(glow);
            }
        }

        static SpriteRenderer Flat(Transform root, string name, Vector2 pos, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = LifeWorldArtBuilder.Solid();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        static void Sign(Transform root, string text, Vector2 pos, float width = 3.5f)
        {
            Flat(root, "Sign board / " + text, pos, new Vector2(width, .55f), new Color32(43, 56, 65, 255), 1800);
            var go = new GameObject("World sign / " + text, typeof(TextMeshPro));
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(pos.x, pos.y, -.1f);
            var t = go.GetComponent<TextMeshPro>();
            t.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Game/UI/Pixel/Fonts/Galmuri/IanBodyPixel18.asset"
            );
            t.fontSize = 3.2f;
            t.text = text;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color32(244, 227, 187, 255);
            t.rectTransform.sizeDelta = new Vector2(width, .55f);
            t.rectTransform.anchoredPosition3D = new Vector3(pos.x, pos.y, -.1f);
            t.sortingOrder = 1801;
        }

        static void DistrictDetails(Transform root)
        {
            var city = rules.Zone("city");
            Color road = new Color32(70, 79, 89, 255);
            Flat(root, "City avenue", city.origin + new Vector2(0, -4), new Vector2(29, 3.8f), road, -4900);
            Flat(root, "Central pedestrian street", city.origin + new Vector2(0, 1), new Vector2(4, 15), road, -4900);
            for (int i = -5; i <= 5; i++)
            {
                Flat(
                    root,
                    "Road lane " + i,
                    city.origin + new Vector2(i * 2.4f, -4),
                    new Vector2(1, .06f),
                    new Color32(197, 177, 126, 255),
                    -4890
                );
            }

            for (int i = -3; i <= 3; i++)
            {
                Flat(
                    root,
                    "Crosswalk " + i,
                    city.origin + new Vector2(i * .4f, -4),
                    new Vector2(.22f, 3),
                    new Color32(182, 184, 173, 255),
                    -4880
                );
            }

            foreach (
                var o in targets.Where(o =>
                    o.zone == "city" && o.kind == WorldInteractionKind.Door && o.id != "alley-path"
                )
            )
            {
                o.transform.localScale = Vector3.one * 2.05f;
                string title =
                    o.targetZone == "shop" ? "상점 07–23"
                    : o.targetZone == "workplace" ? "Workspace"
                    : "학교";
                Sign(
                    root,
                    title,
                    (Vector2)o.transform.position + Vector2.up * (o.targetZone == "school" ? 2.7f : 3.7f),
                    4
                );
                var window = Flat(
                    root,
                    "Night window " + o.id,
                    (Vector2)o.transform.position + new Vector2(.75f, 2.8f),
                    new Vector2(.5f, .7f),
                    new Color(1, .82f, .4f, .85f),
                    o.visual.sortingOrder + 1
                );
                lamps.Add(window);
            }

            Sign(root, "도시 중심 ↑", city.origin + new Vector2(-10, -1), 3.4f);
            var alley = rules.Zone("alley");
            Flat(root, "Residential lane", alley.origin + new Vector2(0, -3.5f), new Vector2(21, 3), road, -4900);
            Sign(root, "이안의 집", alley.origin + new Vector2(-7, 2.8f));
            Sign(root, "도시 →", alley.origin + new Vector2(6, -1.7f), 3);
            foreach (
                var o in targets.Where(o =>
                    o.kind == WorldInteractionKind.Shower || o.kind == WorldInteractionKind.Wash
                )
            )
            {
                var effect = new GameObject("Water / active interaction");
                effect.transform.SetParent(o.transform, false);
                for (int i = 0; i < 5; i++)
                {
                    var drop = new GameObject("Water pixel " + i, typeof(SpriteRenderer));
                    drop.transform.SetParent(effect.transform, false);
                    drop.transform.localScale = new Vector3(.03f, .12f, 1);
                    var sr = drop.GetComponent<SpriteRenderer>();
                    sr.sprite = LifeWorldArtBuilder.Solid();
                    sr.color = new Color(.65f, .88f, .94f, .8f);
                    sr.sortingOrder = o.visual.sortingOrder + 2;
                }

                o.activityEffect = effect;
                effect.SetActive(false);
            }
        }

        static void Block(Transform root, string name, Vector2 pos, Vector2 footprint, Color color, float rise)
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var s = go.GetComponent<SpriteRenderer>();
            s.sprite = LifeWorldArtBuilder.Solid();
            s.drawMode = SpriteDrawMode.Sliced;
            s.size = new Vector2(footprint.x, Mathf.Max(footprint.y, rise));
            s.color = color;
            s.sortingOrder = 1000 - Mathf.RoundToInt(pos.y * 100);
            go.GetComponent<BoxCollider2D>().size = footprint;
        }

        static WorldInteractable Interact(
            Transform root,
            string id,
            string title,
            string zone,
            Vector2 pos,
            WorldInteractionKind kind,
            Sprite sprite,
            bool collision = true
        )
        {
            var z = rules.Zone(zone);
            var go = new GameObject(id);
            go.transform.SetParent(root, false);
            go.transform.position = z.origin + pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1000 - Mathf.RoundToInt(go.transform.position.y * 100);
            if (collision)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(kind == WorldInteractionKind.Door ? 1.15f : 1.4f, .65f);
                col.offset = Vector2.up * .3f;
            }

            var o = go.AddComponent<WorldInteractable>();
            o.id = id;
            o.title = title;
            o.zone = zone;
            o.kind = kind;
            o.visual = sr;
            o.radius = rules.interactionRadius;
            var glow = new GameObject("Subtle pixel outline", typeof(SpriteRenderer));
            glow.transform.SetParent(go.transform, false);
            glow.transform.localScale = Vector3.one * 1.08f;
            var g = glow.GetComponent<SpriteRenderer>();
            g.sprite = sprite;
            g.color = new Color(1, .83f, .35f, .55f);
            g.sortingOrder = sr.sortingOrder - 1;
            g.enabled = false;
            o.highlight = g;
            targets.Add(o);
            return o;
        }

        static WorldInteractable Prop(
            Transform root,
            string id,
            string title,
            string zone,
            float x,
            float y,
            WorldInteractionKind kind,
            string art
        ) => Interact(root, id, title, zone, new Vector2(x, y), kind, LifeWorldArtBuilder.Prop(art));

        static void Door(
            Transform root,
            string id,
            string zone,
            float x,
            float y,
            string target,
            Vector2 arrival,
            string art = "Door"
        )
        {
            var o = Prop(root, id, rules.Zone(target).title + "로 이동", zone, x, y, WorldInteractionKind.Door, art);
            o.targetZone = target;
            o.arrival = arrival;
        }

        static void Objects(Transform root)
        {
            Prop(root, "bed", "침대에서 자기", "home", -5, 2, WorldInteractionKind.Sleep, "Bed");
            Prop(root, "fridge", "냉장고 열기", "home", 5, 2, WorldInteractionKind.Fridge, "Fridge");
            Prop(root, "meal-table", "식사하기", "home", 2, 0, WorldInteractionKind.Meal, "Table");
            Prop(root, "tap-home", "물 마시기", "home", 6, -1, WorldInteractionKind.Drink, "Sink");
            Prop(root, "closet", "옷장 확인", "home", -7, -1, WorldInteractionKind.Wardrobe, "Wardrobe");
            Prop(root, "home-chair", "집에서 쉬기", "home", -5, -2, WorldInteractionKind.Rest, "Bench");
            Door(root, "front-door", "home", 0, -4.5f, "alley", new Vector2(-7, 2));
            Door(root, "bath-door", "home", 7, 3.5f, "bathroom", new Vector2(0, -2));
            Door(root, "bath-exit", "bathroom", 0, -3.3f, "home", new Vector2(6, 2));
            Prop(root, "sink", "세수하기", "bathroom", -3, 2, WorldInteractionKind.Wash, "Sink");
            Prop(root, "shower", "샤워하기", "bathroom", 3, 2, WorldInteractionKind.Shower, "Shower");
            Prop(root, "toilet", "화장실 사용", "bathroom", 0, 2, WorldInteractionKind.Toilet, "Toilet");
            Door(root, "house-entrance", "alley", -7, 4, "home", new Vector2(0, -3));
            Door(root, "city-path", "alley", 8, -4, "city", new Vector2(-11, -5), "Door");
            Door(root, "alley-path", "city", -12, -5, "alley", new Vector2(6, -4));
            Door(root, "shop-front", "city", -8, 4, "shop", new Vector2(0, -3), "Building");
            Door(root, "office-front", "city", 9, 4, "workplace", new Vector2(0, -3), "Building");
            Door(root, "school-front", "city", 0, 5, "school", new Vector2(0, -3), "Building");
            Prop(root, "city-bench", "공원 벤치에서 쉬기", "city", 3, -4, WorldInteractionKind.Rest, "Bench");
            Prop(root, "stock-terminal", "증권거래소 이용", "city", 11, -5, WorldInteractionKind.StockMarket, "Desk");
            Prop(root, "counter", "식료품 구매", "shop", 0, 2, WorldInteractionKind.FoodShop, "Table");
            Prop(
                root,
                "furniture-display",
                "가구 구매·집 관리",
                "shop",
                5,
                1,
                WorldInteractionKind.FurnitureShop,
                "Books"
            );
            Door(root, "shop-exit", "shop", 0, -4.5f, "city", new Vector2(-8, 2));
            Prop(root, "workdesk", "컴퓨터 · 직업 활동", "workplace", 0, 2, WorldInteractionKind.Work, "Desk");
            Prop(root, "work-water", "물 마시기", "workplace", -5, 2, WorldInteractionKind.Drink, "Sink");
            Prop(root, "work-rest", "휴게실에서 쉬기", "workplace", 5, 2, WorldInteractionKind.Rest, "Bench");
            Door(root, "work-exit", "workplace", 0, -4.5f, "city", new Vector2(9, 2));
            Prop(root, "study-desk", "교육 · 학습 활동", "school", 0, 2, WorldInteractionKind.Study, "Books");
            Prop(root, "school-water", "물 마시기", "school", -5, 2, WorldInteractionKind.Drink, "Sink");
            Door(root, "school-exit", "school", 0, -4.5f, "city", new Vector2(0, 3));
        }

        static WorldHUD HUD(PixelHomePresenter p, RectTransform safe, LifeWorldController world)
        {
            var ui = p.gameObject.AddComponent<WorldHUD>();
            ui.world = world;
            ui.shell = p.navigation.stack;
            ui.navigation = p.GetComponent<PolishedNavigation>();
            ui.home = p.GetComponent<HomeManagementPresenter>();
            var top = Card(safe, "World status / padded separate elements", .018f, .88f, .70f, .10f);
            Bar(top, true, null);
            var wallet = Card(safe, "Wallet and menu", .732f, .88f, .25f, .10f);
            ui.cash = T(wallet, "Wallet", "0 G", .02f, .08f, .70f, .84f);
            ui.menuButton = B(wallet, "Open world menu", "메뉴", .73f, .15f, .24f, .70f);
            var vital = Card(safe, "Essential life status", .018f, .02f, .33f, .06f);
            ui.needs = T(vital, "Needs", "", 0, 0, 1, 1);
            var prompt = Card(safe, "Interaction context", .25f, .09f, .5f, .075f);
            ui.promptPanel = prompt.gameObject;
            ui.prompt = T(prompt, "Interaction prompt", "", 0, 0, 1, 1);
            ui.prompt.alignment = TextAlignmentOptions.Center;
            prompt.gameObject.SetActive(false);
            T(safe, "World controls", "WASD / 방향키 · E 사용 · I 가방 · Esc 메뉴", .37f, .02f, .43f, .06f).alignment =
                TextAlignmentOptions.Center;
            ui.feedback = T(safe, "World feedback", "", .20f, .18f, .6f, .11f);
            ui.feedback.alignment = TextAlignmentOptions.Center;
            ui.progress = T(safe, "Activity progress", "", .20f, .30f, .6f, .055f);
            ui.progress.alignment = TextAlignmentOptions.Center;
            RectTransform card;
            ui.menuPanel = Window(ui.shell, "World detailed menu", out card);
            T(card, "Title", "생활 기록 / 상세 메뉴", .03f, .82f, .75f, .13f);
            var close = B(card, "Close world menu", "닫기", .81f, .82f, .16f, .13f);
            var cmd = close.gameObject.AddComponent<UICommand>();
            cmd.shell = ui.shell;
            cmd.panel = ui.menuPanel;
            cmd.command = UICommand.Command.Close;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(close.onClick, cmd.Invoke);
            var content = Scroll(card);
            ui.menus = new Button[7];
            string[] names =
            {
                "생활 행동 안내",
                "상태 · 성장 · 사회관계",
                "도시 · 장소 안내",
                "직업 · 교육",
                "미션",
                "집 관리",
                "설정 · 저장 · 기타",
            };
            for (int i = 0; i < names.Length; i++)
            {
                ui.menus[i] = B(content, "Detailed menu " + i, names[i], 0, 0, 1, 1);
                var le = ui.menus[i].gameObject.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 60;
            }

            ui.contextPanel = Window(ui.shell, "World object activities", out card);
            ui.contextTitle = T(card, "Object title", "", .03f, .82f, .75f, .13f);
            ui.contextClose = B(card, "Close object", "닫기", .81f, .82f, .16f, .13f);
            ui.choices = (RectTransform)Scroll(card);
            ui.choicePrefab = B(ui.choices, "Choice template", "선택", 0, 0, 1, 1);
            var size = ui.choicePrefab.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = 60;
            ui.choicePrefab.gameObject.AddComponent<PixelButtonTextFlow>();
            ui.choicePrefab.gameObject.SetActive(false);
            var inventory = p.navigation.inventoryPanel.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();
            if (inventory != null)
            {
                ui.inventory = T(inventory.content, "Food and daily life inventory", "", 0, 0, 1, 1);
                var le = ui.inventory.gameObject.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 180;
                ui.inventory.gameObject.AddComponent<PixelTextFlow>();
            }

            var shade = R(p.transform, "World transition and sleep fade", 0, 0, 1, 1);
            var canvas = shade.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 400;
            shade.gameObject.AddComponent<GraphicRaycaster>();
            ui.fade = shade.gameObject.AddComponent<Image>();
            ui.fade.color = Color.clear;
            ui.fade.raycastTarget = false;
            return ui;
        }

        static void Bar(RectTransform root, bool world, string title)
        {
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.spacing = 18;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var bar = root.gameObject.AddComponent<CompactWorldStatusBar>();
            bar.world = world;
            bar.fixedPlace = title;
            bar.place = T(root, "Place", title, 0, 0, 1, 1);
            bar.time = T(root, "Clock", "", 0, 0, 1, 1);
            bar.message = T(root, "Situation", "", 0, 0, 1, 1);
            int n = 0;
            foreach (var t in new[] { bar.place, bar.time, bar.message })
            {
                t.margin = Vector4.zero;
                t.alignment = TextAlignmentOptions.MidlineLeft;
                var le = t.gameObject.AddComponent<LayoutElement>();
                le.minWidth =
                    n == 0 ? 100
                    : n == 1 ? 220
                    : 140;
                le.preferredWidth =
                    n == 0 ? 140
                    : n == 1 ? 240
                    : 260;
                le.flexibleWidth = n == 2 ? 1 : 0;
                n++;
            }
        }

        static void NormalizeWorldHUD(WorldHUD ui)
        {
            var root = ui.gameObject;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var safe = ui.GetComponentInChildren<CompactWorldStatusBar>().transform.parent as RectTransform;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.GetComponent<UITypeScale>() == null)
                {
                    var size = t.fontSize;
                    var scale = t.gameObject.AddComponent<UITypeScale>();
                    scale.baseSize = size;
                }
            }
        }

        static void PatchLegacyBars()
        {
            foreach (string name in new[] { "City", "Workplace", "StockMarket" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var fx = UnityEngine.Object.FindAnyObjectByType<LayeredLocationPresentation>();
                if (fx != null && fx.clockLabel != null)
                {
                    var r = fx.clockLabel.transform.parent as RectTransform;
                    if (r.GetComponent<CompactWorldStatusBar>() == null)
                    {
                        fx.clockLabel.gameObject.SetActive(false);
                        r.anchorMin = new Vector2(.018f, .30f);
                        r.anchorMax = new Vector2(.70f, .39f);
                        r.offsetMin = r.offsetMax = Vector2.zero;
                        Bar(
                            r,
                            false,
                            name == "City" ? "도시"
                                : name == "Workplace" ? "Workspace"
                                : "증권거래소"
                        );
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
