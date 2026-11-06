using UnityEngine;
namespace IanGame.Commercial {
[CreateAssetMenu(menuName="Ian/Presentation/Transition Profile")]
public sealed class TransitionProfile : ScriptableObject {
 public SceneId from, to;
 [Min(0)] public float exitSeconds=.32f, coveredSeconds=.15f, entrySeconds=.38f;
 public string departure="외출 준비 중", arrival="도착했습니다";
 public Color curtain=new Color32(28,38,58,255);
 public AudioClip exitSound, entrySound;
}
}
