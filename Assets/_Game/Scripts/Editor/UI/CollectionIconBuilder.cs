#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace IanGame.Editor
{
    // Original, hand-authored integer-pixel UI symbols. These are not wearable art.
    public static class CollectionIconBuilder
    {
        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            var c = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Resources/IanCollection.asset");
            Directory.CreateDirectory("Assets/_Game/UI/Collection");
            foreach (var d in c.items)
            {
                if (
                    d.itemType != ItemType.Headphone
                    && d.itemType != ItemType.Ticket
                    && d.itemType != ItemType.Material
                    && d.itemType != ItemType.Cosmetic
                )
                {
                    continue;
                }

                var pixels = new Color32[32 * 32];
                Color32 ink = new Color32(47, 47, 49, 255),
                    metal = new Color32(88, 91, 92, 255),
                    light = new Color32(180, 181, 169, 255),
                    accent = c.Rarity(d.rarityId).color;
                void R(int x, int y, int w, int h, Color32 color)
                {
                    for (int py = y; py < y + h; py++)
                    {
                        for (int px = x; px < x + w; px++)
                        {
                            if (px >= 0 && px < 32 && py >= 0 && py < 32)
                            {
                                pixels[py * 32 + px] = color;
                            }
                        }
                    }
                }

                if (d.itemType == ItemType.Headphone)
                {
                    R(7, 21, 18, 6, ink);
                    R(4, 17, 6, 8, ink);
                    R(22, 17, 6, 8, ink);
                    R(9, 23, 14, 2, metal);
                    R(3, 7, 8, 13, ink);
                    R(21, 7, 8, 13, ink);
                    R(5, 9, 4, 9, metal);
                    R(23, 9, 4, 9, metal);
                    R(8, 9, 2, 7, accent);
                    R(22, 9, 2, 7, accent);
                    R(11, 7, 10, 3, ink);
                    R(13, 5, 6, 3, ink);
                    R(14, 6, 4, 2, light);
                }
                else if (d.itemType == ItemType.Cosmetic)
                {
                    R(10, 5, 12, 20, ink);
                    R(5, 18, 22, 8, ink);
                    R(3, 16, 7, 7, ink);
                    R(22, 16, 7, 7, ink);
                    R(13, 23, 6, 3, light);
                    R(12, 6, 8, 14, metal);
                    R(11, 7, 1, 12, accent);
                    R(21, 7, 1, 12, accent);
                }
                else if (d.itemType == ItemType.Ticket)
                {
                    R(4, 7, 24, 18, ink);
                    R(6, 9, 20, 14, light);
                    R(18, 9, 2, 14, ink);
                    R(19, 10, 1, 2, light);
                    R(19, 15, 1, 2, light);
                    R(9, 13, 6, 6, accent);
                    R(10, 14, 4, 4, metal);
                    R(3, 14, 3, 4, new Color32(0, 0, 0, 0));
                    R(26, 14, 3, 4, new Color32(0, 0, 0, 0));
                }
                else
                {
                    R(8, 8, 7, 7, ink);
                    R(10, 10, 3, 3, metal);
                    R(18, 17, 7, 7, ink);
                    R(20, 19, 3, 3, light);
                    R(15, 5, 4, 8, metal);
                    R(13, 7, 8, 3, metal);
                    R(5, 22, 7, 3, accent);
                }

                string path = "Assets/_Game/UI/Collection/" + d.itemId + ".png";
                var t = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                t.SetPixels32(pixels);
                t.Apply();
                File.WriteAllBytes(path, t.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                d.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
