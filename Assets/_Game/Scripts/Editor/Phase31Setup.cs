using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static partial class Phase31Setup
    {
        public static void Finish()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play before finishing");
            }

            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("ian.release.oldFast", true);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("ian.release.oldFlags", 1);
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/FamilyHome_IanBedroom.unity");
            AssetDatabase.SaveAssets();
            var bank = AssetDatabase.LoadAssetAtPath<ActorAnimationSet>(
                "Assets/_Game/Data/Phase31/IanYoungAdultAnimations.asset"
            );
            var rows = bank
                .clips.Select(c => new
                {
                    motion = c.motion.ToString(),
                    direction = c.direction.ToString(),
                    frames = c.frames.Length,
                    uniqueFrames = c.frames.Distinct().Count(),
                    c.mirror,
                    c.loop,
                    c.framesPerSecond,
                    missing = c.frames.Any(f => f == null),
                })
                .ToArray();
            File.WriteAllText(
                "Docs/Phase31/FINAL_ASSET_AUDIT.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    new
                    {
                        scene = scene.name,
                        dirty = scene.isDirty,
                        playing = Application.isPlaying,
                        target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                        fastPlay = EditorSettings.enterPlayModeOptionsEnabled,
                        flags = EditorSettings.enterPlayModeOptions.ToString(),
                        clips = rows,
                    },
                    Newtonsoft.Json.Formatting.Indented
                )
            );
        }

        public static void Audit()
        {
            Directory.CreateDirectory("Docs/Phase31");
            var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var records = new System.Collections.Generic.List<object>();
            foreach (
                var name in new[]
                {
                    "IndependentHome_Studio",
                    "FamilyHome_IanBedroom",
                    "Workspace_Office",
                    "School_Classroom",
                }
            )
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/" + name + ".unity");
                var w = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
                records.Add(
                    new
                    {
                        scene = name,
                        player = new
                        {
                            position = w.Player.Position,
                            visual = w.Player.sprite.name,
                            scale = w.Player.visualScale,
                            parent = w.Player.bodyVisual.name,
                            library = w.Player.advanced.library.name,
                        },
                        objects = w
                            .objects.Where(o =>
                                o != null
                                && (
                                    new[]
                                    {
                                        "bed",
                                        "fridge",
                                        "meal-table",
                                        "starter-desk",
                                        "home-chair",
                                        "writing-chair",
                                        "family-bed",
                                        "workdesk",
                                    }.Contains(o.id)
                                    || o.kind == WorldInteractionKind.Study
                                )
                            )
                            .Select(o => new
                            {
                                id = o.id,
                                kind = o.kind.ToString(),
                                position = o.transform.position,
                                use = o.UsePoint,
                                bounds = o.visual.bounds,
                                sprite = o.visual.sprite.name,
                                texture = AssetDatabase.GetAssetPath(o.visual.sprite.texture),
                                action = o.GetComponent<WorldLifeAction>() == null
                                    ? null
                                    : new
                                    {
                                        mode = o.GetComponent<WorldLifeAction>().mode.ToString(),
                                        approach = o.GetComponent<WorldLifeAction>().approach.localPosition,
                                        anchor = o.GetComponent<WorldLifeAction>().anchor.localPosition,
                                        facing = o.GetComponent<WorldLifeAction>().facing,
                                    },
                                slot = o.GetComponent<WorldActivitySlot>()?.useSeatAnchors,
                            })
                            .ToArray(),
                    }
                );
            }

            File.WriteAllText(
                "Docs/Phase31/BEFORE_AUDIT.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    records,
                    Newtonsoft.Json.Formatting.Indented,
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    }
                )
            );
            var cat = GrowthVisualSetup.Catalog;
            File.WriteAllText(
                "Docs/Phase31/ART_BANK_BEFORE.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    cat.profiles.Select(p => new
                    {
                        p.id,
                        p.minAge,
                        p.maxAge,
                        p.height,
                        p.sleepHorizontal,
                        library = AssetDatabase.GetAssetPath(p.animations),
                        clips = p.animations.clips.Select(c => new
                        {
                            motion = c.motion.ToString(),
                            direction = c.direction.ToString(),
                            frames = c.frames.Length,
                            texture = c.frames.Length > 0 ? AssetDatabase.GetAssetPath(c.frames[0].texture) : "",
                        }),
                    }),
                    Newtonsoft.Json.Formatting.Indented
                )
            );
            EditorSceneManager.OpenScene(original);
        }
    }
}
