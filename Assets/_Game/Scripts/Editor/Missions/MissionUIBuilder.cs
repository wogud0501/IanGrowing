using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class MissionUIBuilder
    {
        static TMP_FontAsset font;
        static Color ink = new Color32(25, 32, 37, 255);
        static Color cream = new Color32(236, 232, 215, 255);
        static Color lime = new Color32(210, 230, 116, 255);

        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        public static Image Panel(Transform p, string n, float x, float y, float w, float h, Color color)
        {
            var r = Rect(p, n, x, y, w, h);
            var image = r.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        public static TMP_Text Text(
            Transform p,
            string n,
            string value,
            float x,
            float y,
            float w,
            float h,
            int size,
            Color color
        )
        {
            if (font == null)
            {
                font = StardustTypography.Main;
            }

            var text = Rect(p, n, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        public static Button Button(Transform p, string n, string value, float x, float y, float w, float h)
        {
            var bg = Panel(p, n, x, y, w, h, cream);
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var label = Text(bg.transform, "Label", value, 8, 4, w - 16, h - 8, 18, ink);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = 18;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(.5f, .5f);
            r.offsetMin = new Vector2(8, 4);
            r.offsetMax = new Vector2(-8, -4);
            return b;
        }

        [MenuItem("Ian/Phase 2.2/Build Mission UI")]
        public static void Build()
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (hud == null)
            {
                throw new Exception("Open MainGame2D first.");
            }

            var dashboard = hud.transform.Find("Dashboard");
            if (dashboard.Find("Mission HUD") != null)
            {
                throw new Exception("Mission HUD exists.");
            }

            var root = Rect(dashboard, "Mission HUD", 0, 0, 1440, 900);
            var view = root.gameObject.AddComponent<MissionUI>();
            var main = Panel(root, "Current Mission", 1008, 226, 392, 320, ink).transform;
            Text(main, "Section", "MAIN MISSION", 16, 9, 360, 21, 14, lime);
            view.missionTitle = Text(main, "Mission Title", "미션 준비 중", 16, 34, 360, 44, 23, cream);
            view.missionTitle.enableAutoSizing = true;
            view.missionTitle.fontSizeMin = 15;
            view.missionTitle.fontSizeMax = 23;
            view.chapterText = Text(main, "Chapter", "", 16, 82, 360, 20, 13, lime);
            view.conditionText = Text(main, "Condition", "", 16, 105, 360, 36, 15, cream);
            view.progressText = Text(main, "Progress", "", 16, 145, 360, 21, 15, lime);
            view.rewardText = Text(main, "Rewards", "", 16, 170, 360, 38, 14, cream);
            view.nextText = Text(main, "Next", "", 16, 212, 360, 22, 12, cream);
            view.actionButton = Button(main, "Mission Action", "미션 행동", 16, 239, 172, 34);
            view.claimButton = Button(main, "Claim Reward", "보상 수령", 204, 239, 172, 34);
            view.listButton = Button(main, "Mission List", "60개 미션 목록", 16, 281, 172, 30);
            view.educationButton = Button(main, "Education Activities", "학교 활동", 204, 281, 172, 30);
            var logPanel = dashboard.Find("Log Panel").GetComponent<RectTransform>();
            logPanel.anchoredPosition = new Vector2(1008, -550);
            logPanel.sizeDelta = new Vector2(392, 136);
            var logTitle = dashboard.Find("Log Title").GetComponent<TMP_Text>();
            logTitle.rectTransform.anchoredPosition = new Vector2(1032, -554);
            logTitle.fontSize = 17;
            logTitle.text = "최근 인생 명세서";
            dashboard.Find("Log Subtitle").gameObject.SetActive(false);
            hud.logText.rectTransform.anchoredPosition = new Vector2(1032, -583);
            hud.logText.rectTransform.sizeDelta = new Vector2(340, 94);
            hud.logText.fontSize = 12;
            var toast = Panel(root, "Mission Toast", 344, 198, 1056, 25, new Color(.12f, .18f, .18f, .95f));
            view.toastPanel = toast.gameObject;
            view.toastText = Text(toast.transform, "Toast Text", "", 8, 1, 1040, 23, 13, lime);
            toast.gameObject.SetActive(false);
            var shade = Panel(root, "Mission List Overlay", 0, 0, 1440, 900, new Color(0, 0, 0, .88f));
            view.listPanel = shade.gameObject;
            var list = Panel(shade.transform, "Window", 100, 65, 1240, 760, ink).transform;
            Text(list, "Heading", "메인 미션 60개 / 원본과 제안 데이터 구분", 24, 20, 1100, 44, 27, lime);
            view.closeListButton = Button(list, "Close", "닫기", 1120, 18, 96, 42);
            var viewport = Panel(list, "Viewport", 24, 82, 1192, 650, new Color(.08f, .12f, .14f, 1)).rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Content", 0, 0, 1192, 0);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 40;
            view.rowContainer = content;
            var row = Panel(null, "Mission Row", 0, 0, 1180, 68, new Color(.14f, .19f, .21f, 1));
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 68;
            var rowView = row.gameObject.AddComponent<MissionRowView>();
            rowView.titleText = Text(row.transform, "Name", "", 12, 6, 830, 28, 18, cream);
            rowView.stateText = Text(row.transform, "State", "", 12, 36, 900, 24, 14, lime);
            rowView.progressText = Text(row.transform, "Progress", "", 960, 18, 190, 32, 16, cream);
            PrefabUtility.SaveAsPrefabAsset(row.gameObject, "Assets/_Game/Prefabs/UI/MissionRow.prefab");
            UnityEngine.Object.DestroyImmediate(row.gameObject);
            view.rowPrefab = AssetDatabase
                .LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/MissionRow.prefab")
                .GetComponent<MissionRowView>();
            shade.gameObject.SetActive(false);
            var schoolShade = Panel(root, "Education Overlay", 0, 0, 1440, 900, new Color(0, 0, 0, .85f));
            view.educationPanel = schoolShade.gameObject;
            var school = Panel(schoolShade.transform, "School Activities", 250, 150, 940, 600, ink).transform;
            Text(school, "Title", "학교 · 관계 활동", 28, 22, 750, 48, 30, lime);
            view.closeEducationButton = Button(school, "Close", "닫기", 810, 22, 100, 44);
            view.educationHint = Text(school, "Hint", "School에서 수행합니다.", 28, 82, 880, 54, 17, cream);
            view.activityButtons = new Button[8];
            var catalog = AssetDatabase.LoadAssetAtPath<ActivityCatalog>("Assets/_Game/Data/BasicActivities.asset");
            for (int i = 0; i < 8; i++)
            {
                var activity = catalog.Find(i + 7);
                view.activityButtons[i] = Button(
                    school,
                    "Activity " + activity.action,
                    activity.displayName,
                    28 + (i % 2) * 448,
                    155 + (i / 2) * 97,
                    430,
                    74
                );
            }

            schoolShade.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                root.gameObject,
                "Assets/_Game/Prefabs/UI/MissionHUD.prefab",
                InteractionMode.AutomatedAction
            );
            var nav = Rect(dashboard, "Location Navigation", 40, 198, 280, 25)
                .gameObject.AddComponent<LocationPresenter>();
            nav.homeButton = Button(nav.transform, "Home", "Home", 0, 0, 88, 25);
            nav.schoolButton = Button(nav.transform, "School", "School", 96, 0, 88, 25);
            nav.menuButton = Button(nav.transform, "Menu", "메뉴", 192, 0, 88, 25);
            hud.game = null;
            PrefabUtility.ApplyPrefabInstance(hud.gameObject, InteractionMode.AutomatedAction);
            var bindings = UnityEngine.Object.FindAnyObjectByType<GameplaySceneBindings>();
            bindings.missionUI = view;
            EditorUtility.SetDirty(bindings);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
    }
}
