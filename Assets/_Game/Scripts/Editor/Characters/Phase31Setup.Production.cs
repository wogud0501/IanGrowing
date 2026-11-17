using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static partial class Phase31Setup
    {
        const string Data31 = "Assets/_Game/Data/Characters/Ian/YoungAdult/";

        static T Save31<T>(T value, string name)
            where T : UnityEngine.Object
        {
            string path = Data31 + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old == null)
            {
                AssetDatabase.CreateAsset(value, path);
                return value;
            }

            EditorUtility.CopySerialized(value, old);
            UnityEngine.Object.DestroyImmediate(value);
            EditorUtility.SetDirty(old);
            return old;
        }

        static Texture2D Texture31(string name)
        {
            string path = "Assets/_Game/Art/Characters/Ian/YoungAdult/Life/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Rect Cell31(Texture2D t, int r, int c, int cols = 8, int rows = 8)
        {
            int x = Mathf.RoundToInt(c * t.width / (float)cols),
                x1 = Mathf.RoundToInt((c + 1) * t.width / (float)cols),
                y = Mathf.RoundToInt((rows - r - 1) * t.height / (float)rows),
                y1 = Mathf.RoundToInt((rows - r) * t.height / (float)rows);
            return new Rect(x, y, x1 - x, y1 - y);
        }

        static Rect Opaque31(Texture2D t, Rect cell)
        {
            var pixels = t.GetPixels32();
            int l = (int)cell.xMax,
                r = (int)cell.x,
                b = (int)cell.yMax,
                top = (int)cell.y;
            for (int y = (int)cell.y; y < cell.yMax; y++)
            {
                for (int x = (int)cell.x; x < cell.xMax; x++)
                {
                    if (pixels[y * t.width + x].a > 180)
                    {
                        l = Math.Min(l, x);
                        r = Math.Max(r, x);
                        b = Math.Min(b, y);
                        top = Math.Max(top, y);
                    }
                }
            }

            if (r <= l || top <= b)
            {
                throw new Exception("Empty production frame " + cell);
            }

            return Rect.MinMaxRect(l, b, r + 1, top + 1);
        }

        static Sprite Slice31(Texture2D t, string name, Rect rect, float ppu, bool center = false)
        {
            var pixels = center ? null : t.GetPixels32();
            float headX = rect.center.x;
            if (!center)
            {
                int left = (int)rect.xMax,
                    right = (int)rect.x;
                for (int y = (int)(rect.yMax - rect.height * .22f); y < rect.yMax; y++)
                {
                    for (int x = (int)rect.x; x < rect.xMax; x++)
                    {
                        if (pixels[y * t.width + x].a > 180)
                        {
                            left = Math.Min(left, x);
                            right = Math.Max(right, x);
                        }
                    }
                }

                if (right > left)
                {
                    headX = (left + right) * .5f;
                }
            }

            var sprite = Sprite.Create(
                t,
                rect,
                new Vector2((headX - rect.x) / rect.width, center ? .5f : 0),
                ppu,
                0,
                SpriteMeshType.FullRect
            );
            sprite.name = name;
            return Save31(sprite, name);
        }

        // Generated atlases have gutters, not a guaranteed uniform grid. Discover the
        // eight opaque bands so adjacent hands/feet never bleed into another pose.
        static Rect[,] Frames31(Texture2D t, int rows, int cols)
        {
            var px = t.GetPixels32();
            List<Vector2Int> Bands(int start, int end, Func<int, bool> occupied, int expected)
            {
                var result = new List<Vector2Int>();
                int first = -1;
                for (int i = start; i <= end; i++)
                {
                    bool on = i < end && occupied(i);
                    if (on && first < 0)
                    {
                        first = i;
                    }

                    if (!on && first >= 0)
                    {
                        result.Add(new Vector2Int(first, i));
                        first = -1;
                    }
                }

                while (result.Count > expected)
                {
                    int at = 0,
                        gap = int.MaxValue;
                    for (int i = 0; i < result.Count - 1; i++)
                    {
                        int d = result[i + 1].x - result[i].y;
                        if (d < gap)
                        {
                            gap = d;
                            at = i;
                        }
                    }

                    result[at] = new Vector2Int(result[at].x, result[at + 1].y);
                    result.RemoveAt(at + 1);
                }

                if (result.Count != expected)
                {
                    throw new Exception("Atlas bands: expected " + expected + ", found " + result.Count);
                }

                return result;
            }

            var ys = Bands(0, t.height, y => Enumerable.Range(0, t.width).Any(x => px[y * t.width + x].a > 180), rows);
            ys.Reverse();
            var cells = new Rect[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                var band = ys[r];
                var xs = Bands(
                    0,
                    t.width,
                    x => Enumerable.Range(band.x, band.y - band.x).Any(y => px[y * t.width + x].a > 180),
                    cols
                );
                for (int c = 0; c < cols; c++)
                {
                    cells[r, c] = Rect.MinMaxRect(xs[c].x, band.x, xs[c].y, band.y);
                }
            }

            return cells;
        }

        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            Directory.CreateDirectory(Data31);
            var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var catalog = GrowthVisualSetup.Catalog;
            var profile = catalog.profiles.First(p => p.stage == VisualGrowthStage.Adult);
            var originalBank = AssetDatabase.LoadAssetAtPath<ActorAnimationSet>(
                "Assets/_Game/Data/World/Shared/Advanced/IanAdvanced.asset"
            );
            var texture = Texture31("Ian_YoungAdult_Life");
            var parts = new Sprite[8, 8];
            var bodyCells = Frames31(texture, 8, 8);
            float ppu = bodyCells[0, 0].height / profile.height;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    parts[r, c] = Slice31(texture, "Ian_Life_" + r + "_" + c, bodyCells[r, c], ppu);
                }
            }

            var bank = UnityEngine.Object.Instantiate(originalBank);
            bank.name = "Ian YoungAdult production";
            bank.ageScoped = true;
            var clips = bank.clips.ToList();
            void Add(
                ActorMotion motion,
                ActorDirection dir,
                Sprite[] frames,
                bool loop = false,
                bool mirror = false,
                float fps = 6
            )
            {
                clips.RemoveAll(c => c.motion == motion && c.direction == dir);
                clips.Add(
                    new ActorSpriteClip
                    {
                        motion = motion,
                        direction = dir,
                        frames = frames,
                        loop = loop,
                        mirror = mirror,
                        framesPerSecond = fps,
                        strideLength = 1.6f,
                    }
                );
            }

            Sprite[] Row(int r, int c, int n) => Enumerable.Range(c, n).Select(x => parts[r, x]).ToArray();
            Add(ActorMotion.SitDown, ActorDirection.Down, Row(0, 0, 4));
            Add(ActorMotion.StandUp, ActorDirection.Down, Row(0, 4, 4));
            Add(ActorMotion.SeatedIdle, ActorDirection.Down, Row(7, 0, 2), true, false, 2);
            Add(ActorMotion.Eat, ActorDirection.Down, Row(1, 0, 4), true, false, 5);
            Add(ActorMotion.Drink, ActorDirection.Down, Row(1, 4, 4), true, false, 5);
            foreach (var dir in new[] { ActorDirection.Right, ActorDirection.Left })
            {
                bool mirror = dir == ActorDirection.Left;
                Add(ActorMotion.SitDown, dir, Row(2, 0, 2), false, mirror);
                Add(ActorMotion.StandUp, dir, Row(2, 0, 2).Reverse().ToArray(), false, mirror);
                Add(ActorMotion.SeatedIdle, dir, new[] { parts[2, 1], parts[2, 7] }, true, mirror, 2);
                Add(ActorMotion.UseComputer, dir, Row(2, 2, 4), true, mirror);
                Add(ActorMotion.Work, dir, Row(2, 2, 4), true, mirror);
                Add(ActorMotion.Talk, dir, Row(2, 6, 2), true, mirror, 4);
                Add(ActorMotion.OpenDoor, dir, Row(6, 2, 2), false, mirror);
                Add(ActorMotion.PickUpItem, dir, Row(6, 2, 2), false, mirror);
            }

            Add(ActorMotion.SitDown, ActorDirection.Up, Row(3, 0, 3));
            Add(ActorMotion.StandUp, ActorDirection.Up, Row(3, 0, 3).Reverse().ToArray());
            Add(ActorMotion.SeatedIdle, ActorDirection.Up, Row(3, 2, 2), true, false, 2);
            Add(ActorMotion.UseComputer, ActorDirection.Up, Row(3, 2, 4), true);
            Add(ActorMotion.Work, ActorDirection.Up, Row(3, 2, 4), true);
            Add(ActorMotion.Study, ActorDirection.Up, Row(3, 6, 2), true, false, 4);
            Add(ActorMotion.OpenDoor, ActorDirection.Up, Row(6, 0, 2));
            Add(ActorMotion.PickUpItem, ActorDirection.Up, Row(6, 0, 2));
            Add(ActorMotion.UseComputer, ActorDirection.Down, Row(7, 2, 2), true);
            Add(ActorMotion.Work, ActorDirection.Down, Row(7, 2, 2), true);
            Add(ActorMotion.Study, ActorDirection.Down, Row(7, 4, 2), true, false, 4);
            Add(ActorMotion.Talk, ActorDirection.Down, Row(7, 6, 2), true, false, 4);
            Add(ActorMotion.UsePhone, ActorDirection.Down, Row(6, 6, 2), true, false, 3);
            foreach (
                var entry in new[]
                {
                    (ActorMotion.Exhausted, 0),
                    (ActorMotion.Tired, 0),
                    (ActorMotion.Stressed, 2),
                    (ActorMotion.Sick, 4),
                    (ActorMotion.Happy, 6),
                }
            )
            {
                Add(entry.Item1, ActorDirection.Down, Row(5, entry.Item2, 2), true, false, 2);
            }

            Add(ActorMotion.LieDown, ActorDirection.Down, Row(4, 0, 4));
            var sleep = originalBank.Find(ActorMotion.Sleep, ActorDirection.Down).frames[3];
            var sleepImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sleep.texture));
            bool wasReadable = sleepImporter.isReadable;
            sleepImporter.isReadable = true;
            sleepImporter.SaveAndReimport();
            var sleepTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(sleep.texture));
            var lying = Slice31(
                sleepTexture,
                "Ian_RestingBody",
                Opaque31(sleepTexture, sleep.rect),
                sleep.pixelsPerUnit,
                true
            );
            sleepImporter.isReadable = wasReadable;
            sleepImporter.SaveAndReimport();
            Add(ActorMotion.Sleep, ActorDirection.Down, new[] { lying, lying }, true, false, 2);
            Add(
                ActorMotion.WakeUp,
                ActorDirection.Down,
                new[] { lying, parts[0, 3], parts[0, 2], parts[0, 1] },
                false,
                false,
                5
            );
            bank.clips = clips.ToArray();
            bank = Save31(bank, "IanYoungAdultAnimations");
            profile.animations = bank;
            profile.bodyInteractions = true;
            profile.sleepHorizontal = true;
            profile.seatOffset = Vector2.zero;
            foreach (var growth in catalog.profiles)
            {
                growth.headphoneAnchorOffset = Vector2.down * (.13f * growth.height / profile.height);
                growth.headphoneDisplayHeight = .32f * growth.height / profile.height;
            }

            profile.artNotes =
                "Phase31: retained official 4-direction locomotion, original independent seated life frames; rest body from original art; per-pose pivot/anchor; side/missing directions explicitly queryable.";
            EditorUtility.SetDirty(catalog);
            var propTexture = Texture31("LifeProps");
            var props = new Sprite[6];
            var propCells = Frames31(propTexture, 2, 3);
            for (int i = 0; i < 6; i++)
            {
                var rect = propCells[i / 3, i % 3];
                props[i] = Slice31(propTexture, "Prop_" + i, rect, rect.height / .35f);
            }

            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/IndependentHome_Studio.unity");
            var home = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
            var chairSprite = home.objects.First(o => o.id == "home-chair").visual.sprite;
            var rooms = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(
                "Assets/_Game/Data/SceneManagement/Rooms/RoomScenes.asset"
            );
            foreach (var room in rooms.rooms)
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/" + room.sceneName + ".unity");
                var world = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
                var motor = world.Player;
                motor.controlLocks =
                    motor.GetComponent<PlayerControlLocks>() ?? motor.gameObject.AddComponent<PlayerControlLocks>();
                motor.presentation =
                    motor.GetComponent<PlayerAnimationPresentation>()
                    ?? motor.gameObject.AddComponent<PlayerAnimationPresentation>();
                motor.presentation.motor = motor;
                foreach (var o in world.objects.Where(o => o != null))
                {
                    var action = o.GetComponent<WorldLifeAction>();
                    if (o.kind == WorldInteractionKind.Sleep && action != null)
                    {
                        var blanket =
                            o.GetComponent<GrowthBedPresentation>()
                            ?? o.gameObject.AddComponent<GrowthBedPresentation>();
                        blanket.world = world;
                        blanket.bed = o;
                    }

                    if (action != null && action.mode == LifeActionVisual.Appliance)
                    {
                        action.activePose = "Pick Up Item";
                    }

                    bool desk = o.id == "starter-desk" || o.id == "study-desk" || o.id == "workdesk";
                    if (desk)
                    {
                        action = action ?? o.gameObject.AddComponent<WorldLifeAction>();
                        action.mode = LifeActionVisual.Seat;
                        if (o.id == "starter-desk")
                        {
                            action.restComputer = true;
                            action.activePose = "Use Computer";
                            o.kind = WorldInteractionKind.Rest;
                            o.title = "컴퓨터로 쉬기";
                        }

                        action.approach = Anchor(o, "InteractionApproach", new Vector2(.95f, -.9f));
                        action.anchor = Anchor(o, "ActionAnchor", new Vector2(0, -.1f));
                        action.standUp = Anchor(o, "WakeOrStandUpPoint", new Vector2(.95f, -.9f));
                        action.facing = Vector2.up;
                        action.chairSprite = chairSprite;
                        action.bookProp = o.id == "starter-desk" ? props[5] : null;
                        action.studyProp = props[3];
                        action.propAnchor = Anchor(o, "PropAnchor", new Vector2(.15f, .65f));
                        action.seat =
                            o.GetComponent<WorldActivitySlot>() ?? o.gameObject.AddComponent<WorldActivitySlot>();
                    }

                    if (o.id == "home-chair" || o.id == "writing-chair")
                    {
                        action = action ?? o.gameObject.AddComponent<WorldLifeAction>();
                        action.mode = LifeActionVisual.Seat;
                        action.activePose = "SeatedIdle";
                        action.approach = Anchor(o, "InteractionApproach", new Vector2(0, -.7f));
                        action.anchor = Anchor(o, "ActionAnchor", Vector2.zero);
                        action.standUp = action.approach;
                        action.facing = Vector2.down;
                        action.seat =
                            o.GetComponent<WorldActivitySlot>() ?? o.gameObject.AddComponent<WorldActivitySlot>();
                    }

                    if (action != null && action.mode == LifeActionVisual.Dining)
                    {
                        action.approach = Anchor(o, "InteractionApproach", new Vector2(-1.05f, .65f));
                        action.anchor = Anchor(o, "ActionAnchor", new Vector2(0, .6f));
                        action.standUp = Anchor(o, "WakeOrStandUpPoint", new Vector2(-1.05f, .65f));
                        action.facing = Vector2.down;
                        action.chairSprite = chairSprite;
                        action.tableOcclusion = true;
                        action.mealProp = props[0];
                        action.emptyMealProp = props[1];
                        action.propAnchor = Anchor(o, "PropAnchor", new Vector2(0, .45f));
                        action.seat =
                            o.GetComponent<WorldActivitySlot>() ?? o.gameObject.AddComponent<WorldActivitySlot>();
                    }

                    if (action != null && action.seat != null)
                    {
                        action.seat.useSeatAnchors = true;
                        action.seat.seatAnchor = action.anchor;
                        action.seat.approachAnchor = action.approach;
                        action.seat.standUpAnchor = action.standUp;
                        action.seat.facing = action.facing;
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            BindWearables(bank);
            Phase31Atlas.Configure();
            foreach (
                string path in new[]
                {
                    "Assets/_Game/Art/Characters/Ian/YoungAdult/Life/Ian_YoungAdult_Life.png",
                    "Assets/_Game/Art/Characters/Ian/YoungAdult/Life/LifeProps.png",
                }
            )
            {
                var i = (TextureImporter)AssetImporter.GetAtPath(path);
                i.isReadable = false;
                i.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(original);
            File.WriteAllText(
                "Docs/Phase31/APPLY.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    new
                    {
                        adultBank = Data31 + "IanYoungAdultAnimations.asset",
                        clips = bank.clips.Length,
                        rooms = rooms.rooms.Length,
                        originalAdultRetained = "Assets/_Game/Data/World/Shared/Advanced/IanAdvanced.asset",
                        newlifeFrames = 64,
                        props = 6,
                    },
                    Newtonsoft.Json.Formatting.Indented
                )
            );
        }

        static Transform Anchor(WorldInteractable o, string name, Vector2 local)
        {
            var child = o.transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(o.transform, false);
            }

            child.localPosition = local;
            return child;
        }

        static void BindWearables(ActorAnimationSet bank)
        {
            var catalog = Resources.Load<ItemCatalog>("IanCollection");
            foreach (var item in catalog.items.Where(i => i.itemType == ItemType.Headphone))
            {
                var list = item.appearanceFrames.ToList();
                foreach (
                    var clip in bank.clips.Where(c =>
                        new[]
                        {
                            ActorMotion.SeatedIdle,
                            ActorMotion.Eat,
                            ActorMotion.Drink,
                            ActorMotion.Work,
                            ActorMotion.UseComputer,
                            ActorMotion.Study,
                            ActorMotion.UsePhone,
                            ActorMotion.Talk,
                            ActorMotion.Exhausted,
                            ActorMotion.Sick,
                            ActorMotion.Stressed,
                            ActorMotion.Happy,
                            ActorMotion.OpenDoor,
                        }.Contains(c.motion)
                    )
                )
                {
                    var source = list.FirstOrDefault(f =>
                        f.growthStage == VisualGrowthStage.Adult
                        && f.direction == clip.direction
                        && f.motion == ActorMotion.Idle
                    );
                    if (source == null)
                    {
                        continue;
                    }

                    list.RemoveAll(f =>
                        f.growthStage == VisualGrowthStage.Adult
                        && f.direction == clip.direction
                        && f.motion == clip.motion
                    );
                    bool seated = new[]
                    {
                        ActorMotion.SeatedIdle,
                        ActorMotion.Eat,
                        ActorMotion.Drink,
                        ActorMotion.Work,
                        ActorMotion.UseComputer,
                        ActorMotion.Study,
                    }.Contains(clip.motion);
                    list.Add(
                        new ItemAppearanceFrame
                        {
                            growthStage = VisualGrowthStage.Adult,
                            direction = clip.direction,
                            motion = clip.motion,
                            frames = source.frames,
                            offset = source.offset + Vector2.down * (seated ? .06f : 0),
                            sortingOffset = source.sortingOffset,
                        }
                    );
                }

                item.appearanceFrames = list.ToArray();
            }

            EditorUtility.SetDirty(catalog);
        }
    }
}
