using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    // Targeted, repeatable migration. Never rebuilds the world or touches home/save data.
    public static partial class WorldQualityRecoverySetup
    {
        static LifeWorldController world;
        static LifeWorldRules rules;
        static Dictionary<string, Sprite> art;

        static T Component<T>(GameObject go)
            where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
            }

            return c;
        }

        static WorldInteractable Find(string id) => world.objects.FirstOrDefault(o => o != null && o.id == id);

        static Transform Child(Transform root, string name)
        {
            var t = root.Cast<Transform>().FirstOrDefault(c => c.name == name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(root, false);
            }

            return t;
        }

        static SpriteRenderer Image(Transform root, string name, Sprite sprite, Vector2 p, Vector2? size = null)
        {
            var t = Child(root, name);
            t.localPosition = p;
            var sr = Component<SpriteRenderer>(t.gameObject);
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Game/Art/VisualRebuild/WorldAlphaCut.mat"
            );
            sr.transform.localScale = Vector3.one;
            sr.drawMode = size.HasValue ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple;
            if (size.HasValue)
            {
                sr.size = size.Value;
            }

            sr.sortingOrder = size.HasValue ? -5000 : 1000 - Mathf.RoundToInt(t.position.y * 100);
            return sr;
        }

        static void Boundary(Transform root, string name, Vector2 p, Vector2 size)
        {
            var t = Child(root, name);
            t.localPosition = p;
            var c = Component<BoxCollider2D>(t.gameObject);
            c.size = size;
        }

        static void Caption(Transform root, string name, string text, Vector2 p, float width = 5)
        {
            var t = Child(root, name);
            t.localPosition = p;
            var label = Component<TextMeshPro>(t.gameObject);
            label.font = world.hud.needs.font;
            label.fontSize = 2.5f;
            label.color = new Color32(72, 64, 53, 255);
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(width, .5f);
            label.renderer.sortingOrder = 6000;
        }

        static Transform Room(string id, string title, Vector2 origin, Vector2 size, bool jade = false)
        {
            var z = rules.Zone(id);
            if (z == null)
            {
                z = new WorldZoneDefinition { id = id, location = SceneId.City };
                rules.zones = rules.zones.Concat(new[] { z }).ToArray();
            }

            z.title = title;
            z.origin = origin;
            z.size = size;
            z.spawn = new Vector2(0, -size.y / 2 + 1.4f);
            z.walkableTop = size.y / 2 - 1.3f;
            z.cameraSize = 6.8f;
            var root = world.transform.Cast<Transform>().FirstOrDefault(t => t.name.EndsWith(" / " + id));
            if (root == null)
            {
                root = Child(world.transform, title + " / " + id);
            }

            root.position = origin;
            foreach (var backdrop in root.GetComponentsInChildren<WorldRoomBackdrop>(true))
            {
                world.roomBackdrops = world.roomBackdrops.Where(b => b != backdrop).ToArray();
                UnityEngine.Object.DestroyImmediate(backdrop);
            }

            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.enabled = false;
            }

            var floor = Image(root, "Walkable floor", art[jade ? "JadeFloor" : "OakFloor"], Vector2.zero, size);
            floor.enabled = true;
            if (id == "workplace")
            {
                floor.color = new Color(.85f, .83f, .78f);
            }

            var back = Image(
                root,
                "Back wall",
                art[jade ? "JadeWall" : "IvoryWall"],
                new Vector2(0, size.y / 2 - 1.3f),
                new Vector2(size.x, 2.1f)
            );
            back.enabled = true;
            back.sortingOrder = -4000;
            Boundary(root, "Rear boundary", new Vector2(0, z.walkableTop), new Vector2(size.x, .2f));
            Boundary(root, "West boundary", new Vector2(-size.x / 2, 0), new Vector2(.2f, size.y));
            Boundary(root, "East boundary", new Vector2(size.x / 2, 0), new Vector2(.2f, size.y));
            Boundary(
                root,
                "Front left",
                new Vector2(-size.x / 4 - .45f, -size.y / 2),
                new Vector2(size.x / 2 - .9f, .2f)
            );
            Boundary(
                root,
                "Front right",
                new Vector2(size.x / 4 + .45f, -size.y / 2),
                new Vector2(size.x / 2 - .9f, .2f)
            );
            // Retire the old solid front wall, retaining its GameObject for scene references.
            var old = root.Cast<Transform>().FirstOrDefault(t => t.name == "Front cutaway");
            if (old != null)
            {
                var c = old.GetComponent<Collider2D>();
                if (c != null)
                {
                    c.enabled = false;
                }
            }

            return root;
        }

        static WorldInteractable Prop(
            string id,
            string title,
            string zone,
            Vector2 p,
            Sprite sprite,
            WorldInteractionKind kind,
            float scale = 1
        )
        {
            var o = Find(id);
            if (o == null)
            {
                var go = new GameObject(id, typeof(WorldInteractable));
                go.transform.SetParent(world.transform, false);
                o = go.GetComponent<WorldInteractable>();
                world.objects = world.objects.Concat(new[] { o }).ToArray();
            }

            o.id = id;
            o.title = title;
            o.zone = zone;
            o.kind = kind;
            o.radius = 1.1f;
            o.UsePoint = new Vector2(0, -.65f);
            o.transform.position = rules.Zone(zone).origin + p;
            o.transform.localScale = Vector3.one;
            var sr = o.visual;
            if (sr == null || sr.transform == o.transform)
            {
                if (sr != null)
                {
                    sr.enabled = false;
                }

                var visual = Child(o.transform, "Facility furniture visual");
                sr = Component<SpriteRenderer>(visual.gameObject);
            }

            sr.enabled = true;
            sr.sprite = sprite;
            sr.transform.localScale = Vector3.one * scale;
            sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Game/Art/VisualRebuild/WorldAlphaCut.mat"
            );
            sr.sortingOrder = 1000 - Mathf.RoundToInt(o.transform.position.y * 100);
            o.visual = sr;
            if (o.highlight != null)
            {
                o.highlight.sprite = sprite;
                o.highlight.enabled = false;
                o.highlight.transform.localScale = Vector3.one * scale * 1.035f;
            }

            if (o.shadow != null)
            {
                o.shadow.enabled = false;
            }

            var col = Component<BoxCollider2D>(o.gameObject);
            col.enabled = true;
            col.size = new Vector2(Mathf.Max(.65f, sprite.bounds.size.x * scale * .7f), .45f);
            col.offset = new Vector2(0, .25f);
            return o;
        }

        static WorldInteractable Door(
            string id,
            string title,
            string zone,
            Vector2 p,
            string target,
            Vector2 arrival,
            bool building = false
        )
        {
            var source = Find(building ? "office-front" : "work-exit");
            var o = Find(id);
            if (o == null)
            {
                o = UnityEngine.Object.Instantiate(source, world.transform);
                o.name = id;
                world.objects = world.objects.Concat(new[] { o }).ToArray();
            }

            o.id = id;
            o.title = title;
            o.zone = zone;
            o.kind = WorldInteractionKind.Door;
            o.targetZone = target;
            o.arrival = arrival;
            o.transform.position = rules.Zone(zone).origin + p;
            o.UsePoint = (building ? Vector2.down : Vector2.up) * .8f;
            AdvancedDoorSetup.Attach(world, o);
            return o;
        }

        static void Office()
        {
            var root = Room("workplace", "이안 사무실", new Vector2(220, 0), new Vector2(18, 12));
            var desk = Find("workdesk").visual.sprite;
            var furniture = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Furniture.png")
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
            var employees = rules
                .npcs.Where(d => d.schedule.Any(e => e.zone == "workplace" && e.action == NPCDailyAction.Work))
                .ToArray();
            for (int i = 0; i < employees.Length; i++)
            {
                var p = new Vector2(-6.2f + i % 3 * 3.25f, 1.55f - i / 3 * 2.55f);
                var o = Prop(
                    "staff-desk-" + employees[i].npcId,
                    employees[i].title + "의 업무 자리",
                    "workplace",
                    p,
                    desk,
                    WorldInteractionKind.Work,
                    .65f
                );
                o.UsePoint = new Vector2(0, -.72f);
                var slot = Component<WorldActivitySlot>(o.gameObject);
                slot.staffId = employees[i].npcId;
                slot.facing = Vector2.up;
                foreach (
                    var e in employees[i].schedule.Where(e => e.zone == "workplace" && e.action == NPCDailyAction.Work)
                )
                {
                    e.objectId = o.id;
                    e.position = o.Point - rules.Zone("workplace").origin;
                }

                var chair = Image(
                    root,
                    "Staff chair / " + employees[i].npcId,
                    furniture["Chair"],
                    p + new Vector2(0, -.85f)
                );
                chair.enabled = true;
                chair.transform.localScale = Vector3.one * .65f;
                chair.sortingOrder = o.visual.sortingOrder + 1;
                slot.seat = chair;
            }

            Prop(
                "workdesk",
                "공용 업무용 컴퓨터",
                "workplace",
                new Vector2(6, -3.2f),
                desk,
                WorldInteractionKind.Work,
                .75f
            );
            Prop(
                "work-rest",
                "휴게 소파에서 쉬기",
                "workplace",
                new Vector2(6, -.8f),
                furniture["Settee"],
                WorldInteractionKind.Rest,
                .95f
            );
            for (int i = 0; i < 2; i++)
            {
                string id = "office-break-" + i;
                var point = Find(id);
                if (point == null)
                {
                    var go = new GameObject(id, typeof(WorldInteractable));
                    go.transform.SetParent(world.transform, false);
                    point = go.GetComponent<WorldInteractable>();
                    world.objects = world.objects.Concat(new[] { point }).ToArray();
                }

                point.id = id;
                point.title = "직원 휴게 좌석";
                point.zone = "workplace";
                point.kind = WorldInteractionKind.Rest;
                point.UsePoint = Vector2.zero;
                point.transform.position = rules.Zone("workplace").origin + new Vector2(4.6f + i * 2.6f, 0);
                point.enabled = false;
                var slot = Component<WorldActivitySlot>(point.gameObject);
                slot.staffId = "";
                slot.facing = Vector2.right;
                var chair = Image(root, "Lounge chair " + i, furniture["Chair"], new Vector2(4.6f + i * 2.6f, 0));
                chair.enabled = true;
                chair.transform.localScale = Vector3.one * .65f;
                slot.seat = chair;
            }

            for (int i = 0; i < employees.Length; i++)
            {
                var d = employees[i];
                if (d.schedule.Any(e => e.objectId.StartsWith("office-break-")))
                {
                    int originalMask = d
                        .schedule.First(e => e.zone == "workplace" && e.action == NPCDailyAction.Work)
                        .dayMask;
                    foreach (var entry in d.schedule.Where(e => e.objectId.StartsWith("office-break-")))
                    {
                        entry.dayMask = originalMask;
                        entry.position = Find(entry.objectId).Point - rules.Zone("workplace").origin;
                        int resume = entry.hour * 60 + entry.minute + 15;
                        foreach (
                            var work in d.schedule.Where(e =>
                                e.zone == "workplace"
                                && e.action == NPCDailyAction.Work
                                && e.hour * 60 + e.minute == resume
                            )
                        )
                        {
                            work.dayMask = originalMask;
                        }
                    }

                    continue;
                }

                int start = (d.personality == NPCPersonality.NightWorker ? 2 : 10) * 60 + (i / 2) * 20 + i % 2 * 3;
                int mask = d.schedule.First(e => e.zone == "workplace" && e.action == NPCDailyAction.Work).dayMask;
                var list = d.schedule.ToList();
                list.Add(
                    new NPCScheduleEntry
                    {
                        dayMask = mask,
                        hour = start / 60,
                        minute = start % 60,
                        zone = "workplace",
                        objectId = "office-break-" + (i % 2),
                        position = Find("office-break-" + (i % 2)).Point - rules.Zone("workplace").origin,
                        action = NPCDailyAction.Rest,
                    }
                );
                int end = start + 15;
                var deskObject = Find("staff-desk-" + d.npcId);
                list.Add(
                    new NPCScheduleEntry
                    {
                        dayMask = mask,
                        hour = end / 60,
                        minute = end % 60,
                        zone = "workplace",
                        objectId = deskObject.id,
                        position = deskObject.Point - rules.Zone("workplace").origin,
                        action = NPCDailyAction.Work,
                    }
                );
                d.schedule = list.OrderBy(e => e.hour * 60 + e.minute).ToArray();
            }

            Prop(
                "work-water",
                "탕비실에서 물 마시기",
                "workplace",
                new Vector2(6, 1.8f),
                furniture["KitchenSink"],
                WorldInteractionKind.Drink,
                .75f
            );
            var shelf = Image(root, "Pantry shelf", art["KitchenShelf"], new Vector2(6.2f, 3));
            shelf.enabled = true;
            shelf.transform.localScale = Vector3.one * .7f;
            Caption(root, "Office heading", "업무 구역", new Vector2(-3, 4.1f));
            Caption(root, "Pantry heading", "탕비실 · 휴게", new Vector2(6, 4.1f));
            // Distinct indoor bathroom: no exposed sanitary fixture in the working area.
            var bath = Room("office-bathroom", "사무실 화장실", new Vector2(430, 0), new Vector2(7, 7), true);
            rules.Zone("office-bathroom").cameraSize = 4.3f;
            Prop(
                "office-basin",
                "손 씻기",
                "office-bathroom",
                new Vector2(-1.7f, .5f),
                furniture["Basin"],
                WorldInteractionKind.Wash,
                .9f
            );
            Prop(
                "office-toilet",
                "화장실 이용",
                "office-bathroom",
                new Vector2(1.7f, .5f),
                furniture["Toilet"],
                WorldInteractionKind.Toilet,
                .9f
            );
            Door(
                "office-bath-front",
                "사무실 화장실",
                "workplace",
                new Vector2(7.8f, 3),
                "office-bathroom",
                new Vector2(0, -2),
                false
            ).UsePoint = Vector2.down * .8f;
            AdvancedDoorSetup.Attach(world, Find("office-bath-front"));
            Door(
                "office-bath-exit",
                "사무실로",
                "office-bathroom",
                new Vector2(0, -3.2f),
                "workplace",
                new Vector2(7.8f, 1.6f)
            );
            foreach (var actor in world.GetComponent<WorldNPCScheduler>().actors)
            {
                var animation = actor.GetComponent<WorldActorAnimation>();
                var walk = animation.library.Find(ActorMotion.Walk, ActorDirection.Down);
                float height = walk.frames[0].bounds.size.y * actor.visual.transform.lossyScale.y;
                actor.visual.transform.localScale *= 1.72f / Mathf.Max(.1f, height);
            }

            var exit = Find("work-exit");
            exit.transform.position = new Vector2(222.8f, -5.7f);
            exit.UsePoint = Vector2.up * .8f;
            AdvancedDoorSetup.Attach(world, exit);
            Find("office-front").arrival = new Vector2(2.8f, -4.5f);
            rules.Zone("workplace").spawn = new Vector2(2.8f, -4.5f);
            Find("office-front").title = "이안 사무실로";
            Boundary(root, "Front left", new Vector2(-3.55f, -6), new Vector2(10.9f, .2f));
            Boundary(root, "Front right", new Vector2(6.35f, -6), new Vector2(5.3f, .2f));
            foreach (
                var t in world
                    .transform.Cast<Transform>()
                    .Where(t => t.name == "Sign board / Workspace" || t.name == "World sign / Workspace")
            )
            {
                t.gameObject.SetActive(false);
            }
        }

        static void Facilities()
        {
            var desk = Find("stock-terminal").visual.sprite;
            Room("finance", "이안 증권", new Vector2(350, 0), new Vector2(12, 10));
            Room("realestate", "이안 부동산", new Vector2(390, 0), new Vector2(12, 10));
            Prop(
                "stock-terminal",
                "증권 거래 단말기",
                "finance",
                new Vector2(-2, 1),
                desk,
                WorldInteractionKind.StockMarket,
                .9f
            );
            Prop(
                "property-agency",
                "매물 및 주거 계약",
                "realestate",
                new Vector2(-2, 1),
                desk,
                WorldInteractionKind.RealEstate,
                .9f
            );
            Door(
                "finance-front",
                "이안 증권으로",
                "city",
                new Vector2(-12, .8f),
                "finance",
                new Vector2(0, -3.6f),
                true
            );
            Door("finance-exit", "도시 중심으로", "finance", new Vector2(0, -4.7f), "city", new Vector2(-12, -.7f));
            Door(
                "realestate-front",
                "이안 부동산으로",
                "city",
                new Vector2(12, .8f),
                "realestate",
                new Vector2(0, -3.6f),
                true
            );
            Door(
                "realestate-exit",
                "도시 중심으로",
                "realestate",
                new Vector2(0, -4.7f),
                "city",
                new Vector2(12, -.7f)
            );
            var sofa = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Furniture.png")
                .OfType<Sprite>()
                .First(s => s.name == "Settee");
            foreach (var zone in new[] { "finance", "realestate" })
            {
                Prop(zone + "-waiting", "상담 대기석", zone, new Vector2(3, 0), sofa, WorldInteractionKind.Rest, .9f);
                var root = world.transform.Cast<Transform>().First(t => t.name.EndsWith(" / " + zone));
                Caption(
                    root,
                    "Facility heading",
                    zone == "finance" ? "이안 증권 · 투자 상담" : "이안 부동산 · 주거 상담",
                    new Vector2(0, 3.1f),
                    10
                );
            }

            var city = Child(world.transform, "Facility street signs");
            city.position = rules.Zone("city").origin;
            Caption(city, "Finance sign", "이안 증권", new Vector2(-12, 3.1f));
            Caption(city, "Agency sign", "이안 부동산", new Vector2(12, 3.1f));
            foreach (var name in new[] { "Finance sign", "Agency sign" })
            {
                var text = city.Find(name);
                var board = Image(
                    city,
                    name + " board",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/UI/Pixel/Panel-Parchment.png"),
                    text.localPosition
                );
                board.drawMode = SpriteDrawMode.Sliced;
                board.size = new Vector2(3.4f, .55f);
                board.sortingOrder = 5999;
            }

            var officeSign = world
                .GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(t => t.name == "World sign / Workspace");
            if (officeSign != null)
            {
                officeSign.text = "이안 사무실";
                officeSign.gameObject.SetActive(true);
            }

            var officeBoard = world
                .GetComponentsInChildren<SpriteRenderer>(true)
                .FirstOrDefault(t => t.name == "Sign board / Workspace");
            if (officeBoard != null)
            {
                officeBoard.gameObject.SetActive(true);
            }

            var bench = Find("city-bench");
            bench.transform.position = rules.Zone("city").origin + new Vector2(4.3f, -8.1f);
            bench.visual.sortingOrder = 1810;
            foreach (var d in rules.npcs)
            {
                foreach (var e in d.schedule.Where(e => e.objectId == "city-bench"))
                {
                    e.position =
                        bench.Point
                        - rules.Zone("city").origin
                        + new Vector2((Array.IndexOf(rules.npcs, d) % 3 - 1) * .7f, 0);
                }
            }
        }

        public static void ApplyMenus()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            var theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");
            theme.paperButtons = true;
            theme.button = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/UI/Pixel/Button-Normal.png");
            theme.tab = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/UI/Pixel/Tab-Selected.png");
            EditorUtility.SetDirty(theme);
            foreach (
                var path in new[] { "LifeWorld", "City", "Workplace", "StockMarket", "School", "Home", "MainMenu" }
            )
            {
                var file = "Assets/_Game/Scenes/" + path + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(file) == null)
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(file);
                foreach (
                    var shell in UnityEngine.Object.FindObjectsByType<UIShell>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    )
                )
                {
                    var palette = Component<PixelWindowPalette>(shell.gameObject);
                    palette.theme = theme;
                    palette.normal = theme.button;
                    palette.hover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/UI/Pixel/Button-Hover.png");
                    palette.pressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/UI/Pixel/Button-Pressed.png");
                    palette.disabled = AssetDatabase.LoadAssetAtPath<Sprite>(
                        "Assets/_Game/UI/Pixel/Button-Disabled.png"
                    );
                    palette.Apply();
                    var property = shell.GetComponent<PropertyPresenter>();
                    if (property != null)
                    {
                        var row = property
                            .scroll.content.Cast<Transform>()
                            .FirstOrDefault(t => t.name == "Property image and terms");
                        if (row == null)
                        {
                            row = new GameObject(
                                "Property image and terms",
                                typeof(RectTransform),
                                typeof(LayoutElement),
                                typeof(PropertyDetailsLayout)
                            ).transform;
                            row.SetParent(property.scroll.content, false);
                            row.SetSiblingIndex(1);
                            property.body.transform.SetParent(row, false);
                            property.preview.transform.SetParent(row, false);
                        }

                        var details = row.GetComponent<PropertyDetailsLayout>();
                        details.body = property.body;
                        details.preview = property.preview;
                        var actionRoot = property.actions[0].transform.parent as RectTransform;
                        var card = property.scroll.transform.parent;
                        actionRoot.SetParent(card, false);
                        var fitter = actionRoot.GetComponent<ContentSizeFitter>();
                        if (fitter != null)
                        {
                            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                        }

                        var dock = Component<PropertyActionDock>(card.gameObject);
                        dock.heading = (RectTransform)property.heading.transform.parent;
                        dock.close = (RectTransform)property.close.transform;
                        dock.tabs = (RectTransform)property.tabs[0].transform.parent;
                        dock.feedback = property.feedback;
                        dock.actions = actionRoot;
                        dock.scroll = (RectTransform)property.scroll.transform;
                        dock.grid = actionRoot.GetComponent<GridLayoutGroup>();
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
        }

        [MenuItem("Ian/Life World/Recovery/Apply Facility And Workspace Layout")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            world = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
            if (world == null)
            {
                throw new Exception("Open existing LifeWorld scene first");
            }

            rules = AssetDatabase.LoadAssetAtPath<LifeWorldRules>("Assets/_Game/Data/Phase26/LifeWorldRules.asset");
            art = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Architecture.png")
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
            Office();
            Facilities();
            CriticalRecoveryLayout();
            var theme = Component<WorldUIRecoveryTheme>(world.hud.gameObject);
            theme.paper = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/UI/Pixel/Panel-Parchment.png");
            theme.Apply();
            foreach (var id in new[] { "shop-exit", "school-exit" })
            {
                var o = Find(id);
                o.UsePoint = Vector2.up * .8f;
                AdvancedDoorSetup.Attach(world, o);
            }

            EditorUtility.SetDirty(rules);
            EditorUtility.SetDirty(world);
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            EditorSceneManager.SaveScene(world.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
    }
}
