using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    public static partial class WorldQualityRecoverySetup
    {
        static FamilyHomeArchitecturePlan familyPlan;

        static Sprite[] SharedFamilyAtlas()
        {
            string path = "Assets/_Game/Art/FamilyLife/SharedDining.png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null)
            {
                throw new Exception("Shared family table art is required");
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            ti.spritePixelsPerUnit = w / 4f / 6.8f;
            ti.maxTextureSize = 4096;
            int[] top =  { 17, 115, 235, 355, 479, 579, 681, 785 },
                bottom =  { 112, 242, 363, 479, 583, 687, 790, 885 };
            ti.spritesheet = Enumerable
                .Range(0, 32)
                .Select(i => new SpriteMetaData
                {
                    name = "SharedDining_" + i.ToString("D2"),
                    rect = new Rect(i % 4 * w / 4f, h - bottom[i / 4], w / 4f, bottom[i / 4] - top[i / 4]),
                    alignment = 9,
                    pivot = new Vector2(.5f, 0),
                })
                .ToArray();
            ti.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        static void FamilyWall(Transform root, string name, Vector2 p, Vector2 size)
        {
            var sr = Image(root, name, art["IvoryWall"], p, size);
            sr.enabled = true;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingOrder = 1000 - Mathf.RoundToInt((rules.Zone(FamilyLife.Home).origin + p).y * 100);
            Boundary(
                root,
                name + " collision",
                p,
                new Vector2(size.x > size.y ? size.x : .16f, size.x > size.y ? .16f : size.y)
            );
        }

        static void MoveFamily(string id, Vector2 p)
        {
            var o = Find(id);
            if (o != null)
            {
                o.transform.position = rules.Zone(FamilyLife.Home).origin + p;
            }
        }

        static void InternalFamilyDoor(string id, string title, Vector2 plane)
        {
            var o = Door(id, title, FamilyLife.Home, plane + Vector2.up * .45f, FamilyLife.Home, Vector2.zero);
            o.UsePoint = Vector2.down * .8f;
            AdvancedDoorSetup.Attach(world, o);
            o.GetComponent<WorldDoor>().opens = 0;
            o.GetComponent<WorldDoor>().closes = 24;
        }

        static void FamilyArchitecture()
        {
            string path = "Assets/_Game/Data/World/Shared/FamilyHomeArchitecture.asset";
            familyPlan = AssetDatabase.LoadAssetAtPath<FamilyHomeArchitecturePlan>(path);
            if (familyPlan == null)
            {
                familyPlan = ScriptableObject.CreateInstance<FamilyHomeArchitecturePlan>();
                AssetDatabase.CreateAsset(familyPlan, path);
            }

            if (familyPlan.version == 2)
            {
                familyPlan.version = 3;
                familyPlan.ianBed = new Vector2(0, 4.2f);
                familyPlan.motherBed = new Vector2(-9, 5.2f);
                familyPlan.fatherBed = new Vector2(-6, 5.2f);
            }

            var z = rules.Zone(FamilyLife.Home);
            var root = Room(FamilyLife.Home, "부모님 집", z.origin, familyPlan.size);
            z.cameraSize = familyPlan.playCameraSize;
            z.location = SceneId.Home;
            z.spawn = familyPlan.ianBed + Vector2.down * .75f;
            // This replaces the original single-room authoring, not a runtime patch.
            var oldDivider = root.Find("Sleeping area cutaway");
            if (oldDivider != null)
            {
                oldDivider.gameObject.SetActive(false);
            }

            oldDivider = root.Find("Sleeping area boundary");
            if (oldDivider != null)
            {
                oldDivider.gameObject.SetActive(false);
            }

            foreach (var room in familyPlan.rooms)
            {
                var floor = Image(
                    root,
                    "Room floor / " + room.id,
                    art[room.id == "bath" ? "JadeFloor" : "OakFloor"],
                    room.floor.center,
                    room.floor.size
                );
                floor.enabled = true;
                floor.sortingOrder = -4900;
                floor.color =
                    room.id == "bath" ? new Color(.8f, .88f, .92f)
                    : room.id == "ian" ? new Color(.95f, .91f, .85f)
                    : room.id == "parents" ? new Color(.88f, .82f, .74f)
                    : Color.white;
                Caption(
                    root,
                    "Room title / " + room.id,
                    room.title,
                    room.floor.center + new Vector2(0, room.floor.height / 2 - .65f),
                    room.floor.width - 1
                );
            }

            // Bedrooms and bathroom have actual rear/side/front walls with door apertures.
            FamilyWall(root, "Parents Ian partition", new Vector2(-4, 4.4f), new Vector2(.3f, 6.8f));
            FamilyWall(root, "Ian bathroom partition", new Vector2(4, 4.4f), new Vector2(.3f, 6.8f));
            foreach (
                var entry in new[]
                {
                    new { Id = "parents", X = -8f },
                    new { Id = "ian", X = 0f },
                    new { Id = "bath", X = 8f },
                }
            )
            {
                FamilyWall(root, entry.Id + " front left", new Vector2(entry.X - 2.45f, 1), new Vector2(3.1f, .75f));
                FamilyWall(root, entry.Id + " front right", new Vector2(entry.X + 2.45f, 1), new Vector2(3.1f, .75f));
            }

            FamilyWall(root, "Living kitchen north divider", new Vector2(0, -1.05f), new Vector2(.3f, .9f));
            FamilyWall(root, "Living kitchen south divider", new Vector2(0, -7.2f), new Vector2(.3f, 3.6f));
            InternalFamilyDoor("family-parents-door", "부모님 침실 출입", familyPlan.parentsDoor);
            InternalFamilyDoor("family-ian-door", "이안의 방 출입", familyPlan.ianDoor);
            InternalFamilyDoor("family-bath-front", "가족 욕실 출입", familyPlan.bathDoor);
            var oldBath = world
                .transform.Cast<Transform>()
                .FirstOrDefault(t => t.name.EndsWith(" / " + FamilyLife.Bath));
            if (oldBath != null)
            {
                oldBath.gameObject.SetActive(false);
            }

            var oldExit = Find("family-bath-exit");
            if (oldExit != null)
            {
                oldExit.gameObject.SetActive(false);
            }

            MoveFamily("family-mother-bed", familyPlan.motherBed);
            MoveFamily("family-father-bed", familyPlan.fatherBed);
            MoveFamily("family-bed", familyPlan.ianBed);
            MoveFamily("family-fridge", familyPlan.fridge);
            MoveFamily("family-cook", familyPlan.cook);
            MoveFamily("family-dishes", familyPlan.sink);
            MoveFamily("family-sofa", familyPlan.sofa);
            MoveFamily("family-closet", new Vector2(-11, 5));
            var tv = root.Find("Family TV");
            if (tv != null)
            {
                tv.gameObject.SetActive(true);
                tv.localPosition = familyPlan.tv;
                tv.GetComponent<SpriteRenderer>().enabled = true;
            }

            foreach (
                var entry in new[]
                {
                    new { Id = "family-basin", P = new Vector2(5.6f, 5.4f) },
                    new { Id = "family-shower", P = new Vector2(10.5f, 5.4f) },
                    new { Id = "family-toilet", P = new Vector2(8, 5.4f) },
                }
            )
            {
                var o = Find(entry.Id);
                o.zone = FamilyLife.Home;
                o.transform.position = z.origin + entry.P;
            }

            MoveFamily("family-care", familyPlan.ianBed + new Vector2(.8f, -1));
            MoveFamily("family-serve", new Vector2(6.5f, -3.5f));
            MoveFamily("family-clean", new Vector2(-3, -4));
            MoveFamily("family-tv-mother", familyPlan.sofa + new Vector2(-.6f, .25f));
            MoveFamily("family-tv-father", familyPlan.sofa + new Vector2(.6f, .25f));
            var furnishings = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Furniture.png")
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
            FamilyProp(
                "family-parent-side",
                "침실 생활용품",
                new Vector2(-11, 2.8f),
                furnishings["Nightstand"],
                WorldInteractionKind.Wardrobe,
                1.2f
            );
            FamilyProp(
                "family-nursery-storage",
                "이안의 책과 생활용품",
                new Vector2(2.7f, 5.1f),
                furnishings["Bookcase"],
                WorldInteractionKind.Wardrobe,
                1.5f
            );
            FamilyProp(
                "family-nursery-side",
                "아기 돌봄 용품",
                new Vector2(-2.6f, 4.3f),
                furnishings["Nightstand"],
                WorldInteractionKind.Wardrobe,
                1.2f
            );
            FamilyProp(
                "family-coffee-table",
                "거실에서 차 마시기",
                new Vector2(-8, -4),
                furnishings["DiningTable"],
                WorldInteractionKind.Drink,
                2
            );
            var oldCaption = root.Find("Family home title");
            if (oldCaption != null)
            {
                oldCaption.gameObject.SetActive(false);
            }

            var sprites = SharedFamilyAtlas();
            var dining = Child(root, "One shared family dining table");
            dining.localPosition = familyPlan.dining;
            var shared = Component<FamilySharedDiningPresentation>(dining.gameObject);
            shared.world = world;
            shared.frames = sprites;
            shared.visual = Image(dining, "Shared dining states", sprites[0], Vector2.zero);
            shared.visual.enabled = true;
            shared.visual.sortingOrder = 1000 - Mathf.RoundToInt(dining.position.y * 100);
            var tableCollider = Component<BoxCollider2D>(dining.gameObject);
            tableCollider.offset = new Vector2(0, .25f);
            tableCollider.size = new Vector2(5.7f, .3f);
            for (int i = 0; i < 3; i++)
            {
                var o = Find(
                    i == 0 ? "family-dine-1"
                    : i == 1 ? "family-dine-2"
                    : "family-player-seat"
                );
                o.transform.position = z.origin + familyPlan.dining + new Vector2(-2 + i * 2, .8f);
                o.UsePoint = new Vector2(.75f, .65f);
                o.visual.enabled = false;
                o.visual.sprite = null;
                foreach (var col in o.GetComponents<Collider2D>())
                {
                    col.enabled = false;
                }

                var old = o.GetComponent<WorldFacilityActivityPresentation>();
                if (old != null)
                {
                    if (old.occupied != null)
                    {
                        old.occupied.enabled = false;
                    }

                    UnityEngine.Object.DestroyImmediate(old);
                }

                var slot = o.GetComponent<WorldActivitySlot>();
                FacilityAnchors(
                    slot,
                    Vector2.zero,
                    new Vector2(.75f, .65f),
                    new Vector2(.75f, .65f),
                    true,
                    ActorMotion.Eat,
                    Vector2.down
                );
                if (i == 0)
                {
                    shared.mother = slot;
                }
                else if (i == 1)
                {
                    shared.father = slot;
                }
                else
                {
                    shared.player = slot;
                    var a = FamilyAction(o, LifeActionVisual.Dining, o.UsePoint, Vector2.zero);
                    a.frames = Array.Empty<Sprite>();
                    a.overlay.enabled = false;
                    a.chairSprite = null;
                    a.mealProp = null;
                    a.emptyMealProp = null;
                    a.tableOcclusion = false;
                    a.seat = slot;
                    a.facing = Vector2.down;
                }
            }

            var exit = Find("family-exit");
            exit.transform.position = z.origin + new Vector2(-2, -9.45f);
            exit.UsePoint = Vector2.up * .8f;
            AdvancedDoorSetup.Attach(world, exit);
            Find("family-front").arrival = new Vector2(-2, -7.8f);
            Boundary(root, "Front left", new Vector2(-7.45f, -9), new Vector2(9.1f, .2f));
            Boundary(root, "Front right", new Vector2(5.45f, -9), new Vector2(13.1f, .2f));
            // Update all authored destinations after final furniture/room placement.
            foreach (var npc in rules.npcs.Where(n => FamilyLife.Parent(n.npcId)))
            {
                npc.hasHomePosition = true;
                npc.homePosition =
                    Find(npc.npcId == "family-mother" ? "family-mother-bed" : "family-father-bed").Point - z.origin;
            }

            foreach (var npc in rules.npcs.Where(n => FamilyLife.Parent(n.npcId)))
            {
                foreach (var schedule in npc.schedule.Where(e => e.zone == FamilyLife.Home))
                {
                    var target = Find(schedule.objectId);
                    if (target != null)
                    {
                        schedule.position =
                            (target.GetComponent<WorldActivitySlot>()?.Point ?? target.Point) - z.origin;
                    }
                }
            }

            File.WriteAllText("Docs/FamilyLife/FLOOR_PLAN.json", JsonUtility.ToJson(familyPlan, true));
            EditorUtility.SetDirty(familyPlan);
            Physics2D.SyncTransforms();
        }
    }
}
