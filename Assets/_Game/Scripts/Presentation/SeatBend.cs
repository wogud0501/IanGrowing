using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    // Subdivide the actor's actual sprite mesh at the joints. Texture coordinates
    // stay on the same triangles, including on tightly packed, trimmed sprites.
    public sealed class SeatBend : MonoBehaviour
    {
        struct Vertex
        {
            public Vector2 point;
            public Vector2 uv;

            public static Vertex Between(Vertex a, Vertex b, float t)
            {
                return new Vertex { point = Vector2.Lerp(a.point, b.point, t), uv = Vector2.Lerp(a.uv, b.uv, t) };
            }
        }

        [System.Serializable]
        sealed class Proportions
        {
            public string id;
            public float center;
            public float bottom;
            public float height;
            public Vector2 edges;
            public Vector2 toes;
            public Vector2 cloth;
            public Vector2 shirt;
            public Vector2 skin;
        }

        [System.Serializable]
        sealed class Wardrobe
        {
            public Proportions[] sprites;
        }

        static Dictionary<string, Proportions> wardrobe;

        static Proportions ProportionsOf(Sprite frame)
        {
            if (wardrobe == null)
            {
                wardrobe = new Dictionary<string, Proportions>();
                var asset = Resources.Load<TextAsset>("FurniturePose/FrontPoseProportions");
                foreach (var entry in JsonUtility.FromJson<Wardrobe>(asset.text).sprites)
                {
                    wardrobe.Add(entry.id, entry);
                }
            }

            if (wardrobe.TryGetValue(frame.name, out var proportions))
            {
                return proportions;
            }

            // Home outfits are runtime slices of the wardrobe rather than named sub-assets.
            string slice = $"{frame.texture.name}@{frame.rect.x:0},{frame.rect.y:0}";
            wardrobe.TryGetValue(slice, out proportions);
            return proportions;
        }

        sealed class Shape
        {
            public readonly List<Vector3> points = new();
            public readonly List<Vector2> uv = new();
            public readonly List<int> legSide = new();
            public readonly List<int> indices = new();
            public readonly float center;
            public readonly Vector2 toes;
            public readonly int bodyVertices;
            public readonly Vector4 clothUV;
            public readonly Vector4 skinUV;
            public readonly Vector4 chinUV;
            public readonly float foot;
            public readonly float height;

            public Shape(Sprite source)
            {
                var pts = source.vertices;
                var tex = source.uv;
                var tris = source.triangles;
                var profile = ProportionsOf(source);
                Vector2 size = source.rect.size / source.pixelsPerUnit;
                Vector2 origin = -source.pivot / source.pixelsPerUnit;
                Vector2 FromRect(Vector2 point) => origin + Vector2.Scale(point, size);
                center = profile != null ? origin.x + profile.center * size.x : source.bounds.center.x;
                foot = profile != null ? origin.y + profile.bottom * size.y : source.bounds.min.y;
                height = profile != null ? profile.height * size.y : Mathf.Max(.001f, source.bounds.size.y);
                toes = profile != null ? profile.toes : new Vector2(.055f, .055f);
                float[] bands = { -.001f, .075f, .15f, .20f, .24f, .30f, .37f, .44f, .53f, 1.001f };
                for (int i = 0; i < tris.Length; i += 3)
                {
                    var triangle = new List<Vertex>(3);
                    for (int k = 0; k < 3; k++)
                    {
                        int index = tris[i + k];
                        triangle.Add(new Vertex { point = pts[index], uv = tex[index] });
                    }

                    if (profile != null)
                    {
                        triangle = Clip(triangle, origin.x + profile.edges.x * size.x, true, true);
                        triangle = Clip(triangle, origin.x + profile.edges.y * size.x, false, true);
                    }

                    for (int j = 1; j < bands.Length; j++)
                    {
                        var poly = Clip(triangle, foot + height * bands[j - 1], true);
                        poly = Clip(poly, foot + height * bands[j], false);
                        if (poly.Count < 3)
                        {
                            continue;
                        }

                        foreach (int side in new[] { -1, 1 })
                        {
                            var half = Clip(poly, center, side > 0, true);
                            // Additional vertices at the cuff and elbow keep an arm
                            // mask from interpolating across the entire torso.
                            float edge = center + side * .12f * height;
                            AddHalf(Clip(half, edge, side < 0, true), side);
                            AddHalf(Clip(half, edge, side > 0, true), side);
                        }
                    }
                }

                bodyVertices = points.Count;
                Vector2 Sample(float x, float y) => TexturePoint(
                    new Vector2(center + x * height, foot + y * height), pts, tex, tris);
                var pants = Sample(.055f, .235f);
                var sleeve = Sample(0, .43f);
                var skin = Sample(.054f, .655f);
                var otherCheek = Sample(-.058f, .637f);
                var chin = Sample(.028f, .578f);
                var lip = Sample(-.019f, .619f);
                if (profile != null)
                {
                    pants = TexturePoint(FromRect(profile.cloth), pts, tex, tris);
                    sleeve = TexturePoint(FromRect(profile.shirt), pts, tex, tris);
                    skin = TexturePoint(FromRect(profile.skin), pts, tex, tris);
                    otherCheek = chin = lip = skin;
                }

                clothUV = new Vector4(pants.x, pants.y, sleeve.x, sleeve.y);
                skinUV = new Vector4(skin.x, skin.y, otherCheek.x, otherCheek.y);
                chinUV = new Vector4(chin.x, chin.y, lip.x, lip.y);
                points.AddRange(new Vector3[4]);
                uv.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            }

            void AddHalf(List<Vertex> polygon, int side)
            {
                int first = points.Count;
                foreach (var v in polygon)
                {
                    points.Add(v.point);
                    uv.Add(v.uv);
                    legSide.Add(side);
                }

                for (int k = 2; k < polygon.Count; k++)
                {
                    indices.Add(first);
                    indices.Add(first + k - 1);
                    indices.Add(first + k);
                }
            }

            static Vector2 TexturePoint(Vector2 p, Vector2[] vertices, Vector2[] uv, ushort[] triangles)
            {
                // A sprite's mapping is affine, including trimmed and rotated atlas entries.
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i];
                    int b = triangles[i + 1];
                    int c = triangles[i + 2];
                    Vector2 ab = vertices[b] - vertices[a];
                    Vector2 ac = vertices[c] - vertices[a];
                    float det = ab.x * ac.y - ab.y * ac.x;
                    if (Mathf.Abs(det) < .00001f)
                    {
                        continue;
                    }

                    Vector2 offset = p - vertices[a];
                    float u = (offset.x * ac.y - offset.y * ac.x) / det;
                    float v = (ab.x * offset.y - ab.y * offset.x) / det;
                    return uv[a] + u * (uv[b] - uv[a]) + v * (uv[c] - uv[a]);
                }

                return uv[0];
            }

            static List<Vertex> Clip(List<Vertex> polygon, float boundary, bool greater, bool horizontalAxis = false)
            {
                var result = new List<Vertex>(6);
                if (polygon.Count == 0)
                {
                    return result;
                }

                var previous = polygon[polygon.Count - 1];
                float last = horizontalAxis ? previous.point.x : previous.point.y;
                bool kept = greater ? last >= boundary : last <= boundary;
                foreach (var v in polygon)
                {
                    float value = horizontalAxis ? v.point.x : v.point.y;
                    bool keep = greater ? value >= boundary : value <= boundary;
                    if (keep != kept)
                    {
                        result.Add(Vertex.Between(previous, v, (boundary - last) / (value - last)));
                    }

                    if (keep)
                    {
                        result.Add(v);
                    }

                    kept = keep;
                    previous = v;
                    last = value;
                }

                return result;
            }
        }

        static readonly Dictionary<Sprite, Shape> shapes = new();
        static readonly int MainTexture = Shader.PropertyToID("_MainTex");
        SpriteRenderer source;
        MeshRenderer artwork;
        Mesh mesh;
        static Material lapMaterial;
        static Material bodyMaterial;
        static readonly int FrontFold = Shader.PropertyToID("_FrontFold");
        MaterialPropertyBlock bodyProperties;
        static readonly int Costume = Shader.PropertyToID("_Costume");
        static readonly int ClothUV = Shader.PropertyToID("_ClothUV");
        static readonly int SkinUV = Shader.PropertyToID("_SkinUV");
        static readonly int ChinUV = Shader.PropertyToID("_ChinUV");
        Shape shape;
        Vector3[] projected;
        Color32[] colors;
        bool mirrored;
        MaterialPropertyBlock properties;

        public void Remember(SpriteRenderer body, Sprite frame, bool flip)
        {
            Hide();
            source = body;
            mirrored = flip;
            shape = null;
            if (!source || !frame)
            {
                return;
            }

            if (!artwork)
            {
                var joints = new GameObject("Seated character pose");
                joints.transform.SetParent(body.transform, false);
                mesh = new Mesh { name = "Seated silhouette" };
                mesh.MarkDynamic();
                joints.AddComponent<MeshFilter>().sharedMesh = mesh;
                artwork = joints.AddComponent<MeshRenderer>();
                artwork.enabled = false;
                artwork.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                artwork.receiveShadows = false;
                properties = new MaterialPropertyBlock();
                bodyProperties = new MaterialPropertyBlock();
            }

            if (!shapes.TryGetValue(frame, out shape))
            {
                shape = new Shape(frame);
                shapes.Add(frame, shape);
            }

            projected = shape.points.ToArray();
            colors = new Color32[projected.Length];
            mesh.Clear();
            mesh.vertices = projected;
            mesh.SetUVs(0, shape.uv);
            var jointCoordinates = new Vector2[projected.Length];
            for (int i = 0; i < shape.bodyVertices; i++)
            {
                jointCoordinates[i] = new Vector2(projected[i].x - shape.center, projected[i].y - shape.foot) / shape.height;
            }
            mesh.uv2 = jointCoordinates;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(shape.indices, 0);
            int start = shape.bodyVertices;
            mesh.SetTriangles(new[] { start, start + 1, start + 2, start, start + 2, start + 3 }, 1);
            mesh.bounds = new Bounds(frame.bounds.center, frame.bounds.size * 1.5f);
            if (!lapMaterial)
            {
                lapMaterial = Resources.Load<Material>("FurniturePose/FrontLap");
                bodyMaterial = Resources.Load<Material>("FurniturePose/FrontBody");
            }

            artwork.sharedMaterials = new[] { bodyMaterial, lapMaterial };
            bodyProperties.SetTexture(MainTexture, frame.texture);
            bodyProperties.SetVector(SkinUV, shape.skinUV);
            artwork.SetPropertyBlock(bodyProperties, 0);
            properties.Clear();
            properties.SetTexture(Costume, frame.texture);
            properties.SetVector(ClothUV, shape.clothUV);
            properties.SetVector(SkinUV, shape.skinUV);
            properties.SetVector(ChinUV, shape.chinUV);
            artwork.SetPropertyBlock(properties, 1);
        }

        Vector3 hipLocal;
        Vector3 leftSoleLocal;
        Vector3 rightSoleLocal;
        public Vector2 HipWorld => artwork.transform.TransformPoint(hipLocal);
        public Vector2 LeftFootWorld => artwork.transform.TransformPoint(leftSoleLocal);
        public Vector2 RightFootWorld => artwork.transform.TransformPoint(rightSoleLocal);
        public bool Visible => artwork && artwork.enabled;

        float diningLean;
        WorldActivitySlot currentSeat;

        public bool SetDiningFrame(int frame)
        {
            if (!Visible || !currentSeat || previousProgress < 1)
            {
                return false;
            }

            float lean = frame == 2 ? 1 : (frame % 2) * .45f;
            if (diningLean != lean)
            {
                diningLean = lean;
                previousProgress = -1;
                Show(1, currentSeat);
            }

            return true;
        }

        float previousProgress;
        float previousHeight;
        Vector2 previousFacing;
        Color previousColor;

        public void Show(float progress, WorldActivitySlot slot)
        {
            if (shape == null || !source || !source.enabled)
            {
                Hide();
                return;
            }

            currentSeat = slot;
            slot.OccupiedPose = this;
            // Set the feet and flex the trunk before transferring weight between the soles and cushion.
            float weight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.09f, .88f, progress));
            float leanIn = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .30f, progress));
            float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.58f, 1, progress));
            float lean = leanIn * (1 - settle);
            Vector2 facing = slot.facing;
            bool front = facing.y < -.6f;
            float height = shape.height;
            const float hip = .33f;
            float seatHeight = slot.soleAnchor
                ? (slot.Point.y - slot.SolePoint.y) / (height * Mathf.Abs(source.transform.lossyScale.y))
                : .22f;
            float kneeHeight = Mathf.Max(.09f, seatHeight - .087f);
            Color tint = source.color;

            artwork.sortingLayerID = source.sortingLayerID;
            artwork.sortingOrder = source.sortingOrder;
            if (Visible && progress == previousProgress && seatHeight == previousHeight
                && facing == previousFacing && tint == previousColor)
            {
                return;
            }

            previousProgress = progress;
            previousHeight = seatHeight;
            previousFacing = facing;
            previousColor = tint;
            for (int i = 0; i < shape.bodyVertices; i++)
            {
                Vector3 v = shape.points[i];
                float y = Mathf.Clamp01((v.y - shape.foot) / height);
                float side = (v.x - shape.center) / height;
                float opacity = 1;
                if (front)
                {
                    float folded;
                    if (y < .075f)
                    {
                        folded = y;
                    }
                    else if (y < .20f)
                    {
                        folded = Mathf.Lerp(.075f, kneeHeight, (y - .075f) / .125f);
                    }
                    else if (y < hip)
                    {
                        folded = Mathf.Lerp(kneeHeight, seatHeight, (y - .20f) / (hip - .20f));
                    }
                    else
                    {
                        folded = seatHeight + y - hip;
                    }

                    // Hands relax toward the knees and both shoulders stay level.
                    float knee = Mathf.Clamp01((hip - y) / .13f);
                    // Move each shoe with its own shin. A single quad spanning both
                    // shoes stretches the original central gap instead of separating the feet.
                    float spread = shape.legSide[i];
                    float toe = spread < 0 ? shape.toes.x : shape.toes.y;
                    v.x += spread * height * (.126f - toe) * knee * weight;
                    v.y += height * ((folded - y) * weight - Mathf.Max(0, y - hip) * .115f * lean);

                    float takeover = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.22f, .75f, progress));
                    float trouserBand = Mathf.InverseLerp(.065f, .11f, y) * Mathf.InverseLerp(.31f, .245f, y);
                    opacity -= takeover * trouserBand;

                    // Both elbows come forward as the hands settle on the lap.
                    float arm = Mathf.InverseLerp(.10f, .17f, Mathf.Abs(side))
                        * Mathf.InverseLerp(.54f, .38f, y) * Mathf.InverseLerp(.26f, .35f, y);
                    v.x -= Mathf.Sign(side) * arm * height * .065f * weight;
                    v.y += arm * height * .022f * weight;
                }
                else
                {
                    float shin = Mathf.Clamp(y - .10f, 0, .15f);
                    float thigh = Mathf.Clamp(y - .25f, 0, .18f);
                    float knee = Mathf.Clamp01((y - .10f) / .15f) * Mathf.Clamp01((.43f - y) / .18f);
                    v.x += facing.x * height * (knee * .14f * weight + Mathf.Max(0, y - .43f) * .09f * lean);
                    if (slot.soleAnchor)
                    {
                        float lift = y >= hip ? seatHeight - hip : (seatHeight - hip) * y / hip;
                        v.y += (lift * weight - .08f * Mathf.Max(0, y - hip) * lean) * height;
                    }
                    else
                    {
                        v.y -= (shin * .48f + thigh * .66f) * height * weight;
                    }
                }

                // Diners lean toward their own place setting without sliding their hips or feet.
                v.y -= height * diningLean * weight * .048f * Mathf.InverseLerp(hip, .80f, y);
                v.x = mirrored ? -v.x : v.x;
                projected[i] = v;
                colors[i] = new Color(tint.r, tint.g, tint.b, tint.a * opacity);
            }

            int lap = shape.bodyVertices;
            float hipPosition = Mathf.Lerp(hip, seatHeight, weight);
            float width = Mathf.Lerp(.145f, .217f, weight) * height;
            float bottom = shape.foot + .041f * height;
            float top = shape.foot + (hipPosition + .067f) * height;
            projected[lap] = new Vector3(shape.center - width, bottom);
            projected[lap + 1] = new Vector3(shape.center + width, bottom);
            projected[lap + 2] = new Vector3(shape.center + width, top);
            projected[lap + 3] = new Vector3(shape.center - width, top);
            float coverage = front ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, .74f, progress)) : 0;
            for (int i = lap; i < lap + 4; i++)
            {
                colors[i] = new Color(tint.r, tint.g, tint.b, tint.a * coverage);
            }

            mesh.vertices = projected;
            mesh.colors32 = colors;
            bodyProperties.SetFloat(FrontFold, coverage);
            artwork.SetPropertyBlock(bodyProperties, 0);
            // Sprite pivots can be below, above or off-center relative to the soles.
            // Align the mesh in its local space without moving the navigation root.
            artwork.transform.localPosition = slot.soleAnchor
                ? new Vector3(mirrored ? shape.center : -shape.center, -shape.foot) * weight : Vector3.zero;
            hipLocal = new Vector3(shape.center, shape.foot + height * hipPosition);
            float leftToe = Mathf.Lerp(shape.toes.x, .126f, weight) * height;
            float rightToe = Mathf.Lerp(shape.toes.y, .126f, weight) * height;
            leftSoleLocal = new Vector3(shape.center - leftToe, shape.foot);
            rightSoleLocal = new Vector3(shape.center + rightToe, shape.foot);
            source.forceRenderingOff = true;
            artwork.enabled = true;
        }

        public void Hide()
        {
            if (currentSeat && currentSeat.OccupiedPose == this)
            {
                currentSeat.OccupiedPose = null;
            }

            currentSeat = null;
            diningLean = 0;
            if (artwork && artwork.enabled)
            {
                artwork.enabled = false;
                if (source)
                {
                    source.forceRenderingOff = false;
                }
            }
        }

        void OnDisable() => Hide();

        void OnDestroy()
        {
            if (mesh)
            {
                Destroy(mesh);
            }
        }
    }
}
