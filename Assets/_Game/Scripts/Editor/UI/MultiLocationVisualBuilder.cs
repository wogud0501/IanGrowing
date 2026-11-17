using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IanGame.Commercial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class MultiLocationVisualBuilder
    {
        const string Art = "Assets/_Game/Art/Commercial/";
        const string Data = "Assets/_Game/Data/Presentation/Commercial/";
        static PixelUITheme theme;
        static Sprite panel;
        static EnvironmentSpriteLibrary library;

        static T Get<T>(GameObject g)
            where T : Component
        {
            var c = g.GetComponent<T>();
            return c != null ? c : g.AddComponent<T>();
        }

        static SpriteRenderer Renderer(Transform p, string name, Sprite sprite, Vector3 position, int order)
        {
            var g = new GameObject(name, typeof(SpriteRenderer));
            g.transform.SetParent(p, false);
            g.transform.localPosition = position;
            var r = g.GetComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            return r;
        }

        static Sprite Crop(Sprite source, Rect rect, string name)
        {
            string path = Art + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            var s = Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            s.name = name;
            AssetDatabase.CreateAsset(s, path);
            return s;
        }

        static void Position(RectTransform r, Transform parent, Vector2 min, Vector2 max)
        {
            r.SetParent(parent, false);
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            r.localScale = Vector3.one;
        }

        static TMP_Text Text(Transform p, string name, string value, int size)
        {
            var g = new GameObject(name, typeof(RectTransform));
            var r = g.GetComponent<RectTransform>();
            Position(r, p, Vector2.zero, Vector2.one);
            r.offsetMin = new Vector2(14, 4);
            r.offsetMax = new Vector2(-14, -4);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.font = theme.font;
            t.fontSize = size;
            t.color = theme.cream;
            t.text = value;
            t.raycastTarget = false;
            return t;
        }

        static RectTransform Frame(Transform p, string name, Vector2 min, Vector2 max)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            Position(r, p, min, max);
            var i = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            i.sprite = panel;
            i.type = UnityEngine.UI.Image.Type.Sliced;
            i.raycastTarget = false;
            return r;
        }

        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode");
            }

            theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");
            panel = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "UI/Panel_64.png");
            CommercialSliceBuilder.Derivative(Art + "City/City_Source.png", Art + "City/City_640.png", 640, 360);
            CommercialSliceBuilder.Derivative(
                Art + "Workplace/Office_Source.png",
                Art + "Workplace/Office_640.png",
                640,
                360
            );
            CommercialSliceBuilder.Derivative(
                Art + "Workplace/Convenience_Source.png",
                Art + "Workplace/Convenience_640.png",
                640,
                360
            );
            library = Sprites();
            foreach (string name in new[] { "City", "Workplace" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                if (GameObject.Find("Commercial Presentation") != null)
                {
                    throw new InvalidOperationException(name + " already rebuilt; use targeted edits");
                }

                Room(name);
                CommercialSliceBuilder.ExtendHUD(name);
                Activities(name);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            CommercialSlicePolish.Apply();
            Refine();
            TransitionProfiles();
            CommercialAtlasPrebuild.Configure();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
        }

        static EnvironmentSpriteLibrary Sprites()
        {
            string path = Data + "CityOfficeSprites.asset";
            var asset = AssetDatabase.LoadAssetAtPath<EnvironmentSpriteLibrary>(path);
            if (asset != null)
            {
                return asset;
            }

            CommercialSliceBuilder.Derivative(
                Art + "Animations/CityOffice_Source.png",
                Art + "Animations/CityOffice_Logical.png",
                836,
                470
            );
            var source = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Animations/CityOffice_Logical.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(Art + "Animations/CityOffice_Logical.png"));
            var pixels = tex.GetPixels32();
            var bands = new List<Vector2Int>();
            int top = -1,
                last = -1;
            for (int y = tex.height - 1; y >= 0; y--)
            {
                int count = 0;
                for (int x = 0; x < tex.width; x++)
                {
                    if (pixels[y * tex.width + x].a > 180)
                    {
                        count++;
                    }
                }

                if (count >= 5)
                {
                    if (top < 0)
                    {
                        top = y;
                    }

                    last = y;
                }
                else if (top >= 0 && last - y > 8)
                {
                    bands.Add(new Vector2Int(last, top + 1));
                    top = -1;
                }
            }

            if (top >= 0)
            {
                bands.Add(new Vector2Int(last, top + 1));
            }

            if (bands.Count != 4)
            {
                throw new InvalidOperationException("Expected 4 alpha-separated animation rows; got " + bands.Count);
            }

            asset = ScriptableObject.CreateInstance<EnvironmentSpriteLibrary>();
            AssetDatabase.CreateAsset(asset, path);
            var rows = new Sprite[4][];
            for (int row = 0; row < 4; row++)
            {
                rows[row] = new Sprite[4];
                for (int col = 0; col < 4; col++)
                {
                    int left = col * tex.width / 4,
                        right = (col + 1) * tex.width / 4;
                    int bottom = bands[row].x;
                    var sprite = Sprite.Create(
                        source.texture,
                        new Rect(left, bottom, right - left, bands[row].y - bottom),
                        new Vector2(.5f, 0),
                        100,
                        0,
                        SpriteMeshType.FullRect
                    );
                    sprite.name = new[] { "PedestrianWalk", "CoworkerTyping", "TrafficCar", "Billboard" }[row] + col;
                    AssetDatabase.AddObjectToAsset(sprite, asset);
                    rows[row][col] = sprite;
                }
            }

            asset.walk = rows[0];
            asset.typing = rows[1];
            asset.vehicle = rows[2];
            asset.billboard = rows[3];
            EditorUtility.SetDirty(asset);
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static void Room(string name)
        {
            bool city = name == "City";
            var camera = Camera.main;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 1.8f;
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            camera.backgroundColor = theme.ink;
            var pp = Get<PixelPerfectCamera>(camera.gameObject);
            pp.assetsPPU = 100;
            pp.refResolutionX = 640;
            pp.refResolutionY = 360;
            pp.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pp.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
            var extra = Get<UniversalAdditionalCameraData>(camera.gameObject);
            extra.renderPostProcessing = false;
            extra.antialiasing = AntialiasingMode.None;
            var light = GameObject.Find("Main Light");
            if (light != null)
            {
                light.SetActive(false);
            }

            var root = new GameObject("Commercial Presentation");
            var room = root.AddComponent<RoomAtmosphere>();
            room.workplace = !city;
            root.AddComponent<SceneEntryPoint>();
            root.AddComponent<SceneExitPoint>();
            room.motes = new SpriteRenderer[0];
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                Art + (city ? "City/City_640.png" : "Workplace/Office_640.png")
            );
            room.background = Renderer(root.transform, name + " environmental master", sprite, Vector3.zero, -20);
            room.dayTint = city ? new Color(.90f, .96f, 1) : Color.white;
            room.eveningTint = city ? new Color(1, .83f, .94f) : new Color(1, .86f, .76f);
            room.nightTint = city ? new Color(.68f, .69f, .96f) : new Color(.58f, .65f, .82f);
            var official = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Game/Art/Characters/Ian/Sprites/Ian_Base_Cutout.png"
            );
            room.character = Renderer(
                root.transform,
                "Ian / authoritative official design",
                official,
                new Vector3(0, city ? -.4f : -.55f, 0),
                10
            );
            room.character.transform.localScale = Vector3.one * (1.18f / official.bounds.size.y);
            room.characterOrigin = room.character.transform.localPosition;
            var motion = room.character.gameObject.AddComponent<SpriteMotionPlayer>();
            motion.target = room.character;
            motion.library = AssetDatabase.LoadAssetAtPath<SpriteMotionLibrary>(Data + "IanMotionLibrary.asset");
            var legacy = room.character.gameObject.AddComponent<Character2DPresentation>();
            legacy.spriteRenderer = room.character;
            legacy.visuals = AssetDatabase.LoadAssetAtPath<Character2DVisuals>("Assets/_Game/Data/Ian2DVisuals.asset");
            legacy.enabled = false;
            UnityEngine.Object.FindAnyObjectByType<GameplaySceneBindings>().character = legacy;
            var fx = root.AddComponent<LayeredLocationPresentation>();
            fx.location = city ? SceneId.City : SceneId.Workplace;
            fx.room = room;
            fx.sceneCamera = camera;
            var layers = new List<SpriteRenderer>();
            var depths = new List<float>();
            var origins = new List<Vector3>();
            if (city)
            {
                room.background.enabled = false;
                var rects = new[] { new Rect(0, 260, 640, 100), new Rect(0, 65, 640, 203), new Rect(0, 0, 640, 73) };
                for (int i = 0; i < 3; i++)
                {
                    var rect = rects[i];
                    var position = new Vector3((rect.center.x - 320) / 100, (rect.center.y - 180) / 100, 0);
                    var r = Renderer(
                        root.transform,
                        new[] { "Far skyline band", "Midground storefronts", "Foreground wet road" }[i],
                        Crop(sprite, rect, "City/City_Layer" + i),
                        position,
                        -30 + i * 5
                    );
                    layers.Add(r);
                    depths.Add(new[] { .2f, .6f, 1f }[i]);
                    origins.Add(position);
                }
            }

            fx.layers = layers.ToArray();
            fx.depths = depths.ToArray();
            fx.origins = origins.ToArray();
            var actors = new List<EnvironmentAnimation>();
            AddActor(
                root.transform,
                actors,
                "Passing commuter",
                library.walk,
                city ? new Vector3(-2, -.92f, 0) : new Vector3(0, -.3f, 0),
                city ? .72f : .65f,
                city ? 4 : 1,
                .35f,
                0,
                false
            );
            if (!city)
            {
                actors[0].span = 3;
            }

            if (city)
            {
                AddActor(
                    root.transform,
                    actors,
                    "Distant commuter",
                    library.walk,
                    new Vector3(2, -.83f, 0),
                    .59f,
                    3,
                    -.23f,
                    9,
                    false
                );
                AddActor(
                    root.transform,
                    actors,
                    "Traffic / pooled car",
                    library.vehicle,
                    new Vector3(-3, -1.57f, 0),
                    .55f,
                    18,
                    1.1f,
                    5,
                    false
                );
                AddActor(
                    root.transform,
                    actors,
                    "Animated advertising",
                    library.billboard,
                    new Vector3(-.35f, .65f, 0),
                    .42f,
                    -15,
                    0,
                    1,
                    false
                );
            }
            else
            {
                AddActor(
                    root.transform,
                    actors,
                    "Typing coworker",
                    library.typing,
                    new Vector3(-2.25f, -.82f, 0),
                    .75f,
                    2,
                    0,
                    0,
                    true
                );
                AddActor(
                    root.transform,
                    actors,
                    "Right desk monitor",
                    library.billboard,
                    new Vector3(2.23f, -.2f, 0),
                    .30f,
                    3,
                    0,
                    3,
                    false
                );
            }

            fx.actors = actors.ToArray();
            var detailRect = city ? new Rect(65, 220, 120, 17) : new Rect(27, 341, 100, 7);
            room.animatedDetail = Renderer(
                root.transform,
                "Animated environmental detail",
                Crop(sprite, detailRect, name + "/" + name + "_Light"),
                new Vector3((detailRect.center.x - 320) / 100, (detailRect.center.y - 180) / 100, 0),
                -14
            );
            fx.flicker = new[] { room.animatedDetail };
            fx.rain = new LineRenderer[city ? 24 : 0];
            if (city)
            {
                var material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, Art + "City/Rain.mat");
                for (int i = 0; i < fx.rain.Length; i++)
                {
                    var g = new GameObject("Rain pool " + i);
                    g.transform.SetParent(root.transform, false);
                    var line = g.AddComponent<LineRenderer>();
                    line.sharedMaterial = material;
                    line.useWorldSpace = false;
                    line.positionCount = 2;
                    line.startWidth = line.endWidth = .01f;
                    line.startColor = line.endColor = new Color(.6f, .76f, 1, .3f);
                    line.sortingOrder = 21;
                    fx.rain[i] = line;
                }
            }

            new GameObject("Furniture / job theme attachments").transform.SetParent(root.transform);
        }

        static void AddActor(
            Transform root,
            List<EnvironmentAnimation> list,
            string name,
            Sprite[] frames,
            Vector3 origin,
            float height,
            int order,
            float speed,
            float phase,
            bool seated
        )
        {
            var r = Renderer(root, name, frames[0], origin, order);
            r.transform.localScale = Vector3.one * (height / frames[0].bounds.size.y);
            r.flipX = speed < 0;
            list.Add(
                new EnvironmentAnimation
                {
                    renderer = r,
                    frames = frames,
                    origin = origin,
                    speed = speed,
                    phase = phase,
                    fps = name.Contains("advertising") ? 1.5f : 6,
                    seated = seated,
                }
            );
        }

        static void Activities(string name)
        {
            var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
            var location = UnityEngine.Object.FindAnyObjectByType<Phase23Presenter>();
            var nav = p.navigation;
            // Reuse the existing category builder instead of duplicating activity rules or callbacks.
            var builder = typeof(PixelUIBuilder);
            builder
                .GetField("theme", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .SetValue(null, theme);
            builder
                .GetMethod("Location", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { location, nav, name });
            location.panel.transform.SetParent(nav.stack.overlayRoot, false);
            var card = location.panel.GetComponent<RectTransform>();
            card.anchorMin = new Vector2(.08f, .14f);
            card.anchorMax = new Vector2(.92f, .94f);
            card.offsetMin = card.offsetMax = Vector2.zero;
            Get<CanvasGroup>(location.panel);
            location.panel.SetActive(false);
            var safe = p.GetComponent<CommercialHUD>().safeRoot;
            var access = Frame(safe, name + " activity access", new Vector2(.018f, .105f), new Vector2(.20f, .18f));
            access.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var button = access.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = access.GetComponent<UnityEngine.UI.Image>();
            Text(access, "Label", name == "City" ? "도시 활동" : "직장 활동", 18).alignment =
                TextAlignmentOptions.Center;
            var cmd = access.gameObject.AddComponent<UICommand>();
            cmd.shell = nav.stack;
            cmd.command = UICommand.Command.Open;
            cmd.panel = location.panel;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, cmd.Invoke);
            var fx = UnityEngine.Object.FindAnyObjectByType<LayeredLocationPresentation>();
            var clock = Frame(safe, "Location clock", new Vector2(.018f, .30f), new Vector2(.39f, .355f));
            fx.clockLabel = Text(clock, "Clock / terminal", "", 14);
            if (name == "Workplace")
            {
                var hud = p.GetComponent<CommercialHUD>();
                hud.rewardCoins = new RectTransform[8];
                for (int i = 0; i < hud.rewardCoins.Length; i++)
                {
                    var coin = new GameObject(
                        "Payroll coin pool " + i,
                        typeof(RectTransform),
                        typeof(UnityEngine.UI.Image)
                    ).GetComponent<RectTransform>();
                    coin.SetParent(hud.receipt.transform, false);
                    coin.anchorMin = coin.anchorMax = new Vector2(.5f, 0);
                    coin.sizeDelta = new Vector2(20, 20);
                    var image = coin.GetComponent<UnityEngine.UI.Image>();
                    image.sprite = theme.Icon(PixelIcon.Coin);
                    image.raycastTarget = false;
                    coin.gameObject.SetActive(false);
                    hud.rewardCoins[i] = coin;
                }

                var presenter = fx.gameObject.AddComponent<WorkplaceThemePresenter>();
                presenter.room = fx.room;
                presenter.environment = fx;
                presenter.label = p.GetComponent<CommercialHUD>().title;
                presenter.monitor = fx.actors.Last().renderer;
                string[] ids = { "office", "convenience", "developer", "analyst" };
                string[] names = { "사무실", "편의점 근무", "개발실", "분석실" };
                presenter.themes = new WorkplaceVisualTheme[4];
                for (int i = 0; i < 4; i++)
                {
                    string path = Data + "Workplace-" + ids[i] + ".asset";
                    var t = AssetDatabase.LoadAssetAtPath<WorkplaceVisualTheme>(path);
                    if (t == null)
                    {
                        t = ScriptableObject.CreateInstance<WorkplaceVisualTheme>();
                        AssetDatabase.CreateAsset(t, path);
                    }

                    t.jobId = ids[i];
                    t.displayName = names[i];
                    t.background = AssetDatabase.LoadAssetAtPath<Sprite>(
                        Art + "Workplace/" + (i == 1 ? "Convenience" : "Office") + "_640.png"
                    );
                    t.dayTint =
                        i == 2 ? new Color(.78f, .88f, 1)
                        : i == 3 ? new Color(.88f, .93f, 1)
                        : Color.white;
                    t.seatedCoworker = i != 1;
                    t.monitorFrames = library.billboard;
                    t.monitorTint = i == 3 ? new Color(1, .8f, .6f) : new Color(.7f, .95f, 1);
                    EditorUtility.SetDirty(t);
                    presenter.themes[i] = t;
                }
            }
        }

        public static void TransitionProfiles()
        {
            string prefab = "Assets/_Game/Prefabs/System/GameContext.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                var controller = root.GetComponent<TransitionController>();
                var profiles = new List<TransitionProfile>();
                var places = new[] { SceneId.Home, SceneId.School, SceneId.City, SceneId.Workplace };
                foreach (var from in places)
                {
                    foreach (var to in places)
                    {
                        if (from == to)
                        {
                            continue;
                        }

                        string path = Data + from + "To" + to + ".asset";
                        var p = AssetDatabase.LoadAssetAtPath<TransitionProfile>(path);
                        if (p == null)
                        {
                            p = ScriptableObject.CreateInstance<TransitionProfile>();
                            AssetDatabase.CreateAsset(p, path);
                        }

                        p.from = from;
                        p.to = to;
                        p.curtain = Color.white;
                        p.departure =
                            from == SceneId.Workplace ? "퇴근 접수 · 오늘의 자아를 반납합니다."
                            : from == SceneId.School ? "수업 종료 · 성장은 계속 청구됩니다."
                            : from == SceneId.City ? "이동 중 · 목적지는 있지만 여유는 없습니다."
                            : "외출 준비 · 생존 업무는 계속됩니다.";
                        p.arrival =
                            to == SceneId.City ? "도시 · 당신을 위한 광고가 기다립니다."
                            : to == SceneId.Workplace ? "출근 · 열정은 급여에 포함되지 않습니다."
                            : to == SceneId.School ? "등교 · 규칙적인 하루를 승인합니다."
                            : "귀가 · 현실은 문밖에 두세요.";
                        EditorUtility.SetDirty(p);
                        profiles.Add(p);
                    }
                }

                controller.profiles = profiles.ToArray();
                PrefabUtility.SaveAsPrefabAsset(root, prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void Refine()
        {
            theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");
            panel = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "UI/Panel_64.png");
            theme.button = theme.tab = theme.hover = theme.pressed = panel;
            EditorUtility.SetDirty(theme);
            foreach (string name in new[] { "Home", "School", "City", "Workplace" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var p = UnityEngine.Object.FindAnyObjectByType<Phase23Presenter>();
                var pixel = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
                if (p != null && pixel != null)
                {
                    var body = p.status.transform.parent;
                    var groups = body.Cast<Transform>()
                        .Where(t => t.name == "Categorized location activities")
                        .ToArray();
                    var current =
                        groups.LastOrDefault(t => p.activityButtons.Any(b => b != null && b.transform.IsChildOf(t)))
                        ?? groups.LastOrDefault();
                    foreach (var old in groups)
                    {
                        if (old != current)
                        {
                            UnityEngine.Object.DestroyImmediate(old.gameObject);
                        }
                    }

                    if (current != null)
                    {
                        p.status.transform.SetSiblingIndex(0);
                        current.SetSiblingIndex(1);
                        p.details.transform.SetSiblingIndex(2);
                        p.result.transform.SetSiblingIndex(3);
                        var size = current.GetComponent<LayoutElement>();
                        size.minHeight = size.preferredHeight = 326;
                        var pages = current.Find("Tab pages");
                        if (pages != null)
                        {
                            var le = pages.GetComponent<LayoutElement>();
                            le.minHeight = le.preferredHeight = 280;
                        }
                    }

                    var status = p.status.GetComponent<LayoutElement>();
                    status.minHeight = status.preferredHeight = 54;
                    var details = p.details.GetComponent<LayoutElement>();
                    details.minHeight = details.preferredHeight = 110;
                    var image = p.panel.GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                    {
                        image.sprite = panel;
                        image.type = UnityEngine.UI.Image.Type.Sliced;
                        image.color = Color.white;
                    }

                    foreach (var text in p.panel.GetComponentsInChildren<TMP_Text>(true))
                    {
                        text.color = theme.cream;
                        PixelFontSystemBuilder.Style(text);
                    }

                    if (name != "Home")
                    {
                        pixel.navigation.locationPanel = p.panel;
                    }
                }

                var fx = UnityEngine.Object.FindAnyObjectByType<LayeredLocationPresentation>();
                if (fx != null)
                {
                    fx.room.externalDetailAnimation = true;
                    if (name == "Workplace")
                    {
                        fx.actors[0].origin = new Vector3(0, -.3f, 0);
                        fx.actors[0].span = 3;
                        fx.actors[0].renderer.sortingOrder = 1;
                        fx.actors[0].renderer.transform.localScale =
                            Vector3.one * (.65f / fx.actors[0].frames[0].bounds.size.y);
                    }

                    var hud = pixel.GetComponent<CommercialHUD>();
                    if (hud.safeRoot.Find("Place entry hotspot") == null)
                    {
                        var entry = Frame(
                            hud.safeRoot,
                            "Place entry hotspot",
                            new Vector2(.80f, .20f),
                            new Vector2(.982f, .28f)
                        );
                        var image = entry.GetComponent<UnityEngine.UI.Image>();
                        image.raycastTarget = true;
                        var b = entry.gameObject.AddComponent<UnityEngine.UI.Button>();
                        b.targetGraphic = image;
                        Text(entry, "Label", name == "City" ? "사무실 입구 →" : "퇴근 / 집 →", 18).alignment =
                            TextAlignmentOptions.Center;
                        var route = entry.gameObject.AddComponent<PixelNavigationButton>();
                        route.service = pixel.navigation;
                        route.target = PixelNavigationButton.Target.Location;
                        route.location = name == "City" ? SceneId.Workplace : SceneId.Home;
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, route.Invoke);
                    }
                }

                foreach (
                    var text in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TMP_Text>(true))
                )
                {
                    PixelFontSystemBuilder.Style(text);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
        }
    }
}
