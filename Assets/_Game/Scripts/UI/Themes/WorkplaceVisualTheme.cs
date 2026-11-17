using UnityEngine;

namespace IanGame.Commercial
{
    [CreateAssetMenu(menuName = "Ian/Presentation/Workplace Theme")]
    public sealed class WorkplaceVisualTheme : ScriptableObject
    {
        public string jobId;
        public string displayName;
        public Sprite background;
        public Color dayTint = Color.white;
        public Color nightTint = new Color(.6f, .65f, .8f);
        public bool seatedCoworker = true;
        public Color monitorTint = new Color(.65f, .9f, 1);
        public Sprite[] monitorFrames;
    }
}
