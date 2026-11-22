using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static class Phase26ArtSetup
    {
        const string Folder = "Assets/_Game/Art/Phase26Recovery/";
        const string Catalog = "Assets/_Game/Data/World/Shared/WorldVisualCatalog.asset";
        static string[] Furniture =
        {
            "Bed",
            "Fridge",
            "Table",
            "Wardrobe",
            "Sink",
            "Toilet",
            "Shower",
            "Door",
            "Desk",
            "Books",
            "Chair",
            "Bench",
            "Tree",
            "Lamp",
            "Bin",
            "Pole",
        };
        static string[] Urban = { "Shop", "Workspace", "School", "Residence", "Car", "Signal" };

        static TextureImporter Configure(string file)
        {
            var path = Folder + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var i = (TextureImporter)AssetImporter.GetAtPath(path);
            i.textureType = TextureImporterType.Sprite;
            i.mipmapEnabled = false;
            i.filterMode = FilterMode.Point;
            i.textureCompression = TextureImporterCompression.Uncompressed;
            i.maxTextureSize = 4096;
            i.npotScale = TextureImporterNPOTScale.None;
            i.alphaIsTransparency = true;
            i.wrapMode = TextureWrapMode.Clamp;
            return i;
        }

        internal static Sprite Floor(string file)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + file);
            var old = AssetImporter.GetAtPath(Folder + file) as TextureImporter;
            if (
                existing != null
                && old != null
                && old.spriteImportMode == SpriteImportMode.Single
                && old.filterMode == FilterMode.Point
                && !old.isReadable
                && old.maxTextureSize == 4096
            )
            {
                return existing;
            }

            var i = Configure(file);
            i.spriteImportMode = SpriteImportMode.Single;
            i.spritePixelsPerUnit = 48;
            var s = new TextureImporterSettings();
            i.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteMeshType = SpriteMeshType.FullRect;
            i.SetTextureSettings(s);
            i.isReadable = false;
            i.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(Folder + file);
        }

        internal static Sprite[] Grid(string file, int columns, int rows, string[] names, float cellWorldSize)
        {
            var old = AssetImporter.GetAtPath(Folder + file) as TextureImporter;
            var cached = AssetDatabase.LoadAllAssetsAtPath(Folder + file).OfType<Sprite>().ToDictionary(s => s.name);
            if (
                old != null
                && old.userData == "Phase26Recovery/v2"
                && !old.isReadable
                && old.spriteImportMode == SpriteImportMode.Multiple
                && names.All(n => cached.ContainsKey(n))
            )
            {
                return names.Select(n => cached[n]).ToArray();
            }

            var i = Configure(file);
            i.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            i.spriteImportMode = SpriteImportMode.Multiple;
            i.spritePixelsPerUnit = sourceHeight / (float)rows / cellWorldSize;
            i.isReadable = true;
            i.SaveAndReimport();
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + file);
            int width = t.width,
                height = t.height;
            var pixels = t.GetPixels32();
            if (pixels[0].a > 2)
            {
                throw new Exception(file + " background is not transparent");
            }

            var slices = new SpriteMetaData[columns * rows];
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col,
                        x = Mathf.RoundToInt(col * width / (float)columns),
                        x1 = Mathf.RoundToInt((col + 1) * width / (float)columns),
                        y = Mathf.RoundToInt((rows - 1 - row) * height / (float)rows),
                        y1 = Mathf.RoundToInt((rows - row) * height / (float)rows);
                    int foot = -1;
                    int minX = x1,
                        maxX = x,
                        maxY = y;
                    for (int py = y; py < y1; py++)
                    {
                        for (int px = x; px < x1; px++)
                        {
                            if (pixels[py * width + px].a >= 220)
                            {
                                if (foot < 0)
                                {
                                    foot = py;
                                }

                                minX = Mathf.Min(minX, px);
                                maxX = Mathf.Max(maxX, px);
                                maxY = Mathf.Max(maxY, py);
                            }
                        }
                    }

                    if (foot < 0)
                    {
                        throw new Exception("Empty atlas cell: " + file + " / " + index);
                    }

                    int left = Mathf.Max(x, minX - 2),
                        bottom = Mathf.Max(y, foot - 2),
                        right = Mathf.Min(x1, maxX + 3),
                        top = Mathf.Min(y1, maxY + 3);
                    slices[index] = new SpriteMetaData
                    {
                        name = names[index],
                        rect = new Rect(left, bottom, right - left, top - bottom),
                        alignment = 9,
                        pivot = new Vector2(
                            ((x + x1) * .5f - left) / (right - left),
                            (foot + .5f - bottom) / (top - bottom)
                        ),
                    };
                }
            }

            i.spritesheet = slices;
            i.isReadable = false;
            i.userData = "Phase26Recovery/v2";
            i.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Folder + file).OfType<Sprite>().ToDictionary(s => s.name);
            return names.Select(n => sprites[n]).ToArray();
        }

        [MenuItem("Ian/Life World/Apply Authored Recovery Art")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play before applying art");
            }

            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (previous.isDirty)
            {
                throw new Exception("Save current scene first");
            }

            string previousPath = previous.path;
            var data = AssetDatabase.LoadAssetAtPath<WorldVisualCatalog>(Catalog);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WorldVisualCatalog>();
                AssetDatabase.CreateAsset(data, Catalog);
            }

            data.homeShell = Floor("HomeShell.png");
            data.cityGround = Floor("CityGround.png");
            data.bathroomShell = Floor("BathroomShell.png");
            data.furniture = Grid("FurnitureAtlas.png", 4, 4, Furniture, 3.2f);
            data.urban = Grid("UrbanAtlas.png", 3, 2, Urban, 5.12f);
            if (File.Exists(Folder + "HomeShellNight.png"))
            {
                data.nightHomeShell = Floor("HomeShellNight.png");
            }

            if (File.Exists(Folder + "SignalStates.png"))
            {
                data.signalStates = Grid("SignalStates.png", 3, 1, new[] { "CarsGo", "PedestriansGo", "AllRed" }, 3.4f);
            }

            string[] names = Enumerable.Range(0, 24).Select(n => "Citizen_" + (n / 6) + "_" + (n % 6)).ToArray();
            var frames = Grid("PedestriansAtlas.png", 6, 4, names, 2.133333f);
            data.pedestrians = Enumerable
                .Range(0, 4)
                .Select(row => new WorldPedestrianLook { frames = frames.Skip(row * 6).Take(6).ToArray() })
                .ToArray();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
            ApplyToWorld(UnityEngine.Object.FindAnyObjectByType<LifeWorldController>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previousPath))
            {
                EditorSceneManager.OpenScene(previousPath);
            }
        }

        public static bool ApplyToWorld(LifeWorldController world)
        {
            var data = AssetDatabase.LoadAssetAtPath<WorldVisualCatalog>(Catalog);
            if (data == null)
            {
                return false;
            }

            var rules = AssetDatabase.LoadAssetAtPath<LifeWorldRules>(
                "Assets/_Game/Data/World/Shared/LifeWorldRules.asset"
            );
            rules.Zone("alley").cameraSize = 360f / 54;
            rules.indoorCameraSize = 360f / 64;
            rules.outdoorCameraSize = 360f / 44;
            var viewport = world.follow.target.GetComponent<WorldPixelViewport>();
            if (viewport == null)
            {
                viewport = world.follow.target.gameObject.AddComponent<WorldPixelViewport>();
            }

            viewport.world = world;
            viewport.target = world.follow.target;
            viewport.outdoorPixelsPerUnit = 22;
            foreach (
                var floor in world.GetComponentsInChildren<SpriteRenderer>(true).Where(s => s.name == "Walkable floor")
            )
            {
                var zone = rules.zones.FirstOrDefault(z => floor.transform.parent.name.EndsWith(" / " + z.id));
                if (zone == null)
                {
                    continue;
                }

                Sprite art =
                    zone.id == "bathroom" ? data.bathroomShell
                    : zone.outdoor ? data.cityGround
                    : data.homeShell;
                floor.sprite = art;
                floor.drawMode = SpriteDrawMode.Simple;
                floor.color = Color.white;
                floor.transform.localScale = new Vector3(
                    zone.size.x / art.bounds.size.x,
                    zone.size.y / art.bounds.size.y,
                    1
                );
                if (!zone.outdoor && zone.id != "bathroom" && data.nightHomeShell != null)
                {
                    var backdrop = floor.GetComponent<WorldRoomBackdrop>();
                    if (backdrop == null)
                    {
                        backdrop = floor.gameObject.AddComponent<WorldRoomBackdrop>();
                    }

                    backdrop.floor = floor;
                    backdrop.day = art;
                    backdrop.night = data.nightHomeShell;
                }

                zone.walkableTop =
                    zone.outdoor ? zone.size.y / 2
                    : zone.id == "bathroom" ? 1.35f
                    : 3.15f;
                foreach (var wall in floor.transform.parent.GetComponentsInChildren<BoxCollider2D>())
                {
                    if (
                        wall.name == "Back wall"
                        || wall.name == "Left wall"
                        || wall.name == "Right wall"
                        || wall.name == "Front cutaway"
                    )
                    {
                        var visual = wall.GetComponent<SpriteRenderer>();
                        if (visual != null)
                        {
                            visual.enabled = false;
                        }

                        if (wall.name == "Back wall")
                        {
                            wall.transform.localPosition = new Vector3(0, zone.walkableTop, 0);
                        }
                    }
                }
            }

            foreach (var o in world.objects.Where(x => x != null))
            {
                Sprite art = null;
                float scale = 1;
                int f =
                    o.kind == WorldInteractionKind.Sleep ? 0
                    : o.kind == WorldInteractionKind.Fridge ? 1
                    : o.kind == WorldInteractionKind.Meal ? 2
                    : o.kind == WorldInteractionKind.Wardrobe ? 3
                    : o.kind == WorldInteractionKind.Wash || o.kind == WorldInteractionKind.Drink ? 4
                    : o.kind == WorldInteractionKind.Toilet ? 5
                    : o.kind == WorldInteractionKind.Shower ? 6
                    : o.kind == WorldInteractionKind.Door ? 7
                    : o.kind == WorldInteractionKind.Work || o.kind == WorldInteractionKind.StockMarket ? 8
                    : o.kind == WorldInteractionKind.Study || o.kind == WorldInteractionKind.FurnitureShop ? 9
                    : o.kind == WorldInteractionKind.Rest ? 11
                    : o.kind == WorldInteractionKind.FoodShop ? 2
                    : -1;
                if (o.kind == WorldInteractionKind.NPC)
                {
                    var definition = rules.npcs.FirstOrDefault(n => n.npcId == o.npcId);
                    if (definition != null)
                    {
                        definition.frames = data.pedestrians[o.npcId == "classmate" ? 2 : 0].frames;
                        art = definition.frames[0];
                    }
                }
                else if (f >= 0)
                {
                    art = data.furniture[f];
                    if (f == 0)
                    {
                        scale = .9f;
                    }
                    else if (f == 7)
                    {
                        scale = .85f;
                    }
                    else if (f == 11)
                    {
                        scale = .9f;
                    }

                    if (o.zone == "home" && o.kind == WorldInteractionKind.Rest)
                    {
                        art = data.furniture[10];
                        scale = .85f;
                    }
                }

                if (o.zone == "city" && o.kind == WorldInteractionKind.Door && o.id != "alley-path")
                {
                    art = data.urban[
                        o.targetZone == "shop" ? 0
                        : o.targetZone == "workplace" ? 1
                        : 2
                    ];
                    o.animateDoorLeaf = false;
                }

                if (o.id == "house-entrance")
                {
                    art = data.urban[3];
                    o.animateDoorLeaf = false;
                }

                if (o.id == "alley-path" || o.id == "city-path")
                {
                    art = data.furniture[15];
                    scale = .9f;
                    o.animateDoorLeaf = false;
                }

                if (o.id.StartsWith("tree-"))
                {
                    art = data.furniture[12];
                    scale = 1.4f;
                }

                if (o.id.StartsWith("lamp-"))
                {
                    art = data.furniture[13];
                    scale = 1.2f;
                }

                if (art == null)
                {
                    continue;
                }

                SetArt(o, art, scale);
                if (o.id == "bed")
                {
                    Move(o, rules, new Vector2(-5, .4f));
                }

                if (o.id == "bath-door")
                {
                    Move(o, rules, new Vector2(7, 3));
                }

                if (o.id == "front-door")
                {
                    Move(o, rules, new Vector2(0, -5.65f));
                    o.UsePoint = Vector2.up * .9f;
                }

                if (o.id == "sink")
                {
                    Move(o, rules, new Vector2(-3, .2f));
                }

                if (o.id == "shower")
                {
                    Move(o, rules, new Vector2(3, .1f));
                }

                if (o.id == "toilet")
                {
                    Move(o, rules, new Vector2(0, .2f));
                }

                if (o.id == "bath-exit")
                {
                    Move(o, rules, new Vector2(0, -4.2f));
                    o.UsePoint = Vector2.up * .85f;
                }

                if (o.id == "alley-path")
                {
                    Move(o, rules, new Vector2(-14, -7.2f));
                    o.UsePoint = new Vector2(.8f, -.2f);
                    o.arrival = new Vector2(7, -6);
                }

                if (o.id == "city-path")
                {
                    Move(o, rules, new Vector2(9, -6.3f));
                    o.UsePoint = Vector2.up * .9f;
                    o.arrival = new Vector2(-12, -7.4f);
                }

                if (o.id == "city-bench")
                {
                    Move(o, rules, new Vector2(5, -.7f));
                }

                if (o.id == "stock-terminal")
                {
                    Move(o, rules, new Vector2(11, -.5f));
                }
            }

            foreach (var sr in world.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (
                    sr.name == "City avenue"
                    || sr.name == "Central pedestrian street"
                    || sr.name.StartsWith("Road lane ")
                    || sr.name.StartsWith("Crosswalk ")
                    || sr.name == "Residential lane"
                    || sr.name.StartsWith("Night window ")
                    || sr.name == "Night lamp glow"
                )
                {
                    sr.gameObject.SetActive(false);
                }
            }

            world.lamps = world.lamps.Where(s => s != null && s.gameObject.activeSelf).ToArray();
            foreach (var text in world.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            {
                var entry = world.objects.FirstOrDefault(o =>
                    o.zone == "city"
                    && o.kind == WorldInteractionKind.Door
                    && (
                        (o.targetZone == "shop" && text.text.StartsWith("상점"))
                        || (o.targetZone == "workplace" && text.text == "Workspace")
                        || (o.targetZone == "school" && (text.text == "학교" || text.text == "배움터"))
                    )
                );
                if (entry == null)
                {
                    continue;
                }

                float rise = entry.targetZone == "shop" ? 2.9f : 2.05f;
                text.rectTransform.anchoredPosition3D = entry.transform.position + new Vector3(0, rise, -.1f);
                text.rectTransform.sizeDelta = new Vector2(3.3f, .5f);
                text.fontSize = 4.8f;
                text.rectTransform.sizeDelta = new Vector2(3.3f, .7f);
                text.color = new Color32(246, 232, 198, 255);
                var old = world
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(s => s.name == "Sign board / " + text.text);
                if (old != null)
                {
                    old.gameObject.SetActive(true);
                    old.transform.position = entry.transform.position + Vector3.up * rise;
                    old.size = new Vector2(3.5f, .75f);
                    old.color = new Color32(42, 61, 66, 245);
                }
            }

            var housing = AssetDatabase.LoadAssetAtPath<HousingCatalog>(
                "Assets/_Game/Data/Housing/HousingCatalog.asset"
            );
            if (housing != null)
            {
                foreach (var pair in new[] { ("basic-bed", 0), ("basic-table", 2), ("bookshelf", 9), ("chair", 10) })
                {
                    var item = housing.Furniture(pair.Item1);
                    if (item != null)
                    {
                        item.preview = data.furniture[pair.Item2];
                    }
                }

                EditorUtility.SetDirty(housing);
            }

            rules.Zone("city").spawn = new Vector2(0, .8f);
            foreach (var npc in rules.npcs)
            {
                foreach (var entry in npc.schedule)
                {
                    if (entry.zone == "city" && entry.position.y < -1.8f && entry.position.y > -6.6f)
                    {
                        entry.position = new Vector2(entry.position.x, -.8f);
                    }
                }
            }

            Phase26AudioSetup.Attach(world);
            CompactHUD(world.hud);
            SetupStreet(world, rules, data);
            Effects(world);
            Phase26HomeSetup.ApplyToWorld(world);
            world.roomBackdrops = world.GetComponentsInChildren<WorldRoomBackdrop>(true);
            EditorUtility.SetDirty(rules);
            EditorUtility.SetDirty(world);
            return true;
        }

        static void Move(WorldInteractable o, LifeWorldRules rules, Vector2 p)
        {
            o.transform.position = rules.Zone(o.zone).origin + p;
            o.visual.sortingOrder = 1000 - Mathf.RoundToInt(p.y * 100);
            if (o.highlight != null)
            {
                o.highlight.sortingOrder = o.visual.sortingOrder - 1;
            }
        }

        static void SetArt(WorldInteractable o, Sprite art, float scale)
        {
            o.visual.sprite = art;
            o.transform.localScale = Vector3.one * scale;
            o.visual.color = Color.white;
            if (o.highlight != null)
            {
                o.highlight.sprite = art;
                o.highlight.transform.localScale = Vector3.one * 1.035f;
                o.highlight.color = new Color(1, .84f, .45f, .35f);
            }

            var col = o.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                float width = Mathf.Clamp(art.bounds.size.x * .7f, .6f, 4.4f),
                    depth = .65f;
                if (o.kind == WorldInteractionKind.Sleep)
                {
                    width = 2.1f;
                    depth = 2.1f;
                }

                if (o.kind == WorldInteractionKind.Meal || o.kind == WorldInteractionKind.FoodShop)
                {
                    depth = .9f;
                }

                col.size = new Vector2(width, depth);
                col.offset = Vector2.up * (depth / 2);
            }

            if (
                col != null
                && (o.id.StartsWith("tree-") || o.id.StartsWith("lamp-") || o.id == "alley-path" || o.id == "city-path")
            )
            {
                col.size = new Vector2(o.id.StartsWith("tree-") ? .55f : .25f, .35f);
                col.offset = Vector2.up * .175f;
            }
        }

        static SpriteRenderer Decoration(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            float scale = 1
        )
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1000 - Mathf.RoundToInt(go.transform.position.y * 100);
            return sr;
        }

        static void Anchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static Material MaskMaterial(bool glow)
        {
            string path = Folder + (glow ? "PixelGlow.mat" : "PixelShadow.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Ian/PixelSoftMask");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
            {
                throw new Exception("PixelSoftMask shader must compile before connecting effects");
            }

            material = new Material(shader);
            material.SetFloat("_DstBlend", glow ? 1 : 10);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static SpriteRenderer Mask(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            int order,
            bool glow = false
        )
        {
            var existing = parent.Find(name);
            var go = existing == null ? new GameObject(name, typeof(SpriteRenderer)) : existing.gameObject;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = go.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = LifeWorldArtBuilder.Solid();
            renderer.sharedMaterial = MaskMaterial(glow);
            renderer.color = color;
            renderer.sortingOrder = order;
            go.transform.localScale = new Vector3(
                size.x / renderer.sprite.bounds.size.x,
                size.y / renderer.sprite.bounds.size.y,
                1
            );
            return renderer;
        }

        static void Effects(LifeWorldController world)
        {
            var lamps = world.lamps.ToList();
            foreach (var old in world.Player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (
                    old != world.Player.contactShadow
                    && old.name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    old.enabled = false;
                }
            }

            world.Player.contactShadow = Mask(
                world.Player.transform,
                "Contact shadow",
                Vector2.zero,
                new Vector2(.65f, .2f),
                new Color(0, 0, 0, .25f),
                world.Player.sprite.sortingOrder - 1
            );
            foreach (var o in world.objects.Where(o => o != null))
            {
                o.shadow = Mask(
                    o.transform,
                    "Contact shadow",
                    Vector2.zero,
                    new Vector2(
                        o.kind == WorldInteractionKind.NPC
                            ? .55f
                            : Mathf.Min(2.5f, o.visual.sprite.bounds.size.x * .7f),
                        .22f
                    ),
                    new Color(0, 0, 0, .2f),
                    o.visual.sortingOrder - 1
                );
            }

            var street = world.GetComponentInChildren<WorldStreetLife>(true);
            if (street == null)
            {
                return;
            }

            foreach (var renderer in street.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name.StartsWith("Street planter"))
                {
                    var p = renderer.transform.localPosition;
                    p.y = .8f;
                    renderer.transform.localPosition = p;
                    renderer.sortingOrder = 1000 - Mathf.RoundToInt(renderer.transform.position.y * 100);
                }

                if (
                    renderer.name.StartsWith("Street planter")
                    || renderer.name.StartsWith("Street lantern")
                    || renderer.name == "Utility pole"
                    || renderer.name == "Crossing signal"
                )
                {
                    var col = renderer.GetComponent<BoxCollider2D>();
                    if (col == null)
                    {
                        col = renderer.gameObject.AddComponent<BoxCollider2D>();
                    }

                    col.size = new Vector2(renderer.name.StartsWith("Street planter") ? 1.8f : .35f, .35f);
                    col.offset = Vector2.up * .175f;
                }

                if (renderer.name.StartsWith("Street lantern"))
                {
                    var glow = Mask(
                        renderer.transform,
                        "Lantern glow",
                        new Vector2(0, renderer.sprite.bounds.max.y - .4f),
                        new Vector2(.8f, .6f),
                        new Color(1, .73f, .3f, .27f),
                        renderer.sortingOrder + 1,
                        true
                    );
                    glow.enabled = false;
                    lamps.Add(glow);
                }
            }

            foreach (var person in street.pedestrians)
            {
                person.shadow = Mask(
                    person.visual.transform,
                    "Contact shadow",
                    Vector2.zero,
                    new Vector2(.55f, .18f),
                    new Color(0, 0, 0, .22f),
                    person.visual.sortingOrder - 1
                );
            }

            foreach (var car in street.vehicles)
            {
                car.shadow = Mask(
                    car.body.transform,
                    "Contact shadow",
                    new Vector2(0, .1f),
                    new Vector2(2.6f, .65f),
                    new Color(0, 0, 0, .24f),
                    car.visual.sortingOrder - 1
                );
                car.headlights = Mask(
                    car.body.transform,
                    "Headlight glow",
                    new Vector2(car.direction * 1.5f, .4f),
                    new Vector2(1.5f, .5f),
                    new Color(1, .9f, .63f, .22f),
                    car.visual.sortingOrder - 1,
                    true
                );
                car.headlights.enabled = false;
                lamps.Add(car.headlights);
            }

            world.lamps = lamps.Distinct().ToArray();
        }

        static void CompactHUD(WorldHUD ui)
        {
            var bar = ui.GetComponentsInChildren<CompactWorldStatusBar>(true).FirstOrDefault(x => x.world);
            if (bar != null)
            {
                Anchors((RectTransform)bar.transform, new Vector2(.018f, .89f), new Vector2(.40f, .98f));
                bar.message.gameObject.SetActive(false);
                var group = bar.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (group != null)
                {
                    group.padding = new RectOffset(16, 16, 8, 8);
                    group.spacing = 16;
                }
            }

            var wallet = ui.cash.rectTransform.parent as RectTransform;
            if (wallet != null)
            {
                Anchors(wallet, new Vector2(.702f, .89f), new Vector2(.982f, .98f));
                Anchors(ui.cash.rectTransform, new Vector2(.035f, .08f), new Vector2(.70f, .92f));
                Anchors((RectTransform)ui.menuButton.transform, new Vector2(.73f, .07f), new Vector2(.97f, .93f));
            }

            var controls = ui.GetComponentsInChildren<TMPro.TMP_Text>(true)
                .FirstOrDefault(t => t.name == "World controls");
            if (controls != null)
            {
                var card = ui.menuPanel.transform.GetChild(0) as RectTransform;
                if (card != null)
                {
                    controls.transform.SetParent(card, false);
                    Anchors(controls.rectTransform, new Vector2(.035f, .025f), new Vector2(.965f, .10f));
                    var scroll = card.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
                    if (scroll != null)
                    {
                        Anchors((RectTransform)scroll.transform, new Vector2(.03f, .13f), new Vector2(.97f, .79f));
                    }
                }
            }
        }

        static void SetupStreet(LifeWorldController world, LifeWorldRules rules, WorldVisualCatalog art)
        {
            var existing = world.GetComponentInChildren<WorldStreetLife>(true);
            if (existing != null)
            {
                foreach (var car in existing.vehicles)
                {
                    car.laneY = car.direction > 0 ? -3.35f : -5.35f;
                    var p = car.body.position;
                    car.body.position = new Vector2(p.x, car.laneY);
                }

                if (art.signalStates != null && art.signalStates.Length >= 3)
                {
                    existing.signalStates = art.signalStates;
                    if (existing.signal != null)
                    {
                        existing.signal.sprite = art.signalStates[0];
                        existing.signal.transform.localScale = Vector3.one;
                    }
                }

                return;
            }

            var root = new GameObject("Authored street life");
            root.transform.SetParent(world.transform, false);
            root.transform.position = rules.Zone("city").origin;
            var street = root.AddComponent<WorldStreetLife>();
            street.world = world;
            for (int i = 0; i < 4; i++)
            {
                Decoration(
                    root.transform,
                    "Street planter " + i,
                    art.furniture[14],
                    new Vector2(-12 + i * 8, -.8f),
                    .6f
                );
                Decoration(
                    root.transform,
                    "Street lantern " + i,
                    art.furniture[13],
                    new Vector2(-13 + i * 8, -6.7f),
                    1.1f
                );
            }

            Decoration(root.transform, "Utility pole", art.furniture[15], new Vector2(13, 1.5f), 1.25f);
            street.signal = Decoration(
                root.transform,
                "Crossing signal",
                art.signalStates != null && art.signalStates.Length >= 3 ? art.signalStates[0] : art.urban[5],
                new Vector2(2.5f, -1.7f),
                art.signalStates != null && art.signalStates.Length >= 3 ? 1 : .65f
            );
            street.signalStates = art.signalStates;
            street.pedestrians = new StreetPedestrian[10];
            for (int i = 0; i < 10; i++)
            {
                bool crossing = i >= 8;
                Vector2[] path =
                    crossing
                        ? new[] { new Vector2((i - 8) * .8f - .4f, -1.3f), new Vector2((i - 8) * .8f - .4f, -6.8f) }
                    : i < 4
                        ? new[] { new Vector2(-12, -1), new Vector2(-5, -.8f), new Vector2(3, 0), new Vector2(12, -1) }
                    : new[]
                    {
                        new Vector2(-12, -7.2f),
                        new Vector2(-4, -7.5f),
                        new Vector2(5, -7.1f),
                        new Vector2(12, -7.4f),
                    };
                int start = i % path.Length;
                var frames = art.pedestrians[i % art.pedestrians.Length].frames;
                var visual = Decoration(root.transform, "Citizen " + i, frames[0], path[start], .92f + (i % 3) * .035f);
                street.pedestrians[i] = new StreetPedestrian
                {
                    visual = visual,
                    frames = frames,
                    route = path,
                    next = (start + 1) % path.Length,
                    speed = .95f + (i % 4) * .16f,
                    waiting = i * .23f,
                    crossing = crossing,
                };
            }

            street.vehicles = new StreetVehicle[4];
            for (int i = 0; i < 4; i++)
            {
                float direction = i % 2 == 0 ? 1 : -1,
                    lane = direction > 0 ? -3.35f : -5.35f;
                var go = new GameObject("Neighborhood car " + i, typeof(Rigidbody2D), typeof(BoxCollider2D));
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector2(-12 + i * 8, lane);
                var body = go.GetComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.simulated = false;
                var col = go.GetComponent<BoxCollider2D>();
                col.size = new Vector2(2.5f, .65f);
                col.offset = Vector2.up * .18f;
                var sr = Decoration(go.transform, "Car body", art.urban[4], Vector2.zero, .55f);
                sr.flipX = direction > 0;
                sr.color =
                    i == 1 ? new Color(.88f, .96f, 1)
                    : i == 2 ? new Color(1, .91f, .77f)
                    : Color.white;
                street.vehicles[i] = new StreetVehicle
                {
                    body = body,
                    visual = sr,
                    direction = direction,
                    laneY = lane,
                    speed = 2.5f + i * .32f,
                };
            }
        }
    }
}
