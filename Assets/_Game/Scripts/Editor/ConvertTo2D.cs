using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static class ConvertTo2D
    {
        static Color C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

        static void Rect(Texture2D t, int x, int y, int w, int h, Color c)
        {
            for (int j = Mathf.Max(0, y); j < Mathf.Min(t.height, y + h); j++)
            {
                for (int i = Mathf.Max(0, x); i < Mathf.Min(t.width, x + w); i++)
                {
                    t.SetPixel(i, j, c);
                }
            }
        }

        static void Oval(Texture2D t, int x, int y, int rx, int ry, Color c)
        {
            for (int j = Mathf.Max(0, y - ry); j < Mathf.Min(t.height, y + ry); j++)
            {
                for (int i = Mathf.Max(0, x - rx); i < Mathf.Min(t.width, x + rx); i++)
                {
                    if (Mathf.Pow((i - x) / (float)rx, 2) + Mathf.Pow((j - y) / (float)ry, 2) < 1)
                    {
                        t.SetPixel(i, j, c);
                    }
                }
            }
        }

        static Sprite Save(Texture2D t, string name, float ppu)
        {
            var path = "Assets/_Game/Art/2D/" + name + ".png";
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = ppu;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            imp.SetTextureSettings(settings);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite Character(string state, int frame)
        {
            var t = new Texture2D(256, 384, TextureFormat.RGBA32, false);
            var clear = new Color[256 * 384];
            t.SetPixels(clear);
            var ink = C(27, 29, 42);
            var skin = C(239, 191, 153);
            var lime = C(213, 232, 116);
            var trouser = C(52, 67, 74);
            int dy = frame == 1 ? 2 : 0;
            // Bold outlined paper-doll placeholder; each action has a readable face or prop.
            Oval(t, 128, 17, 85, 11, new Color(0, 0, 0, .28f));
            Rect(t, 76, 22, 43, 114, ink);
            Rect(t, 139, 22, 43, 114, ink);
            Rect(t, 82, 25, 31, 109, trouser);
            Rect(t, 145, 25, 31, 109, trouser);
            Rect(t, 62, 17, 58, 22, ink);
            Rect(t, 138, 17, 59, 22, ink);
            Rect(t, 60, 120 + dy, 136, 126, ink);
            Rect(t, 66, 127 + dy, 124, 112, lime);
            Rect(t, 42, 128 + dy, 27, 103, ink);
            Rect(t, 48, 135 + dy, 17, 91, lime);
            Oval(t, 55, 128 + dy, 14, 19, skin);
            Rect(t, 190, 128 + dy, 27, 103, ink);
            Rect(t, 196, 135 + dy, 16, 91, lime);
            Oval(t, 204, 128 + dy, 14, 19, skin);
            Rect(t, 111, 233 + dy, 34, 34, ink);
            Rect(t, 117, 236 + dy, 22, 32, skin);
            Rect(t, 122, 143 + dy, 12, 85, ink);
            Rect(t, 95, 192 + dy, 23, 8, C(245, 244, 221));
            Oval(t, 128, 296 + dy, 63, 70, ink);
            Oval(t, 128, 292 + dy, 56, 61, skin);
            Rect(t, 74, 325 + dy, 108, 30, ink);
            Rect(t, 80, 314 + dy, 23, 25, ink);
            Rect(t, 159, 328 + dy, 22, 25, ink);
            if (state == "Rest" || frame == 1)
            {
                Rect(t, 91, 289 + dy, 23, 4, ink);
                Rect(t, 144, 289 + dy, 23, 4, ink);
            }
            else
            {
                Oval(t, 103, 294 + dy, 5, 8, ink);
                Oval(t, 155, 294 + dy, 5, 8, ink);
            }

            Rect(t, 87, 278 + dy, 30, 3, C(175, 125, 114));
            Rect(t, 139, 278 + dy, 30, 3, C(175, 125, 114));
            if (state == "Play")
            {
                Oval(t, 128, 263 + dy, 13, 9, ink);
                Rect(t, 211, 315, 7, 34, lime);
                Rect(t, 197, 328, 35, 7, lime);
            }
            else
            {
                Rect(t, 116, 262 + dy, 24, 4, ink);
            }

            if (state == "Work")
            {
                Rect(t, 181, 147, 45, 43, ink);
                Rect(t, 187, 152, 33, 32, C(222, 136, 102));
                Rect(t, 195, 199 + frame * 5, 4, 22, C(240, 232, 202));
            }

            if (state == "Rest")
            {
                Rect(t, 190, 281, 22, 5, lime);
                Rect(t, 207, 297, 24, 5, lime);
            }

            return Save(t, "Ian_" + state + "_" + frame, 100);
        }

        static Sprite Room()
        {
            var t = new Texture2D(800, 520, TextureFormat.RGBA32, false);
            var ink = C(27, 29, 42);
            Rect(t, 0, 0, 800, 520, ink);
            Rect(t, 8, 8, 784, 504, C(45, 61, 66));
            Rect(t, 8, 8, 784, 124, C(78, 76, 75));
            for (int i = 0; i < 10; i++)
            {
                Rect(t, 8 + i * 88, 8, 2, 124, C(57, 61, 65));
            }

            Rect(t, 45, 188, 204, 260, ink);
            Rect(t, 53, 196, 188, 244, C(117, 153, 159));
            Rect(t, 142, 196, 9, 244, ink);
            Rect(t, 53, 308, 188, 9, ink);
            Rect(t, 62, 366, 70, 10, C(235, 211, 161));
            Rect(t, 158, 260, 60, 7, C(235, 211, 161));
            Rect(t, 30, 146, 245, 15, C(221, 153, 104));
            Rect(t, 45, 45, 15, 103, ink);
            Rect(t, 244, 45, 15, 103, ink);
            Rect(t, 100, 169, 102, 79, ink);
            Rect(t, 108, 177, 86, 62, C(216, 232, 119));
            Rect(t, 143, 148, 16, 24, ink);
            Rect(t, 584, 102, 176, 116, ink);
            Rect(t, 592, 110, 160, 100, C(161, 126, 121));
            Rect(t, 574, 88, 28, 91, ink);
            Rect(t, 741, 88, 28, 91, ink);
            Rect(t, 610, 123, 120, 15, C(198, 149, 137));
            Rect(t, 534, 310, 211, 142, ink);
            Rect(t, 542, 318, 195, 126, C(220, 202, 162));
            // Stripes intentionally mark this as a graphic prototype rather than final art.
            for (int i = 0; i < 5; i++)
            {
                Rect(t, 558, 338 + i * 20, 150 - i * 17, 7, ink);
            }

            Rect(t, 312, 8, 200, 102, C(117, 140, 104));
            Rect(t, 320, 14, 184, 4, C(210, 226, 128));
            Oval(t, 54, 64, 28, 35, C(185, 124, 90));
            Rect(t, 49, 84, 9, 80, C(149, 179, 104));
            Oval(t, 34, 142, 22, 12, C(149, 179, 104));
            Oval(t, 71, 120, 22, 12, C(149, 179, 104));
            return Save(t, "Apartment_Placeholder", 100);
        }

        [MenuItem("Ian/Convert Preserved MainGame To 2D")]
        public static void Build()
        {
            const string path = "Assets/_Game/Scenes/MainGame2D.unity";
            if (File.Exists(path))
            {
                throw new InvalidOperationException("2D scene exists; refusing to overwrite.");
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty)
            {
                throw new Exception("Current scene has unsaved changes.");
            }

            if (!EditorSceneManager.SaveScene(scene, path, true))
            {
                throw new Exception("Could not copy scene");
            }

            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var archive = new GameObject("ARCHIVE - 3D Elements (Inactive)");
            foreach (
                var name in new[]
                {
                    "Main Camera",
                    "Directional Light",
                    "Apartment 001 - 3D Stage",
                    "Ian Character",
                    "Global Volume",
                }
            )
            {
                var g = GameObject.Find(name);
                if (g != null)
                {
                    g.transform.SetParent(archive.transform, true);
                }
            }

            archive.SetActive(false);
            var camera = new GameObject("Main Camera 2D", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            var cam = camera.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 50;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = C(18, 23, 29);
            camera.transform.position = new Vector3(0, 0, -10);
            var data = camera.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            Directory.CreateDirectory(Application.dataPath + "/_Game/Art/2D");
            var visuals = ScriptableObject.CreateInstance<Character2DVisuals>();
            visuals.idle = new[] { Character("Idle", 0), Character("Idle", 1) };
            visuals.work = new[] { Character("Work", 0), Character("Work", 1) };
            visuals.rest = new[] { Character("Rest", 0), Character("Rest", 1) };
            visuals.play = new[] { Character("Play", 0), Character("Play", 1) };
            visuals.idle = Enumerable.Repeat(visuals.idle[0], 7).Concat(new[] { visuals.idle[1] }).ToArray();
            visuals.framesPerSecond = 4;
            AssetDatabase.CreateAsset(visuals, "Assets/_Game/Data/Ian2DVisuals.asset");
            var stage = new GameObject("2D Stage");
            var room = new GameObject("Apartment Sprite", typeof(SpriteRenderer));
            room.transform.SetParent(stage.transform);
            room.transform.localPosition = Vector3.zero;
            room.GetComponent<SpriteRenderer>().sprite = Room();
            room.GetComponent<SpriteRenderer>().sortingOrder = 0;
            var actor = new GameObject("Ian 2D Character", typeof(SpriteRenderer), typeof(Character2DPresentation));
            actor.transform.SetParent(stage.transform);
            actor.transform.localPosition = new Vector3(0, .02f, 0);
            var sr = actor.GetComponent<SpriteRenderer>();
            sr.sprite = visuals.idle[0];
            sr.sortingOrder = 10;
            var presentation = actor.GetComponent<Character2DPresentation>();
            presentation.visuals = visuals;
            presentation.spriteRenderer = sr;
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                actor,
                "Assets/_Game/Prefabs/Ian2DCharacter.prefab",
                InteractionMode.AutomatedAction
            );
            var hud = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            hud.game = null;
            var buttons = hud.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var button in buttons)
            {
                for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
                }
            }

            hud.saveButton = buttons.First(b => b.name == "Save");
            hud.loadButton = buttons.First(b => b.name == "Load");
            var footer = hud.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t => t.name == "Footer");
            footer.text = "2D PROTOTYPE 01     /     기능 검증용 그래픽 · 최종 디자인 아님";
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                hud.gameObject,
                "Assets/_Game/Prefabs/GameHUD.prefab",
                InteractionMode.AutomatedAction
            );
            var bindings = new GameObject(
                "Presentation Bindings",
                typeof(GameplaySceneBindings)
            ).GetComponent<GameplaySceneBindings>();
            bindings.game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            bindings.hud = hud;
            bindings.character = presentation;
            // An edit-time refresh is safe; detach before serialization so the HUD asset stays independent.
            hud.Bind(bindings.game);
            hud.Refresh();
            hud.Bind(null);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new Exception("2D scene save failed");
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = actor;
        }
    }
}
