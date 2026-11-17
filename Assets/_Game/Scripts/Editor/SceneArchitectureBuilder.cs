using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static class SceneArchitectureBuilder
    {
        public const string Source = "Assets/_Game/Scenes/LifeWorld.unity";
        public const string Data = "Assets/_Game/Data/SceneArchitecture/";
        public const string Scenes = "Assets/_Game/Scenes/Rooms/";

        [MenuItem("Ian/Scene Architecture/Audit Source")]
        public static void Audit()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play");
            }

            EditorSceneManager.OpenScene(Source);
            var w = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
            Directory.CreateDirectory("Docs/SceneArchitecture");
            File.WriteAllText(
                "Docs/SceneArchitecture/SOURCE_HIERARCHY.txt",
                string.Join("\n", w.gameObject.scene.GetRootGameObjects().Select(o => "ROOT " + o.name))
                    + "\n"
                    + string.Join(
                        "\n",
                        w.transform.Cast<Transform>()
                            .Select(t =>
                                t.name
                                + " pos="
                                + t.position
                                + " children="
                                + t.childCount
                                + " types="
                                + string.Join(
                                    ",",
                                    t.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().Name)
                                )
                            )
                    )
            );
        }
    }
}
