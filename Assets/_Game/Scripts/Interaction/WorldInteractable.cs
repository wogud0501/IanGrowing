using UnityEngine;

namespace IanGame
{
    public sealed class WorldInteractable : MonoBehaviour
    {
        public string id;
        public string title;
        public string zone;
        public string targetZone;
        public string npcId;
        public WorldInteractionKind kind;
        public Vector2 arrival;
        public SpriteRenderer visual;
        public SpriteRenderer highlight;
        public float radius = 1.65f;
        public SpriteRenderer shadow;
        public GameObject activityEffect;
        public bool animateDoorLeaf = true;
        public Vector2 UsePoint = Vector2.down * .8f;
        public Vector2 Point => (Vector2)transform.position + UsePoint;
        public int NPCUses { get; private set; }

        public event System.Action<string> NPCUsed;

        public void RecordNPCUse(string id)
        {
            NPCUses++;
            NPCUsed?.Invoke(id);
        }

        public void Select(bool on)
        {
            selected = on;
            RefreshHighlight();
        }

        bool selected;
        bool guided;

        public void GuideFocus(bool on)
        {
            guided = on;
            RefreshHighlight();
        }

        void RefreshHighlight()
        {
            bool on = selected || guided;
            var door = GetComponent<WorldDoor>();
            if (door != null)
            {
                door.Highlight(on);
            }

            if (highlight != null)
            {
                highlight.enabled = on && door == null;
            }
        }
    }
}
