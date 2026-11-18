using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace IanGame.Editor
{
    // Authoring only: retains room IDs, entrance components, schedules, saves and existing controllers.
    public static class Phase33Setup
    {
        const string Art = "Assets/_Game/Art/WorldLife/";
        const string Data = "Assets/_Game/Data/World/LivingCity/";
        const string Props = "Assets/_Game/Prefabs/World/LivingCity/";
        const string Vehicles = "Assets/_Game/Prefabs/World/Vehicles/";
        const string Residents = "Assets/_Game/Prefabs/NPC/Ambient/";
        static Material Material =>
            AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/VisualRebuild/WorldAlphaCut.mat");

        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");

        [MenuItem("IanGrowing/Phase 3.3/Apply living city slice")]
        public static void Menu()
        {
            Debug.Log(Apply());
        }

        static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static WorldLifeProfile Profile()
        {
            string path = Data + "LivingCity.asset";
            var profile = AssetDatabase.LoadAssetAtPath<WorldLifeProfile>(path);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<WorldLifeProfile>();
            int[] hours = { 0, 6, 8, 10, 12, 14, 17, 20, 23 };
            int[] crowd = { 1, 3, 8, 4, 6, 4, 8, 4, 2 };
            int[] traffic = { 2, 3, 6, 4, 5, 4, 6, 4, 2 };
            profile.zones = new[]
            {
                new ZoneLifeRule
                {
                    zoneId = "city",
                    identity = "Commercial / commute / consumption",
                    ambientCapacity = 8,
                    bands = hours
                        .Select(
                            (hour, i) =>
                                new PopulationBand
                                {
                                    fromHour = hour,
                                    ambientResidents = crowd[i],
                                    vehicles = traffic[i],
                                }
                        )
                        .ToArray(),
                },
                new ZoneLifeRule
                {
                    zoneId = "alley",
                    identity = "Residential / quiet routines",
                    ambientCapacity = 4,
                    bands = hours
                        .Select(
                            (hour, i) =>
                                new PopulationBand
                                {
                                    fromHour = hour,
                                    ambientResidents = Mathf.Min(4, crowd[i]),
                                    vehicles = 0,
                                }
                        )
                        .ToArray(),
                },
                new ZoneLifeRule
                {
                    zoneId = "shop",
                    identity = "Convenience / meals between shifts",
                    ambientCapacity = 1,
                    bands = new[]
                    {
                        new PopulationBand { fromHour = 0 },
                        new PopulationBand { fromHour = 7, ambientResidents = 1 },
                        new PopulationBand { fromHour = 23 },
                    },
                },
            };
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        static void ImportSprites()
        {
            foreach (string path in Directory.GetFiles(Art, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = 40;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(.5f, .05f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                AssetDatabase.SetLabels(
                    AssetDatabase.LoadAssetAtPath<Sprite>(path),
                    new[]
                    {
                        "IanGrowing",
                        Path.GetFileName(path).StartsWith("VFX_") ? "AuthoredPixelVFX" : "AI-Assisted",
                        "Phase33-Reviewed",
                    }
                );
            }
        }

        static GameObject PropPrefab(string name)
        {
            string path = Props + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Sprite(name);
            sr.sharedMaterial = Material;
            var collider = go.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(Mathf.Max(.2f, sr.sprite.bounds.size.x * .78f), .28f);
            collider.offset = Vector2.up * .12f;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject VehiclePrefab(string name)
        {
            string path = Vehicles + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(Rigidbody2D), typeof(BoxCollider2D));
            var body = go.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.simulated = false;
            var visual = new GameObject("Body", typeof(SpriteRenderer));
            visual.transform.SetParent(go.transform, false);
            var sr = visual.GetComponent<SpriteRenderer>();
            sr.sprite = Sprite(name);
            sr.sharedMaterial = Material;
            var collider = go.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(sr.sprite.bounds.size.x * .82f, .55f);
            collider.offset = Vector2.up * .2f;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        static void Atlas()
        {
            const string path = Art + "LivingCity.spriteatlasv2";
            var atlas = new SpriteAtlasAsset();
            atlas.Add(
                Directory
                    .GetFiles(Art, "*.png")
                    .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p))
                    .Cast<UnityEngine.Object>()
                    .ToArray()
            );
            SpriteAtlasAsset.Save(atlas, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            importer.includeInBuild = true;
            var packing = importer.packingSettings;
            packing.padding = 4;
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            importer.packingSettings = packing;
            var texture = importer.textureSettings;
            texture.filterMode = FilterMode.Point;
            texture.generateMipMaps = false;
            texture.readable = false;
            texture.sRGB = true;
            importer.textureSettings = texture;
            foreach (string platform in new[] { "DefaultTexturePlatform", "Standalone", "Android", "iPhone" })
            {
                var settings = importer.GetPlatformSettings(platform);
                settings.name = platform;
                settings.maxTextureSize = 2048;
                settings.format = TextureImporterFormat.RGBA32;
                settings.textureCompression = TextureImporterCompression.Uncompressed;
                settings.overridden = platform != "DefaultTexturePlatform";
                importer.SetPlatformSettings(settings);
            }

            importer.SaveAndReimport();
        }

        static NPCScheduleEntry Entry(
            int hour,
            int minute,
            string zone,
            NPCDailyAction action,
            Vector2 position,
            string objectId = null
        )
        {
            return new NPCScheduleEntry
            {
                hour = hour,
                minute = minute,
                zone = zone,
                action = action,
                position = position,
                objectId = objectId,
            };
        }

        static WorldInteractable[] AmbientResidents(LifeWorldRules rules)
        {
            var result = new List<WorldInteractable>();
            string[] originals =
            {
                "classmate",
                "school-pupil",
                "resident-0",
                "colleague",
                "resident-6",
                "resident-7",
                "resident-2",
                "resident-8",
            };
            var archetypes = new[]
            {
                UrbanNPCArchetype.Student,
                UrbanNPCArchetype.Student,
                UrbanNPCArchetype.OfficeWorker,
                UrbanNPCArchetype.OfficeWorker,
                UrbanNPCArchetype.Delivery,
                UrbanNPCArchetype.Parent,
                UrbanNPCArchetype.Elder,
                UrbanNPCArchetype.JobSeeker,
            };
            string[] titles =
            {
                "등교하는 학생",
                "버스를 기다리는 학생",
                "출근하는 주민",
                "퇴근길 직장인",
                "배달 대기 주민",
                "장보는 주민",
                "산책하는 주민",
                "서류를 든 주민",
            };
            for (int i = 0; i < originals.Length; i++)
            {
                string id = "ambient-urban-" + i;
                var definition = rules.npcs.FirstOrDefault(n => n.npcId == id);
                var template = AssetDatabase.LoadAssetAtPath<WorldInteractable>(
                    "Assets/_Game/Prefabs/NPC/" + originals[i] + ".prefab"
                );
                if (definition == null)
                {
                    var original = rules.npcs.First(n => n.npcId == originals[i]);
                    float x = -8.5f + i * 2.3f;
                    definition = new WorldNPCDefinition
                    {
                        npcId = id,
                        title = titles[i],
                        ambient = true,
                        archetype = archetypes[i],
                        ambientOffsets = new[] { Vector2.zero, new Vector2(1.6f, 0), new Vector2(-1.6f, 0) },
                        homeZone = "residence",
                        personality = original.personality,
                        speed = .95f + i % 3 * .15f,
                        frames = original.frames,
                        animations = original.animations,
                        schedule = new[]
                        {
                            Entry(0, 0, "residence", NPCDailyAction.Sleep, new Vector2(x, 0)),
                            Entry(6, i * 2, "alley", NPCDailyAction.Walk, new Vector2(x, -1.5f)),
                            Entry(8, i * 2, "city", NPCDailyAction.Walk, new Vector2(x, -1.2f)),
                            Entry(
                                10,
                                0,
                                "alley",
                                i % 2 == 0 ? NPCDailyAction.Phone : NPCDailyAction.Walk,
                                new Vector2(x, -.5f)
                            ),
                            Entry(12, i * 3, "city", NPCDailyAction.Walk, new Vector2(x, -7.4f)),
                            Entry(14, 0, "alley", NPCDailyAction.Phone, new Vector2(x, -1.5f)),
                            Entry(17, i * 2, "city", NPCDailyAction.Walk, new Vector2(x, -1.2f)),
                            Entry(20, i * 2, "alley", NPCDailyAction.Walk, new Vector2(x, -1.2f)),
                            Entry(22, i * 2, "residence", NPCDailyAction.Rest, new Vector2(x, 0)),
                        },
                    };
                    if (i < 4)
                    {
                        var workplace = i < 2 ? "school" : "workplace";
                        definition.schedule = definition
                            .schedule.Where(e => e.hour != 10 && e.hour != 14)
                            .Concat(
                                new[]
                                {
                                    Entry(
                                        9,
                                        0,
                                        workplace,
                                        i < 2 ? NPCDailyAction.Study : NPCDailyAction.Work,
                                        new Vector2(-3 + i * 1.5f, -.8f)
                                    ),
                                    Entry(
                                        13,
                                        0,
                                        workplace,
                                        i < 2 ? NPCDailyAction.Study : NPCDailyAction.Work,
                                        new Vector2(-3 + i * 1.5f, -.8f)
                                    ),
                                }
                            )
                            .OrderBy(e => e.hour * 60 + e.minute)
                            .ToArray();
                    }

                    if (i == 5)
                    {
                        definition.schedule = definition
                            .schedule.Concat(
                                new[]
                                {
                                    Entry(
                                        12,
                                        15,
                                        "shop",
                                        NPCDailyAction.Shop,
                                        new Vector2(-5.6f, -.9f),
                                        "shop-browse-1"
                                    ),
                                }
                            )
                            .OrderBy(e => e.hour * 60 + e.minute)
                            .ToArray();
                    }
                    else if (i == 4 || i == 7)
                    {
                        definition.schedule = definition
                            .schedule.Concat(
                                new[]
                                {
                                    Entry(12, 25, "city", NPCDailyAction.Phone, new Vector2(x, -1.2f)),
                                    Entry(18, 10, "city", NPCDailyAction.Phone, new Vector2(x, -1.2f)),
                                }
                            )
                            .OrderBy(e => e.hour * 60 + e.minute)
                            .ToArray();
                    }

                    rules.npcs = rules.npcs.Concat(new[] { definition }).ToArray();
                }

                if (definition.ambientOffsets == null || definition.ambientOffsets.Length == 0)
                {
                    definition.ambientOffsets = new[] { Vector2.zero, new Vector2(1.6f, 0), new Vector2(-1.6f, 0) };
                }

                string[] greetings =
                {
                    "버스는 놓쳐도 등교 시간은 놓치면 안 된대요.",
                    "오늘도 지각 사유는 창의력이 필요하네요.",
                    "행복 증진 포스터가 또 붙었더라고요.",
                    "퇴근은 했고요. 일은 아직 남았습니다.",
                    "다음 주소는 가까운데, 쉬는 시간은 멀어요.",
                    "할인품만 샀는데 영수증은 길어졌네요.",
                    "벤치는 무료라 다행이지요.",
                    "서류는 제출했고, 답장은 기다리는 중입니다.",
                };
                if (string.IsNullOrEmpty(definition.ambientLine))
                {
                    definition.ambientLine = greetings[i];
                }

                string path = Residents + id + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<WorldInteractable>(path);
                if (prefab == null)
                {
                    var actor = UnityEngine.Object.Instantiate(template);
                    actor.name = id;
                    actor.npcId = id;
                    actor.id = "npc-" + id;
                    actor.title = titles[i];
                    prefab = PrefabUtility.SaveAsPrefabAsset(actor.gameObject, path).GetComponent<WorldInteractable>();
                    UnityEngine.Object.DestroyImmediate(actor.gameObject);
                }

                result.Add(prefab);
            }

            foreach (var definition in rules.npcs.Where(n => !n.ambient))
            {
                definition.archetype =
                    definition.npcId.StartsWith("family-") ? UrbanNPCArchetype.Parent
                    : definition.personality == NPCPersonality.Student ? UrbanNPCArchetype.Student
                    : definition.personality == NPCPersonality.ShopWorker ? UrbanNPCArchetype.ShopWorker
                    : definition.workZone == "finance" ? UrbanNPCArchetype.FinanceWorker
                    : definition.personality == NPCPersonality.OfficeWorker
                    || definition.personality == NPCPersonality.NightWorker
                        ? UrbanNPCArchetype.OfficeWorker
                    : UrbanNPCArchetype.Resident;
            }

            EditorUtility.SetDirty(rules);
            AssetDatabase.SaveAssetIfDirty(rules);
            return result.ToArray();
        }

        static GameObject Place(LifeWorldController world, string asset, Vector2 local)
        {
            var root = world.transform.Find("Living city details");
            if (root == null)
            {
                root = new GameObject("Living city details").transform;
                root.SetParent(world.transform, false);
                root.position = world.Room.catalog.rules.Zone(world.Room.roomId).origin;
            }

            string name = asset + "_" + local.x + "_" + local.y;
            var old = root.Find(name);
            if (old != null)
            {
                return old.gameObject;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(PropPrefab(asset), root);
            go.name = name;
            go.transform.localPosition = local;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 1000 - Mathf.RoundToInt(go.transform.position.y * 100);
            return go;
        }

        static void ReplaceFacade(LifeWorldController world, string id, string asset)
        {
            var target = world.objects.FirstOrDefault(o => o.id == id);
            if (target == null || target.visual == null)
            {
                return;
            }

            var original = target.visual;
            // These two entrance roots were authored at .85. Keep collider/door geometry unchanged.
            target.transform.localScale = Vector3.one * .85f;
            var child = target.transform.Find("Production facade");
            if (child == null)
            {
                child = new GameObject("Production facade", typeof(SpriteRenderer)).transform;
                child.SetParent(target.transform, false);
            }

            var visual = child.GetComponent<SpriteRenderer>();
            visual.sprite = Sprite(asset);
            visual.sharedMaterial = original.sharedMaterial;
            visual.sortingOrder = original.sortingOrder;
            child.localScale = Vector3.one / target.transform.lossyScale.x;
            var door = target.GetComponent<WorldDoor>();
            child.localPosition =
                door == null ? Vector3.zero : target.transform.InverseTransformVector(door.VisualOffset);
            if (door != null && door.leaf != null)
            {
                int order = 1000 - Mathf.RoundToInt(door.Plane.y * 100);
                visual.sortingOrder = order - 4;
                door.leaf.sortingOrder = order;
                if (door.frame != null)
                {
                    door.frame.sortingOrder = order + 1;
                }

                float aperture = 1.4f;
                if (door.style != null && door.style.openingFrames.Length > 0)
                {
                    var frame = door.style.openingFrames[0];
                    float fraction =
                        door.style.mechanism == DoorMechanism.SlidingGlass ? .74f
                        : door.style.mechanism == DoorMechanism.HingedJade ? .452f
                        : .48f;
                    aperture = frame.rect.width / frame.pixelsPerUnit * fraction;
                }

                float half = visual.sprite.bounds.size.x * .46f;
                float gap = aperture + .4f;
                float sideWidth = Mathf.Max(.1f, half - gap * .5f);
                foreach (int side in new[] { -1, 1 })
                {
                    string name = "Facade base wall " + side;
                    var wall = target.transform.Find(name);
                    if (wall == null)
                    {
                        wall = new GameObject(name, typeof(BoxCollider2D)).transform;
                        wall.SetParent(target.transform, false);
                    }

                    wall.localScale = Vector3.one / target.transform.lossyScale.x;
                    wall.position = door.Plane + new Vector2(side * (gap * .5f + sideWidth * .5f), .12f);
                    wall.GetComponent<BoxCollider2D>().size = new Vector2(sideWidth, .18f);
                }
            }
            if (original != visual)
            {
                original.enabled = false;
            }

            target.visual = visual;
            var lights = child.Find("Night window pixels");
            if (lights == null)
            {
                lights = new GameObject("Night window pixels", typeof(SpriteRenderer)).transform;
                lights.SetParent(child, false);
            }

            var glow = lights.GetComponent<SpriteRenderer>();
            glow.sprite = Sprite(asset.Replace("_01", "_Lights_01"));
            glow.sharedMaterial = Material;
            glow.sortingOrder = visual.sortingOrder + 1;
            glow.enabled = false;
            var presentation = world.GetComponent<SceneVisualPresentation>();
            presentation.nightWindows = presentation.nightWindows.Concat(new[] { glow }).Distinct().ToArray();
        }

        static void ReplaceStoreDecoration(LifeWorldController world, string name, string asset)
        {
            var original = world.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == name);
            if (original == null || original.transform.Find("Production store prop") != null)
            {
                return;
            }

            var foot = new Vector2(original.bounds.center.x, original.bounds.min.y + .05f);
            float width = original.bounds.size.x;
            var go = new GameObject("Production store prop", typeof(SpriteRenderer));
            go.transform.SetParent(original.transform, false);
            go.transform.position = foot;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Sprite(asset);
            sr.sharedMaterial = Material;
            go.transform.localScale = Vector3.one * (width / sr.sprite.bounds.size.x / original.transform.lossyScale.x);
            sr.sortingOrder = 1000 - Mathf.RoundToInt(foot.y * 100);
            original.enabled = false;
        }

        static void Stamp(LifeWorldController world, string name, string text, Vector2 local)
        {
            var existing = world.transform.Find(name);
            if (existing != null)
            {
                return;
            }

            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(world.transform, false);
            go.transform.position = world.Room.catalog.rules.Zone(world.Room.roomId).origin + local;
            var label = go.GetComponent<TextMeshPro>();
            label.font = world.GetComponent<WorldNPCScheduler>().labelPrefab.font;
            label.text = text;
            label.fontSize = 2;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.87f, .83f, .7f);
            label.rectTransform.sizeDelta = new Vector2(4, .8f);
            label.GetComponent<MeshRenderer>().sortingOrder = 1900;
        }

        static void Traffic(LifeWorldController world, WorldLifeProfile profile)
        {
            var street = world.GetComponentInChildren<WorldStreetLife>(true);
            if (street == null)
            {
                return;
            }

            street.lifeProfile = profile;
            street.zoneId = "city";
            var existing = street.vehicles.ToList();
            string[] names =
            {
                "VEH_Sedan_01",
                "VEH_Taxi_01",
                "VEH_Bus_01",
                "VEH_DeliveryVan_01",
                "VEH_DeliveryScooter_01",
                "VEH_Sedan_01",
            };
            var kinds = new[]
            {
                UrbanVehicleKind.Sedan,
                UrbanVehicleKind.Taxi,
                UrbanVehicleKind.Bus,
                UrbanVehicleKind.DeliveryVan,
                UrbanVehicleKind.DeliveryScooter,
                UrbanVehicleKind.Sedan,
            };
            while (existing.Count < names.Length)
            {
                int index = existing.Count;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(VehiclePrefab(names[index]), street.transform);
                existing.Add(
                    new StreetVehicle
                    {
                        body = go.GetComponent<Rigidbody2D>(),
                        visual = go.GetComponentInChildren<SpriteRenderer>(),
                    }
                );
            }

            for (int i = 0; i < names.Length; i++)
            {
                var vehicle = existing[i];
                vehicle.kind = kinds[i];
                vehicle.visual.sprite = Sprite(names[i]);
                vehicle.visual.transform.localScale = Vector3.one;
                vehicle.visual.color = Color.white;
                vehicle.direction = i % 2 == 0 ? 1 : -1;
                vehicle.visual.flipX = vehicle.direction < 0;
                vehicle.laneY = vehicle.direction > 0 ? -3.35f : -5.35f;
                vehicle.body.transform.localPosition = new Vector2(-14 + i / 2 * 11, vehicle.laneY);
                vehicle.length = vehicle.visual.sprite.bounds.size.x * .85f;
                vehicle.speed = vehicle.kind == UrbanVehicleKind.Bus ? 2.3f : 2.8f + i % 3 * .2f;
                vehicle.stopSeconds = vehicle.kind == UrbanVehicleKind.Bus ? 3.5f : 0;
                vehicle.stopX = 7;
                var collider = vehicle.body.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(vehicle.length, .55f);
                collider.offset = Vector2.up * .18f;
                vehicle.body.simulated = false;
            }

            street.vehicles = existing.ToArray();
            Place(world, "ENV_City_BusStop_01", new Vector2(7, -2));
            Place(world, "ENV_City_Bin_01", new Vector2(10.3f, -.8f));
            Place(world, "ENV_City_BicycleRack_01", new Vector2(-11, 2.2f));
            Place(world, "ENV_City_Vending_01", new Vector2(11, 2.2f));
            Place(world, "ENV_City_Planter_01", new Vector2(2.4f, -9.1f));
            Place(world, "ENV_City_Planter_01", new Vector2(6.6f, -9.1f));
            Stamp(world, "Commute stamp", "직원 행복 증진 캠페인\n오늘도 정상 근무", new Vector2(9, 6.4f));
            Stamp(world, "Bus stop label", "정류장 · 다음 차를 기다립니다", new Vector2(7, 1));
            ReplaceFacade(world, "shop-front", "BLD_ConvenienceStore_01");
        }

        static void Roofline(LifeWorldController world)
        {
            var presentation = world.GetComponent<SceneVisualPresentation>();
            presentation.profile.roofViewPadding = 2.4f;
            EditorUtility.SetDirty(presentation.profile);
            AssetDatabase.SaveAssetIfDirty(presentation.profile);
            var existing = world.transform.Find("Distant residential roofline");
            if (existing != null)
            {
                return;
            }

            const string path = Props + "BLD_Apartment_Backdrop_01.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var source = new GameObject("BLD_Apartment_Backdrop_01", typeof(SpriteRenderer));
                var body = source.GetComponent<SpriteRenderer>();
                body.sprite = Sprite("BLD_Apartment_01");
                body.sharedMaterial = Material;
                body.color = new Color(.67f, .72f, .8f);
                body.sortingOrder = -4600;
                var glow = new GameObject("Distant warm windows", typeof(SpriteRenderer));
                glow.transform.SetParent(source.transform, false);
                var windows = glow.GetComponent<SpriteRenderer>();
                windows.sprite = Sprite("BLD_Apartment_Lights_01");
                windows.sharedMaterial = Material;
                windows.sortingOrder = -4599;
                windows.enabled = false;
                prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                UnityEngine.Object.DestroyImmediate(source);
            }

            var root = new GameObject("Distant residential roofline").transform;
            root.SetParent(world.transform, false);
            root.position = world.Room.catalog.rules.Zone(world.Room.roomId).origin;
            int count = world.Room.roomId == "alley" ? 3 : 5;
            for (int i = 0; i < count; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                go.transform.localPosition = new Vector2(
                    (i - (count - 1) * .5f) * 5.5f,
                    world.Room.roomId == "alley" ? 8.4f : 10.4f
                );
                var light = go.transform.Find("Distant warm windows").GetComponent<SpriteRenderer>();
                presentation.nightWindows = presentation.nightWindows.Concat(new[] { light }).ToArray();
            }
        }

        static void Weather(LifeWorldController world)
        {
            var presentation = world.GetComponent<SceneVisualPresentation>();
            if (presentation == null)
            {
                return;
            }

            var weather = world.GetComponent<WorldWeatherPresentation>();
            if (weather != null)
            {
                foreach (var reflection in weather.wetReflections)
                {
                    reflection.color = new Color(1, 1, 1, .24f);
                }

                return;
            }

            weather = world.gameObject.AddComponent<WorldWeatherPresentation>();
            weather.presentation = presentation;
            var root = new GameObject("Pixel rain", typeof(ParticleSystem));
            root.transform.SetParent(world.transform, false);
            root.transform.position = world.Room.catalog.rules.Zone(world.Room.roomId).origin + Vector2.up * 5;
            var rain = root.GetComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.playOnAwake = false;
            main.maxParticles = 96;
            main.startSpeed = 0;
            main.startLifetime = 1.8f;
            main.startSize3D = true;
            main.startSizeX = .09f;
            main.startSizeY = .3f;
            main.startSizeZ = 1;
            main.startColor = new Color(.7f, .8f, .9f, .65f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = rain.emission;
            emission.rateOverTime = 42;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(world.Room.catalog.rules.Zone(world.Room.roomId).size.x + 4, 16, .01f);
            var velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = -1.3f;
            velocity.y = -9;
            velocity.z = 0;
            string materialPath = Data + "PixelRain.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "VFX_RainDrop_01.png");
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 19000;
            weather.rain = rain;
            var reflections = new List<SpriteRenderer>();
            for (int i = 0; i < 3; i++)
            {
                var puddle = new GameObject("Wet road reflection " + i, typeof(SpriteRenderer));
                puddle.transform.SetParent(world.transform, false);
                puddle.transform.position =
                    world.Room.catalog.rules.Zone(world.Room.roomId).origin
                    + new Vector2(-7 + i * 6, -4.2f + i % 2 * .6f);
                var sr = puddle.GetComponent<SpriteRenderer>();
                sr.sprite = Sprite("VFX_WetReflection_01");
                sr.sharedMaterial = Material;
                sr.color = new Color(1, 1, 1, .24f);
                sr.sortingOrder = -4500;
                sr.enabled = false;
                reflections.Add(sr);
            }

            weather.wetReflections = reflections.ToArray();
        }

        public static string Apply()
        {
            if (
                EditorApplication.isPlaying
                || Enumerable
                    .Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                    .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
            )
            {
                throw new InvalidOperationException("Stop Play and preserve current scene edits before authoring");
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            int scenes = 0;
            try
            {
                EnsureFolder(Data);
                EnsureFolder(Props);
                EnsureFolder(Vehicles);
                EnsureFolder(Residents);
                AssetDatabase.Refresh();
                ImportSprites();
                foreach (var file in Directory.GetFiles(Art, "ENV_*.png"))
                {
                    PropPrefab(Path.GetFileNameWithoutExtension(file));
                }

                foreach (var file in Directory.GetFiles(Art, "INT_*.png"))
                {
                    PropPrefab(Path.GetFileNameWithoutExtension(file));
                }

                foreach (var file in Directory.GetFiles(Art, "VEH_*.png"))
                {
                    VehiclePrefab(Path.GetFileNameWithoutExtension(file));
                }

                Atlas();
                var profile = Profile();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                var catalog = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(
                    "Assets/_Game/Data/SceneManagement/Rooms/RoomScenes.asset"
                );
                var ambient = AmbientResidents(catalog.rules);
                foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes/Rooms" }))
                {
                    var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
                    var world = UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();
                    if (world == null)
                    {
                        continue;
                    }

                    var scheduler = world.GetComponent<WorldNPCScheduler>();
                    scheduler.lifeProfile = profile;
                    scheduler.residentPrefabs = scheduler
                        .residentPrefabs.Concat(ambient)
                        .GroupBy(a => a.npcId)
                        .Select(g => g.First())
                        .ToArray();
                    if (world.Room.roomId == "alley")
                    {
                        ReplaceFacade(world, "house-entrance", "BLD_Apartment_01");
                        Place(world, "ENV_City_Parcels_01", new Vector2(-8.6f, 3.8f));
                        Place(world, "ENV_City_Planter_01", new Vector2(-4.2f, 3.8f));
                        Place(world, "ENV_City_UtilityBox_01", new Vector2(5.5f, 2.8f));
                        Place(world, "ENV_City_Bench_01", new Vector2(4, -.2f));
                        Place(world, "ENV_City_Lamp_01", new Vector2(-10.5f, -1));
                        Stamp(
                            world,
                            "Independence stamp",
                            "독립을 축하합니다\n고지서는 개별 발송됩니다",
                            new Vector2(-7, 6.2f)
                        );
                    }
                    else if (world.Room.roomId == "city")
                    {
                        Traffic(world, profile);
                    }
                    else if (world.Room.roomId == "shop")
                    {
                        Place(world, "ENV_City_Parcels_01", new Vector2(-7.5f, 3.8f));
                        ReplaceStoreDecoration(world, "Grocery shelves", "INT_Store_SnackShelf_01");
                        ReplaceStoreDecoration(world, "Cold drinks", "INT_Store_ColdShelf_01");
                        Place(world, "INT_Store_ColdShelf_01", new Vector2(-1.8f, 3.4f));
                        Place(world, "INT_Store_ColdShelf_01", new Vector2(.8f, 3.4f));
                        Place(world, "INT_Store_NoodleRack_01", new Vector2(-7.5f, .2f));
                        Place(world, "INT_Store_SnackShelf_01", new Vector2(-.7f, .2f));
                        Stamp(world, "Shop shift stamp", "따뜻한 한 끼 · 교대는 아직입니다", new Vector2(2, 4.3f));
                    }

                    if (world.Room.roomId == "city" || world.Room.roomId == "alley")
                    {
                        Weather(world);
                        Roofline(world);
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

            return "Living city applied: "
                + scenes
                + " existing room scenes; existing NPC scheduler and street controller extended";
        }
    }
}
