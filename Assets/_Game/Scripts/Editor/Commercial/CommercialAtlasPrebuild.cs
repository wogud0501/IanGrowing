// [UNITY-SKILL:SPRITEATLAS]
// [TYPE:PREBUILD] Built-in core UI delivery, scoped to presentation assets.
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;

namespace IanGame.Editor
{
    public sealed class CommercialAtlasPrebuild : IPreprocessBuildWithReport
    {
        public const string Path = "Assets/_Game/Art/Commercial/CommercialUI.spriteatlasv2";
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Configure();
        }

        public static void Configure()
        {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            var asset = new SpriteAtlasAsset();
            asset.Add(
                new UnityEngine.Object[]
                {
                    AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/_Game/UI/Pixel"),
                    AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/_Game/Art/Commercial/UI"),
                }
            );
            var animationSprites = System.Array.FindAll(
                AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Data/Commercial/CityOfficeSprites.asset"),
                o => o is Sprite
            );
            if (animationSprites.Length > 0)
            {
                asset.Add(animationSprites);
            }

            SpriteAtlasAsset.Save(asset, Path);
            AssetDatabase.ImportAsset(Path);
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(Path);
            importer.includeInBuild = true;
            var pack = importer.packingSettings;
            pack.padding = 4;
            pack.enableRotation = false;
            pack.enableTightPacking = false;
            importer.packingSettings = pack;
            var texture = importer.textureSettings;
            texture.filterMode = FilterMode.Point;
            texture.generateMipMaps = false;
            texture.readable = false;
            texture.sRGB = true;
            importer.textureSettings = texture;
            foreach (string platform in new[] { "DefaultTexturePlatform", "Standalone", "Android" })
            {
                var settings = importer.GetPlatformSettings(platform);
                settings.name = platform;
                settings.maxTextureSize = 1024;
                settings.format = TextureImporterFormat.RGBA32;
                settings.textureCompression = TextureImporterCompression.Uncompressed;
                settings.overridden = platform != "DefaultTexturePlatform";
                importer.SetPlatformSettings(settings);
            }

            importer.SaveAndReimport();
            // Supersedes the V1 atlas created earlier in this rebuild; no baseline asset is removed.
            const string legacy = "Assets/_Game/Art/Commercial/CommercialUI.spriteatlas";
            if (AssetDatabase.LoadMainAssetAtPath(legacy) != null)
            {
                AssetDatabase.DeleteAsset(legacy);
            }

            if (EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
            {
                throw new BuildFailedException("Sprite packing remains disabled");
            }
        }
    }
}
