using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    static class ResidentActivityArtSetup
    {
        const string Art = "Assets/_Game/Art/Interiors/ResidentActivities/";
        internal const string LibraryPath = "Assets/_Game/Data/World/Interiors/ResidentActivityArt.asset";

        static Sprite[] Grid(string name, int rows, int columns, float ppu)
        {
            string path = Art + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.userData != "resident-activity-grid-1")
            {
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                int w = width / columns;
                int h = height / rows;
                var rectangles = new SpriteMetaData[rows * columns];
                for (int i = 0; i < rectangles.Length; i++)
                {
                    rectangles[i] = new SpriteMetaData
                    {
                        name = $"{name}_{i:D3}",
                        alignment = 9,
                        pivot = new Vector2(.5f, 3f / h),
                        rect = new Rect(i % columns * w, height - (i / columns + 1) * h, w, h),
                    };
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.spritesheet = rectangles;
                importer.userData = "resident-activity-grid-1";
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        public static void Apply()
        {
            var library = AssetDatabase.LoadAssetAtPath<ResidentActivityArt>(LibraryPath);
            if (!library)
            {
                library = ScriptableObject.CreateInstance<ResidentActivityArt>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.tools = Grid("HouseholdTools", 1, 8, 46);
            library.phone = Grid("Handset", 1, 1, 40)[0];
            library.cup = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Game/Art/WorldLife/PROP_TakeawayCoffee_01.png"
            );
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssetIfDirty(library);
            for (int bank = 0; bank < 4; bank++)
            {
                var set = AssetDatabase.LoadAssetAtPath<ActorAnimationSet>(
                    $"Assets/_Game/Data/World/Shared/Advanced/Citizen{bank}.asset"
                );
                var clips = set
                    .clips.Where(c => c.motion < ActorMotion.SitRelax || c.motion > ActorMotion.CarryLaundry)
                    .ToList();
                var sprites = Grid("CitizenActivity" + bank, 11, 16, 30.125f);
                for (int task = 0; task < 11; task++)
                {
                    foreach (ActorDirection direction in Enum.GetValues(typeof(ActorDirection)))
                    {
                        clips.Add(
                            new ActorSpriteClip
                            {
                                motion = (ActorMotion)((int)ActorMotion.SitRelax + task),
                                direction = direction,
                                mirror = set.Find(ActorMotion.Walk, direction, false).mirror,
                                frames = sprites.Skip(task * 16 + (int)direction * 4).Take(4).ToArray(),
                                loop = true,
                                framesPerSecond = task < 4 ? 1.8f : 3.2f,
                            }
                        );
                    }
                }
                set.clips = clips.ToArray();
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssetIfDirty(set);
            }
        }
    }
}
