using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static class PixelFontSystemBuilder
    {
        public static TMP_FontAsset Body;
        public static TMP_FontAsset Heading => Body;
        public static TMP_FontAsset Title => Body;
        public static TMP_FontAsset Numbers => Body;

        public static void Load()
        {
            Body = StardustTypography.Main;
        }

        public static void Build()
        {
            StardustTypography.Build();
            Complete();
        }

        public static void Complete()
        {
            Load();
            var theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");
            theme.font = Body;
            theme.fontLicense = AssetDatabase.LoadAssetAtPath<TextAsset>(StardustTypography.LicensePath);
            EditorUtility.SetDirty(theme);
            foreach (
                var path in AssetDatabase
                    .FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs" })
                    .Select(AssetDatabase.GUIDToAssetPath)
            )
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        Style(text);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            foreach (string name in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var scene = EditorSceneManager.OpenScene(name);
                foreach (
                    var text in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true))
                )
                {
                    Style(text);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
        }

        public static void Style(TMP_Text text)
        {
            if (!Body)
            {
                Load();
            }

            text.font = Body;
            text.fontSharedMaterial = Body.material;
            EditorUtility.SetDirty(text);
        }
    }
}
