using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class ReadabilityAndBirthBuilder
    {
        static PixelUITheme theme;
        static TMP_FontAsset body;
        static TMP_FontAsset heading;
        static TMP_FontAsset numbers;
        static Sprite panel;

        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play Mode");
            }

            theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");
            panel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Commercial/UI/Panel_64.png");
            body = heading = numbers = StardustTypography.Main;
            theme.font = body;
            EditorUtility.SetDirty(theme);
            foreach (
                string path in AssetDatabase
                    .FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs" })
                    .Select(AssetDatabase.GUIDToAssetPath)
            )
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Fix(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            foreach (string name in new[] { "MainMenu", "Home", "School", "City", "Workplace" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                foreach (var root in scene.GetRootGameObjects())
                {
                    Fix(root);
                }

                if (name == "Home")
                {
                    Birth();
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                var modern = so.FindProperty("m_UseModernHangulLineBreakingRules");
                if (modern != null)
                {
                    modern.boolValue = true;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
        }

        static void Fix(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                bool bold = t.font != null && t.font.name.Contains("Heading");
                bool numeric = t.font != null && t.font.name.Contains("Numbers");
                t.font =
                    bold ? heading
                    : numeric ? numbers
                    : body;
                t.fontSharedMaterial = t.font.material;
                t.enableAutoSizing = false;
                t.fontSizeMin = 18;
                t.fontSizeMax = Mathf.Max(18, t.fontSize);
                t.fontSize = t.fontSize >= 28 ? 36 : 18;
                t.characterSpacing = t.wordSpacing = 0;
                t.lineSpacing = 2;
                t.paragraphSpacing = 6;
                t.maxVisibleLines = 99999;
                t.overflowMode = TextOverflowModes.Overflow;
                t.textWrappingMode = TextWrappingModes.Normal;
                var scale = t.GetComponent<UITypeScale>();
                if (scale != null)
                {
                    scale.baseSize = t.fontSize;
                }

                var b = t.GetComponentInParent<UnityEngine.UI.Button>();
                if (b != null)
                {
                    t.lineSpacing = 0;
                    t.margin = new Vector4(10, 4, 10, 4);
                    t.alignment = TextAlignmentOptions.Center;
                    var le = b.GetComponent<LayoutElement>();
                    if (le != null && le.preferredHeight > 0 && le.preferredHeight < 48)
                    {
                        le.minHeight = le.preferredHeight = 48;
                    }
                }
                else if (t.GetComponent<LayoutElement>() != null && t.GetComponentInParent<ScrollRect>() != null)
                {
                    var fit = t.GetComponent<PixelTextFlow>() ?? t.gameObject.AddComponent<PixelTextFlow>();
                    fit.minimumHeight = 48;
                }

                if (t.name == "PixelTooltip")
                {
                    t.fontSize = 18;
                    t.rectTransform.anchorMin = new Vector2(.018f, .285f);
                    t.rectTransform.anchorMax = new Vector2(.60f, .37f);
                    t.rectTransform.offsetMin = new Vector2(12, 4);
                    t.rectTransform.offsetMax = new Vector2(-12, -4);
                }

                EditorUtility.SetDirty(t);
            }
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static TMP_Text Label(
            Transform parent,
            string name,
            string value,
            Vector2 min,
            Vector2 max,
            int size = 18
        )
        {
            var r = Rect(parent, name, min, max);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = body ?? StardustTypography.Main;
            t.fontSize = size;
            t.color = new Color32(255, 238, 205, 255);
            t.text = value;
            t.margin = new Vector4(12, 4, 12, 4);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static UnityEngine.UI.Button Button(
            Transform parent,
            string name,
            string value,
            Vector2 min,
            Vector2 max
        )
        {
            var r = Rect(parent, name, min, max);
            var i = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            i.sprite =
                panel
                ?? AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset").panelDark;
            i.type = UnityEngine.UI.Image.Type.Sliced;
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = i;
            Label(r, "Label", value, Vector2.zero, Vector2.one).alignment = TextAlignmentOptions.Center;
            r.gameObject.AddComponent<PixelButton>();
            return b;
        }

        static void Birth()
        {
            var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
            if (p.GetComponent<BirthIntroductionPopup>() != null)
            {
                var old = p.GetComponent<BirthIntroductionPopup>();
                foreach (var i in old.frame.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    i.sprite = panel;
                }

                return;
            }

            var shell = p.navigation.stack;
            var host = Rect(shell.overlayRoot, "BirthIntroductionPopup", Vector2.zero, Vector2.one);
            var dim = host.gameObject.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0, 0, 0, .52f);
            var group = host.gameObject.AddComponent<CanvasGroup>();
            var safe = Rect(host, "Safe area", Vector2.zero, Vector2.one);
            var safeLayout = safe.gameObject.AddComponent<BirthPopupSafeArea>();
            safeLayout.target = safe;
            var card = Rect(safe, "Birth certificate", new Vector2(.19f, .13f), new Vector2(.81f, .87f));
            var image = card.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = panel;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            Label(card, "System heading", "SYSTEM — 출생 신고 완료", new Vector2(.04f, .82f), new Vector2(.96f, .96f));
            Label(
                card,
                "Birth greeting",
                "축하합니다!\n이안이 태어났습니다.",
                new Vector2(.04f, .61f),
                new Vector2(.96f, .82f),
                24
            );
            Label(
                card,
                "Birth facts",
                "나이: 0세\n초기 자산: 0G\n초기 부채: 1,000,000G\n사회적 지위: 없음",
                new Vector2(.04f, .30f),
                new Vector2(.96f, .61f)
            );
            Label(
                card,
                "Satire",
                "이 게임에서는 태어난 것만으로\n비용이 발생합니다.",
                new Vector2(.04f, .15f),
                new Vector2(.96f, .30f)
            );
            var begin = Button(card, "Begin life", "인생 시작하기", new Vector2(.12f, .035f), new Vector2(.88f, .15f));
            var popup = p.gameObject.AddComponent<BirthIntroductionPopup>();
            popup.view = host.gameObject;
            popup.group = group;
            popup.frame = card;
            popup.begin = begin;
            popup.shell = shell;
            host.gameObject.SetActive(false);
        }
    }
}
