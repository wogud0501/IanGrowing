using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static class Phase32Setup
    {
        [MenuItem("IanGrowing/Phase 3.2/Apply existing scene UX bindings")]
        public static void ApplyMenu()
        {
            Debug.Log(Apply());
        }

        public static string Apply()
        {
            if (EditorApplication.isPlaying)
            {
                throw new System.InvalidOperationException("Stop Play Mode first");
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            if (
                Enumerable
                    .Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                    .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
            )
            {
                throw new System.InvalidOperationException("Save existing scene changes first");
            }

            int scenes = 0;
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes" }))
                {
                    var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
                    bool changed = false;
                    foreach (
                        var hud in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WorldHUD>(true))
                    )
                    {
                        if (hud.GetComponent<CollectionUI>() == null || hud.GetComponent<CityLifeHUD>() == null)
                        {
                            continue;
                        }

                        var ux = hud.GetComponent<GameplayUXPresenter>();
                        if (ux == null)
                        {
                            ux = hud.gameObject.AddComponent<GameplayUXPresenter>();
                            changed = true;
                        }

                        var activities = scene
                            .GetRootGameObjects()
                            .SelectMany(r => r.GetComponentsInChildren<Phase23Presenter>(true))
                            .FirstOrDefault();
                        if (ux.activities != activities)
                        {
                            ux.activities = activities;
                            EditorUtility.SetDirty(ux);
                            changed = true;
                        }
                    }

                    if (!changed)
                    {
                        continue;
                    }

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    scenes++;
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            return "Updated existing scenes: " + scenes;
        }
    }
}
