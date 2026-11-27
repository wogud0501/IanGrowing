#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IanGame.Editor
{
    public static class GrowthVisualSetup
    {
        const string Root = "Assets/_Game/Data/Characters/Growth/";
        const string Art = "Assets/_Game/Art/Characters/Ian/Growth/";
        public static GrowthVisualCatalog Catalog =>
            AssetDatabase.LoadAssetAtPath<GrowthVisualCatalog>(Root + "GrowthVisuals.asset");

        static T Save<T>(T asset, string path)
            where T : UnityEngine.Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            EditorUtility.CopySerialized(asset, old);
            UnityEngine.Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(old);
            return old;
        }

        static Texture2D Texture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Rect Opaque(Texture2D t, Rect cell)
        {
            int x0 = (int)cell.x,
                y0 = (int)cell.y,
                x1 = x0 + (int)cell.width,
                y1 = y0 + (int)cell.height;
            int left = x1,
                right = x0,
                bottom = y1,
                top = y0;
            var pixels = t.GetPixels32();
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    if (pixels[y * t.width + x].a > 180)
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                        bottom = Math.Min(bottom, y);
                        top = Math.Max(top, y);
                    }
                }
            }

            if (right <= left || top <= bottom)
            {
                throw new Exception("Empty sprite " + t.name + " " + cell);
            }

            return Rect.MinMaxRect(left, bottom, right + 1, top + 1);
        }

        static Rect Cell(Texture2D t, int row, int col, int columns = 8, int rows = 8)
        {
            int x = col * t.width / columns,
                x1 = (col + 1) * t.width / columns,
                y = (rows - row - 1) * t.height / rows,
                y1 = (rows - row) * t.height / rows;
            return new Rect(x, y, x1 - x, y1 - y);
        }

        static Sprite Slice(Texture2D t, string name, Rect rect, float ppu, bool center = false)
        {
            var s = Sprite.Create(
                t,
                rect,
                center ? new Vector2(.5f, .5f) : new Vector2(.5f, 0),
                ppu,
                0,
                SpriteMeshType.FullRect
            );
            s.name = name;
            return Save(s, Root + name + ".asset");
        }

        [MenuItem("Ian/Growth/Build official growth visuals and bind room scenes")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            Directory.CreateDirectory(Root);
            var catalog = Catalog;
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<GrowthVisualCatalog>();
                AssetDatabase.CreateAsset(catalog, Root + "GrowthVisuals.asset");
            }
            var furniture = Texture(Art + "GrowthFurniture.png");
            if (furniture.GetPixel(0, 0).a > .01f)
            {
                throw new Exception("Furniture background must be transparent before importing");
            }

            var parts = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                parts[i] = Slice(
                    furniture,
                    "Furniture_" + i,
                    Opaque(furniture, Cell(furniture, i / 3, i % 3, 3, 2)),
                    256
                );
            }

            catalog.emptyCrib = parts[0];
            catalog.cribFront = parts[1];
            catalog.toddlerBed = parts[2];
            catalog.booster = parts[3];
            catalog.stepStool = parts[4];
            catalog.babyBath = parts[5];
            catalog.name = "Growth visuals";
            EditorUtility.SetDirty(catalog);
            IanGaitImport.Apply();
            ApplyToRooms();
            AssetDatabase.SaveAssets();
            File.WriteAllText(
                "Docs/AgeGrowth/BUILD.txt",
                $"{catalog.profiles.Length} official growth sprites. Age is derived from LifeProgression.\n"
                    + DateTime.UtcNow.ToString("O")
            );
        }

        public static void ApplyToRooms()
        {
            var catalog = Catalog;
            if (catalog == null)
            {
                return;
            }

            var rooms = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(
                "Assets/_Game/Data/SceneManagement/Rooms/RoomScenes.asset"
            );
            foreach (var r in rooms.rooms)
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/" + r.sceneName + ".unity");
                var world = UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();
                var motor = world.Player;
                var growth = motor.GetComponent<PlayerGrowthVisual>();
                if (growth == null)
                {
                    growth = motor.gameObject.AddComponent<PlayerGrowthVisual>();
                }

                growth.motor = motor;
                growth.catalog = catalog;
                motor.growthVisual = growth;
                if (r.roomId == "family-ian-bedroom")
                {
                    var bed = world.objects.Single(o => o.id == "family-bed");
                    var presentation = bed.GetComponent<GrowthBedPresentation>();
                    if (presentation == null)
                    {
                        presentation = bed.gameObject.AddComponent<GrowthBedPresentation>();
                    }

                    presentation.world = world;
                    presentation.bed = bed;
                    var action = bed.GetComponent<WorldLifeAction>();
                    var bedSlot = bed.GetComponent<WorldActivitySlot>();
                    if (bedSlot == null)
                    {
                        bedSlot = bed.gameObject.AddComponent<WorldActivitySlot>();
                    }

                    action.seat = bedSlot;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            FinalizeTextures();
        }

        public static void FinalizeTextures()
        {
            foreach (var path in Directory.GetFiles(Art, "*.png"))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.isReadable)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
#endif
