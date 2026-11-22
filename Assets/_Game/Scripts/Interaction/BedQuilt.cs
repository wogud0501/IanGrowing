using UnityEngine;

namespace IanGame
{
    [DefaultExecutionOrder(1450)]
    public sealed class BedQuilt : MonoBehaviour
    {
        public const float TransferSeconds = 2.45f;

        public Transform approachAnchor;
        public Transform sitAnchor;
        public Transform pillowAnchor;
        public Transform exitAnchor;
        [Range(.3f, 1f)]
        public float usableWidth = .88f;

        static Sprite fabric;
        static Sprite fingertips;
        SpriteRenderer mattress;
        SpriteRenderer cloth;
        SpriteRenderer sleeper;
        SpriteRenderer residentBody;
        SpriteRenderer leftHand;
        SpriteRenderer rightHand;
        WorldPlayerMotor player;
        Transform bedside;
        float entry;
        float recline;
        float coverage;
        float residentHeight;
        int residentColumn;
        bool atRest;
        bool resident;

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
            resident = false;
        }

        public void ResidentPose(WorldInteractable actor, int column, bool student, float elapsed)
        {
            resident = true;
            player = null;
            sleeper = actor.visual;
            bedside = actor.transform;
            residentColumn = column;
            residentHeight = student ? 1.24f : 1.53f;
            entry = Mathf.SmoothStep(0, 1, elapsed / .40f);
            recline = Mathf.SmoothStep(0, 1, (elapsed - .36f) / .76f);
            coverage = Mathf.SmoothStep(0, 1, (elapsed - 1.34f) / .86f);
            atRest = elapsed >= TransferSeconds;
        }

        public void Release(SpriteRenderer body)
        {
            if (sleeper != body)
            {
                return;
            }

            if (sleeper)
            {
                sleeper.forceRenderingOff = false;
            }
            if (player)
            {
                player.bodyVisual.localPosition = Vector3.zero;
                player.bodyVisual.localRotation = Quaternion.identity;
                player.bodyVisual.localScale = Vector3.one * player.visualScale;
            }
            if (residentBody)
            {
                residentBody.enabled = false;
            }

            player = null;
            sleeper = null;
            coverage = 0;
            resident = false;
        }

        SpriteRenderer Layer(string label)
        {
            var layer = new GameObject(label).AddComponent<SpriteRenderer>();
            layer.transform.SetParent(mattress.transform, false);
            layer.sharedMaterial = mattress.sharedMaterial;
            layer.sortingLayerID = mattress.sortingLayerID;
            return layer;
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
                        new Vector2(.5f, 0), texture.width, 0, SpriteMeshType.FullRect, new Vector4(5, 4, 5, 12));
                }

                cloth = Layer("Duvet and turned hem");
                cloth.sprite = fabric;
                cloth.drawMode = SpriteDrawMode.Sliced;
            }

            bool occupied = sleeper && sleeper.enabled && sleeper.gameObject.activeInHierarchy && mattress.enabled;
            var bounds = mattress.sprite.bounds;
            bool framed = mattress.sprite.name == "Appliances_12";
            float foot = bounds.min.y + bounds.size.y * (framed ? .16f : .095f);
            float folded = foot + bounds.size.y * .16f;
            float chest = folded;
            if (residentBody)
            {
                residentBody.enabled = occupied && resident;
            }

            if (occupied)
            {
                SpriteRenderer body = sleeper;
                int pose = recline < .18f ? 0 : recline < .75f ? 1
                    : atRest ? 4 : coverage > .015f && coverage < .98f ? 3 : 2;
                if (resident)
                {
                    if (!residentBody)
                    {
                        residentBody = Layer("Resident in nightwear");
                    }

                    body = residentBody;
                    body.enabled = true;
                    body.color = sleeper.color;
                    body.sortingOrder = Mathf.Max(mattress.sortingOrder + 2, sleeper.sortingOrder);
                    sleeper.forceRenderingOff = true;
                    Place(body, SleepwearFrames.ResidentPose(residentColumn, pose), residentHeight, bounds, foot);
                }
                else if (player && player.growthVisual.Current.stage != VisualGrowthStage.Newborn)
                {
                    var profile = player.growthVisual.Current;
                    Place(body, SleepwearFrames.Pose(profile.stage, pose), profile.height * .9f, bounds, foot);
                }

                var torso = body.sprite.bounds;
                var point = body.transform.TransformPoint(torso.center - Vector3.up * (torso.size.y * .055f));
                chest = Mathf.Clamp(mattress.transform.InverseTransformPoint(point).y, folded,
                    bounds.max.y - bounds.size.y * .21f);
                if (atRest)
                {
                    chest += Mathf.Sin(Time.time * 1.64f) * bounds.size.y * .004f;
                }
            }

            float top = Mathf.Lerp(folded, chest, coverage);
            cloth.enabled = mattress.enabled;
            cloth.transform.localPosition = new Vector3(bounds.center.x, foot, 0);
            float width = bounds.size.x * (framed ? .56f : usableWidth);
            float scale = width / fabric.bounds.size.x;
            cloth.transform.localScale = Vector3.one * scale;
            cloth.size = new Vector2(fabric.bounds.size.x, (top - foot) / scale);
            cloth.sortingOrder = occupied && recline > .75f
                ? Mathf.Max(mattress.sortingOrder + 3, sleeper.sortingOrder + 2)
                : mattress.sortingOrder + 1;
            cloth.color = mattress.color;
            Hands(occupied && coverage > .008f && coverage < .995f, bounds, top, width);
        }

        void Place(SpriteRenderer body, Sprite sprite, float height, Bounds bed, float foot)
        {
            body.sprite = sprite;
            body.flipX = false;
            body.transform.rotation = mattress.transform.rotation;
            float worldBed = bed.size.y * Mathf.Abs(mattress.transform.lossyScale.y);
            float h = Mathf.Min(height, worldBed * .68f) * Mathf.Lerp(.91f, 1, recline);
            var parentScale = body.transform.parent.lossyScale;
            float factor = h / sprite.bounds.size.y;
            body.transform.localScale = new Vector3(factor / Mathf.Abs(parentScale.x),
                factor / Mathf.Abs(parentScale.y), 1);
            Vector3 edge = mattress.transform.InverseTransformPoint(bedside.position);
            edge.x = Mathf.Clamp(edge.x, bed.center.x - bed.size.x * .36f, bed.center.x + bed.size.x * .36f);
            edge.y = Mathf.Clamp(edge.y, foot + bed.size.y * .12f, foot + bed.size.y * .35f);
            if (sitAnchor)
            {
                edge = mattress.transform.InverseTransformPoint(sitAnchor.position);
            }

            float pillow = pillowAnchor ? mattress.transform.InverseTransformPoint(pillowAnchor.position).y
                : bed.min.y + bed.size.y * .84f;
            var lying = new Vector3(bed.center.x, pillow - h / Mathf.Abs(mattress.transform.lossyScale.y), 0);
            if (atRest)
            {
                lying.y += Mathf.Sin(Time.time * 1.64f) * bed.size.y * .002f;
            }

            var contact = mattress.transform.TransformPoint(Vector3.Lerp(edge, lying, recline));
            Vector3 start = resident || !player ? bedside.position : player.transform.position;
            body.transform.position = Vector3.Lerp(start, contact, entry);
        }

        void Hands(bool gripping, Bounds bed, float hem, float width)
        {
            if (gripping && !leftHand)
            {
                if (!fingertips)
                {
                    var texture = Resources.Load<Texture2D>("FamilySleep/QuiltGrip");
                    fingertips = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(.5f, .6f), texture.width);
                }

                leftHand = Layer("Left hand on hem");
                rightHand = Layer("Right hand on hem");
                leftHand.sprite = rightHand.sprite = fingertips;
                rightHand.flipX = true;
            }

            if (leftHand)
            {
                leftHand.enabled = rightHand.enabled = gripping;
                float spread = width * Mathf.Lerp(.19f, .13f, coverage);
                leftHand.transform.localPosition = new Vector3(bed.center.x - spread, hem, 0);
                rightHand.transform.localPosition = new Vector3(bed.center.x + spread, hem, 0);
                leftHand.transform.localScale = rightHand.transform.localScale = Vector3.one * width * .095f;
                leftHand.sortingOrder = rightHand.sortingOrder = cloth.sortingOrder + 1;
                leftHand.color = rightHand.color = mattress.color;
            }
        }

        void OnDisable()
        {
            Release(sleeper);
        }
    }
}
