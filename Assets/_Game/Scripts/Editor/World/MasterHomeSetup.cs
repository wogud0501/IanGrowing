using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class MasterHomeSetup
    {
        [MenuItem("Ian/Life World/Apply Bed Only Starter Home")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
            var w = UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();
            const string showcase = "Assets/_Game/Scenes/HomeShowcase.unity";
            if (!System.IO.File.Exists(showcase))
            {
                EditorSceneManager.SaveScene(scene, showcase, true);
            }

            if (w.hud.GetComponent<WorldHUDLayout>() == null)
            {
                w.hud.gameObject.AddComponent<WorldHUDLayout>();
            }

            var setup = w.GetComponent<StarterHomePresentation>();
            if (setup == null)
            {
                setup = w.gameObject.AddComponent<StarterHomePresentation>();
            }

            var root = w
                .transform.Cast<Transform>()
                .First(t => t.name == "Visual Rebuild / authored home" && t.gameObject.activeSelf);
            var decorationNames = new[]
            {
                "Kitchen dish shelf",
                "Living woven rug",
                "Bedroom laundry basket",
                "Laundry footprint",
                "Living houseplant",
                "Plant footprint",
                "Bedside warm pool",
            };
            setup.optionalDecorations = decorationNames
                .Select(n => root.Find(n)?.gameObject)
                .Where(g => g != null)
                .ToArray();
            setup.kitchenFloor = root.Find("Kitchen linoleum").GetComponent<SpriteRenderer>();
            setup.kitchenWall = root.Find("Kitchen backsplash").GetComponent<SpriteRenderer>();
            if (setup.legacyFloor == null)
            {
                setup.legacyFloor = setup.kitchenFloor.sprite;
            }

            if (setup.legacyWall == null)
            {
                setup.legacyWall = setup.kitchenWall.sprite;
            }

            setup.woodFloor = root.Find("Bedroom wood floor").GetComponent<SpriteRenderer>().sprite;
            setup.whiteWall = root.Find("Bedroom back wall").GetComponent<SpriteRenderer>().sprite;
            setup.frontThreshold = w.objects.First(o => o.id == "front-door").visual;
            if (setup.legacyThreshold == null)
            {
                setup.legacyThreshold = setup.frontThreshold.sprite;
            }

            setup.starterThreshold = root.Find("Bedroom open doorway").GetComponent<SpriteRenderer>().sprite;
            var catalog = w.hud.home.catalog;
            var plan = catalog.placementPlan;
            foreach (var footprint in plan.fixedFootprints)
            {
                footprint.optionalFurnishing =
                    StarterHomePresentation.Optional(footprint.id)
                    || footprint.id == "Laundry footprint"
                    || footprint.id == "Plant footprint"
                    || footprint.id.StartsWith("upgrade-");
            }

            plan.starterAccessPoints = new[] { "bed", "front-door", "bath-door" }
                .Select(id => w.objects.First(o => o.id == id).Point)
                .ToArray();
            var definitions = catalog.furniture.ToList();
            var rules = plan.furniture.ToList();
            void Add(string id, string title, int price, string objectId, WorldInteractionKind kind, HomeArea area)
            {
                var original = w.objects.First(o => o.id == objectId);
                var item = definitions.FirstOrDefault(f => f.id == id);
                if (item == null)
                {
                    item = new FurnitureDefinition { id = id };
                    definitions.Add(item);
                }

                item.title = title;
                item.price = price;
                item.starter = false;
                item.description = "구매 후 보관 · 직접 배치 · 회수와 재배치 무료";
                item.preview = original.visual.sprite;
                item.interaction = kind;
                var c = original.GetComponent<BoxCollider2D>();
                var rule = rules.FirstOrDefault(r => r.id == id);
                if (rule == null)
                {
                    rule = new HomeFurnitureRule { id = id };
                    rules.Add(rule);
                }

                rule.areas = area;
                rule.footprint = c.size;
                rule.approach = original.UsePoint;
                rule.height = original.visual.sprite.bounds.size.y * original.visual.transform.lossyScale.y;
                rule.slots = new[] { (Vector2)original.transform.position };
                item.position = rule.slots[0] / 2;
            }

            Add(
                "basic-table",
                "나무 식탁",
                450,
                "meal-table",
                WorldInteractionKind.Meal,
                HomeArea.Dining | HomeArea.Living
            );
            Add("fridge", "작은 냉장고", 800, "fridge", WorldInteractionKind.Fridge, HomeArea.Kitchen);
            Add("cooker", "조리대", 650, "kitchen-prep", WorldInteractionKind.Fridge, HomeArea.Kitchen);
            Add(
                "desk",
                "나무 책상",
                550,
                "starter-desk",
                WorldInteractionKind.Study,
                HomeArea.Study | HomeArea.Bedroom
            );
            Add("sofa", "작은 소파", 700, "living-settee", WorldInteractionKind.Rest, HomeArea.Living);
            Add("wardrobe", "옷장", 500, "closet", WorldInteractionKind.Wardrobe, HomeArea.Bedroom);
            Add("nightstand", "협탁", 200, "bedside-table", WorldInteractionKind.Rest, HomeArea.Bedroom);
            rules.First(r => r.id == "sofa").slots = new[] { new Vector2(1.5f, -1.5f), new Vector2(2, -2.5f) };
            rules.First(r => r.id == "wardrobe").slots = new[] { new Vector2(-1, 1.25f) };
            definitions.First(f => f.id == "bookshelf").preview = w
                .objects.First(o => o.id == "upgrade-storage")
                .visual.sprite;
            catalog.furniture = definitions.ToArray();
            plan.furniture = rules.ToArray();
            var view = w.hud.home;
            var info = view.furnitureInfo.ToList();
            var buy = view.buy.ToList();
            var place = view.place.ToList();
            var reposition = view.reposition.ToList();
            var template = info[4].transform.parent.gameObject;
            for (int i = info.Count; i < definitions.Count; i++)
            {
                var row = UnityEngine.Object.Instantiate(template, template.transform.parent);
                row.name = "Furniture " + definitions[i].id;
                info.Add(row.transform.Find(info[4].name).GetComponent<TMPro.TMP_Text>());
                buy.Add(row.transform.Find(buy[4].name).GetComponent<Button>());
                place.Add(row.transform.Find(place[4].name).GetComponent<Button>());
                reposition.Add(row.transform.Find(reposition[4].name).GetComponent<Button>());
                foreach (var image in row.GetComponentsInChildren<Image>())
                {
                    if (image.sprite == definitions[4].preview)
                    {
                        image.sprite = definitions[i].preview;
                    }
                }
            }

            for (int i = 0; i < info.Count; i++)
            {
                var image = info[i].transform.parent.Find("Pixel preview").GetComponent<Image>();
                image.sprite = definitions[i].preview;
                image.material = null;
                image.preserveAspect = true;
            }

            view.furnitureInfo = info.ToArray();
            view.buy = buy.ToArray();
            view.place = place.ToArray();
            view.reposition = reposition.ToArray();
            var placement = w.hud.GetComponent<WorldFurniturePlacement>();
            if (placement == null)
            {
                placement = w.hud.gameObject.AddComponent<WorldFurniturePlacement>();
            }

            placement.hud = w.hud;
            if (placement.panel == null)
            {
                var overlay = UnityEngine.Object.Instantiate(w.hud.promptPanel, w.hud.shell.overlayRoot);
                overlay.name = "Furniture placement";
                var rect = (RectTransform)overlay.transform;
                rect.anchorMin = new Vector2(.018f, .02f);
                rect.anchorMax = new Vector2(.982f, .2f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                placement.panel = overlay;
                placement.instructions = overlay.GetComponentInChildren<TMPro.TMP_Text>(true);
                placement.instructions.enableAutoSizing = false;
                placement.instructions.textWrappingMode = TMPro.TextWrappingModes.Normal;
                var text = placement.instructions.rectTransform;
                text.anchorMin = Vector2.zero;
                text.anchorMax = Vector2.one;
                text.offsetMin = new Vector2(16, 8);
                text.offsetMax = new Vector2(-240, -8);
                Button Copy(string title, float x)
                {
                    var button = UnityEngine.Object.Instantiate(w.hud.menuButton, overlay.transform);
                    button.name = title;
                    button.onClick = new Button.ButtonClickedEvent();
                    button.GetComponentInChildren<TMPro.TMP_Text>().text = title;
                    var r = (RectTransform)button.transform;
                    r.anchorMin = r.anchorMax = new Vector2(1, .5f);
                    r.pivot = new Vector2(1, .5f);
                    r.anchoredPosition = new Vector2(x, 0);
                    r.sizeDelta = new Vector2(100, 48);
                    return button;
                }

                placement.confirm = Copy("배치", -124);
                placement.cancel = Copy("취소", -12);
                overlay.SetActive(false);
            }

            setup.Apply(false);
            foreach (var o in w.objects)
            {
                if (o != null && o.zone == "home" && StarterHomePresentation.Optional(o.id))
                {
                    o.gameObject.SetActive(false);
                }
            }

            w.upgradedHome.SetActive(false);
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(plan);
            EditorUtility.SetDirty(w);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
