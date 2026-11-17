using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    // Replaces presentation only; the original interaction objects, save schema and
    // economy remain authoritative. No borrowed commercial game assets are used.
    public static class Phase26VisualRebuild
    {
        const string Folder = "Assets/_Game/Art/VisualRebuild/";
        const string Data = "Assets/_Game/Data/Phase26/";
        static readonly string[] Architecture =
        {
            "OakFloor",
            "KitchenFloor",
            "JadeFloor",
            "IvoryWall",
            "JadeWall",
            "KitchenWall",
            "VerticalWall",
            "CutawayWall",
            "WindowDay",
            "DoorArch",
            "KitchenShelf",
            "EntryCoats",
            "Plant",
            "Laundry",
            "Rug",
            "WindowNight",
        };
        static readonly string[] Furniture =
        {
            "Bed",
            "Nightstand",
            "Wardrobe",
            "WritingDesk",
            "Chair",
            "DiningTable",
            "Fridge",
            "KitchenSink",
            "Cooker",
            "Basin",
            "Toilet",
            "Shower",
            "BathDoor",
            "Threshold",
            "Bookcase",
            "Settee",
        };
        static LifeWorldController world;
        static LifeWorldRules rules;
        static Transform root;
        static Sprite[] a;
        static Sprite[] f;
        static readonly List<HomeFixedFootprint> fixedRects = new List<HomeFixedFootprint>();
        public static bool Ready => File.Exists(Folder + "Architecture.png") && File.Exists(Folder + "Furniture.png");

        [MenuItem("Ian/Life World/Apply Visual Rebuild Home")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play before applying visual assets");
            }

            if (!Ready)
            {
                throw new Exception("Both reviewed alpha atlases must be present");
            }

            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (previous.isDirty)
            {
                throw new Exception("Save current scene before applying");
            }

            string path = previous.path;
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
            ApplyIfReady(UnityEngine.Object.FindAnyObjectByType<LifeWorldController>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (path != scene.path)
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        static Sprite[] Import(string file, string[] names, bool architecture)
        {
            string path = Folder + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var cached = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            string stamp = "Ian/VisualRebuild/v3/" + File.GetLastWriteTimeUtc(path).Ticks;
            if (importer.userData == stamp && names.All(cached.ContainsKey))
            {
                return names.Select(n => cached[n]).ToArray();
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            importer.spritePixelsPerUnit = sourceWidth / 384f * 40;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var pixels = texture.GetPixels32();
            int width = texture.width,
                height = texture.height;
            if (pixels[0].a > 2)
            {
                throw new Exception("True zero-alpha background required: " + file);
            }

            var slices = new SpriteMetaData[16];
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    int index = row * 4 + col,
                        x0 = Mathf.RoundToInt(col * width / 4f),
                        x1 = Mathf.RoundToInt((col + 1) * width / 4f),
                        y0 = Mathf.RoundToInt((3 - row) * height / 4f),
                        y1 = Mathf.RoundToInt((4 - row) * height / 4f);
                    int minX = x1,
                        maxX = x0,
                        minY = y1,
                        maxY = y0;
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            if (pixels[y * width + x].a >= 250)
                            {
                                minX = Mathf.Min(minX, x);
                                maxX = Mathf.Max(maxX, x);
                                minY = Mathf.Min(minY, y);
                                maxY = Mathf.Max(maxY, y);
                            }
                        }
                    }

                    if (minX > maxX || minY > maxY)
                    {
                        throw new Exception("Empty atlas cell " + names[index]);
                    }

                    bool floor = architecture && index < 3;
                    int inset = Mathf.CeilToInt(sourceWidth / 1536f * 10);
                    if (floor)
                    {
                        minX += inset;
                        maxX -= inset;
                        minY += inset;
                        maxY -= inset;
                    }
                    else if (architecture && (index == 3 || index == 4 || index == 5 || index == 7))
                    {
                        minX += inset;
                        maxX -= inset;
                    }

                    slices[index] = new SpriteMetaData
                    {
                        name = names[index],
                        rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                        alignment = 9,
                        pivot = floor ? new Vector2(.5f, .5f) : new Vector2(.5f, 0),
                    };
                }
            }

            var parts = slices.ToList();
            if (architecture)
            {
                var doorway = slices[9];
                doorway.name = "DoorwayCutaway";
                doorway.rect = new Rect(
                    doorway.rect.x,
                    doorway.rect.y,
                    doorway.rect.width,
                    Mathf.Round(importer.spritePixelsPerUnit * .35f)
                );
                parts.Add(doorway);
                var shoes = slices[11];
                shoes.name = "ShoeRack";
                shoes.rect = new Rect(
                    shoes.rect.x,
                    shoes.rect.y,
                    shoes.rect.width,
                    Mathf.Round(shoes.rect.height * .42f)
                );
                parts.Add(shoes);
            }
            else
            {
                var threshold = slices[13];
                threshold.name = "ThresholdLow";
                threshold.rect = new Rect(
                    threshold.rect.x,
                    threshold.rect.y,
                    threshold.rect.width,
                    threshold.rect.height > importer.spritePixelsPerUnit * .6f
                        ? Mathf.Round(threshold.rect.height * .48f)
                        : threshold.rect.height
                );
                parts.Add(threshold);
            }

            importer.spritesheet = parts.ToArray();
            importer.isReadable = false;
            importer.userData = stamp;
            importer.SaveAndReimport();
            var result = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            return names.Select(n => result[n]).ToArray();
        }

        static Sprite Part(string file, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(Folder + file).OfType<Sprite>().First(s => s.name == name);

        static Transform Child(string name)
        {
            var t = root.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(root, false);
            }

            return t;
        }

        static SpriteRenderer Draw(string name, Sprite sprite, Vector2 p, int order = 0, Vector2? tiled = null)
        {
            var t = Child(name);
            t.localPosition = p;
            t.localScale = Vector3.one;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = t.gameObject.AddComponent<SpriteRenderer>();
            }

            sr.sprite = sprite;
            sr.color = Color.white;
            sr.enabled = true;
            sr.sortingOrder = order == 0 ? 1000 - Mathf.RoundToInt(t.position.y * 100) : order;
            sr.drawMode = tiled.HasValue ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple;
            if (tiled.HasValue)
            {
                sr.size = tiled.Value;
            }

            return sr;
        }

        static void Floor(string name, Sprite sprite, Rect bounds, Vector2 origin)
        {
            Draw(name, sprite, origin + bounds.center, -5000, bounds.size);
        }

        static void Solid(string name, Rect bounds, bool home = true)
        {
            var t = Child(name);
            t.localPosition = bounds.center;
            var c = t.GetComponent<BoxCollider2D>();
            if (c == null)
            {
                c = t.gameObject.AddComponent<BoxCollider2D>();
            }

            c.size = bounds.size;
            c.offset = Vector2.zero;
            if (home)
            {
                fixedRects.Add(new HomeFixedFootprint { id = name, bounds = bounds });
            }
        }

        static void HWall(string name, float left, float right, float y, bool rear = false, bool kitchen = false)
        {
            var sprite = a[rear ? (kitchen ? 5 : 3) : 7];
            Draw(name, sprite, new Vector2((left + right) / 2, y), 0, new Vector2(right - left, sprite.bounds.size.y));
            if (!rear)
            {
                Solid(name + " collision", new Rect(left, y - .1f, right - left, .2f));
            }
        }

        static void VWall(string name, float x, float bottom, float top)
        {
            Draw(name, a[6], new Vector2(x, bottom), 0, new Vector2(a[6].bounds.size.x, top - bottom));
            Solid(name + " collision", new Rect(x - .12f, bottom, .24f, top - bottom));
        }

        static WorldInteractable Object(string id) =>
            world.GetComponentsInChildren<WorldInteractable>(true).FirstOrDefault(o => o.id == id);

        static WorldInteractable Fit(
            string id,
            Sprite sprite,
            Vector2 p,
            Vector2 footprint,
            Vector2 approach,
            string title = null,
            WorldInteractionKind kind = WorldInteractionKind.Rest,
            bool improved = false
        )
        {
            var o = Object(id);
            if (o == null)
            {
                var go = new GameObject(id, typeof(WorldInteractable));
                go.transform.SetParent(world.transform, false);
                o = go.GetComponent<WorldInteractable>();
                o.id = id;
                o.zone = "home";
                o.title = title ?? id;
                o.kind = kind;
                world.objects = world.objects.Concat(new[] { o }).ToArray();
            }

            if (title != null)
            {
                o.title = title;
            }

            o.gameObject.SetActive(true);
            o.transform.localScale = Vector3.one;
            o.transform.position = rules.Zone(o.zone).origin + p;
            o.UsePoint = approach;
            o.radius = 1.15f;
            if (o.visual != null)
            {
                o.visual.enabled = false;
            }

            var old = o.GetComponent<SpriteRenderer>();
            if (old != null)
            {
                old.enabled = false;
            }

            var t = o.transform.Find("Rebuild native visual");
            if (t == null)
            {
                t = new GameObject("Rebuild native visual", typeof(SpriteRenderer)).transform;
                t.SetParent(o.transform, false);
            }

            t.localScale = Vector3.one;
            t.localPosition = Vector3.up * (-sprite.bounds.min.y);
            var sr = t.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.enabled = true;
            sr.color = Color.white;
            sr.sortingOrder = 1000 - Mathf.RoundToInt(o.transform.position.y * 100);
            o.visual = sr;
            if (o.highlight != null)
            {
                o.highlight.transform.SetParent(t, false);
                o.highlight.transform.localPosition = Vector3.zero;
                o.highlight.transform.localScale = Vector3.one * 1.03f;
                o.highlight.sprite = sprite;
                o.highlight.sortingOrder = sr.sortingOrder - 1;
                o.highlight.enabled = false;
            }

            var col = o.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = o.gameObject.AddComponent<BoxCollider2D>();
            }

            col.enabled = footprint.x > 0;
            col.size = col.enabled ? footprint : Vector2.one * .1f;
            col.offset = Vector2.up * (footprint.y / 2);
            if (o.zone == "home" && col.enabled)
            {
                fixedRects.Add(
                    new HomeFixedFootprint
                    {
                        id = id,
                        bounds = new Rect(p.x - footprint.x / 2, p.y, footprint.x, footprint.y),
                        improvedOnly = improved,
                    }
                );
            }

            o.shadow = Phase26ArtSetup.Mask(
                o.transform,
                "Contact shadow",
                Vector2.zero,
                new Vector2(Mathf.Max(.3f, footprint.x), .15f),
                new Color(.12f, .08f, .15f, .23f),
                sr.sortingOrder - 2
            );
            return o;
        }

        static void ResizeRoom(string id, Vector2 size, float top, float zoom, Vector2 spawn)
        {
            var zone = rules.Zone(id);
            zone.size = size;
            zone.walkableTop = top;
            zone.cameraSize = zoom;
            zone.spawn = spawn;
            var floor = world
                .GetComponentsInChildren<SpriteRenderer>(true)
                .First(s => s.name == "Walkable floor" && s.transform.parent.name.EndsWith(" / " + id));
            floor.enabled = false;
            foreach (var c in floor.transform.parent.GetComponentsInChildren<BoxCollider2D>())
            {
                Vector2 p,
                    s;
                switch (c.name)
                {
                    case "Back wall":
                        p = new Vector2(0, top);
                        s = new Vector2(size.x, .2f);
                        break;
                    case "Front cutaway":
                        p = new Vector2(0, -size.y / 2);
                        s = new Vector2(size.x, .2f);
                        break;
                    case "Left wall":
                        p = new Vector2(-size.x / 2, 0);
                        s = new Vector2(.2f, size.y);
                        break;
                    case "Right wall":
                        p = new Vector2(size.x / 2, 0);
                        s = new Vector2(.2f, size.y);
                        break;
                    default:
                        continue;
                }

                c.transform.localPosition = p;
                c.transform.localScale = Vector3.one;
                c.size = s;
                c.offset = Vector2.zero;
            }
        }

        public static bool ApplyIfReady(LifeWorldController target)
        {
            if (!Ready)
            {
                return false;
            }

            world = target;
            if (world == null)
            {
                throw new Exception("Existing LifeWorld controller missing");
            }

            rules = AssetDatabase.LoadAssetAtPath<LifeWorldRules>(Data + "LifeWorldRules.asset");
            a = Import("Architecture.png", Architecture, true);
            f = Import("Furniture.png", Furniture, false);
            var roots = world
                .transform.Cast<Transform>()
                .Where(t => t.name == "Visual Rebuild / authored home")
                .ToArray();
            root = roots.FirstOrDefault();
            foreach (var previous in roots.Skip(1))
            {
                previous.gameObject.SetActive(false);
            }

            if (root == null)
            {
                root = new GameObject("Visual Rebuild / authored home").transform;
                root.SetParent(world.transform, false);
            }

            root.gameObject.SetActive(true);
            root.localPosition = Vector3.zero;
            fixedRects.Clear();
            ResizeRoom("home", new Vector2(12, 8), 2.5f, 4.5f, new Vector2(0, -2.9f));
            ResizeRoom("bathroom", new Vector2(6, 5.5f), 1.25f, 3, new Vector2(0, -1.8f));
            Floor("Bedroom wood floor", a[0], new Rect(-6, -.75f, 6, 3.25f), Vector2.zero);
            Floor("Kitchen linoleum", a[1], new Rect(0, -.75f, 6, 3.25f), Vector2.zero);
            Floor("Living wood floor", a[0], new Rect(-3.5f, -4, 9.5f, 3.25f), Vector2.zero);
            Solid("Outside house notch", new Rect(-6, -4, 2.5f, 3.25f));
            HWall("Bedroom back wall", -6, 0, 2.5f, true);
            HWall("Kitchen backsplash", 0, 6, 2.5f, true, true);
            VWall("Bedroom kitchen partition", 0, -.75f, 2.5f);
            VWall("Bedroom west wall", -6, -.75f, 2.5f);
            VWall("Living west wall", -3.5f, -4, -.75f);
            VWall("House east wall", 6, -4, 2.5f);
            HWall("Bedroom cutaway left", -6, -3, -.75f);
            HWall("Bedroom cutaway right", -1.5f, 0, -.75f);
            HWall("Kitchen cutaway", 2.5f, 6, -.25f);
            HWall("Entry cutaway left", -3.5f, -.9f, -4);
            HWall("Entry cutaway right", .9f, 6, -4);
            Draw("Bedroom open doorway", Part("Architecture.png", "DoorwayCutaway"), new Vector2(-2.25f, -.75f));
            var window = Draw("Bedroom window", a[8], new Vector2(-4.7f, 2.25f));
            var change = window.GetComponent<WorldRoomBackdrop>();
            if (change == null)
            {
                change = window.gameObject.AddComponent<WorldRoomBackdrop>();
            }

            change.floor = window;
            change.day = a[8];
            change.night = a[15];
            Fit("bed", f[0], new Vector2(-4.75f, .75f), new Vector2(1.2f, 1.25f), new Vector2(1.25f, -.25f));
            Fit("bedside-table", f[1], new Vector2(-3.75f, 1), new Vector2(.7f, .4f), Vector2.down * .7f);
            Fit("closet", f[2], new Vector2(-.75f, 1.25f), new Vector2(.85f, .5f), Vector2.down * .8f);
            var desk = Fit(
                "starter-desk",
                f[3],
                new Vector2(-2.25f, 1.5f),
                new Vector2(1.2f, .5f),
                new Vector2(1, -.7f)
            );
            world.starterWritingDesk = desk.gameObject;
            Fit("writing-chair", f[4], new Vector2(-2.25f, .5f), new Vector2(.55f, .35f), Vector2.down * .7f);
            float fridgeX = 5.75f - f[6].bounds.size.x / 2,
                sinkX = fridgeX - f[6].bounds.size.x / 2 - .12f - f[7].bounds.size.x / 2,
                cookerX = sinkX - f[7].bounds.size.x / 2 - .12f - f[8].bounds.size.x / 2;
            Fit(
                "fridge",
                f[6],
                new Vector2(fridgeX, 1.25f),
                new Vector2(f[6].bounds.size.x * .9f, .6f),
                Vector2.down * .8f
            );
            Fit(
                "tap-home",
                f[7],
                new Vector2(sinkX, 1.25f),
                new Vector2(f[7].bounds.size.x * .9f, .6f),
                Vector2.down * .8f
            );
            Fit(
                "kitchen-prep",
                f[8],
                new Vector2(cookerX, 1.25f),
                new Vector2(f[8].bounds.size.x * .9f, .6f),
                Vector2.down * .8f
            );
            Draw("Kitchen dish shelf", a[10], new Vector2(sinkX, 2.85f));
            Fit("meal-table", f[5], new Vector2(4.25f, -1.8f), new Vector2(1.3f, .6f), new Vector2(-1.2f, .15f));
            Fit("home-chair", f[4], new Vector2(4.25f, -2.8f), new Vector2(.55f, .35f), Vector2.down * .65f);
            Fit(
                "living-settee",
                f[15],
                new Vector2(-.75f, -2.25f),
                new Vector2(1.6f, .55f),
                Vector2.down * .8f,
                "소파에서 쉬기"
            );
            Draw("Living woven rug", a[14], new Vector2(-.5f, -3.3f), -4000);
            var coats = Fit(
                "entry-shoes",
                Part("Architecture.png", "ShoeRack"),
                new Vector2(1.25f, -3.5f),
                new Vector2(1, .35f),
                Vector2.left * .8f
            );
            coats.title = "외출 준비하기";
            Draw("Bedroom laundry basket", a[13], new Vector2(-5.3f, -.4f));
            Solid("Laundry footprint", new Rect(-5.65f, -.4f, .7f, .3f));
            Draw("Living houseplant", a[12], new Vector2(5.35f, -3.55f));
            Solid("Plant footprint", new Rect(5.05f, -3.55f, .6f, .3f));
            Fit(
                "upgrade-desk",
                f[3],
                new Vector2(-2.25f, 1.5f),
                new Vector2(1.2f, .5f),
                new Vector2(1, -.7f),
                improved: true
            );
            Fit(
                "upgrade-storage",
                f[14],
                new Vector2(1.75f, -.9f),
                new Vector2(1, .4f),
                Vector2.down * .7f,
                improved: true
            );
            world.upgradedHome.SetActive(false);
            var front = Fit(
                "front-door",
                Part("Furniture.png", "ThresholdLow"),
                new Vector2(0, -3.9f),
                Vector2.zero,
                Vector2.up * .9f,
                "밖으로 나가기"
            );
            front.animateDoorLeaf = false;
            var bath = Fit(
                "bath-door",
                f[12],
                new Vector2(1, 2.4f),
                Vector2.zero,
                Vector2.down * .85f,
                "화장실 문 열기"
            );
            bath.arrival = new Vector2(0, -1.8f);
            Object("house-entrance").arrival = new Vector2(0, -2.9f);
            var oldDivider = Object("kitchen-divider");
            if (oldDivider != null)
            {
                oldDivider.gameObject.SetActive(false);
            }

            var oldShelf = Object("kitchen-shelf");
            if (oldShelf != null)
            {
                oldShelf.gameObject.SetActive(false);
            }

            Vector2 b = rules.Zone("bathroom").origin;
            Floor("Bathroom jade floor", a[2], new Rect(-3, -2.75f, 6, 4), b);
            Draw("Bathroom jade back wall", a[4], b + new Vector2(0, 1.25f), 0, new Vector2(6, a[4].bounds.size.y));
            Draw("Bathroom front cutaway", a[7], b + new Vector2(0, -2.75f), 0, new Vector2(6, a[7].bounds.size.y));
            Fit("sink", f[9], new Vector2(-1.8f, .4f), new Vector2(.9f, .4f), Vector2.down * .8f);
            Fit("toilet", f[10], new Vector2(0, .4f), new Vector2(.7f, .55f), Vector2.down * .8f);
            Fit("shower", f[11], new Vector2(1.8f, .25f), new Vector2(1.15f, .7f), Vector2.down * .8f);
            var exit = Fit(
                "bath-exit",
                Part("Furniture.png", "ThresholdLow"),
                new Vector2(0, -2.6f),
                Vector2.zero,
                Vector2.up * .8f,
                "집으로 돌아가기"
            );
            exit.arrival = new Vector2(1, 1.35f);
            exit.animateDoorLeaf = false;
            var glow = Phase26ArtSetup.Mask(
                root,
                "Bedside warm pool",
                new Vector2(-3.75f, 1.4f),
                new Vector2(1.5f, 1),
                new Color(1, .64f, .24f, .17f),
                1100,
                true
            );
            glow.enabled = false;
            world.lamps = world.lamps.Concat(new[] { glow }).Distinct().ToArray();
            world.Player.visualScale = .8f;
            world.Player.bodyVisual.localScale = new Vector3(.8f, .8f, 1);
            world.follow.target.GetComponent<WorldPixelViewport>().interiorLogicalPixelsPerUnit = 40;
            Size(world.hud.cash, 26);
            Size(world.hud.needs, 24);
            var vital = world.hud.needs.rectTransform.parent as RectTransform;
            if (vital != null)
            {
                vital.anchorMin = new Vector2(.018f, .02f);
                vital.anchorMax = new Vector2(.46f, .085f);
                vital.offsetMin = vital.offsetMax = Vector2.zero;
            }

            Size(world.hud.prompt, 24);
            foreach (var bar in world.hud.GetComponentsInChildren<CompactWorldStatusBar>(true))
            {
                Size(bar.place, 24);
                Size(bar.time, 26);
                var place = bar.place.GetComponent<UnityEngine.UI.LayoutElement>();
                var time = bar.time.GetComponent<UnityEngine.UI.LayoutElement>();
                if (place != null)
                {
                    place.minWidth = 100;
                    place.preferredWidth = 180;
                    place.flexibleWidth = 1;
                }

                if (time != null)
                {
                    time.minWidth = 195;
                    time.preferredWidth = 220;
                    time.flexibleWidth = 0;
                }
            }

            var shader = Shader.Find("Ian/WorldAlphaCut");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
            {
                throw new Exception("Pixel alpha shader must compile before applying");
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(Folder + "WorldAlphaCut.mat");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, Folder + "WorldAlphaCut.mat");
            }

            foreach (var renderer in world.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite != null && AssetDatabase.GetAssetPath(renderer.sprite).StartsWith(Folder))
                {
                    renderer.sharedMaterial = mat;
                }
            }

            Plan();
            AssetDatabase.LoadAssetAtPath<HomeLayoutPlan>(Data + "HomeLayoutPlan.asset").furnitureMaterial = mat;
            var catalog = AssetDatabase.LoadAssetAtPath<WorldVisualCatalog>(Data + "WorldVisualCatalog.asset");
            catalog.homeArchitecture = a;
            catalog.homeProps = f;
            EditorUtility.SetDirty(catalog);
            world.roomBackdrops = world.GetComponentsInChildren<WorldRoomBackdrop>(true);
            world.follow.target.orthographicSize = 4.5f;
            world.follow.target.targetTexture = null;
            world.follow.target.targetDisplay = 0;
            world.follow.target.transform.position = new Vector3(0, 0, -10);
            EditorUtility.SetDirty(world.Player);
            EditorUtility.SetDirty(world);
            EditorUtility.SetDirty(rules);
            Physics2D.SyncTransforms();
            return true;
        }

        static void Size(TMPro.TMP_Text text, float size)
        {
            text.fontSize = size;
            var scale = text.GetComponent<UITypeScale>();
            if (scale != null)
            {
                scale.baseSize = size;
                EditorUtility.SetDirty(scale);
            }
        }

        static void Plan()
        {
            var plan = AssetDatabase.LoadAssetAtPath<HomeLayoutPlan>(Data + "HomeLayoutPlan.asset");
            plan.version = 4;
            plan.floor = new Rect(-5.85f, -3.85f, 11.7f, 6.2f);
            plan.entrance = new Vector2(0, -2.9f);
            plan.fixedFootprints = fixedRects.Where(r => r.id != "upgrade-desk").ToArray();
            plan.reserved = new[]
            {
                new Rect(-.6f, -3.5f, 1.2f, 1.5f),
                new Rect(-3.05f, -1.25f, 1.6f, 1.05f),
                new Rect(.4f, 1.2f, 1.3f, 1.15f),
            };
            plan.regions = new[]
            {
                new HomeAreaRegion
                {
                    title = "침실",
                    area = HomeArea.Bedroom | HomeArea.Study,
                    bounds = new Rect(-5.85f, -.6f, 5.45f, 2.95f),
                },
                new HomeAreaRegion
                {
                    title = "주방",
                    area = HomeArea.Kitchen,
                    bounds = new Rect(.4f, -.6f, 5.45f, 2.95f),
                },
                new HomeAreaRegion
                {
                    title = "식사 공간",
                    area = HomeArea.Dining,
                    bounds = new Rect(2.75f, -3.6f, 3.1f, 2.9f),
                },
                new HomeAreaRegion
                {
                    title = "생활 공간",
                    area = HomeArea.Living,
                    bounds = new Rect(-3.35f, -3.85f, 9.2f, 3.1f),
                },
            };
            plan.accessPoints = new[]
            {
                "bed",
                "closet",
                "starter-desk",
                "writing-chair",
                "fridge",
                "tap-home",
                "kitchen-prep",
                "meal-table",
                "home-chair",
                "living-settee",
                "front-door",
                "bath-door",
            }
                .Select(id => Object(id).Point)
                .ToArray();
            plan.furniture = new[]
            {
                new HomeFurnitureRule
                {
                    id = "bookshelf",
                    areas = HomeArea.Living,
                    footprint = new Vector2(1, .35f),
                    height = f[14].bounds.size.y,
                    slots = new[] { new Vector2(-2.75f, -2.25f) },
                },
                new HomeFurnitureRule
                {
                    id = "lamp",
                    areas = HomeArea.Living | HomeArea.Bedroom,
                    footprint = new Vector2(.55f, .35f),
                    approach = Vector2.left * .75f,
                    height = f[1].bounds.size.y,
                    slots = new[] { new Vector2(1.5f, -1.25f), new Vector2(-.75f, -.25f) },
                },
                new HomeFurnitureRule
                {
                    id = "chair",
                    areas = HomeArea.Living | HomeArea.Dining,
                    footprint = new Vector2(.55f, .35f),
                    height = f[4].bounds.size.y,
                    slots = new[] { new Vector2(2.25f, -2.25f), new Vector2(1.5f, -2.75f) },
                },
            };
            var housing = AssetDatabase.LoadAssetAtPath<HousingCatalog>(
                "Assets/_Game/Data/Phase256/HousingCatalog.asset"
            );
            housing.placementPlan = plan;
            foreach (
                var pair in new[] { ("basic-bed", 0), ("basic-table", 5), ("bookshelf", 14), ("lamp", 1), ("chair", 4) }
            )
            {
                var item = housing.Furniture(pair.Item1);
                if (item != null)
                {
                    var previous = item.preview;
                    item.preview = f[pair.Item2];
                    foreach (var image in world.hud.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    {
                        if (image.sprite == previous)
                        {
                            image.sprite = item.preview;
                            image.material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "WorldAlphaCut.mat");
                        }
                    }

                    var rule = plan.Rule(item.id);
                    if (rule != null)
                    {
                        item.position = rule.slots[0] / 2;
                    }
                }
            }

            foreach (
                var home in new[]
                {
                    new HousingSnapshot(),
                    new HousingSnapshot { residenceId = "improved" },
                }
            )
            {
                if (!plan.ValidateAccess(home, out var reason))
                {
                    throw new Exception("Visual rebuild preflight: " + reason);
                }

                foreach (var rule in plan.furniture)
                {
                    if (!plan.Find(rule.id, home, null, out var position, out reason))
                    {
                        throw new Exception(
                            "No safe " + home.residenceId + " placement for " + rule.id + ": " + reason
                        );
                    }
                }
            }

            EditorUtility.SetDirty(plan);
            EditorUtility.SetDirty(housing);
        }
    }
}
