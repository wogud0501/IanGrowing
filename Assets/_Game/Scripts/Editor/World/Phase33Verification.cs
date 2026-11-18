using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    public static class Phase33Verification
    {
        [MenuItem("IanGrowing/Phase 3.3/Verify population data and spatial grid")]
        public static void Menu()
        {
            Debug.Log(Run());
        }

        public static string Run()
        {
            var passed = new List<string>();
            void Check(bool condition, string message)
            {
                if (!condition)
                {
                    throw new InvalidOperationException(message);
                }

                passed.Add(message);
            }

            var profile = AssetDatabase.LoadAssetAtPath<WorldLifeProfile>(
                "Assets/_Game/Data/World/LivingCity/LivingCity.asset"
            );
            var catalog = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(
                "Assets/_Game/Data/SceneManagement/Rooms/RoomScenes.asset"
            );
            Check(profile != null && catalog != null, "Shared living-city data exists");
            Check(
                profile.AmbientLimit("city", 8) > profile.AmbientLimit("city", 2),
                "Rush-hour crowd exceeds night crowd"
            );
            Check(
                profile.TrafficLimit("city", 8, 0) > profile.TrafficLimit("city", 2, 0),
                "Rush-hour traffic exceeds night traffic"
            );
            Check(profile.TrafficLimit("alley", 8, 99) == 0, "Residential pedestrian space has no vehicle lane");
            Check(profile.AmbientLimit("shop", 12) == 1, "Additional shop customers have a separate small density cap");
            var rules = catalog.rules;
            Check(
                rules.npcs.Select(n => n.npcId).Distinct().Count() == rules.npcs.Length,
                "Resident IDs remain unique"
            );
            Check(rules.npcs.Count(n => n.ambient) == 8, "Eight scheduled ambient residents");
            foreach (var resident in rules.npcs)
            {
                Check(resident.schedule != null && resident.schedule.Length > 0, resident.npcId + ": schedule present");
                Check(
                    resident.schedule.All(e => rules.Zone(e.zone) != null),
                    resident.npcId + ": all target room IDs resolve"
                );
            }

            var band = new ZoneLifeRule
            {
                ambientCapacity = 10,
                bands = new[]
                {
                    new PopulationBand { fromHour = 20, ambientResidents = 2 },
                    new PopulationBand { fromHour = 8, ambientResidents = 6 },
                },
            };
            Check(
                band.AtHour(2).ambientResidents == 2 && band.AtHour(9).ambientResidents == 6,
                "Unsorted bands and midnight wrap select the previous band"
            );
            var grid = new NPCCrowdSpace();
            grid.Set("a", Vector2.zero);
            grid.Set("b", new Vector2(2, 0));
            Check(grid.Free("a", Vector2.zero), "Personal-space query ignores self");
            Check(!grid.Free("b", new Vector2(.2f, 0)), "Personal space prevents shared occupancy");
            Check(
                grid.Blocks("b", new Vector2(1.2f, 0), new Vector2(.6f, 0)),
                "Inward movement into occupied space blocks"
            );
            Check(
                !grid.Blocks("b", new Vector2(.2f, 0), new Vector2(.4f, 0)),
                "An overlapping actor may leave without pushing"
            );
            grid.Set("a", new Vector2(4, 0));
            Check(grid.Free("b", Vector2.zero), "Moving between cells clears old occupancy");
            grid.Clear();
            Check(grid.Free("a", new Vector2(2, 0)), "New tick ignores stale buckets");
            grid.Set("a", Vector2.zero);
            grid.Set("b", new Vector2(2, 0));
            grid.Remove("a");
            Check(grid.Free("b", Vector2.zero), "Removal clears current occupancy");
            for (int i = 0; i < 200; i++)
            {
                grid.Clear();
                grid.Set("a", Vector2.zero);
                grid.Set("b", new Vector2(2, 0));
                grid.Blocks("b", new Vector2(1.2f, 0), new Vector2(.6f, 0));
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                grid.Clear();
                grid.Set("a", Vector2.zero);
                grid.Set("b", new Vector2(2, 0));
                grid.Blocks("b", new Vector2(1.2f, 0), new Vector2(.6f, 0));
                grid.Free("a", new Vector2(.2f, 0));
            }

            long spatialGridBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Check(spatialGridBytes == 0, "Warmed spatial-grid tick/query loop allocates zero managed bytes");
            int sprites = 0;
            foreach (string path in Directory.GetFiles("Assets/_Game/Art/WorldLife", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Check(
                    importer.textureType == TextureImporterType.Sprite
                        && importer.filterMode == FilterMode.Point
                        && !importer.mipmapEnabled
                        && importer.textureCompression == TextureImporterCompression.Uncompressed
                        && Mathf.Approximately(importer.spritePixelsPerUnit, 40),
                    Path.GetFileName(path) + ": pixel import settings"
                );
                sprites++;
            }

            const string atlasPath = "Assets/_Game/Art/WorldLife/LivingCity.spriteatlasv2";
            var atlasImporter = (UnityEditor.U2D.SpriteAtlasImporter)AssetImporter.GetAtPath(atlasPath);
            Check(
                atlasImporter != null
                    && atlasImporter.includeInBuild
                    && atlasImporter.textureSettings.filterMode == FilterMode.Point,
                "V2 sprite atlas is included with Point filtering"
            );
            var atlas = AssetDatabase
                .LoadAllAssetsAtPath(atlasPath)
                .OfType<UnityEngine.U2D.SpriteAtlas>()
                .FirstOrDefault();
            Check(atlas != null && atlas.spriteCount == sprites, "Packed V2 atlas resolves every imported sprite");

            Directory.CreateDirectory("Docs/Phase33");
            File.WriteAllText(
                "Docs/Phase33/DATA_AND_GRID_RESULTS.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    new
                    {
                        complete = true,
                        passed,
                        spatialGridBytes,
                        warmedIterations = 10000,
                        sprites,
                    },
                    Newtonsoft.Json.Formatting.Indented
                )
            );
            return "Phase33 data/grid checks: "
                + passed.Count
                + ", imported sprites: "
                + sprites
                + ", warmed grid bytes: "
                + spatialGridBytes;
        }
    }
}
