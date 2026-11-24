using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace IanGame.Editor
{
    public static class StardustTypography
    {
        public const string Folder = "Assets/_Game/UI/Pixel/Fonts/PFStardust/";
        public const string MainPath = Folder + "PFStardust_Main.asset";
        public const string LicensePath = Folder + "License.txt";

        public static TMP_FontAsset Main => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainPath);

        [MenuItem("Ian/UI/Build PF Stardust atlases")]
        public static void Build()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(Folder + "PFStardust3.ttf");
            var symbols = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/UI/Pixel/Fonts/Galmuri/Galmuri11.ttf");
            var characters = new SortedSet<char>(Enumerable.Range(32, 95).Select(value => (char)value));
            foreach (var file in Directory.EnumerateFiles("Assets/_Game", "*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file);
                if (extension is not (".asset" or ".unity" or ".prefab" or ".cs" or ".json") || file.Contains("/Fonts/"))
                {
                    continue;
                }

                foreach (string line in File.ReadLines(file))
                {
                    foreach (char c in line)
                    {
                        if (c > 127 && !char.IsSurrogate(c) && !char.IsControl(c))
                        {
                            characters.Add(c);
                        }
                    }

                    if (line.Contains(@"\u"))
                    {
                        foreach (Match match in Regex.Matches(line, @"\\u([a-fA-F0-9]{4})"))
                        {
                            char c = (char)Convert.ToUInt16(match.Groups[1].Value, 16);
                            if (!char.IsSurrogate(c) && !char.IsControl(c))
                            {
                                characters.Add(c);
                            }
                        }
                    }
                }
            }

            characters.UnionWith("가나다라마바사아자차카타파하₩·요일월화수목금토일▲▼→←…㎡−☐♢");
            var common = Create(source, "PFStardust_Main", 1024);
            var extended = Create(source, "PFStardust_Extended", 512);
            var special = Create(symbols, "UI_Symbols", 256);
            Fill(common, new string(characters.ToArray()), out string missing);
            Fill(special, new string((missing ?? string.Empty).Where(c => char.IsSymbol(c) || char.IsPunctuation(c)).ToArray()), out _);
            extended.isMultiAtlasTexturesEnabled = true;
            common.fallbackFontAssetTable = new List<TMP_FontAsset> { extended, special };
            EditorUtility.SetDirty(extended);
            EditorUtility.SetDirty(common);
            AssetDatabase.SaveAssets();
        }

        static TMP_FontAsset Create(Font source, string name, int size)
        {
            string path = Folder + name + ".asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font)
            {
                return font;
            }

            font = TMP_FontAsset.CreateFontAsset(source, 32, 1, GlyphRenderMode.RASTER_HINTED,
                size, size, AtlasPopulationMode.Dynamic, true);
            font.name = name;
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            font.atlasTexture.filterMode = FilterMode.Point;
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            return font;
        }

        static void Fill(TMP_FontAsset font, string characters, out string missing)
        {
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.TryAddCharacters(characters, out missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            foreach (var texture in font.atlasTextures)
            {
                texture.filterMode = FilterMode.Point;
                if (!AssetDatabase.Contains(texture))
                {
                    AssetDatabase.AddObjectToAsset(texture, font);
                }
            }

            EditorUtility.SetDirty(font);
        }
    }
}
