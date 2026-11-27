using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class CabDoorReach : MaskableGraphic
    {
        [Serializable]
        sealed class Contact
        {
            public string sprite;
            public int cuffY;
            public int handBottom;
            public int insideX;
            public int outsideX;
        }

        [Serializable]
        sealed class Contacts
        {
            public Contact[] stages;
        }

        static Contacts contacts;
        Sprite source;
        Contact contact;
        Vector2 pivot;
        Vector2 hand;
        float angle;
        float top;
        float bottom;
        float unitsPerPixel;
        Vector4 uv;

        public override Texture mainTexture => source ? source.texture : base.mainTexture;

        public static CabDoorReach Create(Transform parent, Sprite idle, float height)
        {
            if (contacts == null)
            {
                var data = Resources.Load<TextAsset>("CityTransit/CabHandContacts");
                if (!data)
                {
                    return null;
                }
                contacts = JsonUtility.FromJson<Contacts>(data.text);
            }
            var entry = Array.Find(contacts.stages, s => s.sprite == idle.name);
            if (entry == null)
            {
                return null;
            }
            var go = new GameObject("Ian reaching for the rear door", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CabDoorReach>();
            view.source = idle;
            view.contact = entry;
            view.uv = DataUtility.GetOuterUV(idle);
            view.unitsPerPixel = height / idle.rect.height;
            view.top = Mathf.Round(entry.cuffY - .097f * idle.rect.height);
            view.bottom = entry.handBottom + 1;
            view.pivot = new Vector2(entry.insideX + 2, view.top + 1);
            view.hand = new Vector2(
                (entry.insideX + entry.outsideX) * .5f,
                Mathf.Lerp(entry.cuffY, entry.handBottom, .7f)
            );
            view.rectTransform.pivot = new Vector2(.5f, 0);
            view.rectTransform.sizeDelta = idle.rect.size * view.unitsPerPixel;
            view.raycastTarget = false;
            view.enabled = false;
            return view;
        }

        public Vector2 StandAt(Vector2 handle, float ground, out float extension)
        {
            Vector2 shoulder = Local(pivot);
            Vector2 arm = Local(hand) - shoulder;
            float lift = Mathf.Clamp((handle.y - ground - shoulder.y) / arm.magnitude, -.95f, .98f);
            extension = Mathf.Asin(lift) - Mathf.Atan2(arm.y, arm.x);
            Vector2 grasp = shoulder + Rotate(arm, extension);
            return new Vector2(handle.x - grasp.x, ground);
        }

        public void Sample(Vector2 feet, float radians)
        {
            enabled = true;
            angle = radians;
            rectTransform.anchoredPosition = feet;
            SetVerticesDirty();
        }

        Vector2 Local(Vector2 pixel) =>
            new Vector2(pixel.x - source.rect.width * .5f, source.rect.height - pixel.y) * unitsPerPixel;

        static Vector2 Rotate(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!source)
            {
                return;
            }
            float width = source.rect.width;
            float height = source.rect.height;
            float seam = contact.insideX;
            float shoulder = pivot.x;
            Band(0, top, 0, width, 0, width);
            Band(top, contact.cuffY, 0, shoulder, 0, seam);
            Band(contact.cuffY, bottom, 0, seam, 0, seam);
            Band(bottom, height, 0, width, 0, width);
            Band(top, contact.cuffY, shoulder, width, seam, width, true);
            Band(contact.cuffY, bottom, seam, width, seam, width, true);

            void Band(float y1, float y2, float left1, float right1, float left2, float right2, bool sleeve = false)
            {
                int start = mesh.currentVertCount;
                Vertex(left2, y2);
                Vertex(left1, y1);
                Vertex(right1, y1);
                Vertex(right2, y2);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start + 2, start + 3, start);

                void Vertex(float x, float y)
                {
                    var point = Local(new Vector2(x, y));
                    if (sleeve)
                    {
                        var joint = Local(pivot);
                        point = joint + Rotate(point - joint, angle);
                    }
                    var tex = new Vector2(Mathf.Lerp(uv.x, uv.z, x / width), Mathf.Lerp(uv.w, uv.y, y / height));
                    mesh.AddVert(point, color, tex);
                }
            }
        }
    }
}
