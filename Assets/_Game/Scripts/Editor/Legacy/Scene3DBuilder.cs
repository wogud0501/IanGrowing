using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IanGame.Editor
{
    public static class Scene3DBuilder
    {
        static Transform room;

        static Material Mat(string name, Color color, float metal = 0, float smooth = .25f, bool emissive = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new Exception("URP Lit shader missing");
            }

            var m = new Material(shader) { name = name };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Smoothness", smooth);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 3);
            }

            AssetDatabase.CreateAsset(m, "Assets/_Game/Materials/" + name + ".mat");
            return m;
        }

        static GameObject Shape(string name, PrimitiveType type, Vector3 p, Vector3 s, Material m)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            g.transform.SetParent(room);
            g.transform.localPosition = p;
            g.transform.localScale = s;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        static void Lamp(
            string name,
            LightType type,
            Vector3 pos,
            Color color,
            float power,
            float range,
            Vector3 target
        )
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(room);
            go.transform.position = pos;
            go.transform.LookAt(target);
            var l = go.GetComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = power;
            l.range = range;
            l.shadows = LightShadows.Soft;
            if (type == LightType.Spot)
            {
                l.spotAngle = 65;
                l.innerSpotAngle = 35;
            }
        }

        public static void Build(GameManager game, Camera cam)
        {
            room = new GameObject("Apartment 001 - 3D Stage").transform;
            var floor = Mat("Warm Concrete", new Color(.28f, .27f, .23f));
            var wall = Mat("Charcoal Plaster", new Color(.12f, .17f, .17f));
            var wood = Mat("Walnut", new Color(.28f, .15f, .08f));
            var metal = Mat("Graphite Metal", new Color(.07f, .09f, .1f), .65f, .5f);
            var fabric = Mat("Sage Fabric", new Color(.34f, .44f, .24f));
            var skin = Mat("Ian Clay", new Color(.69f, .54f, .37f), 0, .35f);
            var neon = Mat("Warm Emission", new Color(.8f, .63f, .23f), 0, .2f, true);
            var leaf = Mat("Plant Green", new Color(.18f, .35f, .18f));
            var paper = Mat("Paper", new Color(.75f, .74f, .64f));
            Shape("Floor", PrimitiveType.Cube, new Vector3(0, -.13f, .5f), new Vector3(7, .25f, 6), floor);
            Shape("Back Wall", PrimitiveType.Cube, new Vector3(0, 1.7f, 3), new Vector3(7, 3.5f, .15f), wall);
            Shape("Left Wall", PrimitiveType.Cube, new Vector3(-3.5f, 1.7f, .5f), new Vector3(.15f, 3.5f, 5), wall);
            Shape("Rug", PrimitiveType.Cube, new Vector3(0, .012f, 0), new Vector3(2.4f, .025f, 2), fabric);
            Shape("Desk Top", PrimitiveType.Cube, new Vector3(-1.9f, .83f, 1.6f), new Vector3(1.7f, .1f, .8f), wood);
            foreach (float x in new[] { -2.55f, -1.25f })
            {
                foreach (float z in new[] { 1.3f, 1.9f })
                {
                    Shape("Desk Leg", PrimitiveType.Cube, new Vector3(x, .4f, z), new Vector3(.07f, .8f, .07f), metal);
                }
            }

            Shape(
                "Monitor",
                PrimitiveType.Cube,
                new Vector3(-1.9f, 1.22f, 1.86f),
                new Vector3(.78f, .48f, .065f),
                metal
            );
            Shape(
                "Monitor Screen",
                PrimitiveType.Cube,
                new Vector3(-1.9f, 1.22f, 1.82f),
                new Vector3(.68f, .36f, .01f),
                neon
            );
            Shape(
                "Monitor Stand",
                PrimitiveType.Cube,
                new Vector3(-1.9f, .95f, 1.86f),
                new Vector3(.09f, .2f, .1f),
                metal
            );
            Shape("Keyboard", PrimitiveType.Cube, new Vector3(-1.9f, .9f, 1.35f), new Vector3(.6f, .035f, .22f), metal);
            Shape(
                "Chair Seat",
                PrimitiveType.Cube,
                new Vector3(-1.9f, .46f, .65f),
                new Vector3(.56f, .12f, .5f),
                fabric
            );
            Shape(
                "Chair Back",
                PrimitiveType.Cube,
                new Vector3(-1.9f, .81f, .41f),
                new Vector3(.56f, .65f, .08f),
                fabric
            );
            Shape(
                "Chair Stem",
                PrimitiveType.Cylinder,
                new Vector3(-1.9f, .22f, .65f),
                new Vector3(.1f, .22f, .1f),
                metal
            );
            Shape("Chair Base", PrimitiveType.Cube, new Vector3(-1.9f, .05f, .65f), new Vector3(.6f, .05f, .6f), metal);
            Shape(
                "Sofa Base",
                PrimitiveType.Cube,
                new Vector3(1.8f, .32f, 1.75f),
                new Vector3(1.65f, .55f, .8f),
                fabric
            );
            Shape(
                "Sofa Back",
                PrimitiveType.Cube,
                new Vector3(1.8f, .72f, 2.09f),
                new Vector3(1.65f, .7f, .2f),
                fabric
            );
            foreach (float x in new[] { .98f, 2.62f })
            {
                Shape(
                    "Sofa Arm",
                    PrimitiveType.Cube,
                    new Vector3(x, .58f, 1.75f),
                    new Vector3(.18f, .62f, .85f),
                    fabric
                );
            }

            Shape(
                "Lamp Stem",
                PrimitiveType.Cylinder,
                new Vector3(2.9f, 1.1f, 2.2f),
                new Vector3(.045f, 1.1f, .045f),
                metal
            );
            Shape(
                "Lamp Shade",
                PrimitiveType.Cylinder,
                new Vector3(2.9f, 2.2f, 2.2f),
                new Vector3(.65f, .24f, .65f),
                neon
            );
            Shape(
                "Plant Pot",
                PrimitiveType.Cylinder,
                new Vector3(-2.8f, .25f, -.5f),
                new Vector3(.45f, .25f, .45f),
                wood
            );
            for (int i = 0; i < 7; i++)
            {
                var l = Shape(
                    "Leaf",
                    PrimitiveType.Sphere,
                    new Vector3(-2.8f + Mathf.Sin(i * 2f) * .18f, .65f + i * .065f, -.5f + Mathf.Cos(i * 2f) * .18f),
                    new Vector3(.22f, .6f, .12f),
                    leaf
                );
                l.transform.localRotation = Quaternion.Euler(i * 9, i * 50, 25);
            }

            // Wall notice: a world-space TMP prop.
            Shape("Notice Frame", PrimitiveType.Cube, new Vector3(0, 2.2f, 2.88f), new Vector3(1.6f, .8f, .06f), metal);
            var notice = new GameObject("Happiness Price Notice", typeof(TMPro.TextMeshPro));
            notice.transform.SetParent(room);
            notice.transform.position = new Vector3(0, 2.2f, 2.83f);
            notice.transform.localScale = Vector3.one * .065f;
            var text = notice.GetComponent<TMPro.TextMeshPro>();
            text.font = StardustTypography.Main;
            text.text = "HAPPINESS\nSOLD SEPARATELY";
            text.fontSize = 38;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = new Color(.83f, .88f, .5f);
            text.rectTransform.sizeDelta = new Vector2(24, 10);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Packages/com.unity.ai.assistant/Modules/Unity.AI.Animate/Motion/Runtime/Prefabs/Actors/biped_v1/Biped_Humanoid.fbx"
            );
            if (model == null)
            {
                throw new Exception("Existing biped model could not be loaded");
            }

            var actor = new GameObject("Ian Character");
            actor.transform.position = Vector3.zero;
            var visual = UnityEngine.Object.Instantiate(model, actor.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0, 180, 0);
            visual.transform.localScale = Vector3.one;
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = skin;
                }

                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }

            var rigAnimator = visual.GetComponent<Animator>();
            if (rigAnimator != null)
            {
                rigAnimator.enabled = false;
            }

            var animator = actor.AddComponent<Animator>();
            // Four actual Animator states with root motion curves as prototype action feedback.
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                "Assets/_Game/Animations/Ian/Legacy/IanPrototype.controller"
            );
            var sm = controller.layers[0].stateMachine;
            foreach (var action in new[] { "Idle", "Work", "Rest", "Play" })
            {
                var clip = new AnimationClip { name = action, frameRate = 30 };
                float amplitude =
                    action == "Play" ? .12f
                    : action == "Rest" ? .012f
                    : .025f;
                float duration = action == "Idle" ? 3 : 1.4f;
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve("Visual", typeof(Transform), "m_LocalPosition.y"),
                    new AnimationCurve(
                        new Keyframe(0, 0),
                        new Keyframe(duration * .25f, amplitude),
                        new Keyframe(duration * .5f, 0),
                        new Keyframe(duration * .75f, amplitude),
                        new Keyframe(duration, 0)
                    )
                );
                float tilt =
                    action == "Work" ? 8
                    : action == "Rest" ? -6
                    : action == "Play" ? 12
                    : 1.5f;
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve("Visual", typeof(Transform), "localEulerAnglesRaw.z"),
                    new AnimationCurve(
                        new Keyframe(0, 0),
                        new Keyframe(duration * .3f, tilt),
                        new Keyframe(duration * .65f, -tilt * .5f),
                        new Keyframe(duration, 0)
                    )
                );
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve("Visual", typeof(Transform), "localEulerAnglesRaw.y"),
                    AnimationCurve.Constant(0, duration, 180)
                );
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = action == "Idle";
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, "Assets/_Game/Art/Ian" + action + ".anim");
                var state = sm.AddState(action);
                state.motion = clip;
                if (action == "Idle")
                {
                    sm.defaultState = state;
                }
                else
                {
                    var back = state.AddTransition(sm.defaultState);
                    back.hasExitTime = true;
                    back.exitTime = 1;
                    back.duration = .2f;
                }
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var presentation = actor.AddComponent<CharacterPresentation>();
            presentation.game = game;
            presentation.animator = animator;
            // Save a reusable prefab with no scene reference; the scene instance keeps its manager reference.
            presentation.game = null;
            PrefabUtility.SaveAsPrefabAsset(actor, "Assets/_Game/Prefabs/Characters/IanCharacter.prefab");
            presentation.game = game;
            cam.transform.position = new Vector3(3.6f, 2.85f, -7.3f);
            cam.transform.LookAt(new Vector3(0, 1.05f, .5f));
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 60;
            cam.allowHDR = true;
            var additional = cam.GetUniversalAdditionalCameraData();
            additional.renderPostProcessing = true;
            additional.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var sun = GameObject.Find("Directional Light").GetComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            sun.color = new Color(1, .87f, .68f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            Lamp(
                "Key Softbox",
                LightType.Spot,
                new Vector3(-2, 4, -3),
                new Color(1, .86f, .68f),
                35,
                12,
                new Vector3(0, 1, 0)
            );
            Lamp(
                "Cool Rim",
                LightType.Spot,
                new Vector3(2, 3, 2.5f),
                new Color(.48f, .75f, 1),
                24,
                9,
                new Vector3(0, 1, 0)
            );
            Lamp(
                "Practical Warm Lamp",
                LightType.Point,
                new Vector3(2.9f, 2, 2.2f),
                new Color(1, .64f, .28f),
                3,
                4,
                Vector3.zero
            );
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.26f, .3f, .32f);
            var volumeGo = new GameObject("Global Volume", typeof(Volume));
            var volume = volumeGo.GetComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, "Assets/_Game/Data/ApartmentVolume.asset");
            volume.sharedProfile = profile;
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(.18f);
            bloom.threshold.Override(1.1f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.2f);
            vignette.smoothness.Override(.4f);
            foreach (var c in profile.components)
            {
                AssetDatabase.AddObjectToAsset(c, profile);
            }
        }
    }
}
