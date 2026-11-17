#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IanGame.Editor
{
    public static class Phase28Setup
    {
        [MenuItem("Ian/Phase 2.8/Bind city UX to existing rooms")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(
                "Assets/_Game/Data/SceneArchitecture/RoomScenes.asset"
            );
            var cityScene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/City_Center.unity");
            var cityWorld = UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();
            string[] ids = { "alley", "workplace", "school", "shop", "finance", "realestate" };
            var points = ids.Select(id =>
                {
                    var edge = catalog.portals.First(p => p.sourceRoom == "city" && p.destinationRoom == id);
                    return cityWorld.objects.First(o => o.id == edge.doorId).Point - catalog.rules.Zone("city").origin;
                })
                .ToArray();
            var rules = catalog.rules;
            foreach (var d in rules.npcs)
            {
                if (
                    FamilyLife.Parent(d.npcId)
                    || d.personality == NPCPersonality.NightWorker
                    || d.homeZone == "city"
                    || d.schedule.Length == 0
                )
                {
                    continue;
                }

                var at = d.schedule[WorldNPCScheduler.Schedule(d, 4, 23 * 60 + 30)];
                if (at.zone != "city")
                {
                    continue;
                }

                var home = d.schedule.FirstOrDefault(s => s.zone == d.homeZone);
                if (home == null)
                {
                    continue;
                }

                d.schedule = d
                    .schedule.Concat(
                        new[]
                        {
                            new NPCScheduleEntry
                            {
                                hour = 23,
                                minute = 0,
                                zone = home.zone,
                                objectId = home.objectId,
                                position = home.position,
                                action = home.action,
                                dayMask = 127,
                            },
                        }
                    )
                    .OrderBy(s => s.hour * 60 + s.minute)
                    .ToArray();
            }

            EditorUtility.SetDirty(rules);
            foreach (var room in catalog.rooms)
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/" + room.sceneName + ".unity");
                var w = UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();
                var ui = w.hud.GetComponent<CityLifeHUD>();
                if (ui == null)
                {
                    ui = w.hud.gameObject.AddComponent<CityLifeHUD>();
                }

                ui.world = w;
                ui.cityDoorPoints = points;
                ui.playerIcon = Icon("Arrow");
                ui.homeIcon = Icon("Home");
                ui.workIcon = Icon("Work");
                ui.schoolIcon = Icon("Book");
                ui.shopIcon = Icon("Meal");
                ui.financeIcon = Icon("Coin");
                ui.propertyIcon = Icon("Home");
                ui.parkIcon = Icon("Life");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Docs/Phase28");
            File.WriteAllText(
                "Docs/Phase28/SCENE_BINDINGS.txt",
                string.Join("\n", catalog.rooms.Select(r => r.roomId + " → " + r.sceneName))
                    + "\nNight schedules use existing home entries; no NPC data deleted.\n"
            );
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/City_Center.unity");
        }

        static Sprite Icon(string id)
        {
            string folder = "Assets/_Game/Data/Phase28";
            Directory.CreateDirectory(folder);
            string path = folder + "/Map_" + id + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (saved != null)
            {
                return saved;
            }

            string source = "Assets/_Game/UI/Pixel/Icon-" + id + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(source);
            bool read = importer.isReadable;
            if (!read)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(source);
            var pixels = texture.GetPixels32();
            int left = texture.width,
                right = 0,
                bottom = texture.height,
                top = 0;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[y * texture.width + x].a > 160)
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                        bottom = Math.Min(bottom, y);
                        top = Math.Max(top, y);
                    }
                }
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(left, bottom, right - left + 1, top - bottom + 1),
                new Vector2(.5f, .5f),
                16,
                0,
                SpriteMeshType.FullRect
            );
            sprite.name = "Map_" + id;
            AssetDatabase.CreateAsset(sprite, path);
            if (!read)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            return sprite;
        }
    }
}
#endif
