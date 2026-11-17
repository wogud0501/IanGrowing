using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class PrototypeBuilder
    {
        static TMP_FontAsset font;
        static Sprite circle;
        static Color ink = new Color32(19, 24, 28, 255);
        static Color cream = new Color32(239, 235, 218, 255);
        static Color lime = new Color32(211, 233, 119, 255);
        static Color grey = new Color32(155, 165, 164, 255);
        static Color panel = new Color32(30, 37, 41, 255);
        static Color orange = new Color32(226, 137, 104, 255);

        [MenuItem("Ian/Build First Prototype")]
        public static void Build()
        {
            const string scenePath = "Assets/_Game/Scenes/MainGame.unity";
            if (File.Exists(scenePath))
            {
                throw new InvalidOperationException("MainGame already exists; refusing to overwrite.");
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                throw new InvalidOperationException("Save the current scene before building.");
            }

            MakeAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            var cam = camera.GetComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 35;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ink;
            camera.transform.position = new Vector3(0, 0, -10);
            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            var systems = new GameObject("Game Systems");
            var stats = systems.AddComponent<PlayerStats>();
            var clock = systems.AddComponent<TimeManager>();
            var events = systems.AddComponent<EventManager>();
            var saves = systems.AddComponent<SaveManager>();
            var game = systems.AddComponent<GameManager>();
            var balance = ScriptableObject.CreateInstance<GameBalance>();
            AssetDatabase.CreateAsset(balance, "Assets/_Game/Data/DefaultBalance.asset");
            game.balance = balance;
            game.stats = stats;
            game.clock = clock;
            game.lifeEvents = events;
            game.saves = saves;
            var canvas = new GameObject(
                "Game Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // A centered reference frame keeps the whole dashboard visible on wide and narrow screens.
            var frame = new GameObject("Dashboard", typeof(RectTransform));
            frame.transform.SetParent(canvas.transform, false);
            var fr = frame.GetComponent<RectTransform>();
            fr.anchorMin = fr.anchorMax = new Vector2(.5f, .5f);
            fr.pivot = new Vector2(.5f, .5f);
            fr.sizeDelta = new Vector2(1440, 900);
            var root = frame.transform;
            Text(root, "Edition", "LIFE MANAGEMENT DEPARTMENT  /  BLACK COMEDY EDITION", 40, 25, 1100, 28, 15, grey);
            Text(root, "Title", "이안 키우기", 40, 65, 600, 65, 48, cream);
            Text(root, "Slogan", "이안을 행복하게 만들어 주세요. 행복은 별도 구매입니다.", 43, 137, 1000, 38, 21, grey);
            var ui = canvas.AddComponent<UIManager>();
            ui.game = game;
            Box(root, "Date Card", 1030, 62, 370, 70, panel);
            ui.header = Text(root, "Date and Money", "DAY 001  /  잔액 ₩ 50", 1050, 80, 335, 34, 23, lime);
            Box(root, "Top Rule", 40, 193, 1360, 2, grey);
            Box(root, "Stats Panel", 40, 226, 280, 460, panel);
            Text(root, "Stats Title", "생존 지표", 64, 250, 235, 44, 29, cream);
            Text(root, "Stats Subtitle", "ALL VALUES  0 — 100", 64, 299, 235, 25, 13, grey);
            ui.happinessFill = Gauge(root, "Happiness", 64, 355, "행복", lime, out ui.happinessLabel);
            ui.stressFill = Gauge(root, "Stress", 64, 460, "스트레스", orange, out ui.stressLabel);
            ui.energyFill = Gauge(root, "Energy", 64, 565, "체력", new Color32(130, 188, 202, 255), out ui.energyLabel);
            Text(root, "Subject", "SUBJECT 001  /  IAN", 370, 248, 550, 28, 15, lime);
            Text(root, "Status Tag", "●  생존 중", 802, 248, 156, 28, 17, grey);
            ui.moodText = Text(root, "Mood", "이안은 적응 중입니다.", 370, 636, 585, 34, 22, cream);
            ui.moodText.alignment = TextAlignmentOptions.Center;
            Scene3DBuilder.Build(game, cam);
            Box(root, "Log Panel", 1008, 226, 392, 460, panel);
            Text(root, "Log Title", "인생 명세서", 1032, 249, 340, 43, 29, cream);
            Text(root, "Log Subtitle", "환불 및 이의 제기 불가", 1032, 300, 340, 26, 15, grey);
            ui.logText = Text(root, "Event Log", "", 1032, 346, 340, 316, 15, cream);
            Text(root, "Action Hint", "오늘도 선택하세요. 모든 선택은 하루를 사용합니다.", 40, 714, 920, 33, 21, cream);
            ui.workButton = Button(
                root,
                "Work",
                "일하기    +₩18 / 스트레스 +15",
                40,
                762,
                427,
                67,
                lime,
                ink,
                game.Work
            );
            ui.restButton = Button(
                root,
                "Rest",
                "쉬기    체력 +28 / 최대 -₩4",
                483,
                762,
                427,
                67,
                cream,
                ink,
                game.Rest
            );
            ui.playButton = Button(root, "Play", "놀기    행복 +20 / -₩12", 926, 762, 474, 67, orange, ink, game.Play);
            Button(root, "Save", "저장", 1120, 703, 132, 40, panel, cream, game.Save);
            Button(root, "Load", "불러오기", 1268, 703, 132, 40, panel, cream, game.Load);
            Text(
                root,
                "Footer",
                "3D PROTOTYPE 01     /     행복은 상품입니다. 이안은 고객입니다.",
                40,
                854,
                1320,
                24,
                14,
                grey
            );
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            ui.Refresh();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new Exception("Scene save failed");
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
        }

        static void MakeAssets()
        {
            var fontPath = "Assets/_Game/UI/IanKoreanSDF.asset";
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                var source = new Font("Malgun Gothic");
                // Use an OS-backed dynamic font: no redistribution of the Windows font file.
                font = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 64);
                if (font == null)
                {
                    throw new Exception("Korean system font unavailable");
                }

                font.name = "Ian Korean SDF";
                font.isMultiAtlasTexturesEnabled = true;
                AssetDatabase.CreateAsset(font, fontPath);
                if (font.material != null)
                {
                    AssetDatabase.AddObjectToAsset(font.material, font);
                }

                foreach (var atlas in font.atlasTextures)
                {
                    if (atlas != null && !AssetDatabase.Contains(atlas))
                    {
                        AssetDatabase.AddObjectToAsset(atlas, font);
                    }
                }
            }

            string path = "Assets/_Game/Art/Circle.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(64, 64));
                        tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(63.5f - d)));
                    }
                }

                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            circle = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        static Image Box(Transform p, string n, float x, float y, float w, float h, Color c)
        {
            var r = Rect(p, n, x, y, w, h);
            var i = r.gameObject.AddComponent<Image>();
            i.color = c;
            i.raycastTarget = false;
            return i;
        }

        static Image Ellipse(Transform p, string n, float x, float y, float w, float h, Color c)
        {
            var i = Box(p, n, x, y, w, h, c);
            i.sprite = circle;
            return i;
        }

        static TMP_Text Text(Transform p, string n, string text, float x, float y, float w, float h, int size, Color c)
        {
            var t = Rect(p, n, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Truncate;
            return t;
        }

        static Image Gauge(Transform p, string n, float x, float y, string label, Color c, out TMP_Text t)
        {
            t = Text(p, n + " Label", label, x, y, 232, 34, 23, cream);
            var bg = Box(p, n + " Track", x, y + 45, 232, 14, ink);
            var f = Box(bg.transform, n + " Fill", 0, 0, 232, 14, c);
            var r = f.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = new Vector2(.5f, 1);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return f;
        }

        static Button Button(
            Transform p,
            string n,
            string title,
            float x,
            float y,
            float w,
            float h,
            Color bg,
            Color fg,
            UnityEngine.Events.UnityAction action
        )
        {
            var image = Box(p, n, x, y, w, h, bg);
            image.raycastTarget = true;
            var b = image.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            var colors = b.colors;
            colors.highlightedColor = new Color(.85f, .9f, .8f);
            colors.pressedColor = new Color(.7f, .75f, .65f);
            colors.disabledColor = new Color(.4f, .4f, .4f, .65f);
            b.colors = colors;
            var label = Text(image.transform, "Label", title, 8, 0, w - 16, h, 20, fg);
            label.alignment = TextAlignmentOptions.Center;
            UnityEventTools.AddPersistentListener(b.onClick, action);
            return b;
        }
    }
}
