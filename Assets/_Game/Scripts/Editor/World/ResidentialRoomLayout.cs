using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    public static partial class SceneRoomExport
    {
        [Serializable]
        sealed class ResidentialPlan
        {
            public int version;
            public RoomLayout[] rooms;
            public ObjectLayout[] objects;
        }

        [Serializable]
        sealed class RoomLayout
        {
            public string id;
            public Vector2 size;
            public Vector2 spawn;
            public float cameraSize;
        }

        [Serializable]
        sealed class ObjectLayout
        {
            public string id;
            public Vector2 position;
        }

        static ResidentialPlan residential;

        static void ReadResidentialPlan() =>
            residential = JsonUtility.FromJson<ResidentialPlan>(File.ReadAllText(Data + "FamilyRoomLayout.json"));

        static RoomLayout Layout(string id) => residential.rooms.Single(r => r.id == id);

        static float LivingDoorX(string id) =>
            id == "family-parents-door" ? -3.3f
            : id == "family-ian-door" ? -1.1f
            : id == "family-bath-front" ? 1.1f
            : 3.3f;

        static void ArrangeResidentialRoom(LifeWorldController w, RoomSceneDefinition room)
        {
            var z = Z(room.roomId);
            foreach (var item in residential.objects)
            {
                var o = w.objects.FirstOrDefault(o => o.id == item.id);
                if (o != null)
                {
                    o.transform.position = z.origin + item.position;
                }
            }

            var root = w.transform.Cast<Transform>().Single(t => t.name.EndsWith(" / " + room.roomId));
            if (room.roomId == "family-living")
            {
                var tv = root.Find("Family TV");
                tv.position = z.origin + new Vector2(-1, 1.3f);
                Decor(root, "Seating rug", "Rug", z.origin + new Vector2(-1, -1.5f), 3.2f, -4950);
            }

            if (room.roomId == "family-kitchen")
            {
                var dining = root.Find("One shared family dining table");
                dining.position = z.origin + new Vector2(0, -2.1f);
                for (int i = 0; i < 3; i++)
                {
                    var o = w.objects.Single(o =>
                        o.id
                        == (
                            i == 0 ? "family-dine-1"
                            : i == 1 ? "family-dine-2"
                            : "family-player-seat"
                        )
                    );
                    o.transform.position = (Vector2)dining.position + new Vector2(-2 + i * 2, .8f);
                }

                Decor(
                    root,
                    "Kitchen daylight window",
                    "WindowDay",
                    z.origin + new Vector2(-1.3f, z.size.y / 2 - .05f),
                    1.55f,
                    -3600
                );
            }

            if (room.roomId == "family-bathroom")
            {
                Decor(root, "Family laundry and towels", "Laundry", z.origin + new Vector2(-2, -1.55f), .75f, 1150);
                Decor(root, "Non-slip bathroom mat", "Rug", z.origin + new Vector2(.7f, -1.35f), 1.2f, -4900);
            }

            if (room.roomId == "family-ian-bedroom" || room.roomId == "family-parents-bedroom")
            {
                Decor(
                    root,
                    "Bedroom daylight window",
                    "WindowDay",
                    z.origin + new Vector2(-.4f, z.size.y / 2 - .05f),
                    1.7f,
                    -3600
                );
            }

            foreach (var o in w.objects)
            {
                if (o.kind == WorldInteractionKind.Door)
                {
                    continue;
                }

                o.transform.rotation = Quaternion.identity;
                var local = (Vector2)o.transform.position - z.origin;
                foreach (var npc in split.npcs)
                {
                    foreach (var e in npc.schedule.Where(e => e.objectId == o.id && e.zone == room.roomId))
                    {
                        e.position = (o.GetComponent<WorldActivitySlot>()?.Point ?? o.Point) - z.origin;
                    }

                    if (
                        npc.homeZone == room.roomId
                        && o.id == (npc.npcId == "family-mother" ? "family-mother-bed" : "family-father-bed")
                    )
                    {
                        npc.homePosition = o.Point - z.origin;
                    }
                }
            }

            AlignFamilySorting(w);
            foreach (var link in links.Where(l => l.sourceRoom == room.roomId))
            {
                var door = w.objects.Single(o => o.id == link.doorId);
                var text = UnityEngine.Object.Instantiate(labelTemplate, root);
                text.name = "Destination label / " + link.doorId;
                text.text =
                    link.destinationRoom == "family-kitchen" ? "주방"
                    : link.destinationRoom == "family-bathroom" ? "욕실"
                    : R(link.destinationRoom).title;
                text.fontSize = 2.1f;
                text.color = new Color32(255, 242, 210, 255);
                text.rectTransform.sizeDelta = new Vector2(2.1f, .6f);
                text.transform.position =
                    door.GetComponent<WorldDoor>().Plane
                    + Vector2.up * (room.roomId == "family-living" && link.doorId != "family-exit" ? 1.05f : .12f);
                text.gameObject.SetActive(false);
            }
        }

        public static void RepairRoomBindingsAndPivots()
        {
            var c = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(Data + "RoomScenes.asset");
            foreach (var room in c.rooms)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    Folder + room.sceneName + ".unity"
                );
                var binding = UnityEngine.Object.FindAnyObjectByType<GameplaySceneBindings>();
                if (binding == null)
                {
                    binding = new GameObject(
                        "Presentation Bindings",
                        typeof(GameplaySceneBindings)
                    ).GetComponent<GameplaySceneBindings>();
                }

                binding.hud = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
                binding.missionUI = UnityEngine.Object.FindAnyObjectByType<MissionUI>(FindObjectsInactive.Include);
                if (binding.hud == null || binding.missionUI == null)
                {
                    throw new Exception("Missing shared UI models " + room.roomId);
                }

                if (room.family)
                {
                    var w = UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();
                    AlignFamilySorting(w);
                    foreach (var sr in w.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if (
                            sr.name == "Room owned rear wallpaper"
                            || sr.name.EndsWith(" visible")
                            || sr.name.Contains(" cutaway")
                        )
                        {
                            var zone = c.rules.Zone(room.roomId);
                            Vector2 p;
                            if (sr.name == "Room owned rear wallpaper")
                            {
                                sr.size = new Vector2(zone.size.x, 2.3f);
                                p = zone.origin + new Vector2(0, zone.size.y / 2 + 1.05f);
                            }
                            else
                            {
                                var collider = w.GetComponentsInChildren<BoxCollider2D>()
                                    .Single(x => x.name == sr.name.Replace(" visible", "").Replace(" cutaway", ""));
                                p =
                                    (Vector2)collider.transform.position
                                    + (sr.name.EndsWith(" visible") ? Vector2.up * .2f : Vector2.zero);
                            }

                            sr.transform.position =
                                p
                                - new Vector2(
                                    (.5f - sr.sprite.pivot.x / sr.sprite.rect.width) * sr.size.x,
                                    (.5f - sr.sprite.pivot.y / sr.sprite.rect.height) * sr.size.y
                                );
                            if (sr.name.EndsWith(" visible"))
                            {
                                sr.drawMode = SpriteDrawMode.Sliced;
                            }
                        }
                    }

                    foreach (
                        var window in w.GetComponentsInChildren<SpriteRenderer>()
                            .Where(s => s.name == "Bedroom daylight window" || s.name == "Kitchen daylight window")
                    )
                    {
                        var zone = c.rules.Zone(room.roomId);
                        window.transform.position =
                            zone.origin
                            + new Vector2(window.name.StartsWith("Bedroom") ? -.4f : -1.3f, zone.size.y / 2 - .05f);
                        AttachWindow(w, window);
                    }

                    foreach (
                        var label in w.GetComponentsInChildren<TMPro.TMP_Text>()
                            .Where(t => t.name.StartsWith("Destination label / "))
                    )
                    {
                        label.gameObject.SetActive(false);
                        label.color = new Color32(255, 242, 210, 255);
                        var door = w
                            .objects.Single(o => o.id == label.name.Substring("Destination label / ".Length))
                            .GetComponent<WorldDoor>();
                        label.transform.position =
                            door.Plane
                            + Vector2.up
                                * (room.roomId == "family-living" && door.portal.id != "family-exit" ? 1.05f : .12f);
                        if (label.text == "주방 · 가족 식사")
                        {
                            label.text = "주방";
                        }

                        if (label.text == "가족 욕실")
                        {
                            label.text = "욕실";
                        }
                    }
                }

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
        }

        static void AlignFamilySorting(LifeWorldController w)
        {
            foreach (var o in w.objects.Where(o => o.kind != WorldInteractionKind.Door && o.visual != null))
            {
                o.visual.sortingOrder = 1000 - Mathf.RoundToInt(o.transform.position.y * 100);
                if (o.highlight != null)
                {
                    o.highlight.sortingOrder = o.visual.sortingOrder - 1;
                }

                if (o.shadow != null)
                {
                    o.shadow.sortingOrder = o.visual.sortingOrder - 1;
                }
            }

            var shared = w.GetComponentInChildren<FamilySharedDiningPresentation>();
            if (shared != null)
            {
                shared.visual.sortingOrder = 1000 - Mathf.RoundToInt(shared.transform.position.y * 100);
            }

            var tv = w.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(s => s.name == "Family TV");
            if (tv != null)
            {
                tv.sortingOrder = 1000 - Mathf.RoundToInt(tv.transform.position.y * 100);
            }
        }

        static void AttachWindow(LifeWorldController w, SpriteRenderer window)
        {
            var b = window.GetComponent<WorldRoomBackdrop>();
            if (b == null)
            {
                b = window.gameObject.AddComponent<WorldRoomBackdrop>();
            }

            b.floor = window;
            b.day = window.sprite;
            b.night = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Architecture.png")
                .OfType<Sprite>()
                .Single(s => s.name == "WindowNight");
            if (!w.roomBackdrops.Contains(b))
            {
                w.roomBackdrops = w.roomBackdrops.Concat(new[] { b }).ToArray();
            }
        }

        static void Decor(Transform root, string title, string spriteName, Vector2 p, float width, int order)
        {
            var sprite = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Architecture.png")
                .OfType<Sprite>()
                .Single(s => s.name == spriteName);
            var go = new GameObject(title, typeof(SpriteRenderer));
            go.transform.SetParent(root, false);
            go.transform.position = p;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = Material;
            sr.sortingOrder = order;
            go.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
            if (spriteName == "Laundry")
            {
                var basket = go.AddComponent<BoxCollider2D>();
                float scale = go.transform.localScale.x;
                basket.size = new Vector2(.53f, .23f) / scale;
                basket.offset = Vector2.up * (.115f / scale);
            }

            if (spriteName == "WindowDay")
            {
                AttachWindow(root.GetComponentInParent<LifeWorldController>(), sr);
            }
        }
    }
}
