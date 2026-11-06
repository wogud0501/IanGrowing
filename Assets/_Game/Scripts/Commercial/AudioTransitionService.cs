using UnityEngine;
namespace IanGame.Commercial {
public sealed class AudioTransitionService : MonoBehaviour {
 public AudioSource source;
 public void Play(AudioClip clip){if(clip!=null&&source!=null)source.PlayOneShot(clip);}
}
}
