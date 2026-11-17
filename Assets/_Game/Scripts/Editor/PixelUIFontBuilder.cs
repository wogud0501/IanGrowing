using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace IanGame.Editor
{
    public static class PixelUIFontBuilder
    {
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode first.");
            }

            const string folder = "Assets/_Game/UI/Pixel/Fonts/";
            AssetDatabase.ImportAsset(folder + "NanumGothic-Regular.ttf");
            var source = AssetDatabase.LoadAssetAtPath<Font>(folder + "NanumGothic-Regular.ttf");
            if (source == null || !File.Exists(folder + "OFL.txt"))
            {
                throw new InvalidOperationException("Nanum Gothic source and OFL license required.");
            }

            string path = folder + "IanPixelKoreanSDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(
                    source,
                    64,
                    7,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048,
                    AtlasPopulationMode.Dynamic,
                    true
                );
                font.name = "IanPixelKoreanSDF";
                AssetDatabase.CreateAsset(font, path);
                font.material.name = "Ian Pixel Korean Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures)
                {
                    if (!AssetDatabase.Contains(texture))
                    {
                        AssetDatabase.AddObjectToAsset(texture, font);
                    }
                }
            }

            var theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>(PixelAssetBuilder.Folder + "PixelTheme.asset");
            theme.font = font;
            theme.fontLicense = AssetDatabase.LoadAssetAtPath<TextAsset>(folder + "OFL.txt");
            EditorUtility.SetDirty(theme);
            var texts = new List<string>
            {
                "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz !?+-/.,:%()[]<>→·‘’\n허기목마름지식체력멘탈사회성신용행복생존위험안정성장보상수령친구관계교우학교입학공부책읽기숙제시험성적표졸업이력서취업면접근무급여퇴사해고승진유아기아동기청소년기청년기중년기노년기미상환부채순자산가용자금확인현재미션완료미완료추후해금예정필수조건모두하나충족선행능력생활성장활동설정보관함장비헤드폰가챠주식수면물마시기무료기본모션크게껐다켜짐",
            };
            var paths = AssetDatabase
                .FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p =>
                    p.Contains("PixelUI/")
                    || p.EndsWith("GameHUD.prefab")
                    || p.EndsWith("MissionHUD.prefab")
                    || p.EndsWith("MissionRow.prefab")
                    || p.EndsWith("GameContext.prefab")
                )
                .ToArray();
            foreach (var prefab in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(prefab);
                try
                {
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        texts.Add(text.text ?? "");
                        text.font = font;
                        text.fontSharedMaterial = font.material;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefab);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            foreach (var name in new[] { "MainMenu", "Home", "School", "City", "Workplace" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        texts.Add(text.text ?? "");
                        text.font = font;
                        text.fontSharedMaterial = font.material;
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>("Assets/_Game/Data/MainMissions.asset");
            foreach (var m in catalog.missions)
            {
                texts.Add(m.title);
                texts.Add(m.description);
                texts.Add(m.completionMessage);
            }

            string missing;
            string glyphs = new string(
                string.Join("", texts.ToArray()).Distinct().Where(c => !char.IsControl(c)).ToArray()
            );
            font.TryAddCharacters(glyphs, out missing);
            foreach (var texture in font.atlasTextures)
            {
                if (texture != null && !AssetDatabase.Contains(texture))
                {
                    AssetDatabase.AddObjectToAsset(texture, font);
                }
            }

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Docs/PixelUI");
            File.WriteAllText(
                "Docs/PixelUI/FONT_VALIDATION.txt",
                "Source: Google Fonts / NanumGothic-Regular.ttf\nLicense: SIL OFL 1.1, bundled with original font\nRequested unique characters: "
                    + glyphs.Length
                    + "\nMissing characters: "
                    + (string.IsNullOrEmpty(missing) ? "none" : missing)
                    + "\nAtlas count: "
                    + font.atlasTextures.Length
            );
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
        }
    }
}
