using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IanGame.Editor
{
    public static class Phase26InputSetup
    {
        public const string ControlsPath = "Assets/_Game/Data/Phase26/WorldControls.inputactions";

        public static InputActionAsset EnsureControls()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "WorldControls";
            var map = asset.AddActionMap("World");
            var move = map.AddAction("Move", InputActionType.Value);
            move.expectedControlType = "Vector2";
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick").WithProcessor("stickDeadzone");
            map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e").AddBinding("<Gamepad>/buttonSouth");
            map.AddAction("Inventory", InputActionType.Button, "<Keyboard>/i");
            // Esc and gamepad East have one owner: UIShell.Back. Tab remains UI focus navigation.
            map.AddAction("Menu", InputActionType.Button, "<Keyboard>/m");
            Directory.CreateDirectory(Path.GetDirectoryName(ControlsPath));
            File.WriteAllText(ControlsPath, asset.ToJson());
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
        }

        [MenuItem("Ian/Life World/Repair Gameplay Input References")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play before applying scene input references.");
            }

            var asset = EnsureControls();
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
            var motor = UnityEngine.Object.FindAnyObjectByType<WorldPlayerMotor>();
            if (motor == null)
            {
                throw new Exception("Existing WorldPlayerMotor is missing");
            }

            motor.controls = asset;
            EditorUtility.SetDirty(motor);
            foreach (
                var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            )
            {
                if (t.name == "World controls")
                {
                    t.text = "WASD / 방향키 · E 사용 · I 가방 · Esc 메뉴";
                    EditorUtility.SetDirty(t);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
            AssetDatabase.SaveAssets();
        }
    }
}
