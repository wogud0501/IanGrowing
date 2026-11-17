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

        [MenuItem("Ian/Growth/Build age art and bind existing room scenes")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            Directory.CreateDirectory(Root);
            var rooms = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(
                "Assets/_Game/Data/SceneManagement/Rooms/RoomScenes.asset"
            );
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/FamilyHome_IanBedroom.unity");
            var originalMotor = UnityEngine.Object.FindAnyObjectByType<WorldPlayerMotor>();
            var adult = originalMotor.advanced.library;
            float scale = originalMotor.visualScale;
            float adultRadius = originalMotor.GetComponent<CircleCollider2D>().radius;
            var prior = Catalog;
            if (prior != null && prior.profiles.Length == 9)
            {
                adult = prior.profiles[8].animations;
            }

            var catalog = ScriptableObject.CreateInstance<GrowthVisualCatalog>();
            var profiles = new List<GrowthVisualProfile>();
            string[] files =
            {
                "01_newborn",
                "02_toddler",
                "03_preschool",
                "04_child",
                "05_schoolchild",
                "06_preteen",
                "07_teen",
                "08_late_teen",
            };
            int[] min =  { 0, 1, 3, 5, 8, 11, 14, 17, 19 },
                max =  { 0, 2, 4, 7, 10, 13, 16, 18, int.MaxValue };
            float[] height =  { .35f, .58f, .66f, .78f, .9f, 1f, 1.08f, 1.14f, 1.2f },
                radius =  { .07f, .11f, .13f, .145f, .16f, .17f, .18f, .19f, .21f },
                speed =  { 0, .48f, .56f, .65f, .78f, .86f, .94f, .98f, 1 };
            float adultHeight = adult.Find(ActorMotion.Idle, ActorDirection.Down).frames[0].bounds.size.y * scale;
            float[] proportions = { .23f, .41f, .5f, .59f, .67f, .76f, .85f, .94f, 1 };
            for (int i = 0; i < 9; i++)
            {
                height[i] = adultHeight * proportions[i];
            }

            for (int i = 0; i < 9; i++)
            {
                ActorAnimationSet bank;
                Sprite preview;
                if (i < 8)
                {
                    var t = Texture(Art + files[i] + ".png");
                    float ppu =
                        (Opaque(t, Cell(t, 0, 0)).height + Opaque(t, Cell(t, 0, 1)).height) * .5f / height[i] * scale;
                    var sprites = new Sprite[8, 8];
                    for (int row = 0; row < 8; row++)
                    {
                        for (int col = 0; col < 8; col++)
                        {
                            sprites[row, col] = Slice(
                                t,
                                files[i] + "_" + row + "_" + col,
                                Opaque(t, Cell(t, row, col)),
                                ppu,
                                row == 4 && col < 2
                            );
                        }
                    }

                    var clips = new List<ActorSpriteClip>();
                    Action<ActorMotion, int, int, int, bool, ActorDirection> clip = (
                        motion,
                        row,
                        col,
                        count,
                        loop,
                        direction
                    ) =>
                        clips.Add(
                            new ActorSpriteClip
                            {
                                motion = motion,
                                direction = direction,
                                frames = Enumerable.Range(col, count).Select(c => sprites[row, c]).ToArray(),
                                loop = loop,
                                framesPerSecond = motion == ActorMotion.Idle ? 2 : 6,
                                strideLength = height[i] * 1.2f,
                            }
                        );
                    if (i == 0)
                    {
                        clip(ActorMotion.Idle, 0, 0, 2, true, ActorDirection.Down);
                        clips.Add(
                            new ActorSpriteClip
                            {
                                motion = ActorMotion.InfantCry,
                                direction = ActorDirection.Down,
                                frames = new[] { sprites[0, 5], sprites[1, 5] },
                                loop = true,
                                framesPerSecond = 4,
                            }
                        );
                        clip(ActorMotion.Carried, 4, 4, 2, true, ActorDirection.Down);
                        clip(ActorMotion.CareFeed, 5, 0, 2, true, ActorDirection.Down);
                        clip(ActorMotion.CareBath, 6, 0, 2, true, ActorDirection.Down);
                        clip(ActorMotion.Play, 6, 4, 2, true, ActorDirection.Down);
                        clip(ActorMotion.Interact, 7, 0, 2, true, ActorDirection.Down);
                    }
                    else
                    {
                        for (int d = 0; d < 4; d++)
                        {
                            clip(ActorMotion.Idle, d, 0, 2, true, (ActorDirection)d);
                            clip(ActorMotion.Walk, d, 2, 3, true, (ActorDirection)d);
                            clip(ActorMotion.Run, d, 5, 3, true, (ActorDirection)d);
                            clip(ActorMotion.Turn, d, 0, 2, false, (ActorDirection)d);
                            clip(ActorMotion.Stop, d, 0, 2, false, (ActorDirection)d);
                        }

                        clip(ActorMotion.SitDown, 4, 4, 2, false, ActorDirection.Down);
                        clip(ActorMotion.StandUp, 4, 6, 2, false, ActorDirection.Down);
                        ActorMotion[] actions =
                        {
                            ActorMotion.Eat,
                            ActorMotion.Drink,
                            ActorMotion.WashFace,
                            ActorMotion.Shower,
                            ActorMotion.Study,
                            ActorMotion.UseComputer,
                            ActorMotion.OpenDoor,
                            ActorMotion.PickUpItem,
                            ActorMotion.PutDownItem,
                            ActorMotion.Talk,
                            ActorMotion.Interact,
                            ActorMotion.Play,
                        };
                        for (int a = 0; a < actions.Length; a++)
                        {
                            clip(actions[a], 5 + a / 4, (a % 4) * 2, 2, true, ActorDirection.Down);
                        }

                        // Care feeding uses this age's eating art; these aliases are documented.
                        clips.Add(
                            new ActorSpriteClip
                            {
                                motion = ActorMotion.CareFeed,
                                direction = ActorDirection.Down,
                                frames = new[] { sprites[5, 0], sprites[5, 1] },
                                loop = true,
                                framesPerSecond = 4,
                            }
                        );
                        if (i == 1)
                        {
                            clips.Add(
                                new ActorSpriteClip
                                {
                                    motion = ActorMotion.CareBath,
                                    direction = ActorDirection.Down,
                                    frames = new[] { sprites[5, 6], sprites[5, 7] },
                                    loop = true,
                                    framesPerSecond = 4,
                                }
                            );
                        }
                    }

                    clip(ActorMotion.Sleep, 4, 0, 2, true, ActorDirection.Down);
                    clip(ActorMotion.WakeUp, 4, 2, 2, false, ActorDirection.Down);
                    bank = ScriptableObject.CreateInstance<ActorAnimationSet>();
                    bank.ageScoped = true;
                    bank.clips = clips.ToArray();
                    bank.name = files[i];
                    bank = Save(bank, Root + files[i] + "_Animations.asset");
                    preview = sprites[0, 0];
                }
                else
                {
                    bank = adult;
                    preview = adult.Find(ActorMotion.Idle, ActorDirection.Down).frames[0];
                }

                profiles.Add(
                    new GrowthVisualProfile
                    {
                        stage = (VisualGrowthStage)i,
                        id = ((VisualGrowthStage)i).ToString(),
                        minAge = min[i],
                        maxAge = max[i],
                        height = height[i],
                        colliderRadius = i == 8 ? adultRadius : radius[i],
                        colliderOffset = Vector2.zero,
                        speedFactor = speed[i],
                        stride = height[i] * 1.2f,
                        foot = Vector2.zero,
                        hand = new Vector2(0, height[i] * .45f),
                        eye = new Vector2(0, height[i] * .82f),
                        seatOffset = new Vector2(0, i < 5 ? .12f : 0),
                        showerOffset = Vector2.zero,
                        basinOffset = Vector2.zero,
                        animations = bank,
                        idlePreview = preview,
                        sleepHorizontal = i > 0 && i < 8,
                        artNotes =
                            i == 8
                                ? "Original adult art retained unchanged"
                                : "Generated original age-specific atlas; four-direction locomotion (except newborn); interaction directions use Down fallback. Turn/Stop and care-feed are explicit aliases. Missing motions remain queryable; not authored coverage.",
                    }
                );
            }

            catalog.profiles = profiles.ToArray();
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
            Save(catalog, Root + "GrowthVisuals.asset");
            ApplyToRooms();
            AssetDatabase.SaveAssets();
            File.WriteAllText(
                "Docs/AgeGrowth/BUILD.txt",
                "9 visual profiles; eight original age atlases; original adult bank; 16 scene bindings. Age is derived from LifeProgression.\n"
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

            if (File.Exists(Art + "ToddlerGuardianBath.png"))
            {
                var t = Texture(Art + "ToddlerGuardianBath.png");
                var pieces = new Sprite[2];
                for (int c = 0; c < 2; c++)
                {
                    var r = Opaque(t, Cell(t, 0, c, 2, 1));
                    pieces[c] = Slice(
                        t,
                        "ToddlerGuardianBath_" + c,
                        r,
                        r.height / (catalog.profiles[1].height * 1.25f) * .8f
                    );
                }

                var clip = catalog.profiles[1].animations.clips.First(x => x.motion == ActorMotion.CareBath);
                clip.frames = pieces;
                EditorUtility.SetDirty(catalog.profiles[1].animations);
            }

            foreach (var p in catalog.profiles)
            {
                var sleep = p.animations.Find(ActorMotion.Sleep, ActorDirection.Down);
                p.sleepHorizontal =
                    p.stage != VisualGrowthStage.Adult
                    && sleep != null
                    && sleep.frames[0].bounds.size.x > sleep.frames[0].bounds.size.y;
            }

            EditorUtility.SetDirty(catalog);
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
