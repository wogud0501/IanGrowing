using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    public static class IanGaitImport
    {
        const string Art = "Assets/_Game/Art/Characters/Ian/Growth/Official/";
        const string Banks = "Assets/_Game/Data/Characters/Ian/GrowthStages/";
        const float PixelsPerUnit = 128f;
        const float VisualScale = .8f;

        [Serializable]
        sealed class Stage
        {
            public string id;
            public int minAge;
            public int width;
            public int height;
            public int tileWidth;
            public int contact;
        }

        [Serializable]
        sealed class Sheet
        {
            public Stage[] stages;
        }

        [MenuItem("Ian/Growth/Import official 20-stage growth sheet")]
        public static void Apply()
        {
            var sheet = JsonUtility.FromJson<Sheet>(File.ReadAllText(Art + "GrowthStages.json"));
            if (sheet.stages.Length != 20)
            {
                throw new InvalidDataException("Expected all twenty official growth stages.");
            }

            Directory.CreateDirectory(Banks);
            AssetDatabase.Refresh();
            var profiles = new List<GrowthVisualProfile>();
            for (int i = 0; i < sheet.stages.Length; i++)
            {
                var stage = sheet.stages[i];
                int max = i + 1 == sheet.stages.Length ? int.MaxValue : sheet.stages[i + 1].minAge - 1;
                if (stage.id != $"S{i + 1:D2}" || stage.minAge > max || i == 0 && stage.minAge != 0)
                {
                    throw new InvalidDataException("Invalid growth range " + stage.id);
                }

                string path = Art + "Ian_" + stage.id;
                Configure(path + ".png", stage, false);
                Configure(path + "_Motion.png", stage, true);
                string front = $"Assets/_Game/Art/Characters/Ian/Growth/Presentation/Ian_{stage.id}.png";
                if (File.Exists(front))
                {
                    Configure(front, stage, false);
                }
                else
                {
                    front = path + ".png";
                }
                var idle = AssetDatabase.LoadAssetAtPath<Sprite>(front);
                var frames = AssetDatabase.LoadAllAssetsAtPath(path + "_Motion.png").OfType<Sprite>()
                    .ToDictionary(sprite => sprite.name);
                var profile = Profile(stage, max, idle);
                profile.animations = BindBank(stage, idle, frames, profile.stride);
                profiles.Add(profile);
            }

            var catalog = GrowthVisualSetup.Catalog;
            catalog.profiles = profiles.ToArray();
            catalog.growthSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/_Game/Art/Characters/Ian/Reference/Ian_OfficialGrowth.png");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        static GrowthVisualProfile Profile(Stage stage, int max, Sprite idle)
        {
            // These categories are existing interaction/wardrobe compatibility keys, not a second age.
            int[] starts = { 0, 1, 3, 5, 7, 11, 13, 17, 19, 30, 65 };
            float[] speeds = { 0, .48f, .56f, .65f, .78f, .86f, .94f, .98f, 1, 1, 1 };
            int category = Array.FindLastIndex(starts, age => age <= stage.minAge);
            float height = idle.bounds.size.y * VisualScale;
            float width = (stage.width - 10) / PixelsPerUnit * VisualScale;
            return new GrowthVisualProfile
            {
                id = stage.id,
                stage = (VisualGrowthStage)category,
                minAge = stage.minAge,
                maxAge = max,
                height = height,
                bodyInteractions = true,
                idlePreview = idle,
                stride = height * (stage.minAge < 3 ? .32f : stage.minAge < 13 ? .42f : .52f),
                speedFactor = speeds[category],
                colliderRadius = Mathf.Clamp(width * .215f, .07f, .24f),
                hand = Vector2.up * height * .39f,
                eye = Vector2.up * height * .60f,
                artNotes = "Official front " + stage.id + ". Supplementary directions and furniture poses."
            };
        }

        static void Configure(string path, Stage stage, bool atlas)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = atlas ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, stage.contact / (float)stage.height);
            settings.spritePixelsPerUnit = PixelsPerUnit;
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = atlas
                ? TextureImporterCompression.CompressedHQ
                : TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = atlas;
            if (atlas)
            {
                var slices = new List<SpriteMetaData>();
                for (int row = 0; row < 6; row++)
                {
                    int count = row == 3 ? 6 : stage.minAge == 0 ? 1 : 7;
                    for (int col = 0; col < count; col++)
                    {
                        slices.Add(new SpriteMetaData
                        {
                            name = Key(stage, row, col),
                            rect = new Rect(col * stage.tileWidth, (5 - row) * stage.height, stage.tileWidth, stage.height),
                            alignment = (int)SpriteAlignment.Custom,
                            pivot = settings.spritePivot
                        });
                    }
                }
#pragma warning disable CS0618
                importer.spritesheet = slices.ToArray();
#pragma warning restore CS0618
            }
            importer.SaveAndReimport();
        }

        static string Key(Stage stage, int row, int col) => $"{stage.id}_{row}_{col}";

        static ActorAnimationSet BindBank(Stage stage, Sprite idle, Dictionary<string, Sprite> art, float stride)
        {
            string path = Banks + "Ian_" + stage.id + ".asset";
            var bank = AssetDatabase.LoadAssetAtPath<ActorAnimationSet>(path);
            if (!bank)
            {
                bank = ScriptableObject.CreateInstance<ActorAnimationSet>();
                AssetDatabase.CreateAsset(bank, path);
            }

            var clips = new List<ActorSpriteClip>();
            void Add(ActorMotion motion, ActorDirection direction, Sprite[] frames, bool loop, bool mirror = false)
            {
                var clip = new ActorSpriteClip
                {
                    motion = motion,
                    direction = direction,
                    frames = frames,
                    loop = loop,
                    mirror = mirror,
                    framesPerSecond = motion == ActorMotion.Idle ? 2 : 7,
                    strideLength = motion == ActorMotion.Run ? stride * 1.3f : stride
                };
                if (direction is ActorDirection.Down or ActorDirection.Up
                    && motion is ActorMotion.Idle or ActorMotion.Stop or ActorMotion.Turn or ActorMotion.Walk or ActorMotion.Run
                    && (stage.minAge >= 3 || stage.minAge == 1 && motion is ActorMotion.Walk or ActorMotion.Run))
                {
                    int diagonalRow = direction == ActorDirection.Down ? 4 : 5;
                    clip.diagonalLeft = motion is ActorMotion.Walk or ActorMotion.Run
                        ? Enumerable.Range(1, 6).Select(col => art[Key(stage, diagonalRow, col)]).ToArray()
                        : new[] { art[Key(stage, diagonalRow, 0)], art[Key(stage, diagonalRow, 0)] };
                    clip.mirrorDiagonalRight = true;
                }
                clips.Add(clip);
            }
            for (int d = 0; d < 4; d++)
            {
                int row = Math.Min(d, 2);
                bool mirror = d == (int)ActorDirection.Right;
                var dir = (ActorDirection)d;
                var still = d == 0 ? idle : art[Key(stage, row, 0)];
                foreach (var motion in new[] { ActorMotion.Idle, ActorMotion.Turn, ActorMotion.Stop })
                {
                    Add(motion, dir, new[] { still, still }, true, mirror);
                }
                if (stage.minAge > 0)
                {
                    var walk = Enumerable.Range(1, 6).Select(col => art[Key(stage, row, col)]).ToArray();
                    Add(ActorMotion.Walk, dir, walk, true, mirror);
                    Add(ActorMotion.Run, dir, walk, true, mirror);
                }
            }

            var lean = art[Key(stage, 3, 0)];
            var bend = art[Key(stage, 3, 1)];
            var seated = stage.minAge < 3 ? idle : art[Key(stage, 3, 3)];
            var sleep = art[Key(stage, 3, 4)];
            var reclined = art[Key(stage, 3, 5)];
            foreach (ActorMotion motion in Enum.GetValues(typeof(ActorMotion)))
            {
                if (motion is ActorMotion.Walk or ActorMotion.Run or ActorMotion.Turn or ActorMotion.Stop or ActorMotion.Idle)
                {
                    continue;
                }
                var frames = motion switch
                {
                    ActorMotion.SitDown => new[] { idle, lean, bend, seated },
                    ActorMotion.StandUp => new[] { seated, bend, lean, idle },
                    ActorMotion.LieDown => new[] { seated, reclined, sleep, sleep },
                    ActorMotion.WakeUp => new[] { sleep, reclined, seated, seated },
                    ActorMotion.Sleep => new[] { sleep, sleep },
                    ActorMotion.SeatedIdle or ActorMotion.TalkSit or ActorMotion.SitRelax => new[] { seated, seated },
                    ActorMotion.Carried => new[] { idle, idle },
                    _ => new[] { idle, lean, bend, lean }
                };
                bool loop = motion is not (ActorMotion.SitDown or ActorMotion.StandUp or ActorMotion.WakeUp or ActorMotion.LieDown);
                Add(motion, ActorDirection.Down, frames, loop);
            }
            bank.clips = clips.ToArray();
            bank.ageScoped = true;
            bank.turnaroundDirections = true;
            EditorUtility.SetDirty(bank);
            return bank;
        }
    }
}
