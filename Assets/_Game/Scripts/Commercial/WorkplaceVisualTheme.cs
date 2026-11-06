using UnityEngine;
namespace IanGame.Commercial {
[CreateAssetMenu(menuName="Ian/Presentation/Workplace Theme")]
public sealed class WorkplaceVisualTheme:ScriptableObject {
 public string jobId,displayName;public Sprite background;public Color dayTint=Color.white,nightTint=new Color(.6f,.65f,.8f);public bool seatedCoworker=true;
 public Color monitorTint=new Color(.65f,.9f,1);public Sprite[] monitorFrames;
}
}
