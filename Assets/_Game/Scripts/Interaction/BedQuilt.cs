using UnityEngine;

namespace IanGame
{
    [DefaultExecutionOrder(1450)]
    public sealed class BedQuilt : MonoBehaviour
    {
        static Sprite fabric;
        SpriteRenderer mattress;
        SpriteRenderer cloth;
        SpriteRenderer sleeper;
        WorldPlayerMotor player;
        float recline;
        float entry;
        bool atRest;
        Transform bedside;
        float coverage;
        public float Coverage => coverage;

        public static BedQuilt For(WorldInteractable bed)
        {
            if (!bed.TryGetComponent<BedQuilt>(out var quilt))
            {
                quilt = bed.gameObject.AddComponent<BedQuilt>();
            }

            quilt.mattress = bed.visual;
            return quilt;
        }

        public void Draw(SpriteRenderer body, float amount)
        {
            sleeper = body;
            coverage = Mathf.Clamp01(amount);
        }

        public void PlayerPose(WorldPlayerMotor motor, Transform sit, float lean, float approach, bool asleep)
        {
            player = motor;
            sleeper = motor.sprite;
            bedside = sit;
            recline = lean;
            entry = approach;
            atRest = asleep;
        }

        public void Release(SpriteRenderer body)
        {
            if (sleeper == body)
            {
                if (player)
                {
                    player.bodyVisual.localPosition = Vector3.zero;
                    player.bodyVisual.localRotation = Quaternion.identity;
                    player.bodyVisual.localScale = Vector3.one * player.visualScale;
                }

                player = null;
                sleeper = null;
                coverage = 0;
            }
        }

        void LateUpdate()
        {
            if (!mattress || !mattress.sprite)
            {
                return;
            }

            if (!cloth)
            {
                if (!fabric)
                {
                    var texture = Resources.Load<Texture2D>("FamilySleep/Quilt");
                    fabric = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(.5f, 0), texture.width, 0, SpriteMeshType.FullRect,
                        new Vector4(7, 7, 7, 14));
                }

                cloth = new GameObject("Stitched quilt", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                cloth.transform.SetParent(mattress.transform, false);
                cloth.sprite = fabric;
                cloth.drawMode = SpriteDrawMode.Sliced;
                cloth.sharedMaterial = mattress.sharedMaterial;
            }

            cloth.enabled = mattress.enabled;
            if (!mattress.enabled)
            {
                return;
            }

            var bed = mattress.sprite.bounds;
            bool framedBed = mattress.sprite.name == "Appliances_12";
            float width = framedBed ? .55f : .87f;
            float foot = bed.min.y + bed.size.y * (framedBed ? .16f : .13f);
            float folded = foot + bed.size.y * .13f;
            if (player && player.growthVisual.Current.stage != VisualGrowthStage.Newborn)
            {
                PosePlayer(bed, foot);
            }

            float top = folded;
            bool occupied = sleeper && sleeper.enabled && sleeper.gameObject.activeInHierarchy;
            if (occupied && coverage > 0)
            {
                var bounds = sleeper.sprite.bounds;
                var shoulder = sleeper.transform.TransformPoint(bounds.center + Vector3.up * (bounds.size.y * .015f));
                float chest = mattress.transform.InverseTransformPoint(shoulder).y;
                float breath = Mathf.Sin(Time.unscaledTime * 1.68f) * .006f * bed.size.y;
                top = Mathf.Lerp(folded, Mathf.Clamp(chest + breath, folded, bed.max.y - bed.size.y * .19f), coverage);
            }

            cloth.transform.localPosition = new Vector3(bed.center.x, foot, 0);
            float fabricScale = bed.size.x * width / fabric.bounds.size.x;
            cloth.transform.localScale = Vector3.one * fabricScale;
            cloth.size = new Vector2(fabric.bounds.size.x, (top - foot) / fabricScale);
            cloth.sortingLayerID = mattress.sortingLayerID;
            cloth.sortingOrder = occupied && coverage > 0
                ? Mathf.Max(mattress.sortingOrder + 2, sleeper.sortingOrder + 1)
                : mattress.sortingOrder + 1;
            cloth.color = mattress.color;
        }

        void PosePlayer(Bounds bed, float foot)
        {
            var profile = player.growthVisual.Current;
            int frame = recline < .25f ? 0 : recline < .68f ? 1
                : coverage < .12f ? 2 : atRest && coverage >= .99f ? 4 : 3;
            var sprite = SleepwearFrames.Pose(profile.stage, frame);
            var visual = player.sprite;
            visual.sprite = sprite;
            visual.flipX = false;
            float bodyHeight = Mathf.Min(profile.height * Mathf.Lerp(.79f, .88f, recline),
                bed.size.y * Mathf.Abs(mattress.transform.lossyScale.y) * .62f);
            float scale = bodyHeight / Mathf.Max(.001f, Mathf.Abs(visual.transform.parent.lossyScale.y));
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.rotation = mattress.transform.rotation;
            var edge = mattress.transform.InverseTransformPoint(bedside ? bedside.position : player.transform.position);
            edge.x = Mathf.Clamp(edge.x, bed.center.x - bed.size.x * .22f, bed.center.x + bed.size.x * .22f);
            edge.y = Mathf.Clamp(edge.y, foot, foot + bed.size.y * .22f);
            float pillow = bed.min.y + bed.size.y * .78f;
            var lying = new Vector3(bed.center.x, pillow - bodyHeight / Mathf.Abs(mattress.transform.lossyScale.y), 0);
            lying.y += atRest ? Mathf.Sin(Time.unscaledTime * 1.68f) * bed.size.y * .002f : 0;
            Vector3 mattressPoint = mattress.transform.TransformPoint(Vector3.Lerp(edge, lying, recline));
            visual.transform.position = Vector3.Lerp(player.transform.position, mattressPoint, entry);
        }

        void OnDisable()
        {
            Release(sleeper);
        }
    }
}
