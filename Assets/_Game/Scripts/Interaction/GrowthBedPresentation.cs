using UnityEngine;

namespace IanGame
{
    // The furniture remains family-owned. Only the visible age-appropriate bedding
    // changes; no inventory entitlement or household save is added or removed.
    [DefaultExecutionOrder(1250)]
    public sealed class GrowthBedPresentation : MonoBehaviour
    {
        public LifeWorldController world;
        public WorldInteractable bed;
        Sprite original;
        Vector3 originalScale;
        SpriteRenderer rail;
        SpriteRenderer blanket;
        Sprite blanketSource;
        Sprite blanketSprite;

        void Awake()
        {
            original = bed.visual.sprite;
            originalScale = bed.visual.transform.localScale;
        }

        void LateUpdate()
        {
            var growth = world.Player.growthVisual;
            if (growth?.Current == null)
            {
                return;
            }

            var p = growth.Current;
            var replacement =
                p.stage == VisualGrowthStage.Newborn ? growth.catalog.emptyCrib
                : p.stage == VisualGrowthStage.Toddler ? growth.catalog.toddlerBed
                : original;
            if (replacement == null)
            {
                return;
            }

            if (bed.visual.sprite != replacement)
            {
                bed.visual.sprite = replacement;
                bed.visual.transform.localScale =
                    replacement == original
                        ? originalScale
                        : Vector3.one
                            * ((p.stage == VisualGrowthStage.Newborn ? 1.05f : 1.3f) / replacement.bounds.size.y);
            }

            if (rail == null)
            {
                var go = new GameObject("Infant crib front rail", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                rail = go.GetComponent<SpriteRenderer>();
                rail.sharedMaterial = bed.visual.sharedMaterial;
            }

            rail.enabled = p.stage == VisualGrowthStage.Newborn && growth.catalog.cribFront != null;
            rail.sprite = growth.catalog.cribFront;
            if (rail.enabled)
            {
                rail.transform.position = new Vector3(bed.visual.bounds.center.x, bed.visual.bounds.min.y + .04f, 0);
                rail.transform.localScale = Vector3.one * (bed.visual.bounds.size.x / rail.sprite.bounds.size.x);
                rail.sortingOrder = bed.visual.sortingOrder + 5;
            }

            bool cover =
                growth.BodyInteractions
                && p.stage != VisualGrowthStage.Newborn
                && growth.InBed
                && (growth.ActivePose == "Sleep" || growth.ActivePose == "Wake Up" && world.Player.advanced.Frame == 0);
            if (cover)
            {
                if (blanket == null)
                {
                    var go = new GameObject("Age-appropriate bed blanket", typeof(SpriteRenderer));
                    go.transform.SetParent(transform, false);
                    blanket = go.GetComponent<SpriteRenderer>();
                    blanket.sharedMaterial = bed.visual.sharedMaterial;
                }

                if (blanketSource != replacement)
                {
                    if (blanketSprite != null)
                    {
                        Destroy(blanketSprite);
                    }

                    blanketSource = replacement;
                    var r = replacement.rect;
                    r.height *= .5f;
                    blanketSprite = Sprite.Create(
                        replacement.texture,
                        r,
                        new Vector2(.5f, 0),
                        replacement.pixelsPerUnit,
                        0,
                        SpriteMeshType.FullRect
                    );
                }

                blanket.sprite = blanketSprite;
                blanket.transform.position = new Vector3(bed.visual.bounds.center.x, bed.visual.bounds.min.y, 0);
                blanket.transform.localScale = bed.visual.transform.lossyScale;
                blanket.sortingOrder = bed.visual.sortingOrder + 4;
                blanket.color = bed.visual.color;
            }

            if (blanket != null)
            {
                blanket.enabled = cover;
            }

            if (p.stage == VisualGrowthStage.Newborn && !world.Busy)
            {
                bed.visual.enabled = true;
                growth.InBed = true;
                growth.ActionOffset = growth.SleepPoint(bed.GetComponent<WorldLifeAction>()) - world.Player.Position;
                growth.ActionOrder = bed.visual.sortingOrder + 3;
                world.Player.sprite.enabled = true;
                if (world.Player.contactShadow != null)
                {
                    world.Player.contactShadow.enabled = false;
                }
            }
        }

        void OnDestroy()
        {
            if (blanketSprite != null)
            {
                Destroy(blanketSprite);
            }
        }
    }
}
