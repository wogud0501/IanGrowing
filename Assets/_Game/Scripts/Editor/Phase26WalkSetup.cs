using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static class Phase26WalkSetup
    {
        public const string Atlas = "Assets/_Game/Art/Characters/Ian/Animations/WorldWalkAtlas.png";
        public const string Data = "Assets/_Game/Data/Phase26/IanWalkSet.asset";
        const string Folder = "Assets/_Game/Art/Characters/Ian/Animations/";
        static readonly string[] Directions = { "Down", "Up", "Left", "Right" };

        [MenuItem("Ian/Life World/Import Directional Walk Atlas")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play before importing character animation");
            }

            if (!File.Exists(Atlas))
            {
                throw new FileNotFoundException("Author and inspect the six-column, four-row walk atlas first", Atlas);
            }

            var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (current.isDirty)
            {
                throw new InvalidOperationException("Save the current scene before applying animation references");
            }

            AssetDatabase.ImportAsset(Atlas, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            int width = texture.width,
                height = texture.height;
            var importer = (TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.GetSourceTextureWidthAndHeight(out width, out height);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = height / 4f / 256f * 120f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.isReadable = true;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            width = texture.width;
            height = texture.height;
            var pixels = texture.GetPixels32();
            if (pixels[0].a != 0 || pixels[width - 1].a != 0)
            {
                throw new Exception("Atlas background is not transparent; do not apply it to the scene");
            }

            var slices = new SpriteMetaData[24];
            for (int row = 0; row < 4; row++)
            {
                for (int frame = 0; frame < 6; frame++)
                {
                    int x = Mathf.RoundToInt(frame * width / 6f),
                        x1 = Mathf.RoundToInt((frame + 1) * width / 6f),
                        y = Mathf.RoundToInt((3 - row) * height / 4f),
                        y1 = Mathf.RoundToInt((4 - row) * height / 4f);
                    int foot = -1;
                    for (int py = y; py < y1 && foot < 0; py++)
                    {
                        for (int px = x; px < x1; px++)
                        {
                            if (pixels[py * width + px].a >= 220)
                            {
                                foot = py;
                                break;
                            }
                        }
                    }

                    if (foot < 0)
                    {
                        throw new Exception("Empty animation cell " + row + " / " + frame);
                    }

                    slices[row * 6 + frame] = new SpriteMetaData
                    {
                        name = "Ian_" + Directions[row] + "_" + frame,
                        rect = new Rect(x, y, x1 - x, y1 - y),
                        alignment = 9,
                        pivot = new Vector2(.5f, (foot + .5f - y) / (y1 - y)),
                    };
                }
            }

            importer.spritesheet = slices;
            importer.isReadable = false;
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().ToDictionary(s => s.name);
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "IanWorld.controller")
                ?? AnimatorController.CreateAnimatorControllerAtPath(Folder + "IanWorld.controller");
            var stateMachine = controller.layers[0].stateMachine;
            var sets = new Sprite[4][];
            for (int row = 0; row < 4; row++)
            {
                sets[row] = Enumerable.Range(0, 6).Select(i => sprites["Ian_" + Directions[row] + "_" + i]).ToArray();
                foreach (bool walk in new[] { false, true })
                {
                    string name = (walk ? "Walk_" : "Idle_") + Directions[row],
                        path = Folder + name + ".anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip == null)
                    {
                        clip = new AnimationClip { name = name };
                        AssetDatabase.CreateAsset(clip, path);
                    }

                    clip.frameRate = 10;
                    var frames = walk ? sets[row] : new[] { sets[row][0] };
                    var keys = new ObjectReferenceKeyframe[frames.Length + 1];
                    for (int i = 0; i < keys.Length; i++)
                    {
                        keys[i] = new ObjectReferenceKeyframe { time = i / 10f, value = frames[i % frames.Length] };
                    }

                    AnimationUtility.SetObjectReferenceCurve(
                        clip,
                        new EditorCurveBinding
                        {
                            path = "",
                            type = typeof(SpriteRenderer),
                            propertyName = "m_Sprite",
                        },
                        keys
                    );
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = walk;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    var state =
                        stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == name)
                        ?? stateMachine.AddState(name);
                    state.motion = clip;
                    if (name == "Idle_Down")
                    {
                        stateMachine.defaultState = state;
                    }

                    EditorUtility.SetDirty(clip);
                }
            }

            var data = AssetDatabase.LoadAssetAtPath<WorldWalkSet>(Data);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WorldWalkSet>();
                AssetDatabase.CreateAsset(data, Data);
            }

            data.controller = controller;
            data.down = sets[0];
            data.up = sets[1];
            data.left = sets[2];
            data.right = sets[3];
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            string previous = current.path;
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
            AttachIfReady(UnityEngine.Object.FindAnyObjectByType<WorldPlayerMotor>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(previous);
        }

        public static bool AttachIfReady(WorldPlayerMotor motor)
        {
            var data = AssetDatabase.LoadAssetAtPath<WorldWalkSet>(Data);
            if (data == null || !data.Complete)
            {
                return false;
            }

            if (motor == null)
            {
                throw new Exception("Existing player is missing");
            }

            var animator = motor.sprite.GetComponent<Animator>();
            if (animator == null)
            {
                animator = motor.sprite.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = data.controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            motor.walkSet = data;
            motor.locomotion = animator;
            motor.sprite.sprite = data.down[0];
            motor.sprite.flipX = false;
            motor.sprite.transform.localScale = Vector3.one;
            motor.sprite.transform.localPosition = Vector3.zero;
            motor.bodyVisual.localPosition = Vector3.zero;
            motor.bodyVisual.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(motor);
            EditorUtility.SetDirty(animator);
            return true;
        }
    }
}
