using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static partial class WorldLayoutAuthoring
    {
        const string SofaPath = "Assets/_Game/Art/Interiors/FamilySofaNorth.png";
        const string Furniture = "Assets/_Game/Art/VisualRebuild/Furniture.png";
        const string Architecture = "Assets/_Game/Art/VisualRebuild/Architecture.png";

        [MenuItem("Ian/World/Apply room layout corrections")]
        public static void Apply()
        {
            if (
                EditorApplication.isPlaying
                || Enumerable
                    .Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                    .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
            )
            {
                throw new InvalidOperationException("Save the active scene before applying room layouts.");
            }

            ResidentActivityArtSetup.Apply();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes/Rooms" }))
                {
                    var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
                    var world = scene
                        .GetRootGameObjects()
                        .SelectMany(r => r.GetComponentsInChildren<LifeWorldController>())
                        .Single();
                    if (ApplyRoom(world))
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            ResidentRoutineAuthoring.ApplyRoomRules();
        }

        static Sprite SpriteFrom(string atlas, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(atlas).OfType<Sprite>().Single(s => s.name == name);

        static Transform Named(LifeWorldController world, string name) =>
            world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        static WorldInteractable Object(LifeWorldController world, string id) =>
            world.objects.First(o => o != null && o.id == id);

        static void Place(LifeWorldController world, Transform item, float x, float y)
        {
            if (!item)
            {
                return;
            }

            var origin = world.Room.catalog.rules.Zone(world.Room.roomId).origin;
            var position = origin + new Vector2(x, y);
            int orderDelta = Mathf.RoundToInt((item.position.y - position.y) * 100);
            item.position = new Vector3(position.x, position.y, item.position.z);
            foreach (var sr in item.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sortingOrder > 0 && sr.sortingOrder < 4000)
                {
                    sr.sortingOrder += orderDelta;
                }
            }
        }

        static Transform Anchor(Transform item, string name, Vector2 offset)
        {
            var child = item.Find(name);
            if (!child)
            {
                child = new GameObject(name).transform;
                child.SetParent(item, false);
            }

            child.localPosition = offset;
            return child;
        }

        static void SofaSeat(LifeWorldController world, string id, float side)
        {
            var slot = Object(world, id).GetComponent<WorldActivitySlot>();
            var approach = new Vector2(side * 1.4f, -.33f);
            slot.useSeatAnchors = true;
            slot.seated = true;
            slot.seatAnchor = Anchor(slot.transform, "Seat", slot.transform.InverseTransformVector(Vector2.up * .28f));
            slot.approachAnchor = Anchor(slot.transform, "Approach", approach);
            slot.standUpAnchor = slot.approachAnchor;
            slot.facing = Vector2.up;
            slot.activityMotion = ActorMotion.SeatedIdle;
        }

        static void Furnish(
            LifeWorldController world,
            string name,
            Sprite sprite,
            Vector2 point,
            float width,
            Vector2 footprint
        )
        {
            var group = world.transform.Find("Interior fixtures");
            if (!group)
            {
                group = new GameObject("Interior fixtures").transform;
                group.SetParent(world.transform, false);
            }

            var root = Anchor(group, name, Vector2.zero);
            Place(world, root, point.x, point.y);
            var visual = Anchor(root, "Artwork", Vector2.zero);
            if (!visual.TryGetComponent<SpriteRenderer>(out var renderer))
            {
                renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }
            renderer.sprite = sprite;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Game/Art/VisualRebuild/WorldAlphaCut.mat"
            );
            visual.localScale = Vector3.one * (width / sprite.bounds.size.x);
            renderer.sortingOrder = 1000 - Mathf.RoundToInt(root.position.y * 100);
            if (footprint != Vector2.zero)
            {
                if (!root.TryGetComponent<BoxCollider2D>(out var collider))
                {
                    collider = root.gameObject.AddComponent<BoxCollider2D>();
                }
                collider.size = footprint;
                collider.offset = Vector2.up * (footprint.y / 2);
            }
        }

        public static bool ApplyRoom(LifeWorldController world)
        {
            switch (world.Room.roomId)
            {
                case "family-living":
                    var couch = Object(world, "family-sofa").visual;
                    couch.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SofaPath);
                    if (!couch.sprite)
                    {
                        throw new InvalidOperationException("Missing family sofa sprite.");
                    }

                    couch.transform.localScale = Vector3.one * (3f / couch.sprite.bounds.size.x);
                    SofaSeat(world, "family-tv-mother", -1);
                    SofaSeat(world, "family-tv-father", 1);
                    break;

                case "family-bathroom":
                    Place(world, Object(world, "family-toilet").transform, -1.25f, .12f);
                    Place(world, Named(world, "Contact / family-toilet"), -1.25f, .095f);
                    Place(world, Object(world, "family-basin").transform, 0, .4f);
                    Place(world, Named(world, "Contact / family-basin"), 0, .375f);
                    break;

                case "bathroom":
                    Place(world, Object(world, "toilet").transform, -1.8f, .4f);
                    Place(world, Object(world, "sink").transform, 0, .4f);
                    break;

                case "alley":
                    Phase33Setup.ReplaceFacade(world, "family-front", "BLD_Apartment_01");
                    Place(world, Named(world, "ENV_City_UtilityBox_01_5.5_2.8"), -1f, 4.75f);
                    Place(world, Named(world, "ENV_City_Lamp_01_-10.5_-1"), 8.5f, -.85f);
                    Place(world, Named(world, "ENV_City_Bench_01_4_-0.2"), 4.5f, 1.2f);
                    break;

                case "city":
                    Place(world, Named(world, "ENV_City_BicycleRack_01_-11_2.2"), -6f, 1.25f);
                    Place(world, Named(world, "ENV_City_Vending_01_11_2.2"), 9.6f, 1.2f);
                    Place(world, Named(world, "Utility pole"), -4.3f, -.9f);
                    Place(world, Named(world, "Street planter 0"), -14.2f, 1.65f);
                    Place(world, Named(world, "Street planter 3"), 14.35f, 1.65f);
                    Place(world, Named(world, "ENV_City_Planter_01_2.4_-9.1"), -3.6f, -9.1f);
                    Place(world, Named(world, "ENV_City_Planter_01_6.6_-9.1"), 8.0f, -9.1f);
                    break;

                case "school":
                    Place(world, Object(world, "school-water").transform, -7.3f, .35f);
                    Place(world, Named(world, "Classroom window"), -4.6f, 4.85f);
                    Place(world, Named(world, "Class clock and hooks"), 3.8f, 4.55f);
                    Place(world, Named(world, "School noticeboard"), 6.4f, 4.45f);
                    break;

                case "office-bathroom":
                    Furnish(
                        world,
                        "Toilet privacy screen",
                        SpriteFrom(Architecture, "VerticalWall"),
                        new Vector2(.55f, -.8f),
                        .315f,
                        new Vector2(.24f, 1.75f)
                    );
                    Furnish(
                        world,
                        "Basin supplies",
                        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/World/Interiors/ToiletryShelf.asset"),
                        new Vector2(-1.7f, 2.85f),
                        1.03f,
                        Vector2.zero
                    );
                    break;

                case "finance":
                case "realestate":
                    Place(world, Object(world, world.Room.roomId + "-waiting").transform, -1.95f, -2.45f);
                    break;

                case "residence":
                    Furnish(
                        world,
                        "Lockers by west entrance aisle",
                        SpriteFrom(Furniture, "Wardrobe"),
                        new Vector2(-5f, -5.35f),
                        1.35f,
                        new Vector2(1.1f, .36f)
                    );
                    Furnish(
                        world,
                        "Lockers by east entrance aisle",
                        SpriteFrom(Furniture, "Wardrobe"),
                        new Vector2(4.6f, -5.35f),
                        1.35f,
                        new Vector2(1.1f, .36f)
                    );
                    break;

                default:
                    return ActivityLayout(world);
            }

            ActivityLayout(world);
            return true;
        }
    }
}
